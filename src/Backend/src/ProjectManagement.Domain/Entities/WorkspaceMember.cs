using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Domain.Entities;

/// <summary>
/// WorkspaceMember - join table between User and Workspace with role.
/// </summary>
public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Workspace? Workspace { get; set; }
    public User? User { get; set; }
}
