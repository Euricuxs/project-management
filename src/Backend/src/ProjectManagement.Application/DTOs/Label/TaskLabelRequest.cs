using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Label;

/// <summary>
/// Request model for adding labels to a task.
/// </summary>
public class AddLabelsToTaskRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one label ID is required")]
    public List<Guid> LabelIds { get; set; } = new();
}

/// <summary>
/// Request model for removing a label from a task.
/// </summary>
public class RemoveLabelFromTaskRequest
{
    [Required]
    public Guid LabelId { get; set; }
}
