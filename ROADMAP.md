# Enterprise Transport Platform — Roadmap

Status legend: ✅ Done · 🟡 Partial · ⬜ Not started

| Phase | Area | Status | Milestone |
|-------|------|--------|-----------|
| 0 | Shared Kernel + YARP Gateway | ✅ Done | M0 |
| 1 | Auth Service (JWT, OTP, roles) | 🟡 Partial | M1 |
| 2 | Booking Service (end-to-end) | 🟡 Partial | M2 |
| 3 | Payment Service (QR, safety) | 🟡 Partial | M3 |
| 4 | Ticketing Service (PDF, QR, verify) | ✅ Done | M6 |
| 5 | Notifications (Email/SMS/Push) | ✅ Done | M7 |
| 6 | Admin / Operator Web (React) | ⬜ Not started | MVP-5 |
| 7 | Customer Web (Angular) | ⬜ Not started | MVP-6 |
| 8 | Observability (Jaeger, Grafana, Seq) | ⬜ Not started | M8 |
| 9 | Resilience + Distributed Rate Limiting | ⬜ Not started | M9 |
| 10 | SaaS Foundation (multi-tenant billing) | ⬜ Not started | M10 |
| 11 | CI/CD + Production Docker | ⬜ Not started | M11 |

## Next Up

**M8 — Observability backend**: OTel Collector + Jaeger + Prometheus + Grafana in docker-compose,
Seq or Loki log sink, `traceparent` in RabbitMQ headers for connected traces across all 6 services.

## Detailed milestone tracker

See `docs/PRODUCTION-MILESTONES.md` for full acceptance criteria and status per milestone.
