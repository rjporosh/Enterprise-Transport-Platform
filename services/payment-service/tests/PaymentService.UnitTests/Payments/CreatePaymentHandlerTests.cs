using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Features.Payments.CreatePayment;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests.Payments;

public class CreatePaymentHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesPayment()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var dateTimeProvider = new TestSupport.FakeDateTimeProvider();
        var eventPublisher = new TestSupport.FakeEventPublisher();
        var metrics = new TestSupport.FakePaymentMetrics();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<CreatePaymentHandler>>();

        var handler = new CreatePaymentHandler(db, eventPublisher, dateTimeProvider, logger);

        var command = new CreatePaymentCommand(
            TenantId: Guid.NewGuid(),
            CompanyId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderReference: "ORDER-001",
            PaymentMethod: PaymentMethodType.Card,
            Amount: 100.00m,
            Currency: "USD",
            IdempotencyKey: "idem-001",
            FeeAmount: 2.50m,
            TaxAmount: 5.00m,
            Metadata: null,
            TtlMinutes: 30);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.PaymentId.Should().NotBeEmpty();
        result.Status.Should().Be("Pending");

        var savedPayment = await db.Payments.FirstAsync();
        savedPayment.Should().NotBeNull();
        savedPayment.OrderReference.Should().Be("ORDER-001");
        savedPayment.Amount.Amount.Should().Be(100.00m);
        eventPublisher.PublishedEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_SameIdempotencyKey_ReturnsExistingPayment()
    {
        using var db = new TestSupport.TestPaymentDbContext();
        var dateTimeProvider = new TestSupport.FakeDateTimeProvider();
        var eventPublisher = new TestSupport.FakeEventPublisher();
        var metrics = new TestSupport.FakePaymentMetrics();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<CreatePaymentHandler>>();

        var handler = new CreatePaymentHandler(db, eventPublisher, dateTimeProvider, logger);

        var command = new CreatePaymentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "ORDER-001", PaymentMethodType.Card, 100m, "USD", "idem-dup",
            null, null, null, null);

        var result1 = await handler.Handle(command, CancellationToken.None);
        var result2 = await handler.Handle(command, CancellationToken.None);

        result1.PaymentId.Should().Be(result2.PaymentId);
        db.Payments.Count().Should().Be(1);
    }
}
