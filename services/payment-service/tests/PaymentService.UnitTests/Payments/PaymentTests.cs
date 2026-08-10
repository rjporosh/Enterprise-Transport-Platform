using FluentAssertions;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;
using PaymentService.Domain.Exceptions;
using System.Linq;
using Xunit;

namespace PaymentService.UnitTests.Payments;

public class PaymentTests
{
    [Fact]
    public void Create_ValidPayment_ReturnsPaymentWithPendingStatus()
    {
        var payment = Payment.Create(
            tenantId: Guid.NewGuid(),
            companyId: Guid.NewGuid(),
            organizationId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            orderReference: "ORDER-001",
            idempotencyKey: "idem-001",
            paymentMethod: PaymentMethodType.Card,
            amount: new Money(100.00m, "USD"),
            feeAmount: 2.50m,
            taxAmount: 10.00m,
            metadata: null,
            ttl: TimeSpan.FromMinutes(30));

        payment.Should().NotBeNull();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Amount.Amount.Should().Be(100.00m);
        payment.Currency.Should().Be("USD");
        payment.IdempotencyKey.Should().Be("idem-001");
        payment.DomainEvents.Should().HaveCount(1);
        payment.DomainEvents.First().Should().BeOfType<PaymentCreatedDomainEvent>();
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsArgumentException()
    {
        Action act = () => Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ORDER-001",
            "idem-001",
            PaymentMethodType.Card,
            new Money(-100m, "USD"),
            null,
            null,
            null,
            TimeSpan.FromMinutes(30));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartProcessing_FromPending_UpdatesStatusToProcessing()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing("provider-ref-001");

        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.ProviderReference.Should().Be("provider-ref-001");
        payment.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void StartProcessing_FromFailed_ThrowsInvalidStateTransition()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Fail("Insufficient funds");

        Action act = () => payment.StartProcessing();
        act.Should().Throw<InvalidPaymentStateTransitionException>();
    }

    [Fact]
    public void Succeed_FromProcessing_UpdatesStatusToSucceeded()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Succeed("txn-001", "provider-ref-001");

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.ProviderPaymentId.Should().Be("txn-001");
    }

    [Fact]
    public void Fail_FromPending_UpdatesStatusToFailed()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.Fail("Card declined", "card_declined");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Card declined");
        payment.FailureCode.Should().Be("card_declined");
    }

    [Fact]
    public void Cancel_FromPending_UpdatesStatusToCancelled()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.Cancel("Customer requested");

        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.FailureReason.Should().Be("Customer requested");
    }

    [Fact]
    public void InitiateRefund_FromSucceeded_ReturnsRefund()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Succeed("txn-001");

        var refund = payment.InitiateRefund(50m, "Partial refund", "user-001");

        refund.Should().NotBeNull();
        refund.Amount.Should().Be(50m);
        refund.Status.Should().Be(RefundStatus.Pending);
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.TotalRefundedAmount.Should().Be(50m);
        payment.AvailableRefundAmount.Should().Be(50m);
    }

    [Fact]
    public void InitiateRefund_FullAmount_UpdatesStatusToRefunded()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Succeed("txn-001");

        payment.InitiateRefund(100m, "Full refund", "user-001");

        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.AvailableRefundAmount.Should().Be(0);
    }

    [Fact]
    public void InitiateRefund_MoreThanAvailable_ThrowsInsufficientRefundAmount()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Succeed("txn-001");
        payment.InitiateRefund(50m, "Partial", "user-001");

        Action act = () => payment.InitiateRefund(60m, "Too much", "user-001");
        act.Should().Throw<InsufficientRefundAmountException>();
    }

    [Fact]
    public void InitiateRefund_FromPending_ThrowsInvalidOperation()
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        Action act = () => payment.InitiateRefund(50m, "Refund", "user-001");
        act.Should().Throw<InvalidOperationException>();
    }
}
