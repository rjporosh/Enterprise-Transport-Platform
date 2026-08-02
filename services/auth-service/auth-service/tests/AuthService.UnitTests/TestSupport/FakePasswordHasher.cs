using AuthService.Application.Common.Interfaces;

namespace AuthService.UnitTests.TestSupport;

/// <summary>Trivial reversible "hash" (just prefixes the plaintext) so handler tests can assert Verify behavior without pulling in Infrastructure/BCL crypto. Never used outside tests.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "hashed:";

    public string Hash(string plainTextPassword) => Prefix + plainTextPassword;

    public bool Verify(string plainTextPassword, string storedHash) => storedHash == Prefix + plainTextPassword;
}
