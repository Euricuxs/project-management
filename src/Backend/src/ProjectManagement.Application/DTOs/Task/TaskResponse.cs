namespace ProjectManagement.Application.DTOs.Task;

/// <summary>
/// Response model for a task.
/// </summary>
public class TaskResponse
{
    public Guid Id { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TaskKey { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response model for task list item (lighter weight).
/// </summary>
public class TaskListItemResponse
{
    public Guid Id { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TaskKey { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime CreatedAt { get; set; }
}
