# Folder Structure

```
src/
├── NotificationService.Api/
│   ├── Program.cs                          # Composition root
│   ├── appsettings.json                    # Configuration
│   ├── Dockerfile                          # Multi-stage build
│   ├── Common/                             # ApiResponse, ResultExtensions
│   ├── Endpoints/                          # Minimal API endpoint definitions
│   │   ├── NotificationsEndpoints.cs
│   │   ├── TemplatesEndpoints.cs
│   │   ├── PreferencesEndpoints.cs
│   │   └── ReleaseEndpoints.cs
│   ├── Grpc/                               # gRPC service implementation
│   ├── Middleware/                         # CorrelationId, ExceptionHandling, Localization, Idempotency
│   └── Protos/                             # Protocol Buffer definitions
├── NotificationService.Application/
│   ├── DependencyInjection.cs              # MediatR, validators, pipeline behaviors
│   ├── Common/
│   │   ├── Behaviors/                      # ValidationBehavior, LoggingBehavior
│   │   ├── Interfaces/                     # Port interfaces (INotificationDbContext, IEmailSender, etc.)
│   │   └── Models/                         # Result<T>, Result, Error, PagedResult<T>
│   └── Features/
│       ├── Notifications/                  # Send, Get, Cancel, Retry, Delete
│       ├── Templates/                      # Create, Get, Update, Delete templates
│       ├── Preferences/                    # Get, Update recipient preferences
│       └── Release/                        # SQA release info
├── NotificationService.Domain/
│   ├── Common/                             # Entity, AggregateRoot, DomainEvent
│   ├── Entities/                           # Notification, NotificationTemplate, RecipientPreference, NotificationLog
│   ├── Enums/                              # NotificationChannel, NotificationPriority, NotificationStatus, TemplateChannel
│   ├── Events/                             # 6 domain events (Created, Sent, Delivered, Failed, DeadLettered, Cancelled)
│   └── Exceptions/                         # DomainException, NotFound, Conflict, InvalidState, Template errors
└── NotificationService.Infrastructure/
    ├── DependencyInjection.cs              # Database, channels, messaging, scheduling registration
    ├── Channels/                            # Email (SmtpEmailSender), SMS (Twilio/GenericHttp), Push (FcmPushSender)
    ├── Localization/                        # ResourceLocalizationService (en, bn)
    ├── Messaging/                           # RabbitMqPublisher, NotificationEventConsumer, HttpUserDirectoryClient
    ├── Migrations/                          # EF Core migrations
    ├── Persistence/                         # NotificationDbContext, entity configurations, Outbox
    ├── Retry/                               # ChannelRetryPolicyFactory (Polly)
    ├── Scheduling/                          # QuartzRegistration, NotificationDispatchJob, StuckNotificationRecoveryJob
    └── Templating/                          # ScribanTemplateRenderer

tests/
├── NotificationService.UnitTests/          # xUnit + FluentAssertions + EF InMemory
├── NotificationService.IntegrationTests/   # Testcontainers (Postgres + RabbitMQ)
└── load-test/
    ├── nbomber/                            # .NET-native load/stress tests
    ├── k6/                                 # Scriptable HTTP load/stress tests
    └── jmeter/                             # JMeter .jmx test plan
```

## Naming Conventions

- Projects: `NotificationService.<Layer>`
- Features: `<FeatureName>` directory with `<Command/Query>`, `<Handler>`, `<Validator>`, `<Dto>`
- Endpoints: `<Feature>Endpoints.cs`
- Entities: PascalCase, singular
- DbSets: PascalCase, plural
