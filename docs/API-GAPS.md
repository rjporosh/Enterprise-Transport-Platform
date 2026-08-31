# API Gap Register — Enterprise Transport Platform

**Audit date:** 2026-08-31. **Commit:** `73081634`. **Mode:** audit only (no code changed).

This is the single source of truth for endpoint-level gaps. It **supersedes** the inline
mock-comment "documentation" in
`apps/angular-client/bus-ticketing-customer-web/src/app/core/interceptors/mock-api.interceptor.ts`
and `apps/react-admin/bus-ticketing-admin/src/api/mockAdapter.ts`.

Legend — **Status:** `real` = genuinely implemented & wired · `real/unsafe` = implemented
but has a P0/P1 security or correctness defect · `mock-only` = frontend serves it from
in-app fixtures because no backend exists · `missing` = neither frontend nor backend.
**Auth:** as enforced in code today. **Idem** = `Idempotency-Key` honoured.
Finding IDs (`P0-n` …) refer to `PRODUCTION-GAP-ANALYSIS.md`.

---

## 1. auth-service — `services/auth-service/src/AuthService.Api/Endpoints/AuthEndpoints.cs`

| Method / path | Auth | Status | Idem | OpenAPI | Notes / gap |
|---|---|---|---|---|---|
| POST `/api/v1/auth/register` | anon, `auth-write` RL | real | no | yes (Scalar) | Frontend sends `firstName`/`lastName` (fixed in 2026-08-20 pass). |
| POST `/api/v1/auth/login` | anon, RL | real | no | yes | Returns `TokenPairResponse`. |
| POST `/api/v1/auth/refresh` | anon, RL | real | no | yes | Rotation + theft detection. **Neither SPA calls it — P1-19.** |
| POST `/api/v1/auth/logout` | bearer | real | no | yes | |
| GET `/api/v1/auth/me` | bearer | real | – | yes | `UserDto`. JWT carries no tenant/permission claim — P2-1. |
| POST `/api/v1/auth/change-password` | bearer | real | no | yes | Password-history enforced. |
| POST `/api/v1/auth/forgot-password` | anon, RL | real | no | yes | |
| POST `/api/v1/auth/reset-password` | anon, RL | real | no | yes | |
| POST `/api/v1/auth/otp/request` | anon, RL | real | no | yes | **Customer SPA has no OTP UI — P1-20.** |
| POST `/api/v1/auth/otp/verify` | anon, RL | real | no | yes | en/bn resources exist. |
| POST `/api/v1/auth/security-questions/configure` | bearer | real | no | yes | |
| POST `/api/v1/auth/security-questions/verify` | anon, RL | real | no | yes | |
| GET `/api/v1/auth/audit-logs` | bearer + role `Admin` | real | – | yes | |
| GET `/api/v1/auth/release-info` | anon | real | – | yes | |
| POST/PUT/DELETE/GET `/api/v1/admin/permissions` | bearer + `Admin` | real | no | yes | |
| POST/PUT/DELETE/GET `/api/v1/admin/modules` | bearer + `Admin` | real | no | yes | |
| POST/PUT/GET `/api/v1/admin/roles`, `/roles/{id}/permissions` | bearer + `Admin` | real | no | yes | |
| POST/DELETE `/api/v1/admin/users/{userId}/roles` | bearer + `Admin` | real | no | yes | Grant/revoke on an existing user. **No "list users" endpoint — see §7.** |
| — `GET /api/v1/admin/users` (list) | – | **missing** | – | – | Admin console `GET /users` is **mock-only** (`mockAdapter.ts`). |
| — seed/bootstrap admin | – | **missing** | – | – | Only 3 roles seeded; first admin is a manual SQL insert — P1-21. |

## 2. booking-service — `BookingsEndpoints.cs`, `TripsEndpoints.cs`

