using MediatR;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Preferences.GetRecipientPreference;

/// <summary>Returns the default (all-opted-in, "en") preference set for a recipient who has never customized theirs, rather than a 404 — every recipient implicitly has preferences even before they've ever set them.</summary>
public sealed record GetRecipientPreferenceQuery(string RecipientId) : IRequest<Result<RecipientPreferenceDto>>;
