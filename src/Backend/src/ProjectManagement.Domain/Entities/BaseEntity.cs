namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Base class for all domain entities with common properties.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Shadow properties for EF Core concurrency
    public byte[]? RowVersion { get; set; }
}
