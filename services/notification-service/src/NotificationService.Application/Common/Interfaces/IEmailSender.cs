namespace NotificationService.Application.Common.Interfaces;

public sealed record EmailMessage(string ToAddress, string Subject, string HtmlBody, string? PlainTextBody = null, string? ToDisplayName = null);

public sealed record ChannelSendResult(bool IsSuccess, string? ProviderMessageId, string? Error);

/// <summary>Sends a rendered email. Implemented by SmtpEmailSender (MailKit) in Infrastructure — see docs/programmers-guide/notification-channels.md for how to add another provider (SendGrid, SES, ...) behind this same interface.</summary>
public interface IEmailSender
{
    Task<ChannelSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
