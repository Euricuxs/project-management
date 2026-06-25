namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate an access token for a user.
    /// </summary>
    (string Token, DateTime Expiry) GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);

    /// <summary>
    /// Generate a refresh token.
    /// </summary>
    (string Token, DateTime Expiry) GenerateRefreshToken();

    /// <summary>
    /// Validate an access token and extract claims.
    /// </summary>
    bool ValidateToken(string token, out Guid? userId, out string? email, out IEnumerable<string> roles);

    /// <summary>
    /// Get user ID from token.
    /// </summary>
    Guid? GetUserIdFromToken(string token);
}
