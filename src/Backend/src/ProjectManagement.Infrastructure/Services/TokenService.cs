using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectManagement.Application.Abstractions.Services;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Implementation of JWT token service.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IConfiguration configuration)
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured"),
            Issuer = configuration["Jwt:Issuer"] ?? "ProjectManagement",
            Audience = configuration["Jwt:Audience"] ?? "ProjectManagementApp",
            AccessTokenExpiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60"),
            RefreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "7")
        };
    }

    public (string Token, DateTime Expiry) GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("userId", userId.ToString())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    public (string Token, DateTime Expiry) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var token = Convert.ToBase64String(randomBytes);
        var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        return (token, expiry);
    }

    public bool ValidateToken(string token, out Guid? userId, out string? email, out IEnumerable<string> roles)
    {
        userId = null;
        email = null;
        roles = [];

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var userIdClaim = principal.FindFirst("userId") ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email);
            var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            email = emailClaim?.Value;
            roles = roleClaims;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        if (ValidateToken(token, out var userId, out _, out _))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// JWT configuration settings.
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
