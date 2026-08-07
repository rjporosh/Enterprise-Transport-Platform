using MediatR;
using NotificationService.Api.Common;
using NotificationService.Application.Features.Preferences.GetRecipientPreference;
using NotificationService.Application.Features.Preferences.UpdateRecipientPreference;

namespace NotificationService.Api.Endpoints;

public static class PreferencesEndpoints
{
    public static void MapPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/recipients/{recipientId}/preferences").WithTags("Preferences");

        group.MapGet("/", GetAsync)
            .WithName("GetRecipientPreference")
            .WithSummary("Get a recipient's channel opt-in/out and locale preferences (defaults if never set).")
            .Produces<ApiResponse<RecipientPreferenceDto>>(StatusCodes.Status200OK);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateRecipientPreference")
            .WithSummary("Create or update a recipient's channel opt-in/out and locale preferences.")
            .Produces<ApiResponse<RecipientPreferenceDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAsync(string recipientId, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRecipientPreferenceQuery(recipientId), cancellationToken);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> UpdateAsync(string recipientId, UpdatePreferenceRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var command = new UpdateRecipientPreferenceCommand(recipientId, request.EmailOptOut, request.SmsOptOut, request.PushOptOut, request.Locale);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToApiResult(httpContext, "Preferences updated.");
    }

    private sealed record UpdatePreferenceRequest(bool EmailOptOut, bool SmsOptOut, bool PushOptOut, string Locale);
}
