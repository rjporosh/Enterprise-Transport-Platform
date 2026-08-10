# Auth Service Architecture

## Overview

Auth Service is a standalone, production-grade authentication and authorization microservice built with ASP.NET Core. It serves as the identity provider for the entire Enterprise Transport Platform.

## Layers

| Layer | Project | Responsibility |
|-------|---------|----------------|
| API | `AuthService.Api` | HTTP endpoints, gRPC, middleware, health checks |
| Application | `AuthService.Application` | CQRS handlers, validators, business rules |
| Domain | `AuthService.Domain` | Entities, value objects, domain events, exceptions |
| Infrastructure | `AuthService.Infrastructure` | EF Core persistence, JWT, Redis, RabbitMQ, email/SMS |

## Technology Stack

- **Runtime**: .NET 10
- **Framework**: ASP.NET Core Minimal APIs
- **Identity**: ASP.NET Core Identity patterns (custom implementation)
- **Authentication**: JWT Bearer access tokens + opaque refresh tokens
- **Authorization**: Role-based + permission-based
- **Database**: PostgreSQL (primary), SQL Server, MySQL supported
- **ORM**: Entity Framework Core 10
- **Messaging**: RabbitMQ (outbox pattern)
- **Cache**: Redis (token denylist, role caching)
- **Observability**: Serilog, OpenTelemetry, Prometheus
- **Validation**: FluentValidation
- **Mediator**: MediatR (CQRS)

## Authentication Flow

```
Client -> Gateway -> Auth Service
    |
    +-- POST /api/v1/auth/register
    +-- POST /api/v1/auth/login
    |       Returns: access_token + refresh_token
    +-- POST /api/v1/auth/refresh
    |       Returns: new access_token + refresh_token
    +-- POST /api/v1/auth/logout
    +-- POST /api/v1/auth/change-password
    +-- POST /api/v1/auth/forgot-password
    +-- POST /api/v1/auth/reset-password
    +-- POST /api/v1/auth/otp/request
    +-- POST /api/v1/auth/otp/verify
    +-- POST /api/v1/auth/security-questions/configure
    +-- POST /api/v1/auth/security-questions/verify
```

## Token Strategy

- **Access Token**: Short-lived (15 min), JWT, contains user claims and roles
- **Refresh Token**: Long-lived (30 days), opaque, stored hashed in DB
- **Token Rotation**: Every refresh issues a new token pair and revokes the old one
- **Revocation**: Refresh tokens are revoked on logout; access tokens use Redis denylist for active revocation

## Security

- Password hashing: PBKDF2 (HMAC-SHA256, 100k iterations, 16-byte salt, 32-byte key)
- Refresh token hashing: SHA-256
- OTP hashing: SHA-256
- Security answer hashing: SHA-256
- Account lockout: 5 failed attempts
- Rate limiting: login, OTP, password reset endpoints
- Correlation ID: propagated through all requests
- IP tracing: login, logout, failed login, OTP, password operations

## Multi-Tenancy

Entities support:
- `TenantId`
- `CompanyId`
- `OrganizationId`
- `BranchId`

Tenant context is derived from authenticated identity claims, never from client input.
