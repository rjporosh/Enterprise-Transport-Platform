namespace PaymentService.Application.Common.Interfaces;

public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(string providerName);
    IReadOnlyCollection<string> AvailableProviders { get; }
}
