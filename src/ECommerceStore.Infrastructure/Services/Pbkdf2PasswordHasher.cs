using System.Security.Cryptography;

namespace ECommerceStore.Infrastructure.Services;

/// <summary>
/// Salted PBKDF2-SHA256 password hashing for the single configured admin account (there's no
/// Users table in this project — see appsettings' Admin:Username/Admin:PasswordHash).
/// Stored format: "{iterations}.{saltBase64}.{hashBase64}".
/// </summary>
public static class Pbkdf2PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    // 600,000 follows current OWASP guidance for PBKDF2-HMAC-SHA256. Older hashes (100,000) still verify,
    // because the iteration count is stored inside each hash.
    private const int Iterations = 600_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string hashedValue)
    {
        var parts = hashedValue.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
