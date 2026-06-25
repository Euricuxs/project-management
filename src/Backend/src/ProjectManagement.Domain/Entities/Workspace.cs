namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Workspace entity - top-level organizational unit.
/// Represents a company, team, or organization.
/// </summary>
public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public Guid OwnerId { get; set; }
    public bool IsPublic { get; set; } = true;

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
