# Auth Service Programmer Guide

## Project Structure

```
services/auth-service/
├── src/
│   ├── AuthService.Api/                  # HTTP + gRPC entry point
│   │   ├── Endpoints/                    # Minimal API route definitions
│   │   ├── Middleware/                   # Correlation, exception handling
│   │   ├── Security/                     # CurrentUser, client info
│   │   └── Program.cs                    # Startup
│   ├── AuthService.Application/          # CQRS handlers, validators
│   │   ├── Common/
│   │   │   ├── Interfaces/               # Abstractions (IAuthDbContext, ITokenService, etc.)
│   │   │   ├── Models/                   # Result, TokenPairDto
│   │   │   └── Behaviors/                # Pipeline behaviors
│   │   └── Features/                     # Feature folders (Auth, Admin, System)
│   ├── AuthService.Domain/               # Pure domain, no dependencies
│   │   ├── Entities/                     # User, Role, Permission, etc.
│   │   ├── Enums/                        # AuditAction, UserStatus
│   │   ├── Exceptions/                   # Domain exceptions
│   │   ├── Events/                       # Domain events
│   │   └── Common/                       # Entity, AggregateRoot, DomainEvent
│   └── AuthService.Infrastructure/       # EF Core, JWT, Redis, RabbitMQ
│       ├── Persistence/                  # DbContext, configurations, outbox
│       ├── Security/                     # JwtTokenService, PasswordHasher
│       ├── Services/                     # OtpService, EmailSender, etc.
│       ├── Messaging/                    # RabbitMQ publisher
│       ├── Caching/                      # Redis cache
│       └── Observability/                # Metrics
├── tests/
│   ├── AuthService.UnitTests/            # xUnit + FluentAssertions
│   └── AuthService.IntegrationTests/     # Testcontainers-based E2E
└── docs/                                 # Documentation
```

## Creating a New Feature

1. **Domain**: Add entity/enum/exception in `Domain/`
2. **Application**: Create `Features/{FeatureName}/` with:
   - `{Command|Query}.cs` (MediatR request)
   - `{Command|Query}Handler.cs` (handler)
   - `{Command|Query}Validator.cs` (FluentValidation)
3. **Infrastructure**: Add DbSet in `AuthDbContext`, add configuration, add services
4. **API**: Add endpoint in `AuthEndpoints.cs`
5. **Tests**: Add unit tests in `tests/AuthService.UnitTests/`

## Adding an Entity

1. Create entity in `Domain/Entities/`
2. Add DbSet to `IAuthDbContext` and `AuthDbContext`
3. Create configuration in `Infrastructure/Persistence/Configurations/`
4. Run `dotnet ef migrations add Add{Entity}`

## Database Provider Switching

Edit `appsettings.json`:
```json
{
  "Database": { "Provider": "Postgres" },
  "ConnectionStrings": { "AuthDb": "..." }
}
```

Supported: `Postgres`, `SqlServer`, `MySql`

## Password History

The last 3 password hashes are stored in `password_histories`. Use `IPasswordHistoryValidator`:
- `IsPasswordReusedAsync(userId, password)` — returns true if password matches any of last 3
- `RecordPasswordAsync(userId, passwordHash)` — records new hash and prunes excess

## Security Questions

- Users must configure 3-5 questions
- Answers are normalized: trimmed, lowercased, SHA-256 hashed
- Verification uses `ISecurityAnswerValidator.VerifyAnswersAsync(userId, questionAnswers)`

## OTP

- 6-digit code, 5-minute lifetime
- Max 5 verification attempts
- Max 3 resends per hour
- Hashed before storage (SHA-256)
- Channels: `email`, `sms`

## Events

Domain events implement `INotification` (MediatR) and are persisted to the outbox table. `OutboxProcessor` polls and publishes to RabbitMQ.

## Testing

```bash
# Unit tests
dotnet test tests/AuthService.UnitTests/

# Integration tests (requires Docker)
dotnet test tests/AuthService.IntegrationTests/
```

## Migration Commands

```bash
cd services/auth-service
dotnet ef migrations add <Name> --project src/AuthService.Infrastructure/AuthService.Infrastructure.csproj
dotnet ef database update --project src/AuthService.Infrastructure/AuthService.Infrastructure.csproj
```
