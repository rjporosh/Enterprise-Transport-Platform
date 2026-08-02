using System.Security.Cryptography;
using AuthService.Application.Common.Interfaces;

namespace AuthService.Infrastructure.Security;

/// <summary>
/// PBKDF2 (HMAC-SHA256), 100,000 iterations, 16-byte random salt, 32-byte
/// derived key. No external crypto package required (System.Security.Cryptography
/// is in the BCL) — see docs/architecture/auth-service-architecture.md,
/// "Password hashing" for why PBKDF2 was chosen here over BCrypt/Argon2id
/// (both excellent choices too; this one avoids a native-library dependency
/// in a service that otherwise has none).
///
/// Stored format: "{iterations}.{base64 salt}.{base64 hash}"
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;

    public string Hash(string plainTextPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(plainTextPassword, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string plainTextPassword, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 3) return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var expectedKey = Convert.FromBase64String(parts[2]);

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(plainTextPassword, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);

            // Constant-time comparison — a timing difference here would leak
            // how many leading bytes of the hash matched.
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch
        {
            // Malformed stored hash (e.g. legacy format from a future
            // migration) is a verification failure, not a crash.
            return false;
        }
    }
}
