namespace ProjectManagement.Domain.Entities;

/// <summary>
/// TaskLabel entity - join table for many-to-many relationship between TaskItem and Label.
/// </summary>
public class TaskLabel
{
    public Guid TaskId { get; set; }
    public Guid LabelId { get; set; }

    // Navigation properties
    public TaskItem? Task { get; set; }
    public Label? Label { get; set; }
}
