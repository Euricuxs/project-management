using ProjectManagement.Application.DTOs.Board;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for board operations.
/// </summary>
public interface IBoardService
{
    /// <summary>
    /// Get all boards for a project.
    /// </summary>
    Task<IReadOnlyList<BoardListItemResponse>> GetProjectBoardsAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single board by ID with columns and tasks.
    /// </summary>
    Task<BoardResponse?> GetBoardByIdAsync(
        Guid boardId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new board with default columns.
    /// </summary>
    Task<BoardResponse> CreateBoardAsync(
        CreateBoardRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing board.
    /// </summary>
    Task<BoardResponse> UpdateBoardAsync(
        Guid boardId,
        UpdateBoardRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a board (soft delete).
    /// </summary>
    Task<bool> DeleteBoardAsync(
        Guid boardId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has access to a board.
    /// </summary>
    Task<bool> UserHasAccessAsync(
        Guid boardId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
