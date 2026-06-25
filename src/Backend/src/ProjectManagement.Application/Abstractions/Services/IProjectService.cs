using ProjectManagement.Application.DTOs.Project;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for project operations.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Get all projects for a workspace.
    /// </summary>
    Task<IReadOnlyList<ProjectListItemResponse>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId, bool includeArchived = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a project by ID.
    /// </summary>
    Task<ProjectResponse?> GetProjectByIdAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new project.
    /// </summary>
    Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing project.
    /// </summary>
    Task<ProjectResponse> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive a project (soft delete).
    /// </summary>
    Task<bool> ArchiveProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore an archived project.
    /// </summary>
    Task<bool> RestoreProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently delete a project.
    /// </summary>
    Task<bool> DeleteProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has access to a project.
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}
