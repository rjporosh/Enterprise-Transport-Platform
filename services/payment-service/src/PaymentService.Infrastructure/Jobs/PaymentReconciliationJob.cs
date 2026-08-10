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

public class PaymentReconciliationJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentReconciliationJob> _logger;

    public PaymentReconciliationJob(IServiceProvider serviceProvider, ILogger<PaymentReconciliationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("PaymentReconciliationJob started");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IPaymentProviderFactory>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var metrics = scope.ServiceProvider.GetRequiredService<IPaymentMetrics>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var cutoff = dateTimeProvider.UtcNow.AddMinutes(-5);

        var stalePayments = await dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Processing && p.UpdatedAtUtc < cutoff)
            .OrderBy(p => p.UpdatedAtUtc)
            .Take(100)
            .ToListAsync(context.CancellationToken);

        if (stalePayments.Count == 0)
        {
            _logger.LogInformation("No stale processing payments found for reconciliation");
            return;
        }

        _logger.LogInformation("Reconciling {Count} stale processing payments", stalePayments.Count);

        foreach (var payment in stalePayments)
        {
            try
            {
                var provider = providerFactory.GetProvider(payment.PaymentMethod.ToString());
                var result = await provider.GetStatusAsync(payment.ProviderPaymentId ?? payment.Id.ToString(), context.CancellationToken);

                if (result.Status == PaymentProviderStatus.Succeeded)
                {
                    payment.Succeed(result.ProviderTransactionId ?? result.ProviderReference ?? string.Empty);
                    metrics.RecordPaymentSucceeded(payment.PaymentMethod.ToString(), payment.Amount.Amount, payment.Amount.Currency);
                    _logger.LogInformation("Reconciled payment {PaymentId} to Succeeded", payment.Id);
                }
                else if (result.Status == PaymentProviderStatus.Failed)
                {
                    payment.Fail(result.ErrorMessage ?? "Reconciliation: payment failed.", result.ErrorCode);
                    metrics.RecordPaymentFailed(payment.PaymentMethod.ToString(), result.ErrorCode);
                    _logger.LogInformation("Reconciled payment {PaymentId} to Failed", payment.Id);
                }
                else
                {
                    _logger.LogInformation("Payment {PaymentId} still in transient state {Status}", payment.Id, result.Status);
                    continue;
                }

                foreach (var domainEvent in payment.DomainEvents)
                {
                    await eventPublisher.EnqueueAsync(domainEvent, context.CancellationToken);
                }

                payment.ClearDomainEvents();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling payment {PaymentId}", payment.Id);
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("PaymentReconciliationJob completed. Reconciled {Count} payments", stalePayments.Count);
    }
}
