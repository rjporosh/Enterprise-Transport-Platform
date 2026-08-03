using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // Fixed, well-known IDs so seeded roles are identical (and referenceable)
    // across every environment — dev, CI, staging, prod all get the same GUID
    // for "Customer", never a randomly generated one per migration run.
    private static readonly Guid CustomerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OperatorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AdminRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(200);

        builder.HasData(
            new { Id = CustomerRoleId, Name = Role.WellKnown.Customer, Description = "Default role for self-registered end users who search and book trips." },
            new { Id = OperatorRoleId, Name = Role.WellKnown.Operator, Description = "Bus operator staff — manages routes, buses, and trips for their fleet." },
            new { Id = AdminRoleId, Name = Role.WellKnown.Admin, Description = "Platform administrator — full access across all services." });
    }
}
