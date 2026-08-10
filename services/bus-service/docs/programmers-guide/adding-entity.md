# Programmer's Guide — Adding an Entity

## Step 1: Domain Entity

```csharp
// Domain/Entities/Driver.cs
public sealed class Driver : Entity
{
    public string Name { get; private set; } = default!;
    public string LicenseNumber { get; private set; } = default!;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    private Driver() { } // EF Core

    public static Driver Create(Guid id, string name, string licenseNumber) =>
        new(id, name, licenseNumber);

    public void SoftDelete(string deletedBy, DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now;
    }
}
```

## Step 2: EF Core Configuration

```csharp
// Infrastructure/Persistence/Configurations/DriverConfiguration.cs
public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers", "bus");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
```

Register in `BusDbContext.OnModelCreating`:

```csharp
modelBuilder.ApplyConfiguration(new DriverConfiguration());
```

## Step 3: Repository Interface

```csharp
// Application/Common/Interfaces/IDriverRepository.cs
public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Driver driver, CancellationToken ct = default);
    // ... other operations
}
```

## Step 4: CQRS Feature

Create `Features/Drivers/CreateDriver/` with `CreateDriverCommand`, `Handler`, `Validator`.

## Step 5: Endpoint

Add route in `BusEndpoints.cs`:

```csharp
buses.MapPost("/drivers", CreateDriverAsync)
    .WithName("CreateDriver")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));
```

## Step 6: Migration

```bash
cd src/BusService.Infrastructure
dotnet ef migrations add AddDriver --startup-project ../BusService.Api
dotnet ef database update --startup-project ../BusService.Api
```
