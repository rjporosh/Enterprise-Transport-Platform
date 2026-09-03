using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Retry;

namespace NotificationService.Infrastructure.Channels.Sms;

public sealed class BdSmsOptions
{
    /// <summary>Aggregator send endpoint, e.g. <c>https://smsplus.sslwireless.com/api/v3/send-sms</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>API token / auth key issued by the aggregator.</summary>
    public string ApiToken { get; set; } = string.Empty;
    /// <summary>Sender id / masking id (e.g. <c>8809612345678</c> or an approved brand name).</summary>
    public string SenderId { get; set; } = string.Empty;
    /// <summary>Field name for the token (aggregators differ: <c>api_token</c>, <c>token</c>, <c>apikey</c>).</summary>
    public string TokenField { get; set; } = "api_token";
    /// <summary>Field name for the recipient msisdn (<c>msisdn</c>, <c>to</c>, <c>number</c>).</summary>
    public string RecipientField { get; set; } = "msisdn";
    /// <summary>Field name for the message body (<c>sms</c>, <c>message</c>, <c>text</c>).</summary>
    public string MessageField { get; set; } = "sms";
    /// <summary>Field name for the sender id (<c>sid</c>, <c>senderid</c>, <c>from</c>).</summary>
    public string SenderField { get; set; } = "sid";
}

/// <summary>
/// Bangladesh bulk-SMS adapter — form-encoded POST on the contract shared by
/// SSLWireless / bulksmsbd / Mimsms / Alpha-net style aggregators (field
/// names are configurable via <see cref="BdSmsOptions"/>). Fails hard with a
/// logged reason when unconfigured — never a silent success.
/// </summary>
public sealed class BdSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly BdSmsOptions _bd;
    private readonly ChannelRetryPolicyFactory _retryPolicyFactory;
    private readonly ILogger<BdSmsSender> _logger;

    public BdSmsSender(HttpClient httpClient, IOptions<SmsOptions> options, ChannelRetryPolicyFactory retryPolicyFactory, ILogger<BdSmsSender> logger)
    {
        _httpClient = httpClient;
        _bd = options.Value.Bd;
        _retryPolicyFactory = retryPolicyFactory;
        _logger = logger;
    }

    public async Task<ChannelSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_bd.Endpoint) || string.IsNullOrWhiteSpace(_bd.ApiToken))
        {
            _logger.LogError(
                "Sms:Provider=Bd but Sms:Bd:Endpoint / Sms:Bd:ApiToken are not configured. " +
                "Root cause: missing configuration. Possible solution: set the aggregator endpoint + token (and " +
                "SenderId), or switch Sms:Provider. SMS will not be delivered until this is resolved.");
            return new ChannelSendResult(false, null, "Bangladesh SMS provider is not configured.");
        }

        var policy = _retryPolicyFactory.Create("Sms(Bd)");
        var msisdn = Normalize(message.ToPhoneNumber);

        try
        {
            var providerId = await policy.ExecuteAsync(async () =>
            {
                var form = new Dictionary<string, string>
                {
                    [_bd.TokenField] = _bd.ApiToken,
                    [_bd.SenderField] = _bd.SenderId,
                    [_bd.RecipientField] = msisdn,
                    [_bd.MessageField] = message.Body
                };

                using var response = await _httpClient.PostAsync(_bd.Endpoint, new FormUrlEncodedContent(form), cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                response.EnsureSuccessStatusCode();

                // Aggregator response shapes vary; the raw body is the provider reference.
                return raw.Length > 200 ? raw[..200] : raw;
            });

            return new ChannelSendResult(true, providerId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SMS send to {Recipient} via the Bangladesh aggregator failed after retries. Root cause: {Endpoint} " +
                "rejected the request or is unreachable. Possible solution: verify the endpoint, token, sender id, " +
                "and the field-name mapping (Sms:Bd:*Field) against the aggregator's current API docs.",
                msisdn, _bd.Endpoint);
            return new ChannelSendResult(false, null, ex.Message);
        }
    }

    /// <summary>Bangladeshi aggregators want a bare 88017******** msisdn.</summary>
    private static string Normalize(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("880")) return digits;
        if (digits.StartsWith("0")) return "88" + digits;
        return "880" + digits;
    }
}
