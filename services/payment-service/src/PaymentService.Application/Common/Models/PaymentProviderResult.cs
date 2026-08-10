namespace PaymentService.Application.Common.Models;

public sealed record PaymentProviderResult(
    PaymentProviderStatus Status,
    string? ProviderTransactionId = null,
    string? ProviderReference = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? RawResponse = null)
{
    public bool IsSuccess => Status == PaymentProviderStatus.Succeeded;
    public bool IsTransientFailure => Status == PaymentProviderStatus.Unknown || Status == PaymentProviderStatus.Processing;
}
