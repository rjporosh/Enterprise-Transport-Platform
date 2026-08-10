using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence.Configurations;
using PaymentService.Infrastructure.Persistence.Outbox;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext, IPaymentDbContext
{
    private readonly string _schema;

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
        _schema = "payment";
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentRefund> Refunds => Set<PaymentRefund>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
