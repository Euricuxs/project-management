using System.Security.Cryptography;
using ProjectManagement.Application.Abstractions.Services;

namespace ProjectManagement.Infrastructure.Services;

/// <summary>
/// Implementation of password hashing using BCrypt.
/// </summary>
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12; // OWASP recommended minimum

    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainPassword));

        return BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
    }

    public bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return false;
        if (string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateToken(int length = 64)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
