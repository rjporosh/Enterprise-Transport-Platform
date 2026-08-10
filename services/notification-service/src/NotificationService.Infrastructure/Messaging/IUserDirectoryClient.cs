namespace NotificationService.Infrastructure.Messaging;

public sealed record UserContactInfo(string? Email, string? PhoneNumber, string? Locale);

/// <summary>
/// Resolves an opaque external user id (e.g. Booking Service's CustomerId)
/// to contact info, for upstream events that reference a user by id only —
/// see NotificationEventConsumer. Booking Service's domain events
/// (BookingCreatedDomainEvent etc.) currently carry only a Guid CustomerId,
/// not an email/phone (see services/booking-service/src/BookingService.Domain/Events).
/// Auth Service does not yet expose an admin/service-to-service
/// "get user contact by id" endpoint (only GET /api/v1/auth/me for the
/// signed-in user) — this client is wired against the endpoint shape it
/// would need, and fails gracefully (returns null, is logged, does not
/// throw) until that endpoint exists. Adding it is a cross-service contract
/// change and is intentionally NOT made silently here — see the Known
/// Limitations section of this delivery's final report and
/// docs/architecture/notification-service-architecture.md, "Resolving
/// booking/payment events to a recipient".
/// </summary>
public interface IUserDirectoryClient
{
    Task<UserContactInfo?> ResolveContactAsync(string userId, CancellationToken cancellationToken = default);
}
