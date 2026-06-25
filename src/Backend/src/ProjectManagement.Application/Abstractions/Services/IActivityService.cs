using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Activity;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service interface for activity tracking operations.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Log an activity (create new record).
    /// </summary>
    Task LogActivityAsync(
        Guid userId,
        string? userName,
        string entityType,
        Guid entityId,
        string? entityName,
        Guid projectId,
        string type,
        string description,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log multiple activities in a batch for bulk operations.
    /// Uses a single database transaction for efficiency.
    /// </summary>
    Task LogActivityBatchAsync(
        IEnumerable<ActivityBatchItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activities for a project with pagination and filtering.
    /// </summary>
    Task<PaginatedResult<ActivityListItemResponse>> GetProjectActivitiesAsync(
        Guid projectId,
        ActivityQueryParams query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activities for a specific entity.
    /// </summary>
    Task<IReadOnlyList<ActivityListItemResponse>> GetEntityActivitiesAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent activities across all accessible projects.
    /// </summary>
    Task<IReadOnlyList<ActivityListItemResponse>> GetRecentActivitiesAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single item in a batch activity log operation.
/// </summary>
public class ActivityBatchItem
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? EntityName { get; set; }
    public Guid ProjectId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
