using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using System.Diagnostics.Metrics;

namespace PaymentService.Infrastructure.Observability;

public class PaymentMetrics : IPaymentMetrics
{
    private readonly Meter _meter;
    private readonly ILogger<PaymentMetrics> _logger;
    private readonly Counter<long> _paymentsCreatedCounter;
    private readonly Counter<long> _paymentsSucceededCounter;
    private readonly Counter<long> _paymentsFailedCounter;
    private readonly Counter<long> _refundsCreatedCounter;
    private readonly Counter<long> _refundsSucceededCounter;
    private readonly Counter<long> _idempotencyConflictsCounter;
    private readonly Counter<long> _circuitBreakerOpenedCounter;
    private readonly Histogram<double> _paymentLatencyHistogram;
    private readonly Histogram<double> _providerLatencyHistogram;

    public PaymentMetrics(ILogger<PaymentMetrics> logger)
    {
        _logger = logger;
        _meter = new Meter("PaymentService", "1.0.0");

        _paymentsCreatedCounter = _meter.CreateCounter<long>("payment.created", description: "Total payments created");
        _paymentsSucceededCounter = _meter.CreateCounter<long>("payment.succeeded", description: "Total payments succeeded");
        _paymentsFailedCounter = _meter.CreateCounter<long>("payment.failed", description: "Total payments failed");
        _refundsCreatedCounter = _meter.CreateCounter<long>("refund.created", description: "Total refunds created");
        _refundsSucceededCounter = _meter.CreateCounter<long>("refund.succeeded", description: "Total refunds succeeded");
        _idempotencyConflictsCounter = _meter.CreateCounter<long>("payment.idempotency.conflict", description: "Idempotency conflicts");
        _circuitBreakerOpenedCounter = _meter.CreateCounter<long>("payment.circuitbreaker.opened", description: "Circuit breaker openings");
        _paymentLatencyHistogram = _meter.CreateHistogram<double>("payment.latency", unit: "ms", description: "Payment operation latency");
        _providerLatencyHistogram = _meter.CreateHistogram<double>("payment.provider.latency", unit: "ms", description: "Payment provider latency");
    }

    public void RecordPaymentCreated(string paymentMethod, string status)
    {
        _paymentsCreatedCounter.Add(1, new KeyValuePair<string, object?>("payment.method", paymentMethod), new KeyValuePair<string, object?>("payment.status", status));
    }

    public void RecordPaymentSucceeded(string paymentMethod, decimal amount, string currency)
    {
        _paymentsSucceededCounter.Add(1, new KeyValuePair<string, object?>("payment.method", paymentMethod), new KeyValuePair<string, object?>("currency", currency));
        _paymentLatencyHistogram.Record(0, new KeyValuePair<string, object?>("payment.method", paymentMethod));
    }

    public void RecordPaymentFailed(string paymentMethod, string? failureCode)
    {
        _paymentsFailedCounter.Add(1,
            new KeyValuePair<string, object?>("payment.method", paymentMethod),
            new KeyValuePair<string, object?>("failure.code", failureCode ?? "unknown"));
    }

    public void RecordRefundCreated(string currency, decimal amount)
    {
        _refundsCreatedCounter.Add(1, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordRefundSucceeded(string currency, decimal amount)
    {
        _refundsSucceededCounter.Add(1, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordProviderLatency(string provider, double milliseconds)
    {
        _providerLatencyHistogram.Record(milliseconds, new KeyValuePair<string, object?>("provider", provider));
    }

    public void RecordIdempotencyConflict()
    {
        _idempotencyConflictsCounter.Add(1);
    }

    public void RecordCircuitBreakerOpened(string provider)
    {
        _circuitBreakerOpenedCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
        _logger.LogWarning("Circuit breaker opened for provider {Provider}", provider);
    }
}
