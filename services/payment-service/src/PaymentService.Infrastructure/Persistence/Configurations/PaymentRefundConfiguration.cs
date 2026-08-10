using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        builder.ToTable("payment_refunds");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PaymentId).IsRequired();
        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.Amount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(r => r.Currency).IsRequired().HasMaxLength(3);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
        builder.Property(r => r.ProviderRefundReference).HasMaxLength(500);
        builder.Property(r => r.Status).IsRequired().HasMaxLength(30);
        builder.Property(r => r.FailureReason).HasMaxLength(1000);
        builder.Property(r => r.FailureCode).HasMaxLength(100);
        builder.Property(r => r.InitiatedByUserId).HasMaxLength(200);
        builder.Property(r => r.CreatedAtUtc).IsRequired();
        builder.Property(r => r.UpdatedAtUtc);
        builder.Property(r => r.ProcessedAtUtc);

        builder.HasIndex(r => r.PaymentId);
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAtUtc);
    }
}
