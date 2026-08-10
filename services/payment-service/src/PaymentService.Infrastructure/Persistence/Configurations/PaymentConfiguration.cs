using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.CustomerId).IsRequired();
        builder.Property(p => p.OrderReference).IsRequired().HasMaxLength(200);
        builder.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ProviderReference).HasMaxLength(500);
        builder.Property(p => p.ProviderPaymentId).HasMaxLength(500);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(30);
        builder.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(30);
        builder.Property(typeof(decimal), "_amount").HasColumnName("Amount").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.FeeAmount).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TaxAmount).HasColumnType("numeric(18,2)");
        builder.Property(p => p.FailureReason).HasMaxLength(1000);
        builder.Property(p => p.FailureCode).HasMaxLength(100);
        builder.Property(p => p.Metadata);
        builder.Property(p => p.ExpiresAtUtc).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.UpdatedAtUtc);
        builder.Property(p => p.ProcessedAtUtc);

        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.OrderReference);
        builder.HasIndex(p => p.ProviderPaymentId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAtUtc);
        builder.HasIndex(p => p.IdempotencyKey).IsUnique();

        builder.Ignore(p => p.DomainEvents);

        builder.HasMany(typeof(PaymentRefund), "Refunds")
            .WithOne()
            .HasForeignKey("PaymentId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
