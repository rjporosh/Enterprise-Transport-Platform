using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.FailPayment;

public class FailPaymentHandler : IRequestHandler<FailPaymentCommand, FailPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<FailPaymentHandler> _logger;

    public FailPaymentHandler(IPaymentDbContext context, IEventPublisher eventPublisher, ILogger<FailPaymentHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<FailPaymentResponse> Handle(FailPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Failing payment {PaymentId} with reason: {Reason}", request.PaymentId, request.Reason);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        payment.Fail(request.Reason, request.FailureCode);

        foreach (var domainEvent in payment.DomainEvents)
        {
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
        }

        payment.ClearDomainEvents();
        await _context.SaveChangesAsync(cancellationToken);

        return new FailPaymentResponse(payment.Id, payment.Status.ToString(), payment.FailureReason ?? string.Empty);
    }
}
