namespace PaymentService.Application.Common.Models;

public enum PaymentProviderStatus
{
    Succeeded = 0,
    Failed = 1,
    Processing = 2,
    RequiresAction = 3,
    Unknown = 99
}
