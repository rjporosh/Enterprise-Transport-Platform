using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class GetAgentPaymentMethodsQueryHandler : IRequestHandler<GetAgentPaymentMethodsQuery, PagedResult<AgentPaymentMethodDto>>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<GetAgentPaymentMethodsQueryHandler> _logger;

    public GetAgentPaymentMethodsQueryHandler(IPaymentDbContext context, ILogger<GetAgentPaymentMethodsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<AgentPaymentMethodDto>> Handle(GetAgentPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching payment methods for agent {AgentId}", request.AgentId);

        var query = _context.AgentPaymentMethods
            .AsNoTracking()
            .Where(m => m.AgentId == request.AgentId);

        if (request.OnlyVerified.HasValue)
            query = query.Where(m => m.IsVerified == request.OnlyVerified.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(m => new AgentPaymentMethodDto(
            m.Id,
            m.AgentId,
            m.MethodType.ToString(),
            m.Provider,
            m.AccountNumber,
            m.AccountName,
            m.IsDefault,
            m.IsVerified,
            m.CreatedAtUtc,
            m.UpdatedAtUtc)).ToList();

        return new PagedResult<AgentPaymentMethodDto>(dtos, total, request.Page, request.PageSize);
    }
}