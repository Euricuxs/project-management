namespace ProjectManagement.Application.DTOs.Workspace;

/// <summary>
/// Request model for updating an existing workspace.
/// </summary>
public class UpdateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}