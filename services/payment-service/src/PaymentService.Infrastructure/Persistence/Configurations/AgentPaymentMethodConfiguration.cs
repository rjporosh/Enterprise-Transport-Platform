using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class AgentPaymentMethodConfiguration : IEntityTypeConfiguration<AgentPaymentMethod>
{
    public void Configure(EntityTypeBuilder<AgentPaymentMethod> builder)
    {
        builder.ToTable("agent_payment_methods");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.AgentId).IsRequired();
        builder.Property(m => m.MethodType).IsRequired().HasMaxLength(30);
        builder.Property(m => m.Provider).IsRequired().HasMaxLength(50);
        builder.Property(m => m.AccountNumber).IsRequired().HasMaxLength(100);
        builder.Property(m => m.AccountName).HasMaxLength(200);
        builder.Property(m => m.IsDefault).IsRequired();
        builder.Property(m => m.IsVerified).IsRequired();
        builder.Property(m => m.VerificationToken).HasMaxLength(200);
        builder.Property(m => m.Metadata);
        builder.Property(m => m.CreatedAtUtc).IsRequired();
        builder.Property(m => m.UpdatedAtUtc);

        builder.HasIndex(m => m.AgentId);
        builder.HasIndex(m => m.Provider);
        builder.HasIndex(m => m.IsVerified);
        builder.HasIndex(m => new { m.AgentId, m.Provider, m.AccountNumber }).IsUnique();
    }
}