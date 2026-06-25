namespace ProjectManagement.Application.DTOs.Workspace;

/// <summary>
/// Response model for workspace data.
/// </summary>
public class WorkspaceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}