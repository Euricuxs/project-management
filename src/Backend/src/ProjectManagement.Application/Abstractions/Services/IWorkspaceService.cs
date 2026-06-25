using ProjectManagement.Application.DTOs.Workspace;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for workspace operations.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Get all workspaces for the current user.
    /// </summary>
    Task<IReadOnlyList<WorkspaceListItemResponse>> GetUserWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a workspace by ID.
    /// </summary>
    Task<WorkspaceResponse?> GetWorkspaceByIdAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new workspace.
    /// </summary>
    Task<WorkspaceResponse> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing workspace.
    /// </summary>
    Task<WorkspaceResponse> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a workspace (soft delete).
    /// </summary>
    Task<bool> DeleteWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has access to a workspace.
    /// </summary>
    Task<bool> UserHasAccessAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the count of workspaces for a user.
    /// </summary>
    Task<int> GetUserWorkspaceCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
