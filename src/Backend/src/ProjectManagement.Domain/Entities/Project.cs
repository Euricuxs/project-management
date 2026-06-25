using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Project entity - belongs to a workspace.
/// </summary>
public class Project : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Key { get; set; } // Short identifier like "PROJ", "TASK"
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? IconUrl { get; set; }
    public string Color { get; set; } = "#3b82f6"; // Default blue

    // Navigation properties
    public Workspace? Workspace { get; set; }
    public ICollection<Board> Boards { get; set; } = new List<Board>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
