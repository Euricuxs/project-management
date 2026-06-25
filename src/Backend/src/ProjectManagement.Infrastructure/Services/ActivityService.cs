using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.Common.Models;
using ProjectManagement.Application.DTOs.Activity;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Service implementation for activity tracking operations.
/// Activities are immutable - only logging and retrieval are supported.
/// </summary>
public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ActivityService(ApplicationDbContext context, ILogger<ActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActivityAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse type string to enum, default to TaskUpdated if invalid
            if (!Enum.TryParse<ActivityType>(type, true, out var activityType))
            {
                _logger.LogWarning("Unknown activity type '{Type}' for entity {EntityType}/{EntityId}, defaulting to TaskUpdated",
                    type, entityType, entityId);
                activityType = ActivityType.TaskUpdated;
            }

            var activity = new Activity
            {
                UserId = userId,
                UserName = userName,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                ProjectId = projectId,
                Type = activityType,
                Description = description,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, JsonOptions) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues, JsonOptions) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Activity logged: {Type} for {EntityType}/{EntityId} by User {UserId}",
                type, entityType, entityId, userId);
        }
        catch (Exception ex)
        {
            // Log but don't throw - activity logging should not break main operations
            _logger.LogError(ex, "Failed to log activity: {Type} for {EntityType}/{EntityId}. Error: {Error}",
                type, entityType, entityId, ex.Message);
        }
    }

    public async Task LogActivityBatchAsync(
        IEnumerable<ActivityBatchItem> items,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return;

        _logger.LogDebug("Processing batch of {Count} activity items", itemList.Count);

        try
        {
            var activities = new List<Activity>();

            foreach (var item in itemList)
            {
                // Parse type string to enum
                if (!Enum.TryParse<ActivityType>(item.Type, true, out var activityType))
                {
                    _logger.LogWarning("Unknown activity type '{Type}' in batch, defaulting to TaskUpdated", item.Type);
                    activityType = ActivityType.TaskUpdated;
                }

                var activity = new Activity
                {
                    UserId = item.UserId,
                    UserName = item.UserName,
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    EntityName = item.EntityName,
                    ProjectId = item.ProjectId,
                    Type = activityType,
                    Description = item.Description,
                    OldValues = item.OldValues != null ? JsonSerializer.Serialize(item.OldValues, JsonOptions) : null,
                    NewValues = item.NewValues != null ? JsonSerializer.Serialize(item.NewValues, JsonOptions) : null,
                    IpAddress = item.IpAddress,
                    UserAgent = item.UserAgent,
                    CreatedAt = DateTime.UtcNow
                };

                activities.Add(activity);
            }

            // Use bulk insert for efficiency
            await _context.Activities.AddRangeAsync(activities, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Batch logged {Count} activities successfully", activities.Count);
        }
        catch (Exception ex)
        {
            // Log but don't throw - activity logging should not break main operations
            _logger.LogError(ex, "Failed to log batch of {Count} activities. Error: {Error}",
                itemList.Count, ex.Message);

            // Attempt to log individually as fallback
            var successCount = 0;
            foreach (var item in itemList)
            {
                try
                {
                    await LogActivityAsync(
                        item.UserId, item.UserName, item.EntityType, item.EntityId,
                        item.EntityName, item.ProjectId, item.Type, item.Description,
                        item.OldValues, item.NewValues, item.IpAddress, item.UserAgent,
                        cancellationToken);
                    successCount++;
                }
                catch
                {
                    // Continue with next item
                }
            }

            _logger.LogWarning("Individual fallback logging succeeded for {SuccessCount}/{Total} items",
                successCount, itemList.Count);
        }
    }

    public async Task<PaginatedResult<ActivityListItemResponse>> GetProjectActivitiesAsync(
        Guid projectId,
        ActivityQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.Activities
            .Where(a => a.ProjectId == projectId)
            .AsNoTracking();

        // Apply filters
        if (query.EntityId.HasValue)
        {
            queryable = queryable.Where(a => a.EntityId == query.EntityId.Value);
        }

        if (!string.IsNullOrEmpty(query.EntityType))
        {
            queryable = queryable.Where(a => a.EntityType == query.EntityType);
        }

        if (!string.IsNullOrEmpty(query.Type))
        {
            if (Enum.TryParse<ActivityType>(query.Type, true, out var typeEnum))
            {
                queryable = queryable.Where(a => a.Type == typeEnum);
            }
        }

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(a => a.UserId == query.UserId.Value);
        }

        if (query.FromDate.HasValue)
        {
            queryable = queryable.Where(a => a.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            queryable = queryable.Where(a => a.CreatedAt <= query.ToDate.Value);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new ActivityListItemResponse
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                UserId = a.UserId,
                UserName = a.UserName,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ActivityListItemResponse>(
            items,
            totalCount,
            query.PageNumber,
            query.PageSize
        );
    }

    public async Task<IReadOnlyList<ActivityListItemResponse>> GetEntityActivitiesAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var activities = await _context.Activities
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100) // Limit for single entity
            .AsNoTracking()
            .Select(a => new ActivityListItemResponse
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                UserId = a.UserId,
                UserName = a.UserName,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return activities;
    }

    public async Task<IReadOnlyList<ActivityListItemResponse>> GetRecentActivitiesAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // Get project IDs that the user has access to
        var accessibleProjectIds = await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId)
            .Select(wm => wm.Workspace!.Projects.Select(p => p.Id))
            .SelectMany(ids => ids)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get recent activities from those projects
        var activities = await _context.Activities
            .Where(a => accessibleProjectIds.Contains(a.ProjectId))
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .Select(a => new ActivityListItemResponse
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                UserId = a.UserId,
                UserName = a.UserName,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return activities;
    }
}
