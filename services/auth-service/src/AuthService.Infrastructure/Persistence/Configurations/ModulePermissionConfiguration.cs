using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public sealed class ModulePermissionConfiguration : IEntityTypeConfiguration<ModulePermission>
{
    public void Configure(EntityTypeBuilder<ModulePermission> builder)
    {
        builder.ToTable("module_permissions");
        builder.HasKey(x => new { x.ModuleId, x.PermissionId });
        builder.Property(x => x.AssignedAtUtc).IsRequired();
    }
}
