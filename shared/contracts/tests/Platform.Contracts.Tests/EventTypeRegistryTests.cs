using System.Reflection;
using FluentAssertions;
using Platform.Contracts.Messaging;
using Xunit;

namespace Platform.Contracts.Tests;

/// <summary>
/// Proves the M0 routing-key contract: every published domain event resolves to
/// a stable, explicit key from <see cref="EventTypeRegistry"/> — never the
/// AssemblyQualifiedName-munged garbage or double-prefixed key the audit found
/// (P0-4).
/// </summary>
public sealed class EventTypeRegistryTests
{
    /// <summary>Domain-event assembly → owning-service prefix.</summary>
    private static readonly (Assembly Assembly, string Prefix)[] DomainAssemblies =
    [
        (typeof(AuthService.Domain.Common.DomainEvent).Assembly, "auth"),
        (typeof(BookingService.Domain.Common.DomainEvent).Assembly, "booking"),
        (typeof(BusService.Domain.Common.DomainEvent).Assembly, "bus"),
        (typeof(RouteService.Domain.Common.DomainEvent).Assembly, "route"),
        (typeof(PaymentService.Domain.Common.DomainEvent).Assembly, "payment"),
        (typeof(NotificationService.Domain.Common.DomainEvent).Assembly, "notification"),
    ];

    public static IEnumerable<object[]> AllDomainEvents()
    {
        foreach (var (assembly, prefix) in DomainAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract) continue;
                if (!type.Name.EndsWith("DomainEvent", StringComparison.Ordinal)) continue;
                if (type.Name == "DomainEvent") continue;
                yield return [type, prefix];
            }
        }
    }

    public static IEnumerable<object[]> AllRegistryEntries() =>
        EventTypeRegistry.All.Select(kvp => new object[] { kvp.Key, kvp.Value });

    // ----------------------------------------------------------------------
    // Every real domain event resolves from the explicit registry.
    // ----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(AllDomainEvents))]
    public void Every_domain_event_resolves_from_the_explicit_registry(Type eventType, string servicePrefix)
    {
        var aqn = eventType.AssemblyQualifiedName!;

        var ok = IntegrationEventRoutingKeys.TryResolve(aqn, servicePrefix, out var key, out var fromRegistry);

        ok.Should().BeTrue();
        fromRegistry.Should().BeTrue(
            $"'{eventType.Name}' must have an explicit Platform.Contracts.EventTypeRegistry entry, not fall back to the derived key '{key}'");
        key.Should().StartWith(servicePrefix + ".");
    }

    [Theory]
    [MemberData(nameof(AllDomainEvents))]
    public void Resolved_key_is_never_derived_from_assembly_metadata(Type eventType, string servicePrefix)
    {
        var key = IntegrationEventRoutingKeys.Resolve(eventType.AssemblyQualifiedName!, servicePrefix);

        key.Should().NotContain(",");
        key.Should().NotContain("Version=");
        key.Should().NotContain("Culture=");
        key.Should().NotContain("PublicKeyToken");
        key.Should().NotContain(" ");
        key.Should().NotContain("domainevent");
    }

    [Theory]
    [MemberData(nameof(AllDomainEvents))]
    public void Resolved_key_never_double_prefixes_the_service(Type eventType, string servicePrefix)
    {
        var key = IntegrationEventRoutingKeys.Resolve(eventType.AssemblyQualifiedName!, servicePrefix);

        key.Should().NotStartWith($"{servicePrefix}.{servicePrefix}.");
        key.Should().NotContain("..");
    }

    [Theory]
    [MemberData(nameof(AllDomainEvents))]
    public void Resolver_is_idempotent(Type eventType, string servicePrefix)
    {
        var aqn = eventType.AssemblyQualifiedName!;
        var fullName = eventType.FullName!;
        var bareName = eventType.Name;

        var keys = new[]
        {
            IntegrationEventRoutingKeys.Resolve(aqn, servicePrefix),
            IntegrationEventRoutingKeys.Resolve(aqn, servicePrefix),
            IntegrationEventRoutingKeys.Resolve(fullName, servicePrefix),
            IntegrationEventRoutingKeys.Resolve(bareName, servicePrefix),
        };

        // The stored EventType may be an AQN, a FullName, or a bare name across
        // services — all three forms of the SAME event must resolve identically,
        // every call.
        keys.Should().AllBeEquivalentTo(keys[0]);
    }

    // ----------------------------------------------------------------------
    // Every registry entry is internally consistent.
    // ----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(AllRegistryEntries))]
    public void Every_registry_key_is_lowercase_dotted_and_service_scoped(string shortTypeName, string routingKey)
    {
        routingKey.Should().MatchRegex("^[a-z][a-z0-9-]*(\\.[a-z0-9-]+)+$",
            $"routing key '{routingKey}' for '{shortTypeName}' must be lowercase dot-separated segments");
        routingKey.Should().NotContain("..");
        routingKey.Split('.').Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Registry_has_no_duplicate_routing_keys_within_a_service()
    {
        // Two different event types must never map to the same key.
        var duplicates = EventTypeRegistry.All
            .GroupBy(kvp => kvp.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        duplicates.Should().BeEmpty($"these routing keys are mapped by more than one event type: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void All_EventTypes_constants_are_covered_by_the_registry_or_are_reserved()
    {
        // ticket.* is reserved for the future Ticketing Service (M6) and is not
        // yet published by any domain event, so it is intentionally not in the
        // registry. Everything else must be.
        var registryValues = EventTypeRegistry.All.Values.ToHashSet();

        var constants = typeof(EventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        var uncovered = constants
            .Where(c => !registryValues.Contains(c))
            .Where(c => !c.StartsWith("ticket.", StringComparison.Ordinal))
            .ToArray();

        uncovered.Should().BeEmpty($"these EventTypes constants have no EventTypeRegistry entry: {string.Join(", ", uncovered)}");
    }

    // ----------------------------------------------------------------------
    // Specific high-value keys the notification consumer binds to (P0-4 fix).
    // ----------------------------------------------------------------------

    [Theory]
    [InlineData("BookingConfirmedDomainEvent", "booking", "booking.confirmed")]
    [InlineData("BookingCreatedDomainEvent", "booking", "booking.created")]
    [InlineData("BookingCancelledDomainEvent", "booking", "booking.cancelled")]
    [InlineData("PaymentSucceededDomainEvent", "payment", "payment.succeeded")]
    [InlineData("PaymentFailedDomainEvent", "payment", "payment.failed")]
    [InlineData("UserRegisteredDomainEvent", "auth", "auth.user.registered")]
    [InlineData("BusRegisteredDomainEvent", "bus", "bus.registered")]
    [InlineData("RouteCreatedDomainEvent", "route", "route.created")]
    [InlineData("NotificationSentDomainEvent", "notification", "notification.sent")]
    public void Known_events_resolve_to_the_exact_published_key(string shortName, string prefix, string expected)
    {
        var fullyQualified = $"Some.Namespace.{shortName}, Some.Assembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        IntegrationEventRoutingKeys.Resolve(shortName, prefix).Should().Be(expected);
        IntegrationEventRoutingKeys.Resolve(fullyQualified, prefix).Should().Be(expected);
    }
}
