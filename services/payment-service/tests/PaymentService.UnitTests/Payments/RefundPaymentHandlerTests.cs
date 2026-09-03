using FluentAssertions;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Features.Payments.RefundPayment;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;
using Xunit;

namespace PaymentService.UnitTests.Payments;

public class RefundPaymentHandlerTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<RefundPaymentHandler> Logger =
        NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RefundPaymentHandler>>();

    private static async Task<Payment> SeedSucceededPaymentAsync(TestSupport.TestPaymentDbContext db)
    {
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", $"idem-{Guid.NewGuid():N}", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));
        payment.StartProcessing();
        payment.Succeed("txn-001");
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    [Fact]
    public async Task Handle_WhenProviderConfirmsRefund_MarksRefundSucceeded_AndPaymentPartiallyRefunded()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var provider = new TestSupport.FakePaymentProvider { RefundResult = PaymentProviderStatus.Succeeded };
        var handler = new RefundPaymentHandler(db, provider, new TestSupport.FakeEventPublisher(), Logger);
        var payment = await SeedSucceededPaymentAsync(db);

        var result = await handler.Handle(new RefundPaymentCommand(payment.Id, 50m, "Customer request", "user-001"), CancellationToken.None);

        result.RefundStatus.Should().Be("Succeeded");
        result.RefundAmount.Should().Be(50m);

        var reloaded = await db.Payments.FindAsync(payment.Id);
        reloaded!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    [Fact]
    public async Task Handle_WhenProviderRejectsRefund_FailsRefund_AndLeavesPaymentSucceeded()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var provider = new TestSupport.FakePaymentProvider { RefundResult = PaymentProviderStatus.Failed };
        var handler = new RefundPaymentHandler(db, provider, new TestSupport.FakeEventPublisher(), Logger);
        var payment = await SeedSucceededPaymentAsync(db);

        var result = await handler.Handle(new RefundPaymentCommand(payment.Id, 50m, "Customer request", "user-001"), CancellationToken.None);

        result.RefundStatus.Should().Be("Failed");

        var reloaded = await db.Payments.FindAsync(payment.Id);
        reloaded!.Status.Should().Be(PaymentStatus.Succeeded, "a provider-rejected refund must not move the payment (P0-7)");
    }

    [Fact]
    public async Task Handle_WhenProviderThrows_FailsRefundGracefully()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var provider = new TestSupport.FakePaymentProvider { RefundThrows = true };
        var handler = new RefundPaymentHandler(db, provider, new TestSupport.FakeEventPublisher(), Logger);
        var payment = await SeedSucceededPaymentAsync(db);

        var result = await handler.Handle(new RefundPaymentCommand(payment.Id, 50m, "x", "user-001"), CancellationToken.None);

        result.RefundStatus.Should().Be("Failed");
    }

    [Fact]
    public async Task Handle_RefundMoreThanAvailable_Throws()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var handler = new RefundPaymentHandler(db, new TestSupport.FakePaymentProvider(), new TestSupport.FakeEventPublisher(), Logger);
        var payment = await SeedSucceededPaymentAsync(db);

        var act = async () => await handler.Handle(new RefundPaymentCommand(payment.Id, 150m, "Too much", "user-001"), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientRefundAmountException>();
    }
}
