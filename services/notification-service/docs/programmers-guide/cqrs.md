# CQRS

## Overview

The Notification Service uses CQRS via MediatR to separate reads from writes.

- **Commands** change state: `SendNotificationCommand`, `CancelNotificationCommand`, `RetryNotificationCommand`, `CreateTemplateCommand`, etc.
- **Queries** retrieve state: `GetNotificationsQuery`, `GetNotificationByIdQuery`, `GetTemplatesQuery`, etc.

## Pipeline

```
Request
  ↓
ValidationBehavior    ← runs all FluentValidation validators, collects ALL errors
  ↓
LoggingBehavior       ← logs request with execution time
  ↓
Handler               ← command or query handler
  ↓
Result<T> / Result    ← returned to endpoint
```

## Adding a New Command

1. Create the command record implementing `IRequest<Result<T>>` or `IRequest<Result>`
2. Create the handler implementing `IRequestHandler<TCommand, TResult>`
3. Create a FluentValidation validator
4. Register the endpoint

## Adding a New Query

Same as command, but:
- Query handlers should not modify state
- Use `AsNoTracking()` for read-only queries
- Apply pagination, filtering, and search at the database layer

## Result Pattern

Handlers return `Result<T>` for expected failures:
```csharp
if (errors.Any())
    return Result<T>.Failure(errors);
return Result<T>.Success(value);
```

Multiple errors are collected and returned together — never stop at the first.
