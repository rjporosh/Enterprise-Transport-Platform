# Payment Service - Programmer Guide

## Project Structure

```
services/payment-service/
├── PaymentService.sln
├── src/
│   ├── PaymentService.Domain/         # Entities, value objects, domain events, exceptions
│   ├── PaymentService.Application/   # CQRS, validators, DTOs, interfaces, behaviors
│   ├── PaymentService.Infrastructure/ # EF Core, RabbitMQ, Redis, metrics, providers
│   └── PaymentService.Api/            # Minimal API endpoints, middleware, auth
├── tests/
│   ├── PaymentService.UnitTests/
│   └── PaymentService.IntegrationTests/
├── performance-tests/
│   ├── k6/
│   ├── jmeter/
│   └── nbomber/
└── docs/programmers-guide/
```

## Creating a New Payment Feature

1. Create a folder under `Features/Payments/{FeatureName}/`
2. Add `{FeatureName}Command.cs` or `{FeatureName}Query.cs`
3. Add `{FeatureName}Handler.cs`
4. Add `{FeatureName}Validator.cs` (if input validation needed)
5. Register in `PaymentEndpoints.cs`
6. Add domain events if state changes

## Database Migrations

```bash
dotnet ef migrations add InitialCreate --project src/PaymentService.Infrastructure --startup-project src/PaymentService.Api
dotnet ef database update --project src/PaymentService.Infrastructure --startup-project src/PaymentService.Api
```

## Adding a Payment Provider

1. Create a new class implementing `IPaymentProvider` in `src/PaymentService.Infrastructure/Providers/`
2. Register the provider type in `PaymentProviderFactory._providerTypes`
3. Register the provider as a singleton in `DependencyInjection.cs`
4. Add provider-specific options class (e.g. `BkashOptions`) and register with `services.Configure<T>`
5. Configure via `appsettings.json` under the provider name section (e.g. `Bkash:AppKey`)
6. If the provider has webhooks, update `ProcessWebhookHandler` to parse provider-specific payloads

### bKash Setup

