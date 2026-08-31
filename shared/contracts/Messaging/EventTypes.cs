namespace Platform.Contracts.Messaging;

/// <summary>
/// The complete, authoritative set of RabbitMQ routing keys used on the
/// platform's per-service topic exchanges (<c>&lt;service&gt;.events</c>).
///
/// These strings are a PUBLISHED CONTRACT: consumers (including future
/// Node.js / Java / Python services) bind to them, so a value here never
/// changes once shipped. Adding an event = adding a constant here + a
/// <see cref="EventTypeRegistry"/> entry.
///
/// Word separator is <c>.</c> (AMQP topic convention). The first segment is
/// always the owning service.
///
/// NOTE: the auth.* keys below are exactly what auth-service's outbox already
/// emits today (its name-munging happened to be correct because its event
/// classes are entity-prefixed, not service-prefixed). They are pinned here so
/// centralising the resolver changes NOTHING for auth-service (no regression).
/// The booking/bus/route/payment keys are the CORRECTED values — those services'
/// old munging double-prefixed ("booking.booking.confirmed") or, for payment,
/// produced garbage from an AssemblyQualifiedName (P0-4).
/// </summary>
public static class EventTypes
{
    // ---- auth.events (exchange: "auth.events") — pinned to current emitted values ----
    public const string AuthUserRegistered = "auth.user.registered";
    public const string AuthUserLoggedIn = "auth.user.logged.in";
    public const string AuthPasswordChanged = "auth.password.changed";
    public const string AuthPasswordResetRequested = "auth.password.reset.requested";
    public const string AuthPasswordResetCompleted = "auth.password.reset.completed";
    public const string AuthUserLockedOut = "auth.user.locked.out";
    public const string AuthOtpRequested = "auth.otp.requested";
    public const string AuthOtpVerified = "auth.otp.verified";
    public const string AuthOtpFailed = "auth.otp.failed";
    public const string AuthPermissionChanged = "auth.permission.changed";
    public const string AuthUserRoleChanged = "auth.user.role.changed";
    public const string AuthModuleAssigned = "auth.module.assigned";
    public const string AuthSecurityQuestionsConfigured = "auth.security.questions.configured";

    // ---- booking.events (exchange: "booking.events") ----
    public const string BookingCreated = "booking.created";
    public const string BookingConfirmed = "booking.confirmed";
    public const string BookingCancelled = "booking.cancelled";

    // ---- payment.events (exchange: "payment.events") ----
    public const string PaymentCreated = "payment.created";
    public const string PaymentProcessing = "payment.processing";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
    public const string PaymentCancelled = "payment.cancelled";
    public const string PaymentRefunded = "payment.refunded";

    // ---- bus.events (exchange: "bus.events") ----
    public const string BusRegistered = "bus.registered";
    public const string BusDetailsUpdated = "bus.details.updated";
    public const string BusStatusChanged = "bus.status.changed";
    public const string BusSoftDeleted = "bus.soft.deleted";
    public const string BusRestored = "bus.restored";

    // ---- route.events (exchange: "route.events") ----
    public const string RouteCreated = "route.created";
    public const string RouteUpdated = "route.updated";
    public const string RouteDeleted = "route.deleted";
    public const string RouteStatusChanged = "route.status.changed";
    public const string RouteStopCreated = "route.stop.created";
    public const string RouteStopUpdated = "route.stop.updated";
    public const string RouteScheduleCreated = "route.schedule.created";

    // ---- notification.events (exchange: "notification.events") — published for future downstream consumers ----
    public const string NotificationCreated = "notification.created";
    public const string NotificationSent = "notification.sent";
    public const string NotificationDelivered = "notification.delivered";
    public const string NotificationFailed = "notification.failed";
    public const string NotificationCancelled = "notification.cancelled";
    public const string NotificationDeadLettered = "notification.dead-lettered";

    // ---- ticket.events (exchange: "ticket.events") — reserved for the future Ticketing Service (M6) ----
    public const string TicketIssued = "ticket.issued";
    public const string TicketCancelled = "ticket.cancelled";
    public const string TicketReissued = "ticket.reissued";
}
