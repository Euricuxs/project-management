using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Board;

/// <summary>
/// Request model for updating an existing board.
/// </summary>
public class UpdateBoardRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Board type: "Kanban", "List", or "Timeline".
    /// </summary>
    public string? Type { get; set; }

    public int? Position { get; set; }

    public bool? IsDefault { get; set; }
}
