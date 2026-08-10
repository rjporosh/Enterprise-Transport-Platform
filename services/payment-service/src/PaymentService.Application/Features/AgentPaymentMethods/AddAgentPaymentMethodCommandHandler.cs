using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class AddAgentPaymentMethodCommandHandler : IRequestHandler<AddAgentPaymentMethodCommand, AgentPaymentMethodDto>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<AddAgentPaymentMethodCommandHandler> _logger;

    public AddAgentPaymentMethodCommandHandler(IPaymentDbContext context, ILogger<AddAgentPaymentMethodCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AgentPaymentMethodDto> Handle(AddAgentPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding payment method for agent {AgentId}, provider {Provider}", request.AgentId, request.Provider);

        var existingDefault = await _context.AgentPaymentMethods
            .FirstOrDefaultAsync(m => m.AgentId == request.AgentId && m.IsDefault, cancellationToken);

        var method = AgentPaymentMethod.Create(
            request.AgentId,
            request.MethodType,
            request.Provider,
            request.AccountNumber,
            request.AccountName,
            request.Metadata);

        if (existingDefault is null)
            method.MarkAsDefault();

        _context.AgentPaymentMethods.Add(method);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment method {PaymentMethodId} added for agent {AgentId}", method.Id, request.AgentId);

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