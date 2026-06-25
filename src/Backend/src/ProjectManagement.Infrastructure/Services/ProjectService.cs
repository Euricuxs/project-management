using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Project;
using ProjectManagement.Application.Exceptions;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for project operations.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;
    private readonly IActivityService _activityService;

    public ProjectService(
        ApplicationDbContext context,
        IWorkspaceService workspaceService,
        IActivityService activityService)
    {
        _context = context;
        _workspaceService = workspaceService;
        _activityService = activityService;
    }

    public async Task<IReadOnlyList<ProjectListItemResponse>> GetWorkspaceProjectsAsync(
        Guid workspaceId,
        Guid userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(workspaceId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this workspace.");

        var query = _context.Projects
            .Include(p => p.Boards)
                .ThenInclude(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
            .Where(p => p.WorkspaceId == workspaceId && !p.IsDeleted);

        if (!includeArchived)
        {
            query = query.Where(p => p.Status != ProjectStatus.Archived);
        }

        var projects = await query.ToListAsync(cancellationToken);

        return projects.Select(p => new ProjectListItemResponse
        {
            Id = p.Id,
            WorkspaceId = p.WorkspaceId,
            Name = p.Name,
            Key = p.Key,
            Status = p.Status.ToString(),
            Color = p.Color,
            BoardCount = p.Boards.Count,
            TaskCount = p.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Tasks).Count(t => !t.IsDeleted),
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<ProjectResponse?> GetProjectByIdAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Include(p => p.Workspace)
            .Include(p => p.Boards)
                .ThenInclude(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            return null;

        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(project.WorkspaceId, userId, cancellationToken))
            return null;

        return MapToResponse(project);
    }

    public async Task<ProjectResponse> CreateProjectAsync(
        CreateProjectRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(request.WorkspaceId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this workspace.");

        // Check for duplicate name in workspace
        var existingProject = await _context.Projects
            .AnyAsync(p => p.WorkspaceId == request.WorkspaceId &&
                          p.Name.ToLower() == request.Name.ToLower() &&
                          p.Status != ProjectStatus.Archived &&
                          !p.IsDeleted, cancellationToken);

        if (existingProject)
            throw new AlreadyExistsException($"A project with the name '{request.Name}' already exists in this workspace.");

        // Generate key if not provided
        var key = request.Key;
        if (string.IsNullOrEmpty(key))
        {
            var prefix = new string(request.Name.Where(char.IsLetter).Take(4).ToArray()).ToUpper();
            var count = await _context.Projects
                .Where(p => p.WorkspaceId == request.WorkspaceId && p.Key != null && p.Key.StartsWith(prefix))
                .CountAsync(cancellationToken);
            key = $"{prefix}-{count + 1}";
        }

        var project = new Project
        {
            WorkspaceId = request.WorkspaceId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Key = key,
            Color = request.Color,
            Status = ProjectStatus.Planning,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        project = await _context.Projects
            .Include(p => p.Boards)
                .ThenInclude(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
            .FirstAsync(p => p.Id == project.Id, cancellationToken);

        // Log activity - Project Created
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: null,
            entityType: "Project",
            entityId: project.Id,
            entityName: project.Name,
            projectId: project.Id,
            type: ActivityType.ProjectCreated.ToString(),
            description: $"created project \"{project.Name}\"",
            newValues: new { project.Name, project.Description, project.Key, project.Status },
            cancellationToken: cancellationToken
        );

        return MapToResponse(project);
    }

    public async Task<ProjectResponse> UpdateProjectAsync(
        Guid projectId,
        UpdateProjectRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Include(p => p.Boards)
                .ThenInclude(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            throw new NotFoundException("Project not found.");

        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(project.WorkspaceId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this project.");

        // Check for duplicate name (excluding current project)
        var duplicateName = await _context.Projects
            .AnyAsync(p => p.WorkspaceId == project.WorkspaceId &&
                          p.Name.ToLower() == request.Name.ToLower() &&
                          p.Id != projectId &&
                          p.Status != ProjectStatus.Archived &&
                          !p.IsDeleted, cancellationToken);

        if (duplicateName)
            throw new AlreadyExistsException($"A project with the name '{request.Name}' already exists in this workspace.");

        // Parse status
        if (!Enum.TryParse<ProjectStatus>(request.Status, true, out var status))
            status = ProjectStatus.Planning;

        // Capture old values for activity log
        var oldValues = new
        {
            project.Name,
            project.Description,
            project.Key,
            project.Color,
            project.Status,
            project.StartDate,
            project.EndDate
        };

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.Key = request.Key;
        project.Color = request.Color;
        project.Status = status;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;

        await _context.SaveChangesAsync(cancellationToken);

        // Log activity - Project Updated
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: null,
            entityType: "Project",
            entityId: project.Id,
            entityName: project.Name,
            projectId: project.Id,
            type: ActivityType.ProjectUpdated.ToString(),
            description: $"updated project \"{project.Name}\"",
            oldValues: oldValues,
            newValues: new { project.Name, project.Description, project.Key, project.Color, project.Status, project.StartDate, project.EndDate },
            cancellationToken: cancellationToken
        );

        return MapToResponse(project);
    }

    public async Task<bool> ArchiveProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            return false;

        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(project.WorkspaceId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this project.");

        var projectName = project.Name;
        project.Status = ProjectStatus.Archived;
        await _context.SaveChangesAsync(cancellationToken);

        // Log activity - Project Archived
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: null,
            entityType: "Project",
            entityId: project.Id,
            entityName: projectName,
            projectId: project.Id,
            type: ActivityType.ProjectArchived.ToString(),
            description: $"archived project \"{projectName}\"",
            oldValues: new { project.Status },
            cancellationToken: cancellationToken
        );

        return true;
    }

    public async Task<bool> RestoreProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            return false;

        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(project.WorkspaceId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this project.");

        var projectName = project.Name;
        project.Status = ProjectStatus.Active;
        await _context.SaveChangesAsync(cancellationToken);

        // Log activity - Project Restored
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: null,
            entityType: "Project",
            entityId: project.Id,
            entityName: projectName,
            projectId: project.Id,
            type: ActivityType.ProjectRestored.ToString(),
            description: $"restored project \"{projectName}\"",
            oldValues: new { project.Status },
            newValues: new { Status = ProjectStatus.Active },
            cancellationToken: cancellationToken
        );

        return true;
    }

    public async Task<bool> DeleteProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            return false;

        // Check user has access to workspace
        if (!await _workspaceService.UserHasAccessAsync(project.WorkspaceId, userId, cancellationToken))
            throw new ForbiddenException("You do not have access to this project.");

        var projectName = project.Name;

        // Soft delete
        project.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);

        // Log activity - Project Deleted
        await _activityService.LogActivityAsync(
            userId: userId,
            userName: null,
            entityType: "Project",
            entityId: project.Id,
            entityName: projectName,
            projectId: project.Id,
            type: ActivityType.ProjectDeleted.ToString(),
            description: $"deleted project \"{projectName}\"",
            oldValues: new { project.Name, project.Description, project.Key, project.Status },
            cancellationToken: cancellationToken
        );

        return true;
    }

    public async Task<bool> UserHasAccessAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);

        if (project == null)
            return false;

        return await _workspaceService.UserHasAccessAsync(project.WorkspaceId, userId, cancellationToken);
    }

    private static ProjectResponse MapToResponse(Project project)
    {
        return new ProjectResponse
        {
            Id = project.Id,
            WorkspaceId = project.WorkspaceId,
            Name = project.Name,
            Description = project.Description,
            Key = project.Key,
            Status = project.Status.ToString(),
            Color = project.Color,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            IconUrl = project.IconUrl,
            BoardCount = project.Boards.Count,
            TaskCount = project.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Tasks).Count(t => !t.IsDeleted),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
