using NotificationService.Domain.Common;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Per-recipient notification preferences: which channels they've opted out
/// of (e.g. marketing SMS) and their preferred locale. Keyed by RecipientId —
/// an opaque external identifier (the Auth Service UserId) rather than a
/// foreign key, since Notification Service does not own or replicate user
/// data (see .ai/AI_RULES.md: never access another service's database).
/// SendNotification consults this before dispatch; transactional
/// notifications (booking confirmations, OTPs, payment receipts) bypass the
/// opt-out check by design — only marketing/informational sends honor it.
/// </summary>
public sealed class RecipientPreference : AggregateRoot
{
    public string RecipientId { get; private set; } = default!;
    public bool EmailOptOut { get; private set; }
    public bool SmsOptOut { get; private set; }
    public bool PushOptOut { get; private set; }
    public string Locale { get; private set; } = "en";
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private RecipientPreference() { } // EF Core

    private RecipientPreference(Guid id, string recipientId, string locale, DateTimeOffset nowUtc) : base(id)
    {
        RecipientId = recipientId;
        Locale = locale;
        CreatedAtUtc = nowUtc;
    }

    public static RecipientPreference CreateDefault(string recipientId, string locale, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(recipientId))
            throw new ArgumentException("RecipientId is required.", nameof(recipientId));

        return new RecipientPreference(Guid.NewGuid(), recipientId,
            string.IsNullOrWhiteSpace(locale) ? "en" : locale, nowUtc);
    }

    public void UpdatePreferences(bool emailOptOut, bool smsOptOut, bool pushOptOut, string locale, DateTimeOffset nowUtc)
    {
        EmailOptOut = emailOptOut;
        SmsOptOut = smsOptOut;
        PushOptOut = pushOptOut;
        Locale = string.IsNullOrWhiteSpace(locale) ? Locale : locale;
        UpdatedAtUtc = nowUtc;
    }
}
