using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;

namespace PaymentService.Infrastructure.Providers;

public class ResilientPaymentProvider : IPaymentProvider
{
    public string ProviderName => _inner.ProviderName;
    private readonly IPaymentProvider _inner;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private readonly AsyncCircuitBreakerPolicy<PaymentProviderResult> _circuitBreakerPolicy;
    private readonly ILogger<ResilientPaymentProvider> _logger;

    public ResilientPaymentProvider(
        IPaymentProvider inner,
        AsyncRetryPolicy retryPolicy,
        AsyncTimeoutPolicy timeoutPolicy,
        AsyncCircuitBreakerPolicy<PaymentProviderResult> circuitBreakerPolicy,
        ILogger<ResilientPaymentProvider> logger)
    {
        _inner = inner;
        _retryPolicy = retryPolicy;
        _timeoutPolicy = timeoutPolicy;
        _circuitBreakerPolicy = circuitBreakerPolicy;
        _logger = logger;
    }

    public async Task<PaymentProviderResult> ProcessAsync(PaymentProviderRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(
            () => _inner.ProcessAsync(request, cancellationToken),
            "ProcessAsync");
    }

    public async Task<PaymentProviderResult> ConfirmAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(
            () => _inner.ConfirmAsync(providerPaymentId, cancellationToken),
            "ConfirmAsync");
    }

    public async Task<PaymentProviderResult> FailAsync(string providerPaymentId, string reason, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(
            () => _inner.FailAsync(providerPaymentId, reason, cancellationToken),
            "FailAsync");
    }

    public async Task<PaymentProviderResult> RefundAsync(RefundProviderRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(
            () => _inner.RefundAsync(request, cancellationToken),
            "RefundAsync");
    }

    public async Task<PaymentProviderResult> GetStatusAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(
            () => _inner.GetStatusAsync(providerPaymentId, cancellationToken),
            "GetStatusAsync");
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader, string? timestampHeader)
    {
        return _inner.VerifyWebhookSignature(payload, signatureHeader, timestampHeader);
    }

    public async Task<PaymentProviderResult> VerifyPaymentMethodAsync(string accountNumber, string? metadata = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync(
            () => _inner.VerifyPaymentMethodAsync(accountNumber, metadata, cancellationToken),
            "VerifyPaymentMethodAsync");
    }

    private async Task<PaymentProviderResult> ExecuteWithResilienceAsync(
        Func<Task<PaymentProviderResult>> operation,
        string operationName)
    {
        try
        {
            return await _timeoutPolicy.ExecuteAsync(async ct =>
            {
                return await _retryPolicy.ExecuteAsync(async ct =>
                {
                    return await _circuitBreakerPolicy.ExecuteAsync(async ct => await operation(), ct);
                }, ct);
            }, CancellationToken.None);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open for provider {Provider}. Operation {Operation} rejected.", ProviderName, operationName);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: "circuit_breaker_open",
                ErrorMessage: "Payment provider is temporarily unavailable. Please try again later.");
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Timeout calling provider {Provider} for operation {Operation}.", ProviderName, operationName);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: "provider_timeout",
                ErrorMessage: "Payment provider did not respond in time. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling provider {Provider} for operation {Operation}.", ProviderName, operationName);
            return new PaymentProviderResult(
                PaymentProviderStatus.Unknown,
                ErrorCode: "provider_error",
                ErrorMessage: $"Unexpected provider error: {ex.Message}");
        }
    }
}