1. Register as a merchant on [bKash Developer Portal](https://developer.bkash.com/)
2. Obtain `AppKey`, `AppSecret`, `Username`, `Password` from the sandbox/production dashboard
3. Set `Bkash:BaseUrl` to `https://tokenized.sandbox.bka.sh/v1.2.0-beta` (sandbox) or production URL
4. Set `Bkash:CallbackUrl` to your publicly reachable HTTPS endpoint (e.g. `https://api.yourdomain.com/api/v1/webhooks/bkash`)
5. The provider uses `merchantInvoiceNumber` = payment `IdempotencyKey` for idempotent charge creation
6. Configure `Bkash:WebhookSecret` for webhook signature verification

### Nagad Setup

1. Register as a merchant on [Nagad Developer Portal](https://nagad.com.bd/developer-portal)
2. Obtain `MerchantId` and `SecretKey` from the sandbox/production dashboard
3. Set `Nagad:BaseUrl` to `https://api-sandbox.nagad.com.bd` (sandbox) or production URL
4. Set `Nagad:CallbackUrl` to your publicly reachable HTTPS endpoint (e.g. `https://api.yourdomain.com/api/v1/webhooks/nagad`)
5. Configure `Nagad:WebhookSecret` for webhook signature verification

### Stripe Setup (Card Processing)

1. Create a Stripe account at https://stripe.com
2. Obtain `SecretKey` (sk_test_... or sk_live_...) and `PublishableKey` (pk_test_... or pk_live_...)
3. Set `Stripe:BaseUrl` to `https://api.stripe.com/v1`
4. Set `Stripe:WebhookSecret` from Stripe Dashboard → Webhooks → Endpoint secret
5. The provider creates PaymentIntents with `payment_method_types: ["card"]`
6. Frontend should use Stripe.js/Elements with the `client_secret` returned in `RawResponse`

### Webhook Signature Verification

All webhook endpoints (`/api/v1/webhooks/{providerName}`) verify signatures before processing:

| Provider | Header | Verification Method |
|----------|--------|---------------------|
| bKash | `X-Bkash-Signature` | HMAC-SHA256 of payload using `Bkash:WebhookSecret` |
| Nagad | `X-Nagad-Signature` | HMAC-SHA256 of payload using `Nagad:WebhookSecret` |
| Stripe | `Stripe-Signature` | Stripe `t=timestamp,v1=signature` format with 5-minute tolerance |

To disable verification in development, leave the `WebhookSecret` empty — the provider will reject all webhooks.

## Agent / Merchant / Personal Payment Methods

Agents, merchants, and personal users can register their bank accounts, bKash numbers, or Nagad numbers to receive payments. This is managed via the `AgentPaymentMethod` entity and the `/api/v1/agents/{agentId}/payment-methods` endpoints.

### How an Agent Adds a Payment Method

1. **Caller authenticates** with JWT token having audience `payment-service`
2. **POST** `/api/v1/agents/{agentId}/payment-methods` with:
   ```json
   {
     "agentId": "uuid-of-agent",
     "methodType": "Bkash",
     "provider": "Bkash",
     "accountNumber": "017XXXXXXXXX",
     "accountName": "Agent Name",
     "metadata": "{\"branch\":\"dhaka\"}"
   }
   ```
3. The service validates the input and creates the payment method record
4. If the agent has no default method, this is automatically set as default

### Setting a Default Payment Method

```bash
POST /api/v1/agents/{agentId}/payment-methods/{paymentMethodId}/set-default
```

This unsets any previous default and marks the specified method as default.

### Verifying a Payment Method

Some providers (e.g. bKash) require verification before payouts:

```bash
POST /api/v1/agents/{agentId}/payment-methods/{paymentMethodId}/verify
{
  "verificationToken": "otp-or-token-from-provider"
}
```

### Listing Payment Methods

```bash
GET /api/v1/agents/{agentId}/payment-methods?onlyVerified=true&page=1&pageSize=20
```

### Getting the Default Method

```bash
GET /api/v1/agents/{agentId}/payment-methods/default
```

### Supported Method Types

| Enum Value | Description | Notes |
|------------|-------------|-------|
| `Bkash` | bKash mobile banking | Requires bKash merchant account |
| `Nagad` | Nagad mobile banking | Coming soon |
| `BankTransfer` | Direct bank account | Use provider name as bank name (e.g. `Dutch Bangla`, `City Bank`) |
| `Card` | Credit/Debit card | Processed via Stripe or local acquirer |
| `Cash` | Cash on delivery / counter | No digital payout |

### Database Schema

`agent_payment_methods` table (schema: `payment`):

| Column | Type | Notes |
|--------|------|-------|
| `Id` | uuid | PK |
| `AgentId` | uuid | FK to agent/user |
| `MethodType` | varchar(30) | `Bkash`, `Nagad`, `BankTransfer`, `Card`, `Cash` |
| `Provider` | varchar(50) | e.g. `Bkash`, `Nagad`, `Dutch Bangla` |
| `AccountNumber` | varchar(100) | Phone number or account number |
| `AccountName` | varchar(200) | Display name |
| `IsDefault` | boolean | Whether this is the default payout method |
| `IsVerified` | boolean | Whether the provider confirmed ownership |
| `VerificationToken` | varchar(200) | OTP/token from verification flow |
| `Metadata` | text | JSON for extra provider-specific data |
| `CreatedAtUtc` | timestamptz | |
| `UpdatedAtUtc` | timestamptz | |

Unique constraint on `(AgentId, Provider, AccountNumber)` prevents duplicate registrations.

### Security Considerations

- Never log full account numbers in production — mask them in logs
- Verify ownership before marking `IsVerified = true`
- Use HTTPS for all API calls
- Store bKash/Nagad credentials in secure vault (e.g. AWS Secrets Manager, Azure Key Vault), not in `appsettings.json`
- Webhook endpoints must validate provider signatures (bKash `WebhookSecret`, Nagad HMAC)

## Event Catalog

| Event | Routing Key | Consumer |
|-------|-------------|----------|
| PaymentCreatedDomainEvent | payment.created | Notification Service |
| PaymentProcessingDomainEvent | payment.processing | Internal |
| PaymentSucceededDomainEvent | payment.succeeded | Notification, Booking |
| PaymentFailedDomainEvent | payment.failed | Notification |
| PaymentCancelledDomainEvent | payment.cancelled | Notification |
| PaymentRefundedDomainEvent | payment.refunded | Notification, Booking |

## Debugging

1. Check `logs/runtime-error-logs/` for startup/dependency failures
2. Check `logs/exception-logs/` for application exceptions
3. Check `logs/query-logs/` for slow database queries
4. Use CorrelationId from response header to trace across services
5. OpenTelemetry traces available at Jaeger (`http://localhost:16686`)
