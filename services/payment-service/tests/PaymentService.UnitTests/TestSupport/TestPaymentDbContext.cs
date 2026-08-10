using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Outbox;

namespace PaymentService.UnitTests.TestSupport;

public class TestPaymentDbContext : IPaymentDbContext, IDisposable
{
    private readonly PaymentDbContext _context;

    public TestPaymentDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentDbContext(options);
        Payments = _context.Set<Payment>();
        Refunds = _context.Set<PaymentRefund>();
        OutboxMessages = _context.Set<OutboxMessage>();
    }

    public DbSet<Payment> Payments { get; }
    public DbSet<PaymentRefund> Refunds { get; }
    public DbSet<OutboxMessage> OutboxMessages { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
