namespace ProjectManagement.Domain.Entities;

/// <summary>
/// Column entity - represents a column on a Kanban board.
/// </summary>
public class Column : BaseEntity
{
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; } = 0;
    public int WipLimit { get; set; } = 0; // Work-in-progress limit, 0 = unlimited
    public string Color { get; set; } = "#6b7280"; // Default gray
    public Enums.TaskStatus? TaskStatus { get; set; } // Optional status mapping for this column

    // Navigation properties
    public Board? Board { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
