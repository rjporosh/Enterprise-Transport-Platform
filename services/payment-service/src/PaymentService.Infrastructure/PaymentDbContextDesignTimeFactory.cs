using PaymentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentService.Infrastructure;

public sealed class PaymentDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=payment_db;Username=postgres;Password=postgres");

        return new PaymentDbContext(optionsBuilder.Options);
    }
}
