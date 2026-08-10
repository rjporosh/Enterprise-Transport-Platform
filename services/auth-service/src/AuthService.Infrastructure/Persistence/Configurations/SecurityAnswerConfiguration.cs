using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class SecurityAnswerConfiguration : IEntityTypeConfiguration<SecurityAnswer>
{
    public void Configure(EntityTypeBuilder<SecurityAnswer> builder)
    {
        builder.ToTable("security_answers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AnswerHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.SecurityQuestionId }).IsUnique();
    }
}
