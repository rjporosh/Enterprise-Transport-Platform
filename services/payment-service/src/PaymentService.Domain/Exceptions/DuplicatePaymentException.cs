namespace PaymentService.Domain.Exceptions;

public sealed class DuplicatePaymentException : DomainException
{
    public DuplicatePaymentException(string idempotencyKey)
        : base($"A payment with idempotency key '{idempotencyKey}' already exists.") { }
}
