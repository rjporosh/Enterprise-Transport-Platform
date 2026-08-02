using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        // Unlike Booking Service's Trip/Booking aggregates (which race on seat
        // holds and need Postgres `xmin` optimistic concurrency), User writes
        // are single-actor (the user themselves) so the collision window that
        // Version exists to protect is negligible here. Left unmapped rather
        // than faked with a provider-specific column, since this service must
        // run identically on Postgres/SqlServer/MySQL — see
        // docs/architecture/auth-service-architecture.md, "Database portability".
        builder.Ignore(x => x.Version);

        // EF Core's conventions treat any public collection-of-a-class
        // property as a candidate navigation property by default — including
        // DomainEvents (inherited from AggregateRoot), which is an in-memory-only
        // list of transient events, never a persisted relationship. Without this,
        // EF Core tries to map the abstract DomainEvent type as an entity with
        // no primary key and throws at startup ("The entity type 'DomainEvent'
        // requires a primary key to be defined") the first time the model is
        // built — i.e. on the very first request or migration, not at compile
        // time, which is why this survived a clean build. TestAuthDbContext
        // (used by the unit tests) already had this Ignore; this is the real
        // AuthDbContext catching up to match it.
        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
