namespace ProjectManagement.Application.DTOs.Project;

/// <summary>
/// Request model for creating a new project.
/// </summary>
public class CreateProjectRequest
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Key { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
