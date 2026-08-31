using System.Diagnostics.CodeAnalysis;

namespace Platform.SharedKernel.Correlation;

/// <summary>
/// Validation and generation for the business correlation id. Correlation ids
/// come from untrusted callers, so they are validated (never blindly trusted)
/// before being echoed back or propagated (see .ai/MASTER-RULES.md §39, §41).
/// </summary>
public static class CorrelationId
{
    /// <summary>Upper bound on an accepted client-supplied correlation id.</summary>
    public const int MaxLength = 128;

    /// <summary>Lower bound — anything shorter is treated as noise and replaced.</summary>
    public const int MinLength = 8;

    /// <summary>Generates a fresh correlation id (lowercase "n"-format GUID, 32 chars).</summary>
    public static string New() => Guid.NewGuid().ToString("n");

    /// <summary>
    /// True when <paramref name="value"/> is a safe correlation id to accept from a
    /// caller: printable ASCII, no control chars, no whitespace, length in range,
    /// limited to an unambiguous character set that is safe to place in a log line,
    /// an HTTP header, and a message property.
    /// </summary>
    public static bool IsValid([NotNullWhen(true)] string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Length is < MinLength or > MaxLength) return false;

        foreach (var c in value)
        {
            var ok = c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')
                     or '-' or '_' or '.' or ':';
            if (!ok) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns <paramref name="candidate"/> if it is a valid client-supplied id,
    /// otherwise a freshly generated one. Never throws.
    /// </summary>
    public static string NormalizeOrCreate(string? candidate) =>
        IsValid(candidate) ? candidate : New();
}
