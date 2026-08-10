using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class SetDefaultPaymentMethodCommandHandler : IRequestHandler<SetDefaultPaymentMethodCommand, AgentPaymentMethodDto>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<SetDefaultPaymentMethodCommandHandler> _logger;

    public SetDefaultPaymentMethodCommandHandler(IPaymentDbContext context, ILogger<SetDefaultPaymentMethodCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AgentPaymentMethodDto> Handle(SetDefaultPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting default payment method {PaymentMethodId} for agent {AgentId}", request.PaymentMethodId, request.AgentId);

        var methods = await _context.AgentPaymentMethods
            .Where(m => m.AgentId == request.AgentId)
            .ToListAsync(cancellationToken);

        var target = methods.FirstOrDefault(m => m.Id == request.PaymentMethodId)
            ?? throw new InvalidOperationException($"Payment method {request.PaymentMethodId} not found for agent {request.AgentId}.");

        foreach (var method in methods)
        {
            if (method.Id == target.Id)
                method.MarkAsDefault();
            else
                method.MarkAsNotDefault();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Default payment method set to {PaymentMethodId} for agent {AgentId}", target.Id, request.AgentId);

        return new AgentPaymentMethodDto(
            target.Id,
            target.AgentId,
            target.MethodType.ToString(),
            target.Provider,
            target.AccountNumber,
            target.AccountName,
            target.IsDefault,
            target.IsVerified,
            target.CreatedAtUtc,
            target.UpdatedAtUtc);
    }
}