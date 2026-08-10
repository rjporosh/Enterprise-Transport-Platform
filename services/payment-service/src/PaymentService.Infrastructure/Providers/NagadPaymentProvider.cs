using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Infrastructure.Communication;

namespace PaymentService.Infrastructure.Providers;

public class NagadPaymentProvider : IPaymentProvider, IDisposable
{
    public string ProviderName => "Nagad";
    private readonly ILogger<NagadPaymentProvider> _logger;
    private readonly NagadOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private string? _cachedSessionId;
    private DateTimeOffset _sessionExpiresAt = DateTimeOffset.MinValue;

    public NagadPaymentProvider(
        IOptions<NagadOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<NagadPaymentProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _retryPolicy = PollyPolicies.GetRetryPolicy(logger);
        _timeoutPolicy = PollyPolicies.GetTimeoutPolicy(logger);
    }

    public async Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.MerchantId) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogWarning("Nagad credentials are not configured. Returning stub processing result.");
            return new PaymentProviderResult(
                PaymentProviderStatus.Processing,
                ProviderReference: request.ProviderPaymentId,
                RawResponse: new Dictionary<string, string> { ["message"] = "Nagad stub mode — configure Nagad:MerchantId and Nagad:SecretKey" });
        }

        try
        {
            var sessionId = await GetSessionIdAsync(cancellationToken);
            var invoice = request.IdempotencyKey ?? request.ProviderPaymentId;

            var payload = new
            {
                merchantInvoiceNumber = invoice,
                amount = request.Amount.ToString("F2"),
                currency = request.Currency,
                callbackURL = _options.CallbackUrl,
                merchantAccountInfo = request.CorrelationId ?? string.Empty
            };

            using var client = _httpClientFactory.CreateClient("Nagad");
            client.DefaultRequestHeaders.Add("X-MerchantId", _options.MerchantId);
            client.DefaultRequestHeaders.Add("X-SessionId", sessionId);
            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                client.DefaultRequestHeaders.Add("X-Correlation-Id", request.CorrelationId);

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _timeoutPolicy.ExecuteAsync(async ct =>
                {
                    return await client.PostAsync($"{_options.BaseUrl}/api/v1/payment/create", content, ct);
                }, ct);
            }, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Nagad create payment response: {StatusCode} {Body}", (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nagad create payment failed with status {StatusCode}", (int)response.StatusCode);
                return new PaymentProviderResult(
                    PaymentProviderStatus.Unknown,
                    ErrorCode: $"nagad_http_{(int)response.StatusCode}",
                    ErrorMessage: $"Nagad create payment failed: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 200 || status == 201)
            {
                var paymentId = root.GetProperty("paymentId").GetString() ?? request.ProviderPaymentId;
                var nagadTrxId = root.GetProperty("trxId").GetString() ?? string.Empty;
                return new PaymentProviderResult(
                    PaymentProviderStatus.Processing,
                    ProviderTransactionId: nagadTrxId,
                    ProviderReference: paymentId,
                    RawResponse: new Dictionary<string, string>
                    {
                        ["payment_id"] = paymentId,
                        ["trx_id"] = nagadTrxId,
                        ["amount"] = request.Amount.ToString("F2"),
                        ["currency"] = request.Currency,
                        ["invoice"] = invoice
                    });
            }

            var message = root.GetProperty("statusMessage").GetString() ?? "Unknown Nagad error";
            _logger.LogWarning("Nagad create payment returned status {Status}: {Message}", status, message);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: $"nagad_{status}",
                ErrorMessage: message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Nagad ProcessAsync for payment {PaymentId}", request.ProviderPaymentId);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: "nagad_unexpected",
                ErrorMessage: $"Unexpected Nagad error: {ex.Message}");
        }
    }

    public async Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.MerchantId))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "Nagad not configured");

        try
        {
            var sessionId = await GetSessionIdAsync(cancellationToken);
            using var client = _httpClientFactory.CreateClient("Nagad");
            client.DefaultRequestHeaders.Add("X-MerchantId", _options.MerchantId);
            client.DefaultRequestHeaders.Add("X-SessionId", sessionId);

            var payload = new { paymentID = providerPaymentId };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{_options.BaseUrl}/api/v1/payment/execute", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "nagad_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 200 || status == 201)
            {
                var transactionStatus = root.GetProperty("transactionStatus").GetString() ?? string.Empty;
                var isSuccess = transactionStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                             || transactionStatus.Equals("Authorized", StringComparison.OrdinalIgnoreCase);
                return new PaymentProviderResult(
                    isSuccess ? PaymentProviderStatus.Succeeded : PaymentProviderStatus.Unknown,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId);
            }

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"nagad_{status}", ErrorMessage: root.GetProperty("statusMessage").GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nagad ConfirmAsync failed for payment {PaymentId}", providerPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "nagad_unexpected", ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Nagad FailAsync for payment {PaymentId}: {Reason}", providerPaymentId, reason);
        return await Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Failed,
            ErrorCode: "manual_fail",
            ErrorMessage: reason));
    }

    public async Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.MerchantId))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "Nagad not configured");

        try
        {
            var sessionId = await GetSessionIdAsync(cancellationToken);
            using var client = _httpClientFactory.CreateClient("Nagad");
            client.DefaultRequestHeaders.Add("X-MerchantId", _options.MerchantId);
            client.DefaultRequestHeaders.Add("X-SessionId", sessionId);

            var payload = new
            {
                paymentID = request.ProviderPaymentId,
                amount = request.RefundAmount.ToString("F2"),
                currency = request.Currency,
                reason = request.RefundReason ?? "Refund requested",
                trxId = request.IdempotencyKey ?? Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{_options.BaseUrl}/api/v1/payment/refund", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "nagad_refund_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 200 || status == 201)
            {
                return new PaymentProviderResult(
                    PaymentProviderStatus.Succeeded,
                    ProviderTransactionId: request.ProviderPaymentId,
                    ProviderReference: request.IdempotencyKey ?? request.ProviderPaymentId);
            }

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"nagad_refund_{status}", ErrorMessage: root.GetProperty("statusMessage").GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nagad RefundAsync failed for payment {PaymentId}", request.ProviderPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "nagad_refund_unexpected", ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.MerchantId))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "Nagad not configured");

        try
        {
            var sessionId = await GetSessionIdAsync(cancellationToken);
            using var client = _httpClientFactory.CreateClient("Nagad");
            client.DefaultRequestHeaders.Add("X-MerchantId", _options.MerchantId);
            client.DefaultRequestHeaders.Add("X-SessionId", sessionId);

            var payload = new { paymentID = providerPaymentId };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{_options.BaseUrl}/api/v1/payment/query", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "nagad_query_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 200 || status == 201)
            {
                var transactionStatus = root.GetProperty("transactionStatus").GetString() ?? string.Empty;
                var isCompleted = transactionStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                               || transactionStatus.Equals("Authorized", StringComparison.OrdinalIgnoreCase);
                return new PaymentProviderResult(
                    isCompleted ? PaymentProviderStatus.Succeeded : PaymentProviderStatus.Processing,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId);
            }

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"nagad_query_{status}", ErrorMessage: root.GetProperty("statusMessage").GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nagad GetStatusAsync failed for payment {PaymentId}", providerPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "nagad_query_unexpected", ErrorMessage: ex.Message);
        }
    }

    private async Task<string> GetSessionIdAsync(CancellationToken cancellationToken)
    {
        if (_cachedSessionId is not null && DateTimeOffset.UtcNow < _sessionExpiresAt)
            return _cachedSessionId;

        _logger.LogInformation("Requesting Nagad session ID");

        using var client = _httpClientFactory.CreateClient("Nagad");
        client.DefaultRequestHeaders.Add("X-MerchantId", _options.MerchantId);

        var payload = new
        {
            merchantId = _options.MerchantId,
            secretKey = _options.SecretKey
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync($"{_options.BaseUrl}/api/v1/session/create", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nagad session creation failed: {(int)response.StatusCode} {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        _cachedSessionId = root.GetProperty("sessionId").GetString();
        var expiresIn = root.GetProperty("expiresIn").GetInt32();
        _sessionExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

        _logger.LogInformation("Nagad session ID obtained, expires in {ExpiresIn}s", expiresIn);
        return _cachedSessionId;
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();
            var providedSignature = signatureHeader.ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(providedSignature));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nagad webhook signature verification failed");
            return false;
        }
    }

    public void Dispose()
    {
        _cachedSessionId = null;
        _sessionExpiresAt = DateTimeOffset.MinValue;
    }
}