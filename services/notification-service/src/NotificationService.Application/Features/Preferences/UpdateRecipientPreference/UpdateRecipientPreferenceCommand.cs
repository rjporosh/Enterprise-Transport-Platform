using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Preferences.GetRecipientPreference;

namespace NotificationService.Application.Features.Preferences.UpdateRecipientPreference;

public sealed record UpdateRecipientPreferenceCommand(
    string RecipientId, bool EmailOptOut, bool SmsOptOut, bool PushOptOut, string Locale)
    : IRequest<Result<RecipientPreferenceDto>>;
