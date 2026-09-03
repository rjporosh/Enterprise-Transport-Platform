namespace PaymentService.Domain.Enums;

public enum PaymentMethodType
{
    Card = 0,
    BankTransfer = 1,
    MobileWallet = 2,
    Cash = 3,
    Bkash = 4,
    Nagad = 5,
    /// <summary>Merchant-presented EMVCo QR ("Bangla QR"). Customer scans with any bank / MFS app.</summary>
    Qr = 6,
    Unknown = 99
}
