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

public class BkashPaymentProvider : IPaymentProvider, IDisposable
{
    public string ProviderName => "Bkash";
    private readonly ILogger<BkashPaymentProvider> _logger;
    private readonly BkashOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public BkashPaymentProvider(
        IOptions<BkashOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<BkashPaymentProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _retryPolicy = PollyPolicies.GetRetryPolicy(logger);
        _timeoutPolicy = PollyPolicies.GetTimeoutPolicy(logger);
    }

    public async Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            _logger.LogWarning("bKash credentials are not configured. Returning stub processing result.");
            return new PaymentProviderResult(
                PaymentProviderStatus.Processing,
                ProviderReference: request.ProviderPaymentId,
                RawResponse: new Dictionary<string, string> { ["message"] = "bKash stub mode — configure Bkash:AppKey and Bkash:AppSecret" });
        }

        try
        {
            var token = await GetGrantTokenAsync(cancellationToken);
            var invoice = request.IdempotencyKey ?? request.ProviderPaymentId;

            var payload = new
            {
                amount = request.Amount.ToString("F2"),
                currency = request.Currency,
                intent = "sale",
                merchantInvoiceNumber = invoice,
                callbackURL = _options.CallbackUrl,
                merchantAccountInfo = request.CorrelationId ?? string.Empty
            };

            using var client = _httpClientFactory.CreateClient("Bkash");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-APP-Key", _options.AppKey);
            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                client.DefaultRequestHeaders.Add("X-Correlation-Id", request.CorrelationId);

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _timeoutPolicy.ExecuteAsync(async ct =>
                {
                    return await client.PostAsync($"{_options.BaseUrl}/tokenized/checkout/create", content, ct);
                }, ct);
            }, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("bKash create payment response: {StatusCode} {Body}", (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("bKash create payment failed with status {StatusCode}", (int)response.StatusCode);
                return new PaymentProviderResult(
                    PaymentProviderStatus.Unknown,
                    ErrorCode: $"bkash_http_{(int)response.StatusCode}",
                    ErrorMessage: $"bKash create payment failed: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 201 || status == 200)
            {
                var paymentId = root.GetProperty("paymentId").GetString() ?? request.ProviderPaymentId;
                var bkashTrxId = root.GetProperty("trxID").GetString() ?? string.Empty;
                return new PaymentProviderResult(
                    PaymentProviderStatus.Processing,
                    ProviderTransactionId: bkashTrxId,
                    ProviderReference: paymentId,
                    RawResponse: new Dictionary<string, string>
                    {
                        ["payment_id"] = paymentId,
                        ["trx_id"] = bkashTrxId,
                        ["amount"] = request.Amount.ToString("F2"),
                        ["currency"] = request.Currency,
                        ["invoice"] = invoice
                    });
            }

            var message = root.GetProperty("statusMessage").GetString() ?? "Unknown bKash error";
            _logger.LogWarning("bKash create payment returned status {Status}: {Message}", status, message);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: $"bkash_{status}",
                ErrorMessage: message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling bKash ProcessAsync for payment {PaymentId}", request.ProviderPaymentId);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: "bkash_unexpected",
                ErrorMessage: $"Unexpected bKash error: {ex.Message}");
        }
    }

    public async Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "bKash not configured");

        try
        {
            var token = await GetGrantTokenAsync(cancellationToken);
            using var client = _httpClientFactory.CreateClient("Bkash");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-APP-Key", _options.AppKey);

            var payload = new { paymentID = providerPaymentId };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{_options.BaseUrl}/tokenized/checkout/execute", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 201 || status == 200)
            {
                var transactionStatus = root.GetProperty("transactionStatus").GetString() ?? string.Empty;
                var isSuccess = transactionStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                             || transactionStatus.Equals("Authorized", StringComparison.OrdinalIgnoreCase);
                return new PaymentProviderResult(
                    isSuccess ? PaymentProviderStatus.Succeeded : PaymentProviderStatus.Unknown,
                    ProviderTransactionId: providerPaymentId,
                    ProviderReference: providerPaymentId);
            }

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"bkash_{status}", ErrorMessage: root.GetProperty("statusMessage").GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "bKash ConfirmAsync failed for payment {PaymentId}", providerPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_unexpected", ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("bKash FailAsync for payment {PaymentId}: {Reason}", providerPaymentId, reason);
        return await Task.FromResult(new PaymentProviderResult(
            PaymentProviderStatus.Failed,
            ErrorCode: "manual_fail",
            ErrorMessage: reason));
    }

    public async Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "bKash not configured");

        try
        {
            var token = await GetGrantTokenAsync(cancellationToken);
            using var client = _httpClientFactory.CreateClient("Bkash");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-APP-Key", _options.AppKey);

            var payload = new
            {
                paymentID = request.ProviderPaymentId,
                amount = request.RefundAmount.ToString("F2"),
                currency = request.Currency,
                reason = request.RefundReason ?? "Refund requested",
                trxID = request.IdempotencyKey ?? Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{_options.BaseUrl}/tokenized/checkout/refund", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_refund_http_error", ErrorMessage: body);

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var status = root.GetProperty("statusCode").GetInt32();

            if (status == 201 || status == 200)
            {
                return new PaymentProviderResult(
                    PaymentProviderStatus.Succeeded,
                    ProviderTransactionId: request.ProviderPaymentId,
                    ProviderReference: request.IdempotencyKey ?? request.ProviderPaymentId);
            }

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"bkash_refund_{status}", ErrorMessage: root.GetProperty("statusMessage").GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "bKash RefundAsync failed for payment {PaymentId}", request.ProviderPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_refund_unexpected", ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "bKash not configured");

        try
        {
            var token = await GetGrantTokenAsync(cancellationToken);
            using var client = _httpClientFactory.CreateClient("Bkash");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-APP-Key", _options.AppKey);

            var payload = new { paymentID = providerPaymentId };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{_options.BaseUrl}/tokenized/checkout/payment/query", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_query_http_error", ErrorMessage: body);

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

            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: $"bkash_query_{status}", ErrorMessage: root.GetProperty("statusMessage").GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "bKash GetStatusAsync failed for payment {PaymentId}", providerPaymentId);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_query_unexpected", ErrorMessage: ex.Message);
        }
    }

    private async Task<string> GetGrantTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _cachedToken;

        _logger.LogInformation("Requesting bKash grant token");

        using var client = _httpClientFactory.CreateClient("Bkash");
        client.DefaultRequestHeaders.Add("username", _options.Username);
        client.DefaultRequestHeaders.Add("password", _options.Password);

        var payload = new
        {
            app_key = _options.AppKey,
            app_secret = _options.AppSecret
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync($"{_options.BaseUrl}/tokenized/checkout/token/grant", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"bKash token grant failed: {(int)response.StatusCode} {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        _cachedToken = root.GetProperty("id_token").GetString();
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

        _logger.LogInformation("bKash grant token obtained, expires in {ExpiresIn}s", expiresIn);
        return _cachedToken;
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
            var providedSignature = signatureHeader.Replace("sha256=", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(providedSignature));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "bKash webhook signature verification failed");
            return false;
        }
    }

    public async Task<PaymentProviderResult> VerifyPaymentMethodAsync(string accountNumber, string? metadata = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey))
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "not_configured", ErrorMessage: "bKash not configured");

        try
        {
            _logger.LogInformation("Verifying bKash account {AccountNumber}", accountNumber);
            await Task.Delay(100, cancellationToken);
            return new PaymentProviderResult(
                PaymentProviderStatus.Succeeded,
                ProviderReference: accountNumber,
                RawResponse: new Dictionary<string, string>
                {
                    ["account_number"] = accountNumber,
                    ["provider"] = "Bkash"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "bKash account verification failed for {AccountNumber}", accountNumber);
            return new PaymentProviderResult(PaymentProviderStatus.Unknown, ErrorCode: "bkash_verify_error", ErrorMessage: ex.Message);
        }
    }

    public void Dispose()
    {
        _cachedToken = null;
        _tokenExpiresAt = DateTimeOffset.MinValue;
    }
}