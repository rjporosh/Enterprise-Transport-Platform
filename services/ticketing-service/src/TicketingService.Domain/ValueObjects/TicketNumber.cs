using System.Security.Cryptography;

namespace TicketingService.Domain.ValueObjects;

/// <summary>
/// Human-readable, check-summed ticket number: <c>TKT-YYMMDD-XXXXXX-C</c>.
/// <c>XXXXXX</c> is 6 random base32 chars; <c>C</c> is a checksum char over the
/// rest, so a mistyped number is caught before a lookup. Reissues keep the
/// same number.
/// </summary>
public sealed record TicketNumber(string Value)
{
    // Crockford base32 minus I, L, O, U (unambiguous when read aloud).
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ*";

    public static TicketNumber New(DateTimeOffset now)
    {
        Span<byte> bytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(bytes);
        var body = new char[6];
        for (var i = 0; i < 6; i++)
            body[i] = Alphabet[bytes[i] % Alphabet.Length];

        var core = $"TKT-{now:yyMMdd}-{new string(body)}";
        return new TicketNumber($"{core}-{Checksum(core)}");
    }

    public static bool IsValid(string value)
    {
        var dash = value.LastIndexOf('-');
        return dash > 0 && dash == value.Length - 2 && Checksum(value[..dash]) == value[^1];
    }

    private static char Checksum(string core)
    {
        var sum = 0;
        foreach (var c in core) sum = (sum * 31 + c) % Alphabet.Length;
        return Alphabet[sum];
    }

    public override string ToString() => Value;
}
