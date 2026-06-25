namespace ProjectManagement.Application.DTOs.Auth;

/// <summary>
/// Request model for refreshing access token.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
