namespace NotificationService.Application.Common.Interfaces;

public sealed record PushMessage(string DeviceToken, string Title, string Body, IReadOnlyDictionary<string, string>? Data = null);

/// <summary>Sends a push notification via Firebase Cloud Messaging HTTP v1 API (FcmPushSender in Infrastructure).</summary>
public interface IPushSender
{
    Task<ChannelSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default);
}
