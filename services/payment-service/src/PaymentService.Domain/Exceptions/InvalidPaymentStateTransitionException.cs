namespace PaymentService.Domain.Exceptions;

public sealed class InvalidPaymentStateTransitionException : DomainException
{
    public InvalidPaymentStateTransitionException(string fromState, string toState)
        : base($"Invalid payment state transition from '{fromState}' to '{toState}'.") { }
}
