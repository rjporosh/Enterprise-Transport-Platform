# Notification Service — Architecture

## Overview

The Notification Service is a reusable, production-grade notification platform for the Enterprise Transport Platform. It delivers email, SMS, and push notifications through a provider-abstraction layer, with retry, scheduling, templating, and event-driven consumption.

## Layers

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `NotificationService.Domain` | Aggregates (`Notification`, `NotificationTemplate`, `RecipientPreference`), domain events, enums, exceptions. Zero framework dependencies. |
| Application | `NotificationService.Application` | CQRS commands/queries (MediatR), FluentValidation validators, port interfaces, Result Pattern, pipeline behaviors. |
| Infrastructure | `NotificationService.Infrastructure` | EF Core (multi-provider), outbox + RabbitMQ, SMTP/Twilio/FCM channel senders, Scriban templates, Polly retry, Quartz jobs, Resx localization. |
| Api | `NotificationService.Api` | REST + gRPC endpoints, JWT auth, rate limiting, OpenAPI+Scalar, health checks, OpenTelemetry, middleware pipeline. |

## State Machine

```
Pending ──┐
Scheduled ┼─► Sending ─► Sent ─► Delivered (optional, provider-dependent)
Retrying ─┘        │
                     └─► Failed ─┬─► Retrying (loop, exponential backoff, capped 60min)
                                 └─► DeadLettered (retries exhausted)

Pending/Scheduled/Retrying ─► Cancelled
Any status ─► soft-deleted
```

Every transition is enforced by `Notification.Mark*` methods, which throw `InvalidNotificationStateException` on illegal transitions.

## Why the API Never Calls a Provider Directly

`SendNotificationHandler` inserts a row and enqueues the domain event. The actual send happens asynchronously in `NotificationDispatchJob` (Quartz, every 10s). This:
- Keeps API latency bounded by DB insert time
- Gives all sends the same retry/backoff path
- Never turns a provider outage into a failing API call

`StuckNotificationRecoveryJob` (every 5 min) recovers notifications stuck in `Sending` due to process crashes.

## Database Portability

`Database:Provider` (Postgres | SqlServer | MySql) selects the EF Core provider at startup. Migrations are provider-specific — switching means regenerating them.

Oracle and MongoDB are deliberately not wired.

## Event Consumption

`NotificationEventConsumer` binds to upstream exchanges (Auth, Booking, Payment). It turns domain events into outbound notifications. Auth events carry inline `Email` fields; Booking/Payment events need an Auth Service contact endpoint that does not yet exist — the consumer fails gracefully in that case.

## Retry Layers

1. **In-process Polly** — wraps a single channel-provider call with fast retries for transient failures
2. **State-machine domain retry** — exponential backoff across separate Quartz runs, up to `MaxRetryCount`
