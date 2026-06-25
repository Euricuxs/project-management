using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Board;
using ProjectManagement.Application.DTOs.Task;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for board operations with transaction support.
/// </summary>
public class BoardService : IBoardService
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;

    /// <summary>
    /// Default column names for new Kanban boards.
    /// </summary>
    private static readonly string[] DefaultColumnNames = { "To Do", "In Progress", "Done" };

    /// <summary>
    /// Default task status mapping for columns.
    /// </summary>
    private static readonly Dictionary<string, Domain.Enums.TaskStatus?> DefaultColumnTaskStatus = new()
    {
        { "To Do", Domain.Enums.TaskStatus.Todo },
        { "In Progress", Domain.Enums.TaskStatus.InProgress },
        { "Done", Domain.Enums.TaskStatus.Done }
    };

    public BoardService(ApplicationDbContext context, IProjectService projectService)
    {
        _context = context;
        _projectService = projectService;
    }

    public async Task<IReadOnlyList<BoardListItemResponse>> GetProjectBoardsAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify user has access to project
        if (!await _projectService.UserHasAccessAsync(projectId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this project.");

        var boards = await _context.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .Where(b => b.ProjectId == projectId && !b.IsDeleted)
            .OrderBy(b => b.Position)
            .ToListAsync(cancellationToken);

        return boards.Select(b => new BoardListItemResponse
        {
            Id = b.Id,
            ProjectId = b.ProjectId,
            Name = b.Name,
            Description = b.Description,
            Type = b.Type.ToString(),
            Position = b.Position,
            IsDefault = b.IsDefault,
            CreatedAt = b.CreatedAt,
            ColumnCount = b.Columns.Count,
            TaskCount = b.Columns.SelectMany(c => c.Tasks).Count(t => !t.IsDeleted)
        }).ToList();
    }

    public async Task<BoardResponse?> GetBoardByIdAsync(
        Guid boardId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var board = await GetBoardWithDetailsAsync(boardId, cancellationToken);

        if (board == null)
            return null;

        // Verify user has access
        if (!await _projectService.UserHasAccessAsync(board.ProjectId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this board.");

        return MapToResponse(board);
    }

    public async Task<BoardResponse> CreateBoardAsync(
        CreateBoardRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify user has access to project
        if (!await _projectService.UserHasAccessAsync(request.ProjectId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this project.");

        // Check for duplicate board name in the project
        var existingBoard = await _context.Boards
            .AnyAsync(b => b.ProjectId == request.ProjectId &&
                          b.Name.ToLower() == request.Name.ToLower() &&
                          !b.IsDeleted, cancellationToken);

        if (existingBoard)
            throw new AlreadyExistsException($"A board with the name '{request.Name}' already exists in this project.");

        // Use transaction for board + columns creation
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Determine position (add to end if not specified)
            var maxPosition = await _context.Boards
                .Where(b => b.ProjectId == request.ProjectId && !b.IsDeleted)
                .MaxAsync(b => (int?)b.Position, cancellationToken) ?? -1;

            var board = new Board
            {
                ProjectId = request.ProjectId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Type = ParseBoardType(request.Type),
                Position = request.Position > 0 ? request.Position : maxPosition + 1,
                IsDefault = request.IsDefault
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync(cancellationToken);

            // Create default columns if requested or by default
            var columnNames = request.DefaultColumns?.Count > 0
                ? request.DefaultColumns.ToArray()
                : DefaultColumnNames;

            var columns = new List<Column>();
            for (int i = 0; i < columnNames.Length; i++)
            {
                var columnName = columnNames[i];
                DefaultColumnTaskStatus.TryGetValue(columnName, out var taskStatus);

                columns.Add(new Column
                {
                    BoardId = board.Id,
                    Name = columnName,
                    Position = i,
                    Color = GetColumnColor(i),
                    TaskStatus = taskStatus
                });
            }

            _context.Columns.AddRange(columns);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Reload board with columns
            board = await GetBoardWithDetailsAsync(board.Id, cancellationToken);
            return MapToResponse(board!);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BoardResponse> UpdateBoardAsync(
        Guid boardId,
        UpdateBoardRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardId && !b.IsDeleted, cancellationToken);

        if (board == null)
            throw new NotFoundException("Board not found.");

        // Verify user has access
        if (!await _projectService.UserHasAccessAsync(board.ProjectId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this board.");

        // Check for duplicate name if name is being changed
        if (!string.IsNullOrEmpty(request.Name))
        {
            var nameExists = await _context.Boards
                .AnyAsync(b => b.ProjectId == board.ProjectId &&
                              b.Id != boardId &&
                              b.Name.ToLower() == request.Name.ToLower() &&
                              !b.IsDeleted, cancellationToken);

            if (nameExists)
                throw new AlreadyExistsException($"A board with the name '{request.Name}' already exists in this project.");
        }

        // Update fields
        if (!string.IsNullOrEmpty(request.Name))
            board.Name = request.Name.Trim();

        if (request.Description != null)
            board.Description = request.Description.Trim();

        if (!string.IsNullOrEmpty(request.Type))
            board.Type = ParseBoardType(request.Type);

        if (request.Position.HasValue)
            board.Position = request.Position.Value;

        if (request.IsDefault.HasValue)
            board.IsDefault = request.IsDefault.Value;

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with columns
        board = await GetBoardWithDetailsAsync(board.Id, cancellationToken);
        return MapToResponse(board!);
    }

    public async Task<bool> DeleteBoardAsync(
        Guid boardId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.Boards
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.Id == boardId, cancellationToken);

        if (board == null)
            return false;

        // Verify user has access
        if (!await _projectService.UserHasAccessAsync(board.ProjectId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this board.");

        // Check if board has tasks (edge case)
        var taskCount = board.Columns.SelectMany(c => c.Tasks).Count(t => !t.IsDeleted);
        if (taskCount > 0)
        {
            // Soft delete the board - tasks remain but board is marked deleted
            board.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // Hard delete if no tasks (safe to delete)
        _context.Boards.Remove(board);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UserHasAccessAsync(
        Guid boardId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == boardId && !b.IsDeleted, cancellationToken);

        if (board == null)
            return false;

        return await _projectService.UserHasAccessAsync(board.ProjectId, userId, cancellationToken);
    }

    private async Task<Board?> GetBoardWithDetailsAsync(Guid boardId, CancellationToken cancellationToken)
    {
        return await _context.Boards
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tasks.Where(t => !t.IsDeleted).OrderBy(t => t.Position))
                    .ThenInclude(t => t.TaskLabels)
                        .ThenInclude(tl => tl.Label)
            .FirstOrDefaultAsync(b => b.Id == boardId && !b.IsDeleted, cancellationToken);
    }

    private static BoardResponse MapToResponse(Board board)
    {
        return new BoardResponse
        {
            Id = board.Id,
            ProjectId = board.ProjectId,
            Name = board.Name,
            Description = board.Description,
            Type = board.Type.ToString(),
            Position = board.Position,
            IsDefault = board.IsDefault,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
            TaskCount = board.Columns.SelectMany(c => c.Tasks).Count(),
            Columns = board.Columns.Select(c => new ColumnResponse
            {
                Id = c.Id,
                BoardId = c.BoardId,
                Name = c.Name,
                Position = c.Position,
                Color = c.Color,
                WipLimit = c.WipLimit,
                TaskStatus = c.TaskStatus?.ToString(),
                TaskCount = c.Tasks.Count,
                Tasks = c.Tasks.Select(t => new BoardTaskResponse
                {
                    Id = t.Id,
                    ColumnId = t.ColumnId,
                    Title = t.Title,
                    Description = t.Description,
                    TaskKey = t.TaskKey,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    Position = t.Position,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.Assignee?.FullName,
                    CreatedAt = t.CreatedAt,
                    Labels = t.TaskLabels
                        .Where(tl => tl.Label != null && !tl.Label.IsDeleted)
                        .Select(tl => new BoardLabelResponse
                        {
                            Id = tl.Label!.Id,
                            ProjectId = tl.Label.ProjectId,
                            Name = tl.Label.Name,
                            Color = tl.Label.Color,
                            TaskCount = 0 // Task count not needed for individual task labels
                        }).ToList()
                }).ToList()
            }).ToList()
        };
    }

    private static string GetColumnColor(int position)
    {
        return position switch
        {
            0 => "#ef4444", // Red for To Do
            1 => "#f59e0b", // Amber for In Progress
            2 => "#22c55e", // Green for Done
            _ => "#6b7280"  // Gray for others
        };
    }

    private static Domain.Enums.BoardType ParseBoardType(string type)
    {
        return type?.ToLower() switch
        {
            "kanban" => Domain.Enums.BoardType.Kanban,
            "list" => Domain.Enums.BoardType.List,
            "timeline" => Domain.Enums.BoardType.Timeline,
            _ => Domain.Enums.BoardType.Kanban
        };
    }
}
