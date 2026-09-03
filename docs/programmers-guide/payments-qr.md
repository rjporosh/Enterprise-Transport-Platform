# Payments — QR / Bangla QR

## What it is

`PaymentMethodType.Qr` → `QrPaymentProvider` produces a **genuine EMVCo
Merchant-Presented Mode QR** — the format Bangladesh Bank standardised as
"Bangla QR". Any Bangladeshi bank or MFS (bKash / Nagad / Rocket) app scans
and pays it. The payload is deterministic, self-describing TLV with a
CRC-16/CCITT-FALSE checksum (`EmvcoQr.cs`); `EmvcoQr.IsValid()` /
`EmvcoQr.Parse()` verify and decode it.

## Flow

```
POST /api/v1/payments            { paymentMethod: 6 (Qr), orderReference: <bookingId>, amount, currency }
     → payment Pending
POST /api/v1/payments/{id}/qr    (customer)
     → payment Processing; returns { qrPayload, qrImageDataUri (PNG), expiresAtUtc }
     → customer scans & pays with any bank/MFS app
──── settlement ────
POST /api/v1/webhooks/qr         (acquirer, HMAC-SHA256 signed with Payments:Qr:WebhookSigningKey)
  or POST /api/v1/payments/{id}/settle-qr   (Admin/Operator, audited — the demo stand-in)
     → payment Succeeded → payment.succeeded published
     → booking-service confirms the booking + books the seats + emits booking.confirmed
```

`POST /payments/{id}/confirm` **cannot** settle a QR payment — it only trusts a
server-side `provider.GetStatusAsync`, and a QR merchant has no poll API, so it
returns `Unknown` and `/confirm` 409s. This is deliberate (P0-5): the client's
transaction id is never trusted.

## Config (`Payments:Qr`)

| Key | Meaning |
|-----|---------|
| `MerchantAccountId` | Acquirer AID / reverse-domain id (EMVCo tag 26-00). From merchant onboarding. |
| `MerchantId` | Your merchant id under the scheme (tag 26-01). |
| `MerchantCategoryCode` | ISO 18245 — `4131` = bus lines. |
| `MerchantName` / `MerchantCity` | Shown in the payer's app (tags 59 / 60). |
| `TransactionCurrency` | ISO 4217 numeric — `050` = BDT. |
| `QrValidityMinutes` | QR lifetime. |
| `WebhookSigningKey` | HMAC key for `POST /api/v1/webhooks/qr`. **Empty → the webhook rejects everything**; settle via the audited admin endpoint until a real acquirer callback is wired. |

## Going live with a real acquirer

1. Onboard as a merchant with a Bangla-QR acquirer; get `MerchantAccountId`,
   `MerchantId`, the settlement webhook URL, and the webhook HMAC key.
2. Put the real values in config (from a secret store, not `appsettings.json`).
3. Point the acquirer's settlement callback at `POST /api/v1/webhooks/qr`.
4. Remove `Admin`/`Operator` access to `settle-qr` in production (or keep it as
   a manual reconciliation tool — it is audited either way).

## bKash / Nagad

`BkashPaymentProvider` / `NagadPaymentProvider` are HTTP-real but
**credential-gated**: with empty `AppKey` / `MerchantId` they log a "stub mode —
configure X" warning and return `Processing` (no fake success). Nagad's request
contract still needs the real DFS RSA/AES envelope rewrite before it works
against production Nagad — tracked as roadmap **M5**. Until then, QR is the
production-ready electronic payment path.
