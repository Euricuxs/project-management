using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Board;

/// <summary>
/// Request model for creating a new board.
/// </summary>
public class CreateBoardRequest
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Board type: "Kanban", "List", or "Timeline". Defaults to "Kanban".
    /// </summary>
    public string Type { get; set; } = "Kanban";

    public int Position { get; set; } = 0;

    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// List of default column names to create. If null or empty, default columns will be created.
    /// </summary>
    public List<string>? DefaultColumns { get; set; }
}
