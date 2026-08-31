namespace Platform.SharedKernel.Results;

/// <summary>
/// A single, machine-readable error. The platform's Result pattern returns a
/// COLLECTION of these so a caller sees every problem in one round-trip
/// (.ai/MASTER-RULES.md §14/§15).
///
/// <para><see cref="Code"/> is a stable, language-neutral identifier
/// (e.g. <c>"payment.amount.invalid"</c>) — it never changes across locales and
/// is what a client keys UI/behaviour off. <see cref="Message"/> is a
/// human-readable, localisable default.</para>
/// </summary>
public sealed record Error(string Code, string Message, string? Field = null, ErrorType Type = ErrorType.Failure)
{
    public static Error Validation(string code, string message, string? field = null) =>
        new(code, message, field, ErrorType.Validation);

    public static Error NotFound(string code, string message) =>
        new(code, message, null, ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, null, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, null, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, null, ErrorType.Forbidden);

    public static Error Unexpected(string code, string message) =>
        new(code, message, null, ErrorType.Unexpected);
}

/// <summary>Maps to an HTTP status class without the domain layer knowing about HTTP.</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Unexpected = 6
}
