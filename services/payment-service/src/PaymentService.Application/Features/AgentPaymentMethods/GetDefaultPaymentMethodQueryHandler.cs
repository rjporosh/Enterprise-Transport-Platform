using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class GetDefaultPaymentMethodQueryHandler : IRequestHandler<GetDefaultPaymentMethodQuery, AgentPaymentMethodDto?>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<GetDefaultPaymentMethodQueryHandler> _logger;

    public GetDefaultPaymentMethodQueryHandler(IPaymentDbContext context, ILogger<GetDefaultPaymentMethodQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AgentPaymentMethodDto?> Handle(GetDefaultPaymentMethodQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching default payment method for agent {AgentId}", request.AgentId);

        var method = await _context.AgentPaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.AgentId == request.AgentId && m.IsDefault, cancellationToken);

        if (method is null)
            return null;

        return new AgentPaymentMethodDto(
            method.Id,
            method.AgentId,
            method.MethodType.ToString(),
            method.Provider,
            method.AccountNumber,
            method.AccountName,
            method.IsDefault,
            method.IsVerified,
            method.CreatedAtUtc,
            method.UpdatedAtUtc);
    }
}