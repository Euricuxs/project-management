using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Task;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;
using AppTaskStatus = ProjectManagement.Domain.Enums.TaskStatus;
using ValidationException = ProjectManagement.Application.Exceptions.ValidationException;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for task operations.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly IBoardService _boardService;
    private readonly IActivityService _activityService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ApplicationDbContext context,
        IBoardService boardService,
        IActivityService activityService,
        ILogger<TaskService> logger)
    {
        _context = context;
        _boardService = boardService;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Reporter)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);

        if (task == null)
            return null;

        // Verify user has access
        if (!await HasTaskAccessAsync(task, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this task.");

        return MapToResponse(task);
    }

    public async Task<IReadOnlyList<TaskListItemResponse>> GetTasksByColumnAsync(Guid columnId, Guid userId, CancellationToken cancellationToken = default)
    {
        var column = await _context.Columns
            .Include(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == columnId && !c.IsDeleted, cancellationToken);

        // Ensure Board is loaded
        if (column.Board == null)
        {
            await _context.Entry(column).Reference(c => c.Board).LoadAsync(cancellationToken);
        }

        if (column == null)
            throw new NotFoundException("Column not found.");

        // Verify user has access
        if (!await _boardService.UserHasAccessAsync(column.BoardId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this column.");

        var tasks = await _context.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.ColumnId == columnId && !t.IsDeleted)
            .OrderBy(t => t.Position)
            .ToListAsync(cancellationToken);

        return tasks.Select(MapToListItem).ToList();
    }

    public async Task<IReadOnlyList<TaskListItemResponse>> GetTasksByBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Verify user has access
        if (!await _boardService.UserHasAccessAsync(boardId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this board.");

        var tasks = await _context.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.Column!.BoardId == boardId && !t.IsDeleted)
            .OrderBy(t => t.ColumnId)
            .ThenBy(t => t.Position)
            .ToListAsync(cancellationToken);

        return tasks.Select(MapToListItem).ToList();
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("CreateTaskAsync started - ColumnId: {ColumnId}, Title: {Title}, UserId: {UserId}",
            request.ColumnId, request.Title, userId);

        // Validate user exists in database
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
        {
            _logger.LogWarning("CreateTaskAsync failed - User {UserId} does not exist in database", userId);
            throw new ValidationException($"User with ID {userId} not found.");
        }

        var column = await _context.Columns
            .Include(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && !c.IsDeleted, cancellationToken);

        if (column == null)
        {
            _logger.LogWarning("CreateTaskAsync failed - Column {ColumnId} not found", request.ColumnId);
            throw new NotFoundException("Column not found.");
        }

        // Ensure Board is loaded for task key generation
        if (column.Board == null)
        {
            _logger.LogDebug("Board not loaded via Include, attempting lazy load for Column {ColumnId}", request.ColumnId);
            await _context.Entry(column).Reference(c => c.Board).LoadAsync(cancellationToken);
        }

        _logger.LogDebug("Column {ColumnId} loaded with Board: {BoardName}", request.ColumnId, column.Board?.Name);

        // Verify user has access
        if (!await _boardService.UserHasAccessAsync(column.BoardId, userId, cancellationToken))
        {
            _logger.LogWarning("CreateTaskAsync failed - User {UserId} has no access to column {ColumnId}", userId, request.ColumnId);
            throw new ForbiddenException("You do not have access to this column.");
        }

        // Validate title
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Task title is required.");

        if (request.Title.Length > 500)
            throw new ValidationException("Task title cannot exceed 500 characters.");

        // Validate description
        if (request.Description != null && request.Description.Length > 10000)
            throw new ValidationException("Description cannot exceed 10000 characters.");

        // Validate priority
        if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority))
            throw new ValidationException($"Invalid priority value: {request.Priority}. Valid values: Low, Medium, High, Critical.");

        // Validate status
        if (!Enum.TryParse<AppTaskStatus>(request.Status, true, out var status))
            throw new ValidationException($"Invalid status value: {request.Status}. Valid values: Todo, InProgress, InReview, Done, Cancelled.");

        // Determine position
        var position = request.Position;
        if (position <= 0)
        {
            var maxPosition = await _context.Tasks
                .Where(t => t.ColumnId == request.ColumnId && !t.IsDeleted)
                .MaxAsync(t => (int?)t.Position, cancellationToken) ?? -1;
            position = maxPosition + 1;
        }

        // Generate task key (e.g., "BOARD-1")
        var boardName = column.Board?.Name ?? "BOARD";
        if (string.IsNullOrEmpty(boardName))
        {
            boardName = "BOARD";
            _logger.LogWarning("Board name was null/empty for BoardId {BoardId}, using default 'BOARD'", column.BoardId);
        }

        var sanitizedBoardName = new string(boardName.Where(c => char.IsLetterOrDigit(c)).ToArray());
        if (string.IsNullOrEmpty(sanitizedBoardName))
        {
            sanitizedBoardName = "BOARD";
        }
        var keyPrefix = sanitizedBoardName.ToUpperInvariant().Replace(" ", "-").Substring(0, Math.Min(5, sanitizedBoardName.Length));

        // Generate task key with retry logic for race condition handling
        string taskKey;
        var maxRetries = 5;
        var counter = 1;

        while (counter <= maxRetries)
        {
            taskKey = $"{keyPrefix}-{counter}";

            // Check if key exists (including deleted for safety)
            var keyExists = await _context.Tasks
                .IgnoreQueryFilters()
                .AnyAsync(t => t.TaskKey == taskKey, cancellationToken);

            if (!keyExists)
            {
                _logger.LogDebug("Generated unique Key: {TaskKey} (counter: {Counter})", taskKey, counter);

                var task = new TaskItem
                {
                    ColumnId = request.ColumnId,
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim(),
                    TaskKey = taskKey,
                    Priority = priority,
                    Status = status,
                    Position = position,
                    DueDate = request.DueDate,
                    AssigneeId = request.AssigneeId,
                    ReporterId = userId,
                };

                _context.Tasks.Add(task);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Task created successfully - Id: {TaskId}, Key: {TaskKey}", task.Id, task.TaskKey);
                }
                catch (DbUpdateException ex)
                {
                    // Check for unique constraint violation (race condition fallback)
                    var innerMsg = ex.InnerException?.Message ?? "";
                    if (innerMsg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("TaskKey collision detected for {TaskKey}, retrying with counter {Counter}", taskKey, counter + 1);
                        _context.Entry(task).State = EntityState.Detached;
                        counter++;
                        continue;
                    }
                    throw;
                }

                // Reload with includes
                await _context.Entry(task).Reference(t => t.Assignee).LoadAsync(cancellationToken);
                await _context.Entry(task).Reference(t => t.Reporter).LoadAsync(cancellationToken);

                // Log activity - Task Created
                await _activityService.LogActivityAsync(
                    userId: userId,
                    userName: task.Reporter?.FullName,
                    entityType: "Task",
                    entityId: task.Id,
                    entityName: task.Title,
                    projectId: column.Board?.ProjectId ?? Guid.Empty,
                    type: ActivityType.TaskCreated.ToString(),
                    description: $"created task \"{task.Title}\"",
                    newValues: new { task.Title, task.Description, task.Priority, task.Status, AssigneeId = task.AssigneeId },
                    cancellationToken: cancellationToken
                );

                return MapToResponse(task);
            }

            counter++;
        }

        throw new ValidationException($"Failed to generate unique task key after {maxRetries} attempts.");
    }

    public async Task<TaskResponse> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Column)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);

        if (task == null)
            throw new NotFoundException("Task not found.");

        // Verify user has access
        if (!await HasTaskAccessAsync(task, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this task.");

        // Update title if provided
        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Task title cannot be empty.");
            if (request.Title.Length > 500)
                throw new ValidationException("Task title cannot exceed 500 characters.");
            task.Title = request.Title.Trim();
        }

        // Update description if provided
        if (request.Description != null)
        {
            if (request.Description.Length > 10000)
                throw new ValidationException("Description cannot exceed 10000 characters.");
            task.Description = request.Description.Trim();
        }

        // Update priority if provided
        if (request.Priority != null)
        {
            if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority))
                throw new ValidationException($"Invalid priority value: {request.Priority}.");
            task.Priority = priority;
        }

        // Update status if provided
        AppTaskStatus? previousStatus = null;
        if (request.Status != null)
        {
            if (!Enum.TryParse<AppTaskStatus>(request.Status, true, out var status))
                throw new ValidationException($"Invalid status value: {request.Status}.");

            previousStatus = task.Status;
            task.Status = status;

            // Set CompletedAt when status changes to Done
            if (task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (status != AppTaskStatus.Done)
            {
                task.CompletedAt = null;
            }
        }

        // Update due date if provided (null means keep, explicitly set null means remove)
        task.DueDate = request.DueDate;

        // Update position if provided
        if (request.Position.HasValue)
        {
            task.Position = request.Position.Value;
        }

        // Move to different column if requested
        if (request.ColumnId.HasValue && request.ColumnId.Value != task.ColumnId)
        {
            var targetColumn = await _context.Columns
                .FirstOrDefaultAsync(c => c.Id == request.ColumnId && !c.IsDeleted, cancellationToken);

            if (targetColumn == null)
                throw new NotFoundException("Target column not found.");

            task.ColumnId = request.ColumnId.Value;
        }

        // Update assignee if provided (null means unassign)
        Guid? previousAssigneeId = null;
        if (request.AssigneeId != task.AssigneeId)
        {
            previousAssigneeId = task.AssigneeId;
        }
        task.AssigneeId = request.AssigneeId;

        // Capture old values for activity log
        var oldValues = new
        {
            task.Title,
            task.Description,
            task.Priority,
            Status = previousStatus?.ToString() ?? task.Status.ToString(),
            task.DueDate,
            task.ColumnId,
            task.Position,
            task.AssigneeId
        };

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        await _context.Entry(task).Reference(t => t.Assignee).LoadAsync(cancellationToken);
        await _context.Entry(task).Reference(t => t.Reporter).LoadAsync(cancellationToken);

        // Load board for projectId
        await _context.Entry(task).Reference(t => t.Column).LoadAsync(cancellationToken);
        await _context.Entry(task.Column!).Reference(c => c.Board).LoadAsync(cancellationToken);
        var projectId = task.Column?.Board?.ProjectId ?? Guid.Empty;

        // Log activity - Task Updated
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: task.Reporter?.FullName,
            entityType: "Task",
            entityId: task.Id,
            entityName: task.Title,
            projectId: projectId,
            type: ActivityType.TaskUpdated.ToString(),
            description: $"updated task \"{task.Title}\"",
            oldValues: oldValues,
            newValues: new { task.Title, task.Description, task.Priority, Status = task.Status.ToString(), task.DueDate, task.ColumnId, task.Position, task.AssigneeId },
            cancellationToken: cancellationToken
        );

        // Log assignment changes separately
        if (previousAssigneeId.HasValue && request.AssigneeId.HasValue)
        {
            await _activityService.LogActivityAsync(
                userId: userId,
                userName: task.Reporter?.FullName,
                entityType: "Task",
                entityId: task.Id,
                entityName: task.Title,
                projectId: projectId,
                type: ActivityType.TaskAssigned.ToString(),
                description: $"reassigned task \"{task.Title}\"",
                newValues: new { AssigneeId = task.AssigneeId, AssigneeName = task.Assignee?.FullName },
                cancellationToken: cancellationToken
            );
        }
        else if (previousAssigneeId.HasValue && !request.AssigneeId.HasValue)
        {
            await _activityService.LogActivityAsync(
                userId: userId,
                userName: task.Reporter?.FullName,
                entityType: "Task",
                entityId: task.Id,
                entityName: task.Title,
                projectId: projectId,
                type: ActivityType.TaskUnassigned.ToString(),
                description: $"unassigned task \"{task.Title}\"",
                cancellationToken: cancellationToken
            );
        }
        else if (!previousAssigneeId.HasValue && request.AssigneeId.HasValue)
        {
            await _activityService.LogActivityAsync(
                userId: userId,
                userName: task.Reporter?.FullName,
                entityType: "Task",
                entityId: task.Id,
                entityName: task.Title,
                projectId: projectId,
                type: ActivityType.TaskAssigned.ToString(),
                description: $"assigned task \"{task.Title}\" to {task.Assignee?.FullName}",
                newValues: new { AssigneeId = task.AssigneeId, AssigneeName = task.Assignee?.FullName },
                cancellationToken: cancellationToken
            );
        }

        return MapToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Column)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);

        if (task == null)
            throw new NotFoundException("Task not found.");

        // Verify user has access
        if (!await HasTaskAccessAsync(task, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this task.");

        var taskTitle = task.Title;

        // Load board for projectId
        if (task.Column?.Board == null)
        {
            await _context.Entry(task).Reference(t => t.Column).LoadAsync(cancellationToken);
            await _context.Entry(task.Column!).Reference(c => c.Board).LoadAsync(cancellationToken);
        }
        var projectId = task.Column?.Board?.ProjectId ?? Guid.Empty;

        // Soft delete
        task.IsDeleted = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Log activity - Task Deleted
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: null,
            entityType: "Task",
            entityId: task.Id,
            entityName: taskTitle,
            projectId: projectId,
            type: ActivityType.TaskDeleted.ToString(),
            description: $"deleted task \"{taskTitle}\"",
            oldValues: new { task.Title, task.Status, task.Priority },
            cancellationToken: cancellationToken
        );

        return true;
    }

    public async Task<TaskResponse> MoveTaskAsync(Guid taskId, Guid targetColumnId, int targetPosition, Guid userId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(t => t.Column)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);

        if (task == null)
            throw new NotFoundException("Task not found.");

        // Verify user has access
        if (!await HasTaskAccessAsync(task, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this task.");

        var targetColumn = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == targetColumnId && !c.IsDeleted, cancellationToken);

        if (targetColumn == null)
            throw new NotFoundException("Target column not found.");

        // Use transaction for reordering
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var oldColumnId = task.ColumnId;
            var oldPosition = task.Position;
            var oldStatus = task.Status;

            // Update positions of other tasks in old column (shift up)
            if (oldColumnId != targetColumnId)
            {
                var oldColumnTasks = await _context.Tasks
                    .Where(t => t.ColumnId == oldColumnId && t.Id != taskId && !t.IsDeleted && t.Position > oldPosition)
                    .ToListAsync(cancellationToken);

                foreach (var t in oldColumnTasks)
                {
                    t.Position--;
                }
            }

            // Update positions of tasks in target column (shift down)
            var targetColumnTasks = await _context.Tasks
                .Where(t => t.ColumnId == targetColumnId && t.Id != taskId && !t.IsDeleted && t.Position >= targetPosition)
                .ToListAsync(cancellationToken);

            foreach (var t in targetColumnTasks)
            {
                t.Position++;
            }

            // Move the task
            task.ColumnId = targetColumnId;
            task.Position = targetPosition;

            // Sync task status with target column if it has a status mapping
            if (targetColumn.TaskStatus.HasValue)
            {
                task.Status = targetColumn.TaskStatus.Value;

                // Set CompletedAt when moving to Done
                if (task.Status == AppTaskStatus.Done && oldStatus != AppTaskStatus.Done)
                {
                    task.CompletedAt = DateTime.UtcNow;
                }
                // Clear CompletedAt when moving out of Done
                else if (oldStatus == AppTaskStatus.Done && task.Status != AppTaskStatus.Done)
                {
                    task.CompletedAt = null;
                }
            }

            // Capture values for activity logging
            var taskTitle = task.Title;
            var movedBetweenColumns = oldColumnId != targetColumnId;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Reload with includes
            await _context.Entry(task).Reference(t => t.Assignee).LoadAsync(cancellationToken);
            await _context.Entry(task).Reference(t => t.Reporter).LoadAsync(cancellationToken);

            // Load board for projectId
            if (task.Column?.Board == null)
            {
                await _context.Entry(task).Reference(t => t.Column).LoadAsync(cancellationToken);
            }
            var projectId = task.Column?.Board?.ProjectId ?? Guid.Empty;

            // Log activity - Task Moved
            if (movedBetweenColumns)
            {
                await _activityService.LogActivityAsync(
                    userId: userId,
                    userName: task.Reporter?.FullName,
                    entityType: "Task",
                    entityId: task.Id,
                    entityName: taskTitle,
                    projectId: projectId,
                    type: ActivityType.TaskMoved.ToString(),
                    description: $"moved task \"{taskTitle}\" to column \"{targetColumn.Name}\"",
                    oldValues: new { ColumnId = oldColumnId, Position = oldPosition, Status = oldStatus.ToString() },
                    newValues: new { ColumnId = task.ColumnId, Position = task.Position, Status = task.Status.ToString() },
                    cancellationToken: cancellationToken
                );
            }

            return MapToResponse(task);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> HasTaskAccessAsync(TaskItem task, Guid userId, CancellationToken cancellationToken)
    {
        // Load board if not already loaded
        if (task.Column?.Board == null)
        {
            await _context.Entry(task).Reference(t => t.Column).LoadAsync(cancellationToken);
            await _context.Entry(task.Column!).Reference(c => c.Board).LoadAsync(cancellationToken);
        }

        return await _boardService.UserHasAccessAsync(task.Column!.BoardId, userId, cancellationToken);
    }

    private static TaskResponse MapToResponse(TaskItem task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            ColumnId = task.ColumnId,
            Title = task.Title,
            Description = task.Description,
            TaskKey = task.TaskKey,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            Position = task.Position,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            AssigneeId = task.AssigneeId,
            AssigneeName = task.Assignee?.FullName,
            ReporterId = task.ReporterId,
            ReporterName = task.Reporter?.FullName,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
        };
    }

    private static TaskListItemResponse MapToListItem(TaskItem task)
    {
        return new TaskListItemResponse
        {
            Id = task.Id,
            ColumnId = task.ColumnId,
            Title = task.Title,
            TaskKey = task.TaskKey,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            Position = task.Position,
            DueDate = task.DueDate,
            AssigneeId = task.AssigneeId,
            AssigneeName = task.Assignee?.FullName,
            CreatedAt = task.CreatedAt,
        };
    }
}
