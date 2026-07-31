# C4 Model — Level 1: System Context

Who uses the platform, and what other systems it talks to. This is the
target state from `MASTER_SPEC.md`; boxes marked **(built)** exist today,
everything else is planned.

```mermaid
C4Context
    title Bus Ticketing Platform — System Context

    Person(customer, "Customer", "Searches trips, books seats, pays for tickets")
    Person(admin, "Operations staff", "Manages bookings, monitors the platform")

    System(platform, "Bus Ticketing Platform", "Search, book, and manage intercity bus travel")

    System_Ext(paymentGateway, "Payment Gateway", "e.g. Stripe/SSLCommerz — not integrated yet")
    System_Ext(smsProvider, "SMS/Email Provider", "Booking confirmations — not integrated yet")

    Rel(customer, platform, "Searches trips, books seats", "HTTPS")
    Rel(admin, platform, "Manages bookings, views dashboards", "HTTPS")
    Rel(platform, paymentGateway, "Charges the customer", "HTTPS (planned)")
    Rel(platform, smsProvider, "Sends confirmations", "HTTPS (planned)")
```

## Built vs planned

| Actor / system | Status |
|---|---|
| Customer -> Platform (search, book) | **Built** — `apps/angular-client` + `services/booking-service` |
| Operations staff -> Platform (manage bookings) | **Built** — `apps/react-admin` + `services/booking-service` |
| Platform -> Payment Gateway | Planned — see `services/payment-service` (scaffold only) |
| Platform -> SMS/Email Provider | Planned — see `services/notification-service` (scaffold only) |

See [C4_Container.md](./C4_Container.md) for what's inside "Bus Ticketing Platform".
