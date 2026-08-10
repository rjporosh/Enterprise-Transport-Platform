# ADR-0003: Payment Provider Abstraction

## Status

Accepted

## Context

Payment providers may change over time (Stripe, PayPal, Braintree, etc.). Business logic must not depend on a specific provider.

## Decision

Use `IPaymentProvider` abstraction with `IPaymentProviderFactory`:

```text
IPaymentProviderFactory
    ├── DefaultPaymentProvider
    ├── StripeProvider
    └── PayPalProvider
```

Provider selection is configuration-driven via `Payment:Provider` setting.

## Consequences

- Business logic is provider-agnostic
- New providers can be added without modifying handlers
- Circuit breaker per provider
- Graceful fallback to default provider
