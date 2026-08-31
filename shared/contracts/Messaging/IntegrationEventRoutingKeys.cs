using System.Text;

namespace Platform.Contracts.Messaging;

/// <summary>
/// Resolves the stable RabbitMQ routing key for an outbox row. Replaces the
/// five separate <c>ToRoutingKey</c>/<c>DeriveRoutingKey</c> helpers the audit
/// found broken (P0-4):
///   * booking/bus/route double-prefixed ("booking.booking.confirmed");
///   * payment split an AssemblyQualifiedName on '.' → "…, culture=neutral…".
///
/// Resolution order:
///   1. Extract the short CLR type name (works for AssemblyQualifiedName,
///      Namespace.FullName, or a bare name).
///   2. Explicit lookup in <see cref="EventTypeRegistry"/> — the published contract.
///   3. Deterministic fallback: "&lt;servicePrefix&gt;.&lt;dotted-name&gt;" with NO
///      double prefix, so a not-yet-registered event still gets a sane,
///      language-neutral key (and <see cref="TryResolve"/> tells the caller it
///      was a fallback so it can log a warning).
/// </summary>
public static class IntegrationEventRoutingKeys
{
    private static readonly string[] Suffixes = ["DomainEvent", "IntegrationEvent", "Event"];

    /// <summary>Resolves the routing key, using the registry then the deterministic fallback.</summary>
    public static string Resolve(string storedEventType, string servicePrefix)
    {
        TryResolve(storedEventType, servicePrefix, out var key, out _);
        return key;
    }

    /// <summary>
    /// Resolves the routing key. <paramref name="fromRegistry"/> is <c>true</c> when
    /// the key came from the explicit published contract, <c>false</c> when the
    /// deterministic fallback was used (caller should log that so the event gets
    /// registered).
    /// </summary>
    public static bool TryResolve(string storedEventType, string servicePrefix, out string routingKey, out bool fromRegistry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedEventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(servicePrefix);

        var shortName = ExtractShortTypeName(storedEventType);

        if (EventTypeRegistry.TryGet(shortName, out var registered))
        {
            routingKey = registered;
            fromRegistry = true;
            return true;
        }

        routingKey = BuildFallbackKey(shortName, servicePrefix.Trim().ToLowerInvariant());
        fromRegistry = false;
        return true;
    }

    /// <summary>
    /// "BookingService.Domain.Events.BookingConfirmedDomainEvent, BookingService.Domain, Version=1.0.0.0, …"
    /// → "BookingConfirmedDomainEvent".
    /// </summary>
    internal static string ExtractShortTypeName(string storedEventType)
    {
        var value = storedEventType.Trim();

        // Drop the assembly-name and version tail of an AssemblyQualifiedName.
        var comma = value.IndexOf(',');
        if (comma >= 0) value = value[..comma];

        // Drop generic arity / args if ever present.
        var backtick = value.IndexOf('`');
        if (backtick >= 0) value = value[..backtick];

        // Take the last namespace segment.
        var lastDot = value.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < value.Length - 1) value = value[(lastDot + 1)..];

        return value.Trim();
    }

    private static string BuildFallbackKey(string shortTypeName, string servicePrefix)
    {
        var name = shortTypeName;
        foreach (var suffix in Suffixes)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length];
                break;
            }
        }

        var dotted = ToDottedLower(name);

        // Never double-prefix: "BookingConfirmed" → "booking.confirmed" already
        // starts with the service prefix, so return it as-is.
        return dotted.StartsWith(servicePrefix + ".", StringComparison.Ordinal) || dotted == servicePrefix
            ? dotted
            : $"{servicePrefix}.{dotted}";
    }

    /// <summary>"BookingConfirmed" → "booking.confirmed"; "BusDetailsUpdated" → "bus.details.updated".</summary>
    internal static string ToDottedLower(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase)) return pascalCase;

        var sb = new StringBuilder(pascalCase.Length + 8);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];
            if (char.IsUpper(c) && i > 0) sb.Append('.');
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
