using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class UserSecurityQuestionConfiguration : IEntityTypeConfiguration<UserSecurityQuestion>
{
    public void Configure(EntityTypeBuilder<UserSecurityQuestion> builder)
    {
        builder.ToTable("user_security_questions");
        builder.HasKey(x => new { x.UserId, x.SecurityQuestionId });
        builder.Property(x => x.ConfiguredAtUtc).IsRequired();
    }
}
