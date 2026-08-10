using FluentAssertions;
using PaymentService.Application.Features.Payments.RefundPayment;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;
using PaymentService.Domain.Common;
using Xunit;

namespace PaymentService.UnitTests.Payments;

public class RefundPaymentHandlerTests
{
    [Fact]
    public async Task Handle_ValidRefund_ReturnsRefundResponse()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var eventPublisher = new TestSupport.FakeEventPublisher();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RefundPaymentHandler>>();

        var handler = new RefundPaymentHandler(db, eventPublisher, logger);

        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Succeed("txn-001");
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var command = new RefundPaymentCommand(
            payment.Id,
            50m,
            "Customer request",
            "user-001");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.RefundId.Should().NotBeEmpty();
        result.RefundAmount.Should().Be(50m);
        result.RefundStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_RefundMoreThanAvailable_ThrowsException()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var eventPublisher = new TestSupport.FakeEventPublisher();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RefundPaymentHandler>>();

        var handler = new RefundPaymentHandler(db, eventPublisher, logger);

        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", "idem-001", PaymentMethodType.Card,
            new Money(100m, "USD"), null, null, null, TimeSpan.FromMinutes(30));

        payment.StartProcessing();
        payment.Succeed("txn-001");
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var command = new RefundPaymentCommand(payment.Id, 150m, "Too much", "user-001");

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InsufficientRefundAmountException>();
    }
}
