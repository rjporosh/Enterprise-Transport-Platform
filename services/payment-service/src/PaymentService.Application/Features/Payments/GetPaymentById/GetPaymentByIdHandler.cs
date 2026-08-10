using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Exceptions;

namespace PaymentService.Application.Features.Payments.GetPaymentById;

public class GetPaymentByIdHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    private readonly IPaymentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetPaymentByIdHandler> _logger;

    public GetPaymentByIdHandler(IPaymentDbContext context, ICurrentUser currentUser, ILogger<GetPaymentByIdHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting payment {PaymentId}", request.PaymentId);

        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", request.PaymentId);
            return null;
        }

        if (!string.IsNullOrEmpty(_currentUser.TenantId) && payment.TenantId != Guid.Parse(_currentUser.TenantId))
        {
            _logger.LogWarning("Unauthorized access to payment {PaymentId} by tenant {TenantId}", request.PaymentId, _currentUser.TenantId);
            throw new PaymentNotFoundException(request.PaymentId);
        }

        return new PaymentDto(
            payment.Id,
            payment.TenantId,
            payment.CompanyId,
            payment.OrganizationId,
            payment.CustomerId,
            payment.OrderReference,
            payment.Status.ToString(),
            payment.PaymentMethod.ToString(),
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.FeeAmount,
            payment.TaxAmount,
            payment.ProviderReference,
            payment.ProviderPaymentId,
            payment.FailureReason,
            payment.FailureCode,
            payment.TotalRefundedAmount,
            payment.AvailableRefundAmount,
            payment.IsRefundable,
            payment.ExpiresAtUtc,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.ProcessedAtUtc);
    }
}
