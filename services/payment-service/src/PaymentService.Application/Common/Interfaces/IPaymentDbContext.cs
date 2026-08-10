using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Common.Interfaces;

public interface IPaymentDbContext
{
    DbSet<Payment> Payments { get; }
    DbSet<PaymentRefund> Refunds { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
