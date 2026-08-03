namespace AuthService.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the password hashing algorithm so Application/tests
/// never reference a concrete crypto implementation. Infrastructure's
/// implementation uses PBKDF2 (Rfc2898DeriveBytes, SHA-256, 100k iterations) —
/// see docs/architecture/auth-service-architecture.md, "Password hashing".
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainTextPassword);

    /// <summary>True if <paramref name="plainTextPassword"/> matches the stored hash.</summary>
    bool Verify(string plainTextPassword, string storedHash);
}
