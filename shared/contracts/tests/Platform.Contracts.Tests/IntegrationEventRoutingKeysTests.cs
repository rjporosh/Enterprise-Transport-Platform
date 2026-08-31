using FluentAssertions;
using Platform.Contracts.Messaging;
using Xunit;

namespace Platform.Contracts.Tests;

/// <summary>Unit coverage for the resolver's parsing + deterministic fallback.</summary>
public sealed class IntegrationEventRoutingKeysTests
{
    [Theory]
    [InlineData("BookingConfirmedDomainEvent", "BookingConfirmedDomainEvent")]
    [InlineData("BookingService.Domain.Events.BookingConfirmedDomainEvent", "BookingConfirmedDomainEvent")]
    [InlineData("BookingService.Domain.Events.BookingConfirmedDomainEvent, BookingService.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "BookingConfirmedDomainEvent")]
    [InlineData("  Ns.SomeEvent`1[[System.String]], Asm ", "SomeEvent")]
    public void ExtractShortTypeName_handles_bare_fullname_and_aqn(string input, string expected)
    {
        IntegrationEventRoutingKeys.ExtractShortTypeName(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("BookingConfirmed", "booking.confirmed")]
    [InlineData("BusDetailsUpdated", "bus.details.updated")]
    [InlineData("PaymentSucceeded", "payment.succeeded")]
    public void ToDottedLower_converts_pascalcase_to_dotted(string input, string expected)
    {
        IntegrationEventRoutingKeys.ToDottedLower(input).Should().Be(expected);
    }

    [Fact]
    public void Unknown_event_uses_deterministic_fallback_without_double_prefix()
    {
        var ok = IntegrationEventRoutingKeys.TryResolve(
            "MyService.Domain.Events.WidgetFrobnicatedDomainEvent, MyService.Domain, Version=2.0.0.0, Culture=neutral, PublicKeyToken=abc",
            "widget",
            out var key,
            out var fromRegistry);

        ok.Should().BeTrue();
        fromRegistry.Should().BeFalse();
        key.Should().Be("widget.frobnicated");   // "Widget" prefix collapsed, not "widget.widget.frobnicated"
        key.Should().NotContain(",");
        key.Should().NotContain("Version=");
    }

    [Fact]
    public void Unknown_event_not_already_prefixed_gets_the_service_prefix()
    {
        var key = IntegrationEventRoutingKeys.Resolve(
            "Some.OrderShippedDomainEvent, Some, Version=1.0.0.0",
            "fulfilment");

        key.Should().Be("fulfilment.order.shipped");
    }

    [Fact]
    public void Registered_event_ignores_the_fallback_prefix_argument()
    {
        // The registry value wins regardless of what prefix the caller passes.
        IntegrationEventRoutingKeys.Resolve("BookingConfirmedDomainEvent", "wrong-prefix")
            .Should().Be(EventTypes.BookingConfirmed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Null_or_blank_input_throws(string? input)
    {
        var act = () => IntegrationEventRoutingKeys.Resolve(input!, "svc");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Blank_service_prefix_throws()
    {
        var act = () => IntegrationEventRoutingKeys.Resolve("SomethingDomainEvent", "  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Fallback_strips_all_recognised_suffixes()
    {
        IntegrationEventRoutingKeys.Resolve("Ns.ThingHappenedIntegrationEvent, A", "svc").Should().Be("svc.thing.happened");
        IntegrationEventRoutingKeys.Resolve("Ns.ThingHappenedEvent, A", "svc").Should().Be("svc.thing.happened");
        IntegrationEventRoutingKeys.Resolve("Ns.ThingHappened, A", "svc").Should().Be("svc.thing.happened");
    }
}
