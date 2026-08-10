# Creating CRUD

## Pattern

Every feature follows this structure:

```
Features/
└── <FeatureName>/
    ├── Create<Feature>/
    │   ├── Create<Feature>Command.cs
    │   ├── Create<Feature>CommandHandler.cs
    │   ├── Create<Feature>Validator.cs
    │   └── <Feature>Dto.cs
    ├── Get<Feature>ById/
    │   ├── Get<Feature>ByIdQuery.cs
    │   ├── Get<Feature>ByIdHandler.cs
    │   └── <Feature>Dto.cs
    ├── Get<Features>/
    │   ├── Get<Features>Query.cs
    │   ├── Get<Features>Handler.cs
    │   ├── Get<Features>Validator.cs
    │   └── <Feature>Dto.cs
    ├── Update<Feature>/
    │   ├── Update<Feature>Command.cs
    │   ├── Update<Feature>CommandHandler.cs
    │   ├── Update<Feature>Validator.cs
    ├── Delete<Feature>/
    │   └── Delete<Feature>CommandHandler.cs
```

## Example: Creating a Template

1. Define the command record:
```csharp
public sealed record CreateTemplateCommand(...) : IRequest<Result<TemplateDto>>;
```

2. Implement the handler:
```csharp
public sealed class CreateTemplateHandler : IRequestHandler<CreateTemplateCommand, Result<TemplateDto>>
{
    private readonly INotificationDbContext _dbContext;
    // ...
    public async Task<Result<TemplateDto>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        // business logic
        var template = NotificationTemplate.Create(...);
        _dbContext.NotificationTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<TemplateDto>.Success(ToDto(template));
    }
}
```

3. Add FluentValidation validator:
```csharp
public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        // ...
    }
}
```

4. Register endpoint in the appropriate `*Endpoints.cs`:
```csharp
group.MapPost("/", CreateAsync)
    .WithName("CreateTemplate")
    .Produces<ApiResponse<TemplateDto>>(StatusCodes.Status201Created);
```

## Soft Delete

Use `entity.SoftDelete(nowUtc)` instead of `_dbContext.Remove(entity)`. Global query filters exclude soft-deleted rows by default.

## Result Pattern

Handlers return `Result<T>` or `Result` for expected business failures. Throw exceptions only for truly exceptional cases (infrastructure failures). The centralized exception handler maps domain exceptions to HTTP responses.
