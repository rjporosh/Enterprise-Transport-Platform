using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.CreatePayment;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePaymentHandler> _logger;

    public CreatePaymentHandler(
        IPaymentDbContext context,
        IEventPublisher eventPublisher,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePaymentHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating payment for customer {CustomerId}, order {OrderReference}, amount {Amount} {Currency}",
            request.CustomerId,
            request.OrderReference,
            request.Amount,
            request.Currency);

        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey, cancellationToken);

        if (existingPayment is not null)
        {
            _logger.LogInformation(
                "Duplicate payment request with idempotency key {IdempotencyKey}. Returning existing payment {PaymentId}",
                request.IdempotencyKey,
                existingPayment.Id);

            return new CreatePaymentResponse(existingPayment.Id, existingPayment.Status.ToString(), existingPayment.ExpiresAtUtc);
        }

        var now = _dateTimeProvider.UtcNow;
        var ttl = request.TtlMinutes.HasValue ? TimeSpan.FromMinutes(request.TtlMinutes.Value) : (TimeSpan?)null;

        var amount = new Money(request.Amount, request.Currency);
        var feeAmount = request.FeeAmount.HasValue ? new Money(request.FeeAmount.Value, request.Currency) : null;
        var taxAmount = request.TaxAmount.HasValue ? new Money(request.TaxAmount.Value, request.Currency) : null;

        var payment = Payment.Create(
            request.TenantId,
            request.CompanyId,
            request.OrganizationId,
            request.CustomerId,
            request.OrderReference,
            request.IdempotencyKey,
            request.PaymentMethod,
            amount,
            feeAmount?.Amount,
            taxAmount?.Amount,
            request.Metadata,
            ttl,
            now);

        _context.Payments.Add(payment);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        }

        payment.ClearDomainEvents();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} created successfully with status {Status}",
            payment.Id,
            payment.Status);

        return new CreatePaymentResponse(payment.Id, payment.Status.ToString(), payment.ExpiresAtUtc);
    }
}
