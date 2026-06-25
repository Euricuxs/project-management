namespace ProjectManagement.Application.DTOs.Workspace;

/// <summary>
/// Request model for creating a new workspace.
/// </summary>
public class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}