using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.Payments.GetPayments;

public class GetPaymentsHandler : IRequestHandler<GetPaymentsQuery, PagedResult<PaymentDto>>
{
    private readonly IPaymentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetPaymentsHandler> _logger;

    public GetPaymentsHandler(IPaymentDbContext context, ICurrentUser currentUser, ILogger<GetPaymentsHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting payments for tenant {TenantId}, page {Page}", request.TenantId, request.Page);

        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId);

        if (request.CustomerId.HasValue)
            query = query.Where(p => p.CustomerId == request.CustomerId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PaymentDto(
                p.Id,
                p.TenantId,
                p.CompanyId,
                p.OrganizationId,
                p.CustomerId,
                p.OrderReference,
                p.Status.ToString(),
                p.PaymentMethod.ToString(),
                p.Amount.Amount,
                p.Amount.Currency,
                p.FeeAmount,
                p.TaxAmount,
                p.ProviderReference,
                p.ProviderPaymentId,
                p.FailureReason,
                p.FailureCode,
                p.TotalRefundedAmount,
                p.AvailableRefundAmount,
                p.IsRefundable,
                p.ExpiresAtUtc,
                p.CreatedAtUtc,
                p.UpdatedAtUtc,
                p.ProcessedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<PaymentDto>(items, totalCount, request.Page, request.PageSize);
    }
}
