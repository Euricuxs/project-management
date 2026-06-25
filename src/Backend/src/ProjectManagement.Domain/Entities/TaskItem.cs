namespace ProjectManagement.Domain.Entities;

/// <summary>
/// TaskItem entity - represents a work item on a Kanban board.
/// Named TaskItem to avoid conflict with System.Threading.Tasks.Task.
/// </summary>
public class TaskItem : BaseEntity
{
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TaskKey { get; set; } // e.g., "PROJ-123"
    public int Position { get; set; } = 0;
    public Enums.TaskPriority Priority { get; set; } = Enums.TaskPriority.Medium;
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Todo;
    public DateTime? DueDate { get; set; }
    public int EstimatedHours { get; set; } = 0;
    public int ActualHours { get; set; } = 0;
    public Guid? AssigneeId { get; set; }
    public Guid ReporterId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Column? Column { get; set; }
    public User? Assignee { get; set; }
    public User? Reporter { get; set; }
    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}
