using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Retry;

namespace NotificationService.Infrastructure.Channels.Sms;

/// <summary>
/// Generic REST SMS gateway adapter: POSTs {to, from, body} as JSON with a
/// bearer API key. Covers regional/local SMS aggregators (common outside
/// the US/EU where Twilio coverage or pricing isn't ideal) that expose a
/// simple REST send endpoint but have no first-class .NET SDK — configure
/// Sms:GenericHttpEndpoint/GenericHttpApiKey and select Sms:Provider=GenericHttp.
/// </summary>
public sealed class GenericHttpSmsSender : ISmsSender
{
    private sealed record GatewayResponse(string? MessageId);

    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ChannelRetryPolicyFactory _retryPolicyFactory;
    private readonly ILogger<GenericHttpSmsSender> _logger;

    public GenericHttpSmsSender(HttpClient httpClient, IOptions<SmsOptions> options, ChannelRetryPolicyFactory retryPolicyFactory, ILogger<GenericHttpSmsSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _retryPolicyFactory = retryPolicyFactory;
        _logger = logger;
    }

    public async Task<ChannelSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.GenericHttpEndpoint))
        {
            _logger.LogError(
                "Sms:GenericHttpEndpoint is not configured but Sms:Provider=GenericHttp. " +
                "Root cause: missing configuration. Possible solution: set Sms:GenericHttpEndpoint and " +
                "Sms:GenericHttpApiKey, or switch Sms:Provider to Twilio.");
            return new ChannelSendResult(false, null, "SMS gateway endpoint is not configured.");
        }

        var policy = _retryPolicyFactory.Create("Sms(GenericHttp)");

        try
        {
            var messageId = await policy.ExecuteAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.GenericHttpEndpoint);
                if (!string.IsNullOrWhiteSpace(_options.GenericHttpApiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.GenericHttpApiKey);

                request.Content = JsonContent.Create(new { to = message.ToPhoneNumber, from = _options.FromNumber, body = message.Body });

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<GatewayResponse>(cancellationToken: cancellationToken);
                return payload?.MessageId;
            });

            return new ChannelSendResult(true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SMS send to {Recipient} via generic HTTP gateway failed after retries. Root cause: gateway at " +
                "{Endpoint} rejected the request or is unreachable. Possible solution: verify the endpoint URL, " +
                "API key, and the gateway's expected request/response contract still matches this adapter.",
                message.ToPhoneNumber, _options.GenericHttpEndpoint);
            return new ChannelSendResult(false, null, ex.Message);
        }
    }
}
