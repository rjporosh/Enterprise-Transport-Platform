# ADR-0001: Payment Service State Machine

## Status

Accepted

## Context

Payment processing requires a robust state machine to prevent invalid transitions and ensure financial consistency. The system must handle creation, processing, success, failure, cancellation, and refund states safely.

## Decision

Implement a strict state machine in the Payment aggregate root:

```
Pending → Processing → Succeeded → PartiallyRefunded → Refunded
    ↓         ↓            ↓
  Failed    Failed      Failed
    ↓         ↓
  Cancelled  Cancelled
```

- Invalid transitions throw `InvalidPaymentStateTransitionException`
- `PartiallyRefunded` is a distinct state from `Succeeded`
- `Refunded` is terminal (no further refunds allowed)
- All transitions raise domain events

## Consequences

- Clear payment lifecycle
- Prevents accidental state corruption
- Audit trail via domain events
- Idempotent state transitions
