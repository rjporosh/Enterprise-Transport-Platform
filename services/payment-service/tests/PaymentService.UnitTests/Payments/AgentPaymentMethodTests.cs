using FluentAssertions;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests.Payments;

public class AgentPaymentMethodTests
{
    [Fact]
    public void Create_WithValidData_ReturnsAgentPaymentMethod()
    {
        var agentId = Guid.NewGuid();
        var method = AgentPaymentMethod.Create(
            agentId,
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            "Agent Name",
            "{\"branch\":\"dhaka\"}");

        method.AgentId.Should().Be(agentId);
        method.MethodType.Should().Be(PaymentMethodType.Bkash);
        method.Provider.Should().Be("Bkash");
        method.AccountNumber.Should().Be("017XXXXXXXXX");
        method.AccountName.Should().Be("Agent Name");
        method.IsDefault.Should().BeFalse();
        method.IsVerified.Should().BeFalse();
        method.Metadata.Should().Be("{\"branch\":\"dhaka\"}");
    }

    [Fact]
    public void Create_WithEmptyAgentId_ThrowsArgumentException()
    {
        Action act = () => AgentPaymentMethod.Create(
            Guid.Empty,
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            null,
            null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*AgentId*");
    }

    [Fact]
    public void Create_WithEmptyProvider_ThrowsArgumentException()
    {
        Action act = () => AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            string.Empty,
            "017XXXXXXXXX",
            null,
            null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Provider*");
    }

    [Fact]
    public void Create_WithEmptyAccountNumber_ThrowsArgumentException()
    {
        Action act = () => AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            "Bkash",
            string.Empty,
            null,
            null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*AccountNumber*");
    }

    [Fact]
    public void MarkAsDefault_SetsIsDefaultToTrue()
    {
        var method = AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            null,
            null);

        method.MarkAsDefault();
        method.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void MarkAsNotDefault_SetsIsDefaultToFalse()
    {
        var method = AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            null,
            null);
        method.MarkAsDefault();
        method.MarkAsNotDefault();
        method.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Verify_SetsIsVerifiedToTrue()
    {
        var method = AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            null,
            null);

        method.Verify("token-123");
        method.IsVerified.Should().BeTrue();
        method.VerificationToken.Should().Be("token-123");
    }

    [Fact]
    public void UpdateAccount_ChangesAccountNumberAndName()
    {
        var method = AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            "Old Name",
            null);

        method.UpdateAccount("018YYYYYYYYY", "New Name", "{\"branch\":\"ctg\"}");
        method.AccountNumber.Should().Be("018YYYYYYYYY");
        method.AccountName.Should().Be("New Name");
        method.Metadata.Should().Be("{\"branch\":\"ctg\"}");
    }

    [Fact]
    public void UpdateAccount_WithEmptyAccountNumber_ThrowsArgumentException()
    {
        var method = AgentPaymentMethod.Create(
            Guid.NewGuid(),
            PaymentMethodType.Bkash,
            "Bkash",
            "017XXXXXXXXX",
            null,
            null);

        Action act = () => method.UpdateAccount(string.Empty, null, null);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AccountNumber*");
    }
}