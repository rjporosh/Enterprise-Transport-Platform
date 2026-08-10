namespace PaymentService.Domain.Exceptions;

public sealed class PaymentProviderException : DomainException
{
    public string? ProviderErrorCode { get; }

    public PaymentProviderException(string message, string? providerErrorCode = null)
        : base(message) => ProviderErrorCode = providerErrorCode;

    public PaymentProviderException(string message, string? providerErrorCode, Exception innerException)
        : base(message, innerException) => ProviderErrorCode = providerErrorCode;
}
