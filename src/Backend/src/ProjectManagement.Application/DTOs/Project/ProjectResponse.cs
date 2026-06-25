namespace ProjectManagement.Application.DTOs.Project;

/// <summary>
/// Response model for project data.
/// </summary>
public class ProjectResponse
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Key { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? IconUrl { get; set; }
    public int BoardCount { get; set; }
    public int TaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
