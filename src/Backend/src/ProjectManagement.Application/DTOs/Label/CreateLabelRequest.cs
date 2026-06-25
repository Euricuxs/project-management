using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Label;

/// <summary>
/// Request model for creating a new label.
/// </summary>
public class CreateLabelRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (e.g., #FF5733)")]
    public string Color { get; set; } = "#6b7280";
}
