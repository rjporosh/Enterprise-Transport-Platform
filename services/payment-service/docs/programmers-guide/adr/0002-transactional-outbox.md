# ADR-0002: Transactional Outbox Pattern

## Status

Accepted

## Context

Payment events must be published reliably. A naive approach of publishing to RabbitMQ after saving to PostgreSQL can lose events if the process crashes between the two operations.

## Decision

Use the Transactional Outbox pattern:

1. Domain events are serialized to `OutboxMessage` within the same DbContext/SaveChanges as the aggregate change
2. `OutboxProcessor` (BackgroundService) polls for unprocessed outbox messages
3. Messages are published to RabbitMQ durable topic exchange
4. Failed publishes increment retry count and are retried on next poll

## Consequences

- Atomic consistency between payment state and event publication
- No lost financial events
- Automatic retry on transient failures
- Dead-letter handling via max retry count