| Method / path | Auth | Status | Idem | OpenAPI | Notes / gap |
|---|---|---|---|---|---|
| GET `/api/v1/trips/search?origin&destination&date&page&pageSize` | anon (by design) | real | – | yes | Paged; `X-Pagination` header. Redis cache-aside 30s. |
| POST `/api/v1/bookings/` | bearer | **real/unsafe** | **no** | yes | `CustomerId` bound from the request body, not claims — **P0-10**. No duplicate-booking idempotency — P1-4/P0. Seat-hold concurrency bypasses `xmin` — **P1-3**. |
| GET `/api/v1/bookings/{id}` | bearer | **real/unsafe** | – | yes | **IDOR** — no ownership check, returns passenger PII — **P0-9**. |
| POST `/api/v1/bookings/{id}/cancel` | bearer | **real/unsafe** | no | yes | `RequestedByCustomerId` from the body; check bypassable — **P0-10**. |
| — GET `/api/v1/bookings/mine` | – | **mock-only** | – | – | Both SPAs serve this from fixtures. **P1-4.** |
| — GET `/api/v1/bookings` (admin list, paged/filter) | – | **mock-only** | – | – | Admin console mock. **P1-4.** |
| — GET `/api/v1/trips` (admin list) | – | **mock-only** | – | – | Only `/trips/search` exists; admin list is mock. |
| — booking → `payment.succeeded` consumer | – | **missing** | – | – | No inbound consumer; `Booking.Confirm()` never runs — **P0/P1-1**. |
| — expired-hold release job | – | **missing** | – | – | Seats leak — **P1-2**. |
| — no rate limiting on any booking endpoint | – | gap | – | – | **P1-13.** |

## 3. bus-service — `BusEndpoints.cs`

| Method / path | Auth | Status | Idem | OpenAPI | Notes / gap |
|---|---|---|---|---|---|
| POST `/api/v1/buses/` | bearer + `Operator`/`Admin`, `bus-write` RL | real | **yes (Redis)** | yes | Idempotency middleware registered **before** `UseAuthentication` and caches any status incl. DELETE — P1-13 note. |
| GET `/api/v1/buses/{id}` | bearer, `bus-read` RL | real | – | yes | |
| GET `/api/v1/buses/` | bearer, `bus-read` RL | real | – | yes | `Result<T>` envelope; admin console unwraps it (fixed 2026-08-20). |
| PUT `/api/v1/buses/{id}` | bearer + `Operator`/`Admin` | real | yes | yes | |
| POST `/api/v1/buses/{id}/status` | bearer + `Operator`/`Admin` | real | yes | yes | |
| DELETE `/api/v1/buses/{id}` | bearer + `Operator`/`Admin` | real | yes | yes | Soft delete. |
| POST `/api/v1/buses/{id}/restore` | bearer + `Operator`/`Admin` | real | yes | yes | |
| POST/GET/DELETE/restore `/api/v1/depots` | bearer + `Admin` (write) | real | yes | yes | |
| GET `/api/v1/release-info` | anon | real | – | yes | |
| — bus → operator display name | – | **missing** | – | – | `BusDto` carries `operatorId` only; no operator directory. Admin shows the raw GUID. |

## 4. route-service — `RouteEndpoints.cs`, `StopEndpoints.cs`, `ScheduleEndpoints.cs`

| Method / path | Auth | Status | Idem | OpenAPI | Notes / gap |
|---|---|---|---|---|---|
| POST/PUT/DELETE/restore `/api/v1/routes` | bearer + `Admin`/`Operator` (write), `Admin` (delete) | real | no | yes | Soft delete, optimistic concurrency. |
| GET `/api/v1/routes/{id}`, `/api/v1/routes/` | bearer | real | – | yes | `RouteDto` duration is a `TimeSpan` string; admin parses it (fixed 2026-08-20). |
| GET `/api/v1/routes/search` | bearer | real | – | yes | |
| POST/PUT/DELETE/GET `/api/v1/stops` | bearer + roles (write) | real | no | yes | |
| POST/PUT/DELETE/GET + activate/suspend `/api/v1/schedules` | bearer + roles | real | no | yes | |
| GET `/api/v1/release/info` | anon | real | – | yes | |
| — rate limiter is a single global bucket | – | gap | – | – | Not partitioned by IP/user — **P1-13**. |

## 5. payment-service — `PaymentEndpoints.cs`, `AgentPaymentMethodEndpoints.cs`

