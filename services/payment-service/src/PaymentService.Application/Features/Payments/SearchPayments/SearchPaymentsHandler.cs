using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Features.Payments;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.Payments.SearchPayments;

public class SearchPaymentsHandler : IRequestHandler<SearchPaymentsQuery, PagedResult<PaymentDto>>
{
    private readonly IPaymentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SearchPaymentsHandler> _logger;

    public SearchPaymentsHandler(IPaymentDbContext context, ICurrentUser currentUser, ILogger<SearchPaymentsHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<PaymentDto>> Handle(SearchPaymentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching payments for tenant {TenantId}", request.TenantId);

        var query = _context.Payments.AsNoTracking().Where(p => p.TenantId == request.TenantId);

        if (request.CustomerId.HasValue)
            query = query.Where(p => p.CustomerId == request.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(request.OrderReference))
            query = query.Where(p => p.OrderReference.Contains(request.OrderReference));

        if (!string.IsNullOrWhiteSpace(request.ProviderReference))
            query = query.Where(p => p.ProviderReference != null && p.ProviderReference.Contains(request.ProviderReference));

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            query = query.Where(p => p.CreatedAtUtc >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(p => p.CreatedAtUtc <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentDto>(items, totalCount, request.Page, request.PageSize);
    }
}
