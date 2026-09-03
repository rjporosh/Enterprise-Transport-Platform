using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.GenerateQr;

public sealed class GenerateQrHandler : IRequestHandler<GenerateQrCommand, GenerateQrResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GenerateQrHandler> _logger;

    public GenerateQrHandler(
        IPaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        IEventPublisher eventPublisher,
        ICurrentUser currentUser,
        ILogger<GenerateQrHandler> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<GenerateQrResponse> Handle(GenerateQrCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        // Ownership: a customer may only generate a QR for their own payment.
        var callerId = _currentUser.CustomerId ?? _currentUser.UserId;
        if (!_currentUser.IsInRole("Admin") && !_currentUser.IsInRole("Operator") && callerId is not null && payment.CustomerId != callerId)
            throw new PaymentNotFoundException(request.PaymentId);

        if (payment.PaymentMethod != PaymentMethodType.Qr)
            throw new InvalidPaymentStateTransitionException(payment.PaymentMethod.ToString(), "Qr");

        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentStateTransitionException(payment.Status.ToString(), PaymentStatus.Processing.ToString());

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.StartProcessing();
            foreach (var domainEvent in payment.DomainEvents)
                await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
            payment.ClearDomainEvents();
        }

        var provider = _providerFactory.GetProvider("Qr");
        var result = await provider.ProcessAsync(new PaymentProviderRequest(
            ProviderPaymentId: payment.Id.ToString(),
            Amount: payment.Amount.Amount,
            Currency: payment.Currency,
            OrderReference: payment.OrderReference,
            CustomerId: payment.CustomerId,
            PaymentMethod: "Qr"), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var payload = result.RawResponse?.GetValueOrDefault("qr_payload") ?? string.Empty;
        var image = result.RawResponse?.GetValueOrDefault("qr_image_data_uri") ?? string.Empty;
        var minutes = int.TryParse(result.RawResponse?.GetValueOrDefault("qr_expires_in_minutes"), out var m) ? m : 15;

        _logger.LogInformation("Issued EMVCo QR for payment {PaymentId}", payment.Id);

        return new GenerateQrResponse(
            payment.Id,
            payment.Status.ToString(),
            payload,
            image,
            DateTimeOffset.UtcNow.AddMinutes(minutes));
    }
}
