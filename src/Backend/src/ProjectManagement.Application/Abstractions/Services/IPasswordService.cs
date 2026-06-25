namespace ProjectManagement.Application.Abstractions.Services;

/// <summary>
/// Service for password hashing and verification.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hash a plain text password.
    /// </summary>
    string HashPassword(string plainPassword);

    /// <summary>
    /// Verify a plain text password against a hash.
    /// </summary>
    bool VerifyPassword(string plainPassword, string hashedPassword);

    /// <summary>
    /// Generate a secure random token.
    /// </summary>
    string GenerateToken(int length = 64);
}
