namespace ProjectManagement.Domain.Entities;

/// <summary>
/// User entity - represents an application user.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool EmailConfirmed { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = new List<WorkspaceMember>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}".Trim();
}
