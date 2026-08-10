namespace NotificationService.Application.Common.Interfaces;

public sealed record SmsMessage(string ToPhoneNumber, string Body);

/// <summary>Sends a rendered SMS. Provider selected at runtime via Sms:Provider config (Twilio | GenericHttp) — see SmsProviderFactory in Infrastructure.</summary>
public interface ISmsSender
{
    Task<ChannelSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
