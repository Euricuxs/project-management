using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Task;

/// <summary>
/// Request model for updating an existing task.
/// </summary>
public class UpdateTaskRequest
{
    [StringLength(500, MinimumLength = 1)]
    public string? Title { get; set; }

    [StringLength(10000)]
    public string? Description { get; set; }

    /// <summary>
    /// Priority: Low, Medium, High, Critical.
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Status: Todo, InProgress, InReview, Done, Cancelled.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Due date for the task. Set to null to remove due date.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Position/order within the column.
    /// </summary>
    public int? Position { get; set; }

    /// <summary>
    /// Column ID to move task to another column.
    /// </summary>
    public Guid? ColumnId { get; set; }

    /// <summary>
    /// Assignee user ID. Set to null to unassign.
    /// </summary>
    public Guid? AssigneeId { get; set; }
}
