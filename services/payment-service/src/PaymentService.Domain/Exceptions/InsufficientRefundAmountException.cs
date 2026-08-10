namespace PaymentService.Domain.Exceptions;

public sealed class InsufficientRefundAmountException : DomainException
{
    public InsufficientRefundAmountException(decimal available, decimal requested)
        : base($"Insufficient refundable amount. Available: {available}, Requested: {requested}.") { }
}
