namespace NotificationService.Application.Features.Preferences.GetRecipientPreference;

public sealed record RecipientPreferenceDto(
    Guid Id, string RecipientId, bool EmailOptOut, bool SmsOptOut, bool PushOptOut, string Locale,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
