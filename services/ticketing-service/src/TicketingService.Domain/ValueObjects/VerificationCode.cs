using System.Security.Cryptography;

namespace TicketingService.Domain.ValueObjects;

/// <summary>
/// Opaque, URL-safe code embedded in the ticket QR. Resolving
/// <c>/api/v1/tickets/verify/{code}</c> returns the ticket's public status —
/// it is a bearer capability, so it is long and random, not the ticket number.
/// </summary>
public sealed record VerificationCode(string Value)
{
    public static VerificationCode New() =>
        new(Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
            .Replace('+', '-').Replace('/', '_').TrimEnd('='));

    public override string ToString() => Value;
}
