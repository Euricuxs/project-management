namespace ProjectManagement.Application.DTOs.Workspace;

/// <summary>
/// Response model for workspace list items.
/// </summary>
public class WorkspaceListItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsPublic { get; set; }
    public string Role { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
    public DateTime CreatedAt { get; set; }
}