| Method / path | Auth | Status | Idem | OpenAPI | Notes / gap |
|---|---|---|---|---|---|
| POST `/api/v1/payments/` | bearer, `PaymentPolicy` RL | **real/unsafe** | **yes** (domain unique index on `IdempotencyKey`) | yes | Requires a `TenantId` the customer JWT never carries — **P1-5**. Tenant from spoofable `X-Tenant-Id` header — **P0-11**. |
| GET `/api/v1/payments/{id}` | bearer, RL | **real/unsafe** | – | yes | Tenant check skipped when `X-Tenant-Id` absent → cross-tenant read — **P0-11**. No ownership check. |
| GET `/api/v1/payments/` | bearer, RL | **real/unsafe** | – | yes | Filters on a query-param `TenantId`, not claims — P0-11. |
| POST `/api/v1/payments/{id}/process` | bearer, RL | **real/unsafe** | no | yes | No ownership check. Stub-mode providers leave payment stuck `Processing`. |
| POST `/api/v1/payments/{id}/confirm` | bearer, RL | **real/unsafe** | no | yes | **Marks payment `Succeeded` from client-supplied data, no provider call, no signature, no ownership — P0-5.** |
| POST `/api/v1/payments/{id}/fail` | bearer, RL | real/unsafe | no | yes | No ownership check; provider `FailAsync` is local-only. |
| POST `/api/v1/payments/{id}/cancel` | bearer, RL | real/unsafe | no | yes | No ownership check. |
| POST `/api/v1/payments/{id}/refund` | bearer, RL | **real/unsafe** | no | yes | **Flips status to `Refunded` but never calls `provider.RefundAsync`; `PaymentRefund` stuck `Pending` forever — P0-7.** |
| GET `/api/v1/payments/search` | bearer, RL | real | – | yes | Previously had no auth (data-exposure bug), now fixed. |
| POST `/api/v1/webhooks/{providerName}` | **anon, no RL** | **real/unsafe** | **no** | yes | **Signature bypass via unknown provider → `DefaultPaymentProvider` returns `true` — P0-6.** No event dedup. `DateTimeOffset.Parse` on a header unguarded → 500. |
| POST/GET `/api/v1/agents/{agentId}/payment-methods` (+ `/default`, `/set-default`, `/verify`) | bearer, RL | real/unsafe | no | yes | `/verify` and the hourly job call the **fake** `VerifyPaymentMethodAsync` (auto-"verified") — P1-7. |
| — real bKash callback verification | – | **missing** | – | – | Webhook HMAC scheme is fabricated — **P1-7**. |
| — real Nagad protocol | – | **missing** | – | – | Session protocol is invented — **P1-8**. |
| — Bangla QR / EMVCo QR | – | **missing** | – | – | Absent — **P1-9**. |
| — no gRPC in payment-service | – | gap | – | – | `.ai/payment-service.md` expects gRPC; not implemented. |

## 6. notification-service — `NotificationsEndpoints.cs`, `TemplatesEndpoints.cs`, `PreferencesEndpoints.cs`, `ReleaseEndpoints.cs`

| Method / path | Auth | Status | Idem | OpenAPI | Notes / gap |
|---|---|---|---|---|---|
| POST `/api/v1/notifications/` (send) | **anon**, `notification-write` RL | **real/unsafe** | in-memory dict (not distributed) | yes | **Unauthenticated — anyone sends arbitrary email/SMS/push — P0-8.** |
| GET `/api/v1/notifications/{id}` | **anon** | **real/unsafe** | – | yes | **Unauthenticated PII read — P0-8.** |
| GET `/api/v1/notifications/` (history) | **anon** | **real/unsafe** | – | yes | **Unauthenticated PII read — P0-8.** |
| POST `/api/v1/notifications/{id}/cancel` | anon | real/unsafe | no | yes | Unauthenticated state change. |
| POST `/api/v1/notifications/{id}/retry` | bearer | real | no | yes | |
| POST `/api/v1/notifications/{id}/delete` | bearer | real | no | yes | Soft delete. |
| POST/PUT/GET/DELETE `/api/v1/templates` | bearer | real | no | yes | **No templates are seeded — every event notification fails until an operator creates them — P1-10.** |
| GET/PUT `/api/v1/recipients/{recipientId}/preferences` | **anon** | real/unsafe | – | yes | Unauthenticated read/write of a recipient's channel + locale prefs. |
| GET `/api/v1/release` | anon | real | – | yes | |
| — SMS: Bangladesh provider | – | **missing** | – | – | Only Twilio / GenericHttp — **P1-11**. |
| — Email: PDF attachment / link | – | **missing** | – | – | `EmailMessage` has no attachment field — **P1-12**. |
| — inbound event dedup / inbox | – | **missing** | – | – | Duplicate delivery → duplicate notification — P1-15/P1-18. |
| — gRPC `NotificationGrpcServiceImpl` | bearer (service) | real | – | n/a | Wired; not consumed by anything in-repo. |

