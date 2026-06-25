namespace ProjectManagement.Application.DTOs.Project;

/// <summary>
/// Request model for updating an existing project.
/// </summary>
public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Key { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public string Status { get; set; } = "Planning";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
