using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Features.AgentPaymentMethods;

public class VerifyAgentPaymentMethodCommandHandler : IRequestHandler<VerifyAgentPaymentMethodCommand, AgentPaymentMethodDto>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<VerifyAgentPaymentMethodCommandHandler> _logger;

    public VerifyAgentPaymentMethodCommandHandler(IPaymentDbContext context, ILogger<VerifyAgentPaymentMethodCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AgentPaymentMethodDto> Handle(VerifyAgentPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying payment method {PaymentMethodId}", request.PaymentMethodId);

        var method = await _context.AgentPaymentMethods
            .FirstOrDefaultAsync(m => m.Id == request.PaymentMethodId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment method {request.PaymentMethodId} not found.");

        method.Verify(request.VerificationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment method {PaymentMethodId} verified", method.Id);

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