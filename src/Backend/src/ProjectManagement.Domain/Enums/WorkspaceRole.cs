namespace ProjectManagement.Domain.Enums;

/// <summary>
/// User role within a workspace.
/// </summary>
public enum WorkspaceRole
{
    Owner = 0,
    Admin = 1,
    Member = 2,
    Guest = 3
}
