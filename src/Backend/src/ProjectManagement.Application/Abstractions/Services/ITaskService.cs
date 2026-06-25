using ProjectManagement.Application.DTOs.Task;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for task operations.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Get a task by ID.
    /// </summary>
    Task<TaskResponse?> GetTaskByIdAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks for a column.
    /// </summary>
    Task<IReadOnlyList<TaskListItemResponse>> GetTasksByColumnAsync(Guid columnId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tasks for a board.
    /// </summary>
    Task<IReadOnlyList<TaskListItemResponse>> GetTasksByBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new task.
    /// </summary>
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing task.
    /// </summary>
    Task<TaskResponse> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a task (soft delete).
    /// </summary>
    Task<bool> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Move a task to a different column/position.
    /// </summary>
    Task<TaskResponse> MoveTaskAsync(Guid taskId, Guid targetColumnId, int targetPosition, Guid userId, CancellationToken cancellationToken = default);
}
