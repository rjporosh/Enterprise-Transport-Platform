namespace PaymentService.Application.Common.Interfaces;

public interface IPaymentMetrics
{
    void RecordPaymentCreated(string paymentMethod, string status);
    void RecordPaymentSucceeded(string paymentMethod, decimal amount, string currency);
    void RecordPaymentFailed(string paymentMethod, string? failureCode);
    void RecordRefundCreated(string currency, decimal amount);
    void RecordRefundSucceeded(string currency, decimal amount);
    void RecordProviderLatency(string provider, double milliseconds);
    void RecordIdempotencyConflict();
    void RecordCircuitBreakerOpened(string provider);
}
