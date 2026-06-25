namespace ProjectManagement.Application.DTOs.Activity;

/// <summary>
/// Response model for an activity record.
/// </summary>
public class ActivityResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? EntityName { get; set; }
    public Guid ProjectId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response model for activity list item (without full state snapshots).
/// </summary>
public class ActivityListItemResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? EntityName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Query parameters for filtering activities.
/// </summary>
public class ActivityQueryParams
{
    public Guid? ProjectId { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? Type { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
