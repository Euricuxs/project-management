using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Task;

/// <summary>
/// Request model for creating a new task.
/// </summary>
public class CreateTaskRequest
{
    [Required]
    public Guid ColumnId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(10000)]
    public string? Description { get; set; }

    /// <summary>
    /// Priority: Low, Medium, High, Critical. Defaults to Medium.
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Status: Todo, InProgress, InReview, Done, Cancelled. Defaults to Todo.
    /// </summary>
    public string Status { get; set; } = "Todo";

    /// <summary>
    /// Due date for the task. Optional.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Position/order within the column. Defaults to appending at the end.
    /// </summary>
    public int Position { get; set; } = 0;

    /// <summary>
    /// Assignee user ID. Optional.
    /// </summary>
    public Guid? AssigneeId { get; set; }
}
