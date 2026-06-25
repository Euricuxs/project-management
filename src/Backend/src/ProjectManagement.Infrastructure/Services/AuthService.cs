using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ProjectManagement.Application.Abstractions.Services;
using ProjectManagement.Application.DTOs.Auth;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;
using ProjectManagement.Infrastructure.Entities;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Implementation of authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AuthServiceResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
            return AuthServiceResult.Failed("INVALID_CREDENTIALS", "Invalid email or password");
        }

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            return AuthServiceResult.Failed("INVALID_CREDENTIALS", "Invalid email or password");
        }

        // Check if email is confirmed (if required)
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: Email not confirmed for user {UserId}", user.Id);
            return AuthServiceResult.Failed("EMAIL_NOT_CONFIRMED", "Please verify your email address");
        }

        // Generate tokens
        var roles = new[] { "User" }; // TODO: Get roles from workspace membership
        var (accessToken, accessExpiry) = _tokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var (refreshToken, refreshExpiry) = _tokenService.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = refreshExpiry,
            CreatedByIp = GetClientIp(_httpContextAccessor)
        };
        _context.RefreshTokens.Add(refreshTokenEntity);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Login successful for user {UserId}", user.Id);

        return AuthServiceResult.Succeeded(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = accessExpiry,
            RefreshTokenExpiry = refreshExpiry
        });
    }

    public async Task<AuthServiceResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: Email already exists {Email}", request.Email);
            return AuthServiceResult.Failed("EMAIL_EXISTS", "An account with this email already exists");
        }

        // Create new user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true // Auto-confirm for now, implement email verification later
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registration successful for user {UserId}", user.Id);

        // Auto-login after registration
        return await LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = request.Password
        }, cancellationToken);
    }

    public async Task<AuthServiceResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh attempt");

        // Find the refresh token
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Token refresh failed: Token not found");
            return AuthServiceResult.Failed("INVALID_TOKEN", "Invalid refresh token");
        }

        if (!storedToken.IsActive)
        {
            _logger.LogWarning("Token refresh failed: Token is not active (expired or revoked)");
            return AuthServiceResult.Failed("INVALID_TOKEN", "Refresh token has expired or been revoked");
        }

        // Get user by ID
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Token refresh failed: User not found for token");
            return AuthServiceResult.Failed("INVALID_TOKEN", "User not found");
        }

        // SECURITY: Detect token reuse attack
        // If this token was already used (has a replaced token), someone may have stolen it
        if (!string.IsNullOrEmpty(storedToken.ReplacedByToken) && storedToken.ReplacedByToken != "refreshed")
        {
            _logger.LogWarning("Potential token reuse attack detected for user {UserId}. Revoking all user tokens.", user.Id);

            // Revoke ALL active tokens for this user as a security measure
            var allUserTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.IsActive)
                .ToListAsync(cancellationToken);

            var clientIp = GetClientIp(_httpContextAccessor);
            foreach (var token in allUserTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = clientIp;
                token.ReplacedByToken = "security_revoke";
            }

            await _context.SaveChangesAsync(cancellationToken);

            return AuthServiceResult.Failed("INVALID_TOKEN", "Invalid refresh token");
        }

        // Revoke the old token (rotation)
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = GetClientIp(_httpContextAccessor);
        storedToken.ReplacedByToken = "refreshed"; // Mark as rotated

        // Generate new tokens
        var roles = new[] { "User" };
        var (newAccessToken, accessExpiry) = _tokenService.GenerateAccessToken(user.Id, user.Email, roles);
        var (newRefreshToken, refreshExpiry) = _tokenService.GenerateRefreshToken();

        // Store new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = refreshExpiry,
            CreatedByIp = GetClientIp(_httpContextAccessor)
        };
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refresh successful for user {UserId}", user.Id);

        return AuthServiceResult.Succeeded(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiry = accessExpiry,
            RefreshTokenExpiry = refreshExpiry
        });
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token revocation attempt");

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Token revocation failed: Token not found");
            return false;
        }

        if (storedToken.IsRevoked)
        {
            _logger.LogWarning("Token revocation failed: Token already revoked");
            return false;
        }

        // Revoke the token
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = GetClientIp(_httpContextAccessor);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token revoked successfully");
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Revoking all tokens for user {UserId}", userId);

        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = GetClientIp(_httpContextAccessor);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked {Count} tokens for user {UserId}", tokens.Count, userId);
        return true;
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return null;

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    private static string GetClientIp(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
            return "unknown";

        // Check X-Forwarded-For header first (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
