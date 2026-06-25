namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Label entity - represents a colored label that can be assigned to tasks.
/// Labels are scoped to a project for organization.
/// </summary>
public class Label : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6b7280"; // Default gray color

    // Navigation properties
    public Project? Project { get; set; }
    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}
