using FluentAssertions;
using PaymentService.Infrastructure.Providers;
using Xunit;

namespace PaymentService.UnitTests.Providers;

public class EmvcoQrTests
{
    private static readonly QrCodeOptions Options = new()
    {
        MerchantAccountId = "bd.demo.transport",
        MerchantId = "DEMO0000001",
        MerchantName = "Enterprise Transport",
        MerchantCity = "Dhaka",
        CountryCode = "BD",
        TransactionCurrency = "050"
    };

    [Fact]
    public void Build_ProducesAValidEmvcoPayloadWithACorrectCrc()
    {
        var payload = EmvcoQr.Build(Options, 1250.00m, "11111111-1111-1111-1111-111111111111");

        EmvcoQr.IsValid(payload).Should().BeTrue();
        payload.Should().StartWith("000201010212");      // format indicator (00) + POI method 12 (dynamic)
        payload.Should().Contain("5303050");             // currency tag 53, value 050 (BDT)
        payload.Should().Contain("54071250.00");         // amount tag 54, length 07, value 1250.00

        var top = EmvcoQr.Parse(payload);
        top["54"].Should().Be("1250.00");
        top["58"].Should().Be("BD");
    }

    [Fact]
    public void Build_EmbedsThePaymentIdAsTheBillNumber()
    {
        var paymentId = "22222222-2222-2222-2222-222222222222";
        var payload = EmvcoQr.Build(Options, 500m, paymentId);

        var top = EmvcoQr.Parse(payload);
        var additional = EmvcoQr.Parse(top["62"]);
        additional["01"].Should().Be(paymentId);
    }

    [Fact]
    public void IsValid_RejectsATamperedPayload()
    {
        var payload = EmvcoQr.Build(Options, 100m, "id");
        var tampered = payload.Replace("5406100.00", "5406900.00"); // amount 100.00 -> 900.00, CRC now stale

        EmvcoQr.IsValid(tampered).Should().BeFalse();
    }
}
