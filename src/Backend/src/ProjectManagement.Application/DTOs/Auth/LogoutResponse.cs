namespace ProjectManagement.Application.DTOs.Auth;

/// <summary>
/// Response model for logout confirmation.
/// </summary>
public class LogoutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
