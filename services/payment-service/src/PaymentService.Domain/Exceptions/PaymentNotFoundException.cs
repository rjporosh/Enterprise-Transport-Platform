namespace PaymentService.Domain.Exceptions;

public sealed class PaymentNotFoundException : DomainException
{
    public PaymentNotFoundException(Guid paymentId)
        : base($"Payment with ID {paymentId} was not found.") { }
}
