using ProjectManagement.Application.DTOs.Auth;

namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate a user with email and password.
    /// </summary>
    Task<AuthServiceResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a new user.
    /// </summary>
    Task<AuthServiceResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using a refresh token.
    /// </summary>
    Task<AuthServiceResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token (logout).
    /// </summary>
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all refresh tokens for a user.
    /// </summary>
    Task<bool> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID.
    /// </summary>
    Task<UserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an authentication operation.
/// </summary>
public class AuthServiceResult
{
    public bool Success { get; set; }
    public AuthResponse? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthServiceResult Succeeded(AuthResponse data) => new()
    {
        Success = true,
        Data = data
    };

    public static AuthServiceResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
