# Integrating with Other Services

This guide explains how other services (Auth, Booking, Payment, Bus, Route) can integrate with the Notification Service.

## Three Integration Patterns

The Notification Service supports three ways to send notifications:

### 1. REST API (Simplest)

Direct HTTP call from any service.

```bash
POST /api/v1/notifications
Content-Type: application/json

{
  "recipient": "user@example.com",
  "channel": "Email",
  "subject": "Booking Confirmed",
  "body": "Your booking {{bookingId}} is confirmed.",
  "priority": "Normal",
  "isTransactional": true
}
```

**When to use**: External callers, frontend, admin console, simple service-to-service.

### 2. gRPC (Synchronous, Low-Latency)

Internal service-to-service call when the caller needs an immediate acknowledgement.

```csharp
var channel = GrpcChannel.ForAddress("http://notification-service:5301");
var client = new NotificationGrpcService.NotificationGrpcServiceClient(channel);

var response = await client.SendNotificationAsync(new SendNotificationRequest
{
    Recipient = "user@example.com",
    Channel = GrpcChannel.Email,
    Subject = "Booking Confirmed",
    Body = "Your booking is confirmed.",
    Priority = GrpcPriority.Normal,
    IsTransactional = true
});
```

**When to use**: Booking/Payment/Auth services need to know the notification was queued before proceeding.

### 3. RabbitMQ (Asynchronous, Event-Driven)

Fire-and-forget via domain events. The upstream service publishes its own event; Notification Service consumes it.

**When to use**: Fully decoupled, no immediate acknowledgement needed, high throughput.

## Step-by-Step: REST Integration

### Step 1: Identify the notification trigger

Example: Booking Service creates a booking → send confirmation email.

### Step 2: Call the Notification Service API

```csharp
// In BookingService.Application/Features/Bookings/CreateBooking/CreateBookingHandler.cs
public async Task<Result<BookingDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
{
    // ... create booking ...
    
    // Send notification (fire-and-forget or await)
    var notificationResult = await _mediator.Send(new SendNotificationCommand(
        Recipient: booking.CustomerEmail,
        Channel: NotificationChannel.Email,
        TemplateKey: "booking.confirmed",
        TemplateVariables: new Dictionary<string, object?>
        {
            ["bookingId"] = booking.Id.ToString(),
            ["travelDate"] = booking.TravelDate.ToString("yyyy-MM-dd")
        },
        Subject: null,
        Body: null,
        DataPayload: null,
        RecipientId: booking.CustomerId.ToString(),
        SourceReference: $"booking:{booking.Id}",
        Locale: "en",
        Priority: NotificationPriority.High,
        ScheduledForUtc: null,
        MaxRetryCount: 3,
        IsTransactional: true
    ), cancellationToken);
    
    // ... return booking ...
}
```

### Step 3: Handle the response

```csharp
if (!notificationResult.IsSuccess)
{
    _logger.LogWarning("Booking confirmation notification failed: {Errors}", 
        string.Join(", ", notificationResult.Errors.Select(e => e.Message)));
    // Don't fail the booking creation — notifications are side effects
}
```

## Step-by-Step: RabbitMQ Integration

### Step 1: Publish your domain event

In the upstream service (e.g., Booking Service), publish a domain event through your outbox:

```csharp
// BookingConfirmedDomainEvent
public sealed record BookingConfirmedDomainEvent(
    Guid BookingId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    DateTimeOffset TravelDate
) : DomainEvent;
```

### Step 2: Ensure your event is published to RabbitMQ

The upstream service's `OutboxProcessor` relays the event to its own topic exchange (e.g., `booking.events`).

### Step 3: Configure Notification Service to consume it

In `appsettings.json`:

```json
{
  "RabbitMq": {
    "UpstreamBindings": [
      { "Exchange": "booking.events", "RoutingKey": "booking.confirmed" }
    ]
  }
}
```

### Step 4: Add routing map entry

In `NotificationEventConsumer.cs`, add to `RoutingKeyMap`:

```csharp
["booking.confirmed"] = ("booking.confirmed", NotificationChannel.Email),
```

### Step 5: Create the template

```bash
POST /api/v1/templates
{
  "key": "booking.confirmed",
  "channel": "Email",
  "locale": "en",
  "name": "Booking Confirmed",
  "subject": "Booking {{bookingId}} confirmed",
  "body": "Hi {{customerName}}, your booking {{bookingId}} is confirmed for {{travelDate}}."
}
```

The consumer automatically flattens the event payload into template variables (`customerName`, `bookingId`, `travelDate`).

## Step-by-Step: gRPC Integration

### Step 1: Add the Notification Service proto to your service

Copy `notification.proto` to your service's `Protos/` folder or reference it as a shared dependency.

### Step 2: Create the gRPC client

```csharp
// In your service's Infrastructure or Communication layer
public class NotificationGrpcClient
{
    private readonly NotificationGrpcService.NotificationGrpcServiceClient _client;
    
    public NotificationGrpcClient(NotificationGrpcService.NotificationGrpcServiceClient client)
    {
        _client = client;
    }
    
    public async Task<SendNotificationResponse> SendAsync(SendNotificationRequest request, CancellationToken ct)
    {
        return await _client.SendNotificationAsync(request, cancellationToken: ct);
    }
}
```

### Step 3: Register in DI

```csharp
services.AddGrpcClient<NotificationGrpcService.NotificationGrpcServiceClient>(options =>
{
    options.Address = new Uri(configuration["NotificationService:GrpcUrl"] ?? "http://localhost:5301");
});
```

### Step 4: Use in your handler

```csharp
var response = await _notificationGrpcClient.SendAsync(new SendNotificationRequest
{
    Recipient = customerEmail,
    Channel = GrpcChannel.Email,
    Subject = "Payment Receipt",
    Body = "Your payment of {{amount}} was successful.",
    Priority = GrpcPriority.High,
    IsTransactional = true
});
```

## Cross-Service Communication Rules

Per the platform's architecture rules:

- **Never** access another service's database directly
- **Never** share EF Core entities across service boundaries
- **Always** use HTTP, gRPC, or RabbitMQ for inter-service communication
- **Always** propagate `CorrelationId` and `TraceId` across service boundaries

## Recipient Resolution

### For Auth Service Events

Auth Service events (`user.registered`, `password.changed`) carry `Email` inline. The consumer uses it directly.

### For Booking/Payment Service Events

Booking/Payment events carry only `CustomerId`. The consumer needs to resolve the email/phone:

1. **Inline contact fields** (preferred): Include `Email`/`PhoneNumber` in the event payload
2. **User Directory lookup**: Call `GET /api/v1/users/{id}/contact` on Auth Service (endpoint not yet implemented)

### Step-by-Step: Include Inline Contact in Events

In Booking Service's `BookingConfirmedDomainEvent`:

```csharp
public sealed record BookingConfirmedDomainEvent(
    Guid BookingId,
    Guid CustomerId,
    string CustomerEmail,      // <-- add this
    string CustomerPhoneNumber, // <-- add this
    string CustomerName,
    DateTimeOffset TravelDate
) : DomainEvent;
```

The consumer's `ExtractRecipient` method will pick it up automatically:

```csharp
private static string? ExtractRecipient(JsonElement root, NotificationChannel channel, out string? recipientId)
{
    recipientId = TryGetString(root, "UserId") ?? TryGetString(root, "CustomerId");
    return channel switch
    {
        NotificationChannel.Sms => TryGetString(root, "PhoneNumber") ?? TryGetString(root, "Phone"),
        _ => TryGetString(root, "Email")
    };
}
```
