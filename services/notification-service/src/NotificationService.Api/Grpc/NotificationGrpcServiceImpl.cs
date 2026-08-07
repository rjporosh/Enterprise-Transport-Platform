using Grpc.Core;
using MediatR;
using NotificationService.Application.Features.Notifications.GetNotificationById;
using NotificationService.Application.Features.Notifications.SendNotification;
using NotificationService.Domain.Enums;

namespace NotificationService.Api.Grpc;

/// <summary>Server-side implementation of the internal notification.proto contract — see that file's header comment for why this exists alongside the REST endpoints and the RabbitMQ event consumer.</summary>
public sealed class NotificationGrpcServiceImpl : NotificationGrpcService.NotificationGrpcServiceBase
{
    private readonly IMediator _mediator;

    public NotificationGrpcServiceImpl(IMediator mediator) => _mediator = mediator;

    public override async Task<SendNotificationReply> SendNotification(SendNotificationRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateKey))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "template_key is required."));

        var command = new SendNotificationCommand(
            Recipient: request.Recipient,
            Channel: ToDomainChannel(request.Channel),
            TemplateKey: request.TemplateKey,
            TemplateVariables: request.TemplateVariables.ToDictionary(kv => kv.Key, kv => (object?)kv.Value),
            Subject: null,
            Body: null,
            DataPayload: null,
            RecipientId: string.IsNullOrWhiteSpace(request.RecipientId) ? null : request.RecipientId,
            SourceReference: string.IsNullOrWhiteSpace(request.SourceReference) ? null : request.SourceReference,
            Locale: string.IsNullOrWhiteSpace(request.Locale) ? null : request.Locale,
            Priority: NotificationPriority.Normal,
            ScheduledForUtc: null,
            MaxRetryCount: null,
            IsTransactional: request.IsTransactional);

        var result = await _mediator.Send(command, context.CancellationToken);

        if (!result.IsSuccess)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Message));
            throw new RpcException(new Status(StatusCode.FailedPrecondition, message));
        }

        return new SendNotificationReply
        {
            NotificationId = result.Value!.NotificationId.ToString(),
            Status = result.Value.Status.ToString()
        };
    }

    public override async Task<GetNotificationStatusReply> GetNotificationStatus(GetNotificationStatusRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.NotificationId, out var notificationId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "notification_id must be a GUID."));

        var result = await _mediator.Send(new GetNotificationByIdQuery(notificationId), context.CancellationToken);
        if (!result.IsSuccess)
            throw new RpcException(new Status(StatusCode.NotFound, result.Errors.First().Message));

        return new GetNotificationStatusReply
        {
            NotificationId = result.Value!.Id.ToString(),
            Status = result.Value.Status.ToString(),
            LastError = result.Value.LastError ?? string.Empty
        };
    }

    private static NotificationChannel ToDomainChannel(GrpcChannel channel) => channel switch
    {
        GrpcChannel.Email => NotificationChannel.Email,
        GrpcChannel.Sms => NotificationChannel.Sms,
        GrpcChannel.Push => NotificationChannel.Push,
        _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "channel must be specified."))
    };
}
