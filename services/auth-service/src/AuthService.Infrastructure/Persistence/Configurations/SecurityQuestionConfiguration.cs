using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class SecurityQuestionConfiguration : IEntityTypeConfiguration<SecurityQuestion>
{
    public void Configure(EntityTypeBuilder<SecurityQuestion> builder)
    {
        builder.ToTable("security_questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuestionText).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);
    }
}
