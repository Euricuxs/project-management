using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs.Label;

/// <summary>
/// Request model for updating an existing label.
/// </summary>
public class UpdateLabelRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (e.g., #FF5733)")]
    public string? Color { get; set; }
}
