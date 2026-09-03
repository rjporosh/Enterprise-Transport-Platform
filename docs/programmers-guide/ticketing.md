# Ticketing Service

`services/ticketing-service` — the bounded context that owns **issued travel
tickets**: number, verification code, QR, PDF, and lifecycle. Booking-service
keeps only the reservation lifecycle.

## Flow

```
booking.confirmed  (booking.events)
   → BookingConfirmedConsumer  (inbox-deduplicated)
   → IssueTicketCommand  (idempotent on BookingId)
       → resolve TicketTemplate (operator's active → platform default → create default)
       → Ticket.Issue()  → TKT-YYMMDD-XXXXXX-C  + opaque verification code
       → QuestPdfTicketRenderer.Render()  → A5 PDF with a QR to the verify URL
       → ticket.issued  (ticket.events)   [notification-service delivers it — M7]
```

## Endpoints

| Method | Route | Auth |
|---|---|---|
| GET | `/api/v1/tickets/verify/{code}` | **public** — gate staff scan the QR |
| GET | `/api/v1/tickets/mine` | customer |
| GET | `/api/v1/tickets/{id}` | owner or Admin/Operator |
| GET | `/api/v1/tickets/{id}/pdf` | owner or Admin/Operator — `application/pdf`, print-ready |
| POST | `/api/v1/tickets/{id}/cancel` | owner or Admin/Operator |
| POST | `/api/v1/tickets/{id}/reissue` | owner or Admin/Operator — **same number**, PDF regenerated |
| GET/POST/PUT | `/api/v1/ticket-templates` | Admin/Operator |
| POST | `/api/v1/ticket-templates/{id}/logo` | Admin/Operator — PNG ≤ 512 KB, multipart `file` |

The gateway already routes `/api/v1/tickets/**` and `/api/v1/ticket-templates/**`
to the `ticketing` cluster.

## Templates

Layout/branding is **data, not a cloned image**: `Name`, `BrandName`,
`PrimaryColorHex`, `AccentColorHex`, `TermsText`, `FooterText`, optional PNG
logo. One template per operator is `IsDefault`; the platform default has
`OperatorId = Guid.Empty` and is auto-created on first use (seed it explicitly
in production).

## Ticket number

`TKT-YYMMDD-XXXXXX-C` — 6 random base32 chars + a checksum char.
`TicketNumber.IsValid()` catches a mistyped number before a DB lookup. Reissue
(reprint) keeps the number and the verification code.

## Config

| Key | Meaning |
|---|---|
| `ConnectionStrings:TicketingDb` | Postgres (schema `ticketing`). |
| `Database:Provider` | Postgres \| SqlServer \| MySql. |
| `RabbitMq:*` | broker for the consumer + outbox. |
| `Ticketing:PublicBaseUrl` | origin the QR / PDF links resolve against (the gateway, e.g. `http://localhost:8088`). |

## Run / migrate

```bash
dotnet ef database update \
  --project services/ticketing-service/src/TicketingService.Infrastructure \
  --startup-project services/ticketing-service/src/TicketingService.Api
dotnet run --project services/ticketing-service/src/TicketingService.Api   # :5801 locally, :5205 in compose
```

QuestPDF runs under the **Community licence** (set in `Program.cs`). Its only
native runtime deps are `libfontconfig1` + `libfreetype6` (installed in the
Dockerfile).
