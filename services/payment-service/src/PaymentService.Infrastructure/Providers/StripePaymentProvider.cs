using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Infrastructure.Communication;

namespace PaymentService.Infrastructure.Providers;

public class StripePaymentProvider : IPaymentProvider
{
    public string ProviderName => "Stripe";
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly StripeOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;

    public StripePaymentProvider(
        IOptions<StripeOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<StripePaymentProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _retryPolicy = PollyPolicies.GetRetryPolicy(logger);
        _timeoutPolicy = PollyPolicies.GetTimeoutPolicy(logger);
    }

    public async Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogWarning("Stripe credentials are not configured. Returning stub processing result.");
            return new PaymentProviderResult(
                PaymentProviderStatus.Processing,
                ProviderReference: request.ProviderPaymentId,
                RawResponse: new Dictionary<string, string> { ["message"] = "Stripe stub mode — configure Stripe:SecretKey" });
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("Stripe");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                client.DefaultRequestHeaders.Add("X-Correlation-Id", request.CorrelationId);

            var amountInCents = (long)(request.Amount * 100);
            var payload = new Dictionary<string, string>
            {
                ["amount"] = amountInCents.ToString(),
                ["currency"] = request.Currency.ToLowerInvariant(),
                ["payment_method_types[]"] = "card",
                ["metadata[order_reference]"] = request.OrderReference,
                ["metadata[customer_id]"] = request.CustomerId.ToString(),
                ["metadata[idempotency_key]"] = request.IdempotencyKey ?? string.Empty,
                ["description"] = $"Payment for order {request.OrderReference}"
            };

            using var content = new FormUrlEncodedContent(payload);

            using var response = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _timeoutPolicy.ExecuteAsync(async ct =>
                {
                    return await client.PostAsync($"{_options.BaseUrl}/payment_intents", content, ct);
                }, ct);
            }, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Stripe create payment intent response: {StatusCode}", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stripe create payment intent failed: {Body}", body);
                return new PaymentProviderResult(
                    PaymentProviderStatus.Unknown,
                    ErrorCode: $"stripe_http_{(int)response.StatusCode}",
                    ErrorMessage: $"Stripe payment intent creation failed: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var paymentIntentId = root.GetProperty("id").GetString() ?? request.ProviderPaymentId;
            var clientSecret = root.GetProperty("client_secret").GetString() ?? string.Empty;
            var status = root.GetProperty("status").GetString() ?? string.Empty;

            return new PaymentProviderResult(
                status == "succeeded" ? PaymentProviderStatus.Succeeded : PaymentProviderStatus.Processing,
                ProviderTransactionId: paymentIntentId,
                ProviderReference: paymentIntentId,
                RawResponse: new Dictionary<string, string>
                {
                    ["payment_intent_id"] = paymentIntentId,
                    ["client_secret"] = clientSecret,
                    ["status"] = status,
                    ["amount"] = request.Amount.ToString("F2"),
                    ["currency"] = request.Currency
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Stripe ProcessAsync for payment {PaymentId}", request.ProviderPaymentId);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: "stripe_unexpected",
                ErrorMessage: $"Unexpected Stripe error: {ex.Message}");
        }
    }

    public async Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "Stripe not configured");

        try
        {
            using var client = _httpClientFactory.CreateClient("Stripe");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            using var response = await client.GetAsync($"{_options.BaseUrl}/payment_intents/{Uri.EscapeDataString(providerPaymentId)}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "stripe_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString() ?? string.Empty;

            return status switch
            {
                "succeeded" => new PaymentProviderResult(
                    PaymentProviderStatus.Succeeded,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId),
                "processing" => new PaymentProviderResult(
                    PaymentProviderStatus.Processing,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId),
                "requires_payment_method" or "canceled" => new PaymentProviderResult(
                    PaymentProviderStatus.Failed,
                    ErrorCode: "stripe_failed",
                    ErrorMessage: $"Payment intent status: {status}"),
                _ => new PaymentProviderResult(
                    PaymentProviderStatus.Unknown,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe ConfirmAsync failed for payment {PaymentId}", providerPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "stripe_unexpected", ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stripe FailAsync for payment {PaymentId}: {Reason}", providerPaymentId, reason);
        return await Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Failed,
            ErrorCode: "manual_fail",
            ErrorMessage: reason));
    }

    public async Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "Stripe not configured");

        try
        {
            using var client = _httpClientFactory.CreateClient("Stripe");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            var amountInCents = (long)(request.RefundAmount * 100);
            var payload = new Dictionary<string, string>
            {
                ["payment_intent"] = request.ProviderPaymentId,
                ["amount"] = amountInCents.ToString(),
                ["reason"] = request.RefundReason ?? "requested_by_customer"
            };

            using var content = new FormUrlEncodedContent(payload);

            using var response = await client.PostAsync($"{_options.BaseUrl}/refunds", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "stripe_refund_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString() ?? string.Empty;

            if (status == "succeeded" || status == "pending")
            {
                return new PaymentProviderResult(
                    PaymentProviderStatus.Succeeded,
                    ProviderTransactionId: request.ProviderPaymentId,
                    ProviderReference: request.IdempotencyKey ?? request.ProviderPaymentId);
            }

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"stripe_refund_{status}", ErrorMessage: $"Refund status: {status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe RefundAsync failed for payment {PaymentId}", request.ProviderPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "stripe_refund_unexpected", ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "Stripe not configured");

        try
        {
            using var client = _httpClientFactory.CreateClient("Stripe");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

            using var response = await client.GetAsync($"{_options.BaseUrl}/payment_intents/{Uri.EscapeDataString(providerPaymentId)}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "stripe_query_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString() ?? string.Empty;

            return status switch
            {
                "succeeded" => new PaymentProviderResult(
                    PaymentProviderStatus.Succeeded,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId),
                "processing" => new PaymentProviderResult(
                    PaymentProviderStatus.Processing,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId),
                "requires_payment_method" or "canceled" => new PaymentProviderResult(
                    PaymentProviderStatus.Failed,
                    ErrorCode: "stripe_failed",
                    ErrorMessage: $"Payment intent status: {status}"),
                _ => new PaymentProviderResult(
                    PaymentProviderStatus.Unknown,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe GetStatusAsync failed for payment {PaymentId}", providerPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "stripe_query_unexpected", ErrorMessage: ex.Message);
        }
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        try
        {
            var signatureParts = signatureHeader.Split(',');
            var timestamp = signatureParts.FirstOrDefault(p => p.StartsWith("t="))?.Replace("t=", string.Empty);
            var signature = signatureParts.FirstOrDefault(p => p.StartsWith("v1="))?.Replace("v1=", string.Empty);

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
                return false;

            if (!long.TryParse(timestamp, out var timestampSeconds))
                return false;

            var webhookTimestamp = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            var tolerance = TimeSpan.FromMinutes(5);
            if (Math.Abs((DateTimeOffset.UtcNow - webhookTimestamp).TotalSeconds) > tolerance.TotalSeconds)
            {
                _logger.LogWarning("Stripe webhook timestamp too old");
                return false;
            }

            var signedPayload = $"{timestamp}.{payload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            return false;
        }
    }
}