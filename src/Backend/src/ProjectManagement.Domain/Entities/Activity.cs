namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Activity entity for tracking all changes in the system.
/// This is an immutable audit log - records should never be modified or deleted.
/// </summary>
public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The type of activity that occurred.
    /// </summary>
    public Enums.ActivityType Type { get; set; }

    /// <summary>
    /// The user who performed the action.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional display name for the user (denormalized for performance).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The type of entity affected (e.g., "Task", "Project", "Board").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity affected.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Optional name/title of the entity (denormalized for display).
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// The project this activity belongs to (for efficient querying).
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// JSON snapshot of the old state before the change (null for create).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON snapshot of the new state after the change (null for delete).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional IP address of the client (for security audit).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Optional user agent string (for security audit).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the activity occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Project? Project { get; set; }
}
