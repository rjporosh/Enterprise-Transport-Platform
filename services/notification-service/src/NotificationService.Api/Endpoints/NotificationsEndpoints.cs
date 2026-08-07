using MediatR;
using NotificationService.Api.Common;
using NotificationService.Application.Features.Notifications.CancelNotification;
using NotificationService.Application.Features.Notifications.GetNotificationById;
using NotificationService.Application.Features.Notifications.GetNotifications;
using NotificationService.Application.Features.Notifications.RetryNotification;
using NotificationService.Application.Features.Notifications.SendNotification;
using NotificationService.Domain.Enums;

namespace NotificationService.Api.Endpoints;

public static class NotificationsEndpoints
{
    public static void MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").WithTags("Notifications");

        group.MapPost("/", SendAsync)
            .WithName("SendNotification")
            .WithSummary("Send or schedule a notification on one channel (Email/SMS/Push).")
            .Produces<ApiResponse<SendNotificationResultDto>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequireRateLimiting("notification-write");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetNotificationById")
            .WithSummary("Get a single notification, including its delivery-attempt log.")
            .Produces<ApiResponse<NotificationDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", GetListAsync)
            .WithName("GetNotifications")
            .WithSummary("Paged, filterable, searchable notification history.")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CancelNotification")
            .WithSummary("Cancel a Pending/Scheduled/Retrying notification before it sends.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/retry", RetryAsync)
            .WithName("RetryNotification")
            .WithSummary("Give a DeadLettered notification a fresh retry budget (operator action).")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequireAuthorization();

        group.MapPost("/{id:guid}/delete", SoftDeleteAsync)
            .WithName("SoftDeleteNotification")
            .WithSummary("Soft-delete a notification (hidden from listings/lookups, retained for audit).")
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization();
    }

    private static async Task<IResult> SendAsync(SendNotificationCommand command, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.ToCreatedApiResult(httpContext, result.IsSuccess ? $"/api/v1/notifications/{result.Value!.NotificationId}" : string.Empty,
            "Notification accepted.");
    }

    private static async Task<IResult> GetByIdAsync(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNotificationByIdQuery(id), cancellationToken);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> GetListAsync(
        IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken,
        int page = 1, int pageSize = 20, NotificationChannel? channel = null, NotificationStatus? status = null,
        string? recipient = null, string? sourceReference = null, string? search = null,
        DateTimeOffset? createdFromUtc = null, DateTimeOffset? createdToUtc = null)
    {
        var result = await mediator.Send(
            new GetNotificationsQuery(page, pageSize, channel, status, recipient, sourceReference, search, createdFromUtc, createdToUtc),
            cancellationToken);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> CancelAsync(Guid id, CancelNotificationRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelNotificationCommand(id, request.Reason), cancellationToken);
        return result.ToApiResult(httpContext, "Notification cancelled.");
    }

    private static async Task<IResult> RetryAsync(Guid id, RetryNotificationRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RetryNotificationCommand(id, request.AdditionalAttempts), cancellationToken);
        return result.ToApiResult(httpContext, "Notification re-queued for delivery.");
    }

    private static async Task<IResult> SoftDeleteAsync(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new NotificationService.Application.Features.Notifications.DeleteNotification.DeleteNotificationCommand(id), cancellationToken);
        return result.ToApiResult(httpContext, "Notification removed from active views.");
    }

    private sealed record CancelNotificationRequest(string Reason);
    private sealed record RetryNotificationRequest(int AdditionalAttempts = 3);
}
