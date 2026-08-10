using FluentAssertions;
using Microsoft.Extensions.Logging;
using PaymentService.Infrastructure.Providers;
using Xunit;

namespace PaymentService.UnitTests.Providers;

public class WebhookSignatureVerificationTests
{
    [Fact]
    public void Bkash_VerifyWebhookSignature_WithValidSignature_ReturnsTrue()
    {
        var secret = "test-webhook-secret";
        var payload = "{\"event\":\"payment.success\",\"paymentId\":\"123\"}";
        var provider = new TestableBkashProvider(secret);

        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        var result = provider.VerifyWebhookSignature(payload, signature, null);
        result.Should().BeTrue();
    }

    [Fact]
    public void Bkash_VerifyWebhookSignature_WithInvalidSignature_ReturnsFalse()
    {
        var provider = new TestableBkashProvider("secret");
        var result = provider.VerifyWebhookSignature("payload", "sha256=invalid", null);
        result.Should().BeFalse();
    }

    [Fact]
    public void Bkash_VerifyWebhookSignature_WithMissingSecret_ReturnsFalse()
    {
        var provider = new TestableBkashProvider(string.Empty);
        var result = provider.VerifyWebhookSignature("payload", "sha256=abc", null);
        result.Should().BeFalse();
    }

    [Fact]
    public void Nagad_VerifyWebhookSignature_WithValidSignature_ReturnsTrue()
    {
        var secret = "test-webhook-secret";
        var payload = "{\"event\":\"payment.success\",\"paymentId\":\"456\"}";
        var provider = new TestableNagadProvider(secret);

        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        var result = provider.VerifyWebhookSignature(payload, signature, null);
        result.Should().BeTrue();
    }

    [Fact]
    public void Nagad_VerifyWebhookSignature_WithInvalidSignature_ReturnsFalse()
    {
        var provider = new TestableNagadProvider("secret");
        var result = provider.VerifyWebhookSignature("payload", "invalid-signature", null);
        result.Should().BeFalse();
    }

    [Fact]
    public void Stripe_VerifyWebhookSignature_WithValidSignature_ReturnsTrue()
    {
        var secret = "whsec_test_secret";
        var payload = "{\"id\":\"evt_test\",\"object\":\"event\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var provider = new TestableStripeProvider(secret);

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();
        var signatureHeader = $"t={timestamp},v1={signature}";

        var result = provider.VerifyWebhookSignature(payload, signatureHeader, timestamp);
        result.Should().BeTrue();
    }

    [Fact]
    public void Stripe_VerifyWebhookSignature_WithOldTimestamp_ReturnsFalse()
    {
        var provider = new TestableStripeProvider("whsec_test_secret");
        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var signatureHeader = $"t={oldTimestamp},v1=abc123";

        var result = provider.VerifyWebhookSignature("payload", signatureHeader, oldTimestamp);
        result.Should().BeFalse();
    }

    [Fact]
    public void Stripe_VerifyWebhookSignature_WithMissingSecret_ReturnsFalse()
    {
        var provider = new TestableStripeProvider(string.Empty);
        var result = provider.VerifyWebhookSignature("payload", "t=123,v1=abc", "123");
        result.Should().BeFalse();
    }

    private sealed class TestableBkashProvider : BkashPaymentProvider
    {
        public TestableBkashProvider(string webhookSecret) : base(
            Microsoft.Extensions.Options.Options.Create(new PaymentService.Infrastructure.Providers.BkashOptions
            {
                WebhookSecret = webhookSecret,
                AppKey = "test",
                AppSecret = "test",
                Username = "test",
                Password = "test",
                BaseUrl = "https://test.bkash.com"
            }),
            null!,
            new NoOpLogger<BkashPaymentProvider>()) { }
    }

    private sealed class TestableNagadProvider : NagadPaymentProvider
    {
        public TestableNagadProvider(string webhookSecret) : base(
            Microsoft.Extensions.Options.Options.Create(new PaymentService.Infrastructure.Providers.NagadOptions
            {
                WebhookSecret = webhookSecret,
                MerchantId = "test",
                SecretKey = "test",
                BaseUrl = "https://test.nagad.com"
            }),
            null!,
            new NoOpLogger<NagadPaymentProvider>()) { }
    }

    private sealed class TestableStripeProvider : StripePaymentProvider
    {
        public TestableStripeProvider(string webhookSecret) : base(
            Microsoft.Extensions.Options.Options.Create(new PaymentService.Infrastructure.Providers.StripeOptions
            {
                WebhookSecret = webhookSecret,
                SecretKey = "test",
                BaseUrl = "https://api.stripe.com/v1"
            }),
            null!,
            new NoOpLogger<StripePaymentProvider>()) { }
    }

    private sealed class NoOpLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}