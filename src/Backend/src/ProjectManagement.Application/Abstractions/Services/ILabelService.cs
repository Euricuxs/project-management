using ProjectManagement.Application.DTOs.Label;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for label operations.
/// </summary>
public interface ILabelService
{
    /// <summary>
    /// Get all labels for a project.
    /// </summary>
    Task<IReadOnlyList<LabelListItemResponse>> GetProjectLabelsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a label by ID.
    /// </summary>
    Task<LabelResponse?> GetLabelByIdAsync(Guid labelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new label.
    /// </summary>
    Task<LabelResponse> CreateLabelAsync(Guid projectId, CreateLabelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing label.
    /// </summary>
    Task<LabelResponse> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a label (soft delete).
    /// </summary>
    Task<bool> DeleteLabelAsync(Guid labelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add labels to a task.
    /// </summary>
    Task<bool> AddLabelsToTaskAsync(Guid taskId, AddLabelsToTaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a label from a task.
    /// </summary>
    Task<bool> RemoveLabelFromTaskAsync(Guid taskId, Guid labelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all labels for a task.
    /// </summary>
    Task<IReadOnlyList<LabelListItemResponse>> GetTaskLabelsAsync(Guid taskId, CancellationToken cancellationToken = default);
}
