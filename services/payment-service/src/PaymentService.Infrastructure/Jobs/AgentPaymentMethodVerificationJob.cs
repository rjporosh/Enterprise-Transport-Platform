using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Models;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Persistence;
using Quartz;

namespace PaymentService.Infrastructure.Jobs;

public class AgentPaymentMethodVerificationJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentPaymentMethodVerificationJob> _logger;

    public AgentPaymentMethodVerificationJob(IServiceProvider serviceProvider, ILogger<AgentPaymentMethodVerificationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("AgentPaymentMethodVerificationJob started");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IPaymentProviderFactory>();

        var unverifiedMethods = await dbContext.AgentPaymentMethods
            .Where(m => !m.IsVerified)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(100)
            .ToListAsync(context.CancellationToken);

        if (unverifiedMethods.Count == 0)
        {
            _logger.LogInformation("No unverified agent payment methods found");
            return;
        }

        _logger.LogInformation("Verifying {Count} agent payment methods", unverifiedMethods.Count);

        foreach (var method in unverifiedMethods)
        {
            try
            {
                var provider = providerFactory.GetProvider(method.Provider);
                var result = await provider.VerifyPaymentMethodAsync(method.AccountNumber, method.Metadata, context.CancellationToken);

                if (result.Status == PaymentProviderStatus.Succeeded)
                {
                    method.Verify(result.ProviderReference);
                    _logger.LogInformation("Verified agent payment method {MethodId} for agent {AgentId}", method.Id, method.AgentId);
                }
                else
                {
                    _logger.LogWarning("Failed to verify agent payment method {MethodId}: {Error}", method.Id, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying agent payment method {MethodId}", method.Id);
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("AgentPaymentMethodVerificationJob completed");
    }
}
