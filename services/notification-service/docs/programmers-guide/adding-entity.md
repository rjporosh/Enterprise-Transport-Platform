# Adding a New Entity

## Steps

1. **Domain**: Create the entity in `Domain/Entities/`
   - Inherit from `AggregateRoot` (if aggregate) or `Entity`
   - Use private constructor + static `Create()` factory
   - Add domain methods that enforce business rules
   - Raise domain events via `Raise(new SomeDomainEvent(...))`

2. **DbContext**: Add `DbSet<T>` in `NotificationDbContext` and create an `IEntityTypeConfiguration<T>` in `Persistence/Configurations/`

3. **Migration**: 
```bash
dotnet ef migrations add Add<EntityName> --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api
dotnet ef database update --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api
```

4. **Application**: Create command/query handlers, validators, and DTOs in `Application/Features/<EntityName>s/`

5. **API**: Map endpoints in `Api/Endpoints/<EntityName>Endpoints.cs`

6. **Tests**: Add unit tests (EF InMemory) and integration tests (Testcontainers)

## Example: Adding a NotificationTag entity

```csharp
// Domain/Entities/NotificationTag.cs
public sealed class NotificationTag : Entity
{
    public Guid NotificationId { get; private set; }
    public string Tag { get; private set; } = default!;
    // ...
}
```

```csharp
// Persistence/Configurations/NotificationTagConfiguration.cs
public class NotificationTagConfiguration : IEntityTypeConfiguration<NotificationTag>
{
    public void Configure(EntityTypeBuilder<NotificationTag> builder)
    {
        builder.ToTable("notification_tags", "notification");
        builder.HasKey(t => t.Id);
        // ...
    }
}
```

## Soft Delete Convention

If the entity should support soft delete:
- Add `public bool IsDeleted { get; private set; }`
- Add `public void SoftDelete(DateTimeOffset nowUtc)` method
- Add query filter in `NotificationDbContext.OnModelCreating`:
  ```csharp
  modelBuilder.Entity<NotificationTag>().HasQueryFilter(t => !t.IsDeleted);
  ```

## Concurrency

For entities that need optimistic concurrency:
- Add `public byte[] RowVersion { get; private set; } = Array.Empty<byte>();`
- Configure in entity configuration:
  ```csharp
  builder.Property(t => t.RowVersion).IsRowVersion();
  ```
