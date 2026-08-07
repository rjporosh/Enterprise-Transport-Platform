using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Retry;

namespace NotificationService.Infrastructure.Channels.Sms;

/// <summary>
/// Sends SMS via Twilio's REST API directly over HttpClient (Basic Auth
/// with Account SID / Auth Token) rather than the Twilio SDK — one fewer
/// dependency for a single POST endpoint, and keeps this provider's
/// implementation fully visible/auditable in one file.
/// https://www.twilio.com/docs/sms/api/message-resource#create-a-message-resource
/// </summary>
public sealed class TwilioSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ChannelRetryPolicyFactory _retryPolicyFactory;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(HttpClient httpClient, IOptions<SmsOptions> options, ChannelRetryPolicyFactory retryPolicyFactory, ILogger<TwilioSmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _retryPolicyFactory = retryPolicyFactory;
        _logger = logger;
    }

    public async Task<ChannelSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var policy = _retryPolicyFactory.Create("Sms(Twilio)");

        try
        {
            var messageSid = await policy.ExecuteAsync(async () =>
            {
                var url = $"https://api.twilio.com/2010-04-01/Accounts/{_options.TwilioAccountSid}/Messages.json";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                var basicAuth = Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes($"{_options.TwilioAccountSid}:{_options.TwilioAuthToken}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);

                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = message.ToPhoneNumber,
                    ["From"] = _options.FromNumber,
                    ["Body"] = message.Body
                });

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Twilio returned {(int)response.StatusCode}: {body}");

                using var json = System.Text.Json.JsonDocument.Parse(body);
                return json.RootElement.TryGetProperty("sid", out var sid) ? sid.GetString() : null;
            });

            return new ChannelSendResult(true, messageSid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SMS send to {Recipient} via Twilio failed after retries. Root cause: gateway rejected the request " +
                "or is unreachable. Possible solution: verify Sms:TwilioAccountSid/TwilioAuthToken/FromNumber and " +
                "that the destination number is not on Twilio's geo-permissions block list.",
                message.ToPhoneNumber);
            return new ChannelSendResult(false, null, ex.Message);
        }
    }
}