## 7. Frontend expects / backend missing (consolidated)

| Consumer | Call | Backend reality | Milestone |
|---|---|---|---|
| Angular customer — My Bookings page | `GET /api/v1/bookings/mine` | No such endpoint; served from `mock-api.interceptor.ts` fixtures | M2 (P1-4) |
| Angular customer — Payment page | `POST /api/v1/payments/{id}/confirm` | Served from mock; real confirm is unsafe (P0-5) and needs a tenant the JWT lacks (P1-5). The page is a **simulated card form** — no real charge. | M1 + M3 |
| Angular customer — Tickets feature (`features/tickets`, `state/ticket`) | ticket view / PDF / download | **No backend at all** | M6 (P1-6) |
| React admin — Dashboard | `GET /api/v1/dashboard/stats` | No aggregation endpoint anywhere; `mockAdapter.ts` fixture | future |
| React admin — Users module | `GET /api/v1/users` | auth-service has grant/revoke by user id but no user list; `mockAdapter.ts` fixture | M1/M10 |
| React admin — Bookings list | `GET /api/v1/bookings` | Only get-by-id/cancel; `mockAdapter.ts` fixture | M2 (P1-4) |
| React admin — Trips list | `GET /api/v1/trips` | Only `/trips/search`; `mockAdapter.ts` fixture | M2 |
| React admin — Buses table operator column | operator display name | `BusDto.operatorId` GUID only; no operator directory | future |
| React admin — Routes "active trips" count | live count | route-service has no link to booking `Trip`; shown as `0` | future |
| Both SPAs — session | token refresh on 401 | `/auth/refresh` exists; **neither app uses it** | M1 (P1-19) |
| Both SPAs — every request | correlation-id header | No SPA injects one | M0 (P1-16) |
| Both SPAs — i18n | en/bn | No i18n library in either app | M10 (P2-4) |

## 8. OpenAPI / Scalar coverage

- All 6 services expose `/scalar` + `/openapi/v1.json` (native `Microsoft.AspNetCore.OpenApi`,
  no Swashbuckle). `Scalar.AspNetCore` version skew: auth/booking/route/payment 2.1.2,
  bus 2.2.0, notification 2.9.0 — align.
- `booking-service` registers `AddOpenApi("v1")` bare (no title/theme options) — cosmetic.
- **Not documented in any OpenAPI doc:** which endpoints require `Idempotency-Key`, the
  correlation-id header contract, per-endpoint auth policy details (role requirements),
  the standard error envelope, pagination header shape. Add these via OpenApi transformers
  in M0/M10.
- No checked-in `.json`/`.yaml` OpenAPI artefacts and no AsyncAPI/message-contract docs —
  needed for the future polyglot services (P2-9).

## 9. Cross-cutting per-endpoint gaps

| Concern | State |
|---|---|
| `Idempotency-Key` | bus only (Redis, with the pre-auth ordering bug); notification in-memory; payment domain-level on create only; auth/booking/route none. Dangerous mutations (booking create, payment process/confirm/refund, webhooks, notification send) mostly unprotected — P1-13/P1-15. |
| Correlation ID | Ingress middleware in all 6; **not** propagated to outbound HTTP, RabbitMQ (`BasicProperties.CorrelationId` never set), or Quartz — P1-16. |
| Rate limiting | In-memory fixed-window; route global bucket; booking none; `X-Forwarded-For` trusted without a proxy allowlist — P1-13. |
| Tenant scoping | Only payment attempts it, from a spoofable header — P0-11. Others don't tenant-scope at all — P2-1. |
| Result / error envelope | Per-service `Result`/`Error` types drift; validation doesn't always return all errors — P2-3. |
| Pagination | Present on list endpoints (`X-Pagination` header) but max-page-size enforcement is inconsistent. |
