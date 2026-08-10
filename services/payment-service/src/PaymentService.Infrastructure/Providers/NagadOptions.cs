namespace PaymentService.Infrastructure.Providers;

public class NagadOptions
{
    public string MerchantId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-sandbox.nagad.com.bd";
    public string CallbackUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}