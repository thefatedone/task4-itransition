using System.Security.Cryptography;

namespace UserManagementApp.Services;

// note: small static helper for password hashing and confirmation token generation
public static class AuthHelper
{
    public static string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public static bool VerifyPassword(string plainPassword, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
    }

    // important: cryptographically random token used in the e-mail confirmation link
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}