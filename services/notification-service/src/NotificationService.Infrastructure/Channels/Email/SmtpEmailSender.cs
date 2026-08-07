using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Retry;

namespace NotificationService.Infrastructure.Channels.Email;

/// <summary>
/// Sends email over real SMTP via MailKit. Works unmodified against any
/// standards-compliant SMTP server — a local dev catcher (MailHog/Papercut,
/// see docker-compose.yml), a real relay (SendGrid/SES/Postmark's SMTP
/// endpoint), or on-prem Exchange — only Smtp:* configuration changes.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ChannelRetryPolicyFactory _retryPolicyFactory;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ChannelRetryPolicyFactory retryPolicyFactory, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _retryPolicyFactory = retryPolicyFactory;
        _logger = logger;
    }

    public async Task<ChannelSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var policy = _retryPolicyFactory.Create("Email");

        try
        {
            var messageId = await policy.ExecuteAsync(async () =>
            {
                var mime = new MimeMessage();
                mime.From.Add(new MailboxAddress(_options.FromDisplayName, _options.FromAddress));
                mime.To.Add(new MailboxAddress(message.ToDisplayName ?? string.Empty, message.ToAddress));
                mime.Subject = message.Subject;

                var builder = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.PlainTextBody };
                mime.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                var secureSocketOptions = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.UserName))
                    await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

                var response = await client.SendAsync(mime, cancellationToken);
                await client.DisconnectAsync(quit: true, cancellationToken);

                return mime.MessageId;
            });

            return new ChannelSendResult(true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Email send to {Recipient} failed after retries. Root cause: SMTP dependency unavailable or " +
                "rejected the message (host={Host}, port={Port}). Possible solution: verify SMTP credentials, " +
                "that the host/port are reachable, and that the sender address is not blocked by the relay.",
                message.ToAddress, _options.Host, _options.Port);
            return new ChannelSendResult(false, null, ex.Message);
        }
    }
}
