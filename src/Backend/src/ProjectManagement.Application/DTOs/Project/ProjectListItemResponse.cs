namespace ProjectManagement.Application.DTOs.Project;

/// <summary>
/// Response model for project list items.
/// </summary>
public class ProjectListItemResponse
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Key { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int BoardCount { get; set; }
    public int TaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
