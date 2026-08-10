namespace PaymentService.Domain.Enums;

public enum PaymentMethodType
{
    Card = 0,
    BankTransfer = 1,
    MobileWallet = 2,
    Cash = 3,
    Unknown = 99
}
