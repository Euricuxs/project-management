namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Result of token validation.
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
    public string? Error { get; set; }
}