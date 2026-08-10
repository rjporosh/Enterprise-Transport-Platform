using PaymentService.Application.Common.Interfaces;
using PaymentService.Infrastructure.Observability;
using NSubstitute;

namespace PaymentService.UnitTests.TestSupport;

public class FakePaymentMetrics : IPaymentMetrics
{
    public void RecordPaymentCreated(string paymentMethod, string status) { }
    public void RecordPaymentSucceeded(string paymentMethod, decimal amount, string currency) { }
    public void RecordPaymentFailed(string paymentMethod, string? failureCode) { }
    public void RecordRefundCreated(string currency, decimal amount) { }
    public void RecordRefundSucceeded(string currency, decimal amount) { }
    public void RecordProviderLatency(string provider, double milliseconds) { }
    public void RecordIdempotencyConflict() { }
    public void RecordCircuitBreakerOpened(string provider) { }
}
