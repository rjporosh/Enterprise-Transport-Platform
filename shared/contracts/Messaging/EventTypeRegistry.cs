namespace Platform.Contracts.Messaging;

/// <summary>
/// Maps a domain-event CLR type name (as persisted in an outbox row's
/// <c>EventType</c> column — which may be an AssemblyQualifiedName, a FullName,
/// or a bare type name) to its stable published routing key.
///
/// This is the explicit, reviewable replacement for the per-service
/// string-munging that produced <c>booking.booking.confirmed</c> (double
/// prefix) and, in payment-service, garbage from splitting an
/// AssemblyQualifiedName on '.' (P0-4).
/// </summary>
public static class EventTypeRegistry
{
    /// <summary>Known domain-event short type names → stable routing keys.</summary>
    private static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // auth-service
        ["UserRegisteredDomainEvent"] = EventTypes.AuthUserRegistered,
        ["UserLoggedInDomainEvent"] = EventTypes.AuthUserLoggedIn,
        ["PasswordChangedDomainEvent"] = EventTypes.AuthPasswordChanged,
        ["PasswordResetRequestedDomainEvent"] = EventTypes.AuthPasswordResetRequested,
        ["PasswordResetCompletedDomainEvent"] = EventTypes.AuthPasswordResetCompleted,
        ["UserLockedOutDomainEvent"] = EventTypes.AuthUserLockedOut,
        ["OtpRequestedDomainEvent"] = EventTypes.AuthOtpRequested,
        ["OtpVerifiedDomainEvent"] = EventTypes.AuthOtpVerified,
        ["OtpFailedDomainEvent"] = EventTypes.AuthOtpFailed,
        ["PermissionChangedDomainEvent"] = EventTypes.AuthPermissionChanged,
        ["UserRoleChangedDomainEvent"] = EventTypes.AuthUserRoleChanged,
        ["ModuleAssignedDomainEvent"] = EventTypes.AuthModuleAssigned,
        ["SecurityQuestionsConfiguredDomainEvent"] = EventTypes.AuthSecurityQuestionsConfigured,

        // booking-service
        ["BookingCreatedDomainEvent"] = EventTypes.BookingCreated,
        ["BookingConfirmedDomainEvent"] = EventTypes.BookingConfirmed,
        ["BookingCancelledDomainEvent"] = EventTypes.BookingCancelled,

        // payment-service
        ["PaymentCreatedDomainEvent"] = EventTypes.PaymentCreated,
        ["PaymentProcessingDomainEvent"] = EventTypes.PaymentProcessing,
        ["PaymentSucceededDomainEvent"] = EventTypes.PaymentSucceeded,
        ["PaymentFailedDomainEvent"] = EventTypes.PaymentFailed,
        ["PaymentCancelledDomainEvent"] = EventTypes.PaymentCancelled,
        ["PaymentRefundedDomainEvent"] = EventTypes.PaymentRefunded,

        // bus-service
        ["BusRegisteredDomainEvent"] = EventTypes.BusRegistered,
        ["BusDetailsUpdatedDomainEvent"] = EventTypes.BusDetailsUpdated,
        ["BusStatusChangedDomainEvent"] = EventTypes.BusStatusChanged,
        ["BusSoftDeletedDomainEvent"] = EventTypes.BusSoftDeleted,
        ["BusRestoredDomainEvent"] = EventTypes.BusRestored,

        // route-service
        ["RouteCreatedDomainEvent"] = EventTypes.RouteCreated,
        ["RouteUpdatedDomainEvent"] = EventTypes.RouteUpdated,
        ["RouteDeletedDomainEvent"] = EventTypes.RouteDeleted,
        ["RouteStatusChangedDomainEvent"] = EventTypes.RouteStatusChanged,
        ["StopCreatedDomainEvent"] = EventTypes.RouteStopCreated,
        ["StopUpdatedDomainEvent"] = EventTypes.RouteStopUpdated,
        ["ScheduleCreatedDomainEvent"] = EventTypes.RouteScheduleCreated,

        // notification-service (its own outbound events, on notification.events)
        ["NotificationCreatedDomainEvent"] = EventTypes.NotificationCreated,
        ["NotificationSentDomainEvent"] = EventTypes.NotificationSent,
        ["NotificationDeliveredDomainEvent"] = EventTypes.NotificationDelivered,
        ["NotificationFailedDomainEvent"] = EventTypes.NotificationFailed,
        ["NotificationCancelledDomainEvent"] = EventTypes.NotificationCancelled,
        ["NotificationDeadLetteredDomainEvent"] = EventTypes.NotificationDeadLettered,
    };

    /// <summary>True when <paramref name="shortTypeName"/> has an explicit registered key.</summary>
    public static bool TryGet(string shortTypeName, out string routingKey) =>
        Map.TryGetValue(shortTypeName, out routingKey!);

    /// <summary>All registered (short type name, routing key) pairs — used by contract tests.</summary>
    public static IReadOnlyDictionary<string, string> All => Map;
}
