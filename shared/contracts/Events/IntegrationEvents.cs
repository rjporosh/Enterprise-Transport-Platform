namespace Platform.Contracts.Events;

/// <summary>
/// Marker for a versionable integration-event contract. The concrete records
/// below document the JSON shape that travels on RabbitMQ for each
/// <see cref="Platform.Contracts.Messaging.EventTypes"/> key.
///
/// <para>Today each service serialises its own domain-event record to the
/// outbox; these contracts pin the field names/types that cross the service
/// boundary and give polyglot consumers (Node/Java/Python) a canonical schema
/// to generate from. A contract test asserts the live domain-event JSON
/// deserialises into the matching contract with all required fields populated.</para>
///
/// <para>Versioning: additive changes only. A breaking change ships a new
/// record (e.g. <c>BookingConfirmedV2</c>) and a new routing key; consumers
/// migrate gradually (.ai/MASTER-RULES.md §82/§83).</para>
/// </summary>
public interface IIntegrationEventContract
{
    /// <summary>The <see cref="Platform.Contracts.Messaging.EventTypes"/> routing key this contract is published under.</summary>
    static abstract string RoutingKey { get; }

    /// <summary>Contract schema version.</summary>
    static abstract int Version { get; }
}

/// <summary>Fields present on every domain event (see each service's <c>DomainEvent</c> base).</summary>
public abstract record IntegrationEventBase
{
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

// ---------------------------------------------------------------------------
// booking.events
// ---------------------------------------------------------------------------

/// <summary><see cref="Platform.Contracts.Messaging.EventTypes.BookingCreated"/></summary>
public sealed record BookingCreatedV1 : IntegrationEventBase, IIntegrationEventContract
{
    public static string RoutingKey => Messaging.EventTypes.BookingCreated;
    public static int Version => 1;

    public required Guid BookingId { get; init; }
    public required Guid TripId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
    public required IReadOnlyCollection<string> SeatNumbers { get; init; }
}

/// <summary><see cref="Platform.Contracts.Messaging.EventTypes.BookingConfirmed"/></summary>
public sealed record BookingConfirmedV1 : IntegrationEventBase, IIntegrationEventContract
{
    public static string RoutingKey => Messaging.EventTypes.BookingConfirmed;
    public static int Version => 1;

    public required Guid BookingId { get; init; }
    public required Guid TripId { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid PaymentId { get; init; }

    // Journey + customer snapshot so ticketing can issue and notification can
    // deliver a ticket without calling back into booking / route / bus / auth.
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required string OriginCity { get; init; }
    public required string DestinationCity { get; init; }
    public required DateTimeOffset DepartureUtc { get; init; }
    public required DateTimeOffset ArrivalUtc { get; init; }
    public required string BusPlateNumber { get; init; }
    public required string BusType { get; init; }
    public required IReadOnlyCollection<string> SeatNumbers { get; init; }
    public required IReadOnlyCollection<string> PassengerNames { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
}

/// <summary><see cref="Platform.Contracts.Messaging.EventTypes.BookingCancelled"/></summary>
public sealed record BookingCancelledV1 : IntegrationEventBase, IIntegrationEventContract
{
    public static string RoutingKey => Messaging.EventTypes.BookingCancelled;
    public static int Version => 1;

    public required Guid BookingId { get; init; }
    public required Guid TripId { get; init; }
    public required string Reason { get; init; }
}

// ---------------------------------------------------------------------------
// payment.events
// ---------------------------------------------------------------------------

/// <summary><see cref="Platform.Contracts.Messaging.EventTypes.PaymentSucceeded"/></summary>
public sealed record PaymentSucceededV1 : IntegrationEventBase, IIntegrationEventContract
{
    public static string RoutingKey => Messaging.EventTypes.PaymentSucceeded;
    public static int Version => 1;

    public required Guid PaymentId { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid CustomerId { get; init; }

    /// <summary>The originating booking id (Payment.OrderReference).</summary>
    public required string OrderReference { get; init; }
    public required string ProviderReference { get; init; }
    public string? ProviderTransactionId { get; init; }
}

/// <summary><see cref="Platform.Contracts.Messaging.EventTypes.PaymentFailed"/></summary>
public sealed record PaymentFailedV1 : IntegrationEventBase, IIntegrationEventContract
{
    public static string RoutingKey => Messaging.EventTypes.PaymentFailed;
    public static int Version => 1;

    public required Guid PaymentId { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid CustomerId { get; init; }

    /// <summary>The originating booking id (Payment.OrderReference).</summary>
    public required string OrderReference { get; init; }
    public required string Reason { get; init; }
    public string? ProviderErrorCode { get; init; }
}

// ---------------------------------------------------------------------------
// ticket.events — reserved for the Ticketing Service (M6). Defined now so the
// booking/payment/notification event flow can be designed against a stable
// contract; NOT yet published by any service.
// ---------------------------------------------------------------------------

/// <summary><see cref="Platform.Contracts.Messaging.EventTypes.TicketIssued"/></summary>
public sealed record TicketIssuedV1 : IntegrationEventBase, IIntegrationEventContract
{
    public static string RoutingKey => Messaging.EventTypes.TicketIssued;
    public static int Version => 1;

    public required Guid TicketId { get; init; }
    public required string TicketNumber { get; init; }
    public required Guid BookingId { get; init; }
    public required Guid TripId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string VerificationCode { get; init; }
}
