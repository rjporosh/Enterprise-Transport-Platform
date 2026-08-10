# Agent, Merchant & Personal Payment Method Setup Guide

## Overview

This guide explains how agents, merchants, and personal users register their bank accounts, bKash numbers, or Nagad numbers to receive payments through the Payment Service.

## Prerequisites

- A valid JWT token from the Auth Service with audience `payment-service`
- Your `AgentId` (or `UserId` for personal users)
- Bank account details OR bKash/Nagad registered mobile number

## API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/v1/agents/{agentId}/payment-methods` | Add a new payment method |
| `GET` | `/api/v1/agents/{agentId}/payment-methods` | List all payment methods |
| `GET` | `/api/v1/agents/{agentId}/payment-methods/default` | Get default payment method |
| `POST` | `/api/v1/agents/{agentId}/payment-methods/{id}/set-default` | Set default payment method |
| `POST` | `/api/v1/agents/{agentId}/payment-methods/{id}/verify` | Verify a payment method |

## Step-by-Step: Adding bKash Number

### 1. Add bKash as Payment Method

```bash
curl -X POST https://api.yourdomain.com/api/v1/agents/{agentId}/payment-methods \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "11111111-1111-1111-1111-111111111111",
    "methodType": "Bkash",
    "provider": "Bkash",
    "accountNumber": "017XXXXXXXXX",
    "accountName": "Your Name or Business Name",
    "metadata": "{\"branch\":\"dhaka\"}"
  }'
```

**Expected Response (201 Created):**
```json
{
  "id": "uuid",
  "agentId": "11111111-1111-1111-1111-111111111111",
  "methodType": "Bkash",
  "provider": "Bkash",
  "accountNumber": "017XXXXXXXXX",
  "accountName": "Your Name or Business Name",
  "isDefault": true,
  "isVerified": false,
  "createdAtUtc": "2026-08-11T01:00:00Z",
  "updatedAtUtc": "2026-08-11T01:00:00Z"
}
```

### 2. Verify bKash Number (if required)

For bKash merchant accounts, you may need to verify ownership:

```bash
curl -X POST https://api.yourdomain.com/api/v1/agents/{agentId}/payment-methods/{paymentMethodId}/verify \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "verificationToken": "OTP_RECEIVED_FROM_BKASH"
  }'
```

### 3. List All Payment Methods

```bash
curl -X GET "https://api.yourdomain.com/api/v1/agents/{agentId}/payment-methods?onlyVerified=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4. Set as Default (if not already default)

```bash
curl -X POST https://api.yourdomain.com/api/v1/agents/{agentId}/payment-methods/{paymentMethodId}/set-default \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Step-by-Step: Adding Bank Account

### 1. Add Bank Account as Payment Method

```bash
curl -X POST https://api.yourdomain.com/api/v1/agents/{agentId}/payment-methods \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "11111111-1111-1111-1111-111111111111",
    "methodType": "BankTransfer",
    "provider": "Dutch Bangla Bank",
    "accountNumber": "1234567890",
    "accountName": "Your Business Name",
    "metadata": "{\"branch\":\"Motijheel\",\"routingNumber\":\"090270056\"}"
  }'
```

## Step-by-Step: Adding Nagad Number

Same flow as bKash, just change `methodType` and `provider` to `Nagad`:

```bash
curl -X POST https://api.yourdomain.com/api/v1/agents/{agentId}/payment-methods \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "11111111-1111-1111-1111-111111111111",
    "methodType": "Nagad",
    "provider": "Nagad",
    "accountNumber": "017YYYYYYYYY",
    "accountName": "Your Name"
  }'
```

> **Note:** Nagad numbers are processed via the Nagad payment provider. Ensure `Nagad:*` configuration is set in `appsettings.json`.

## Payment Method Types Reference

| Type | Use Case | Example `Provider` Values |
|------|----------|---------------------------|
| `Bkash` | bKash mobile banking | `Bkash` |
| `Nagad` | Nagad mobile banking | `Nagad` |
| `BankTransfer` | Bank account | `Dutch Bangla`, `City Bank`, `Brac Bank`, `Islami Bank` |
| `Card` | Credit/Debit card | `Stripe`, `Visa`, `MasterCard` |
| `Cash` | Cash payment | `Cash` |

## Business Rules

1. An agent can have multiple payment methods but only **one default**
2. Setting a new default automatically unsets the previous default
3. Duplicate `(AgentId, Provider, AccountNumber)` combinations are rejected by a unique database constraint
4. Payment methods can be updated but not deleted (mark as inactive via future enhancement if needed)
5. Verification is provider-specific — not all methods require it

## Frontend / POS Integration

When a customer pays at a POS terminal or through an online checkout:

1. The POS/checkout calls `POST /api/v1/payments` with the `PaymentMethod` matching the agent's registered method
2. The Payment Service routes to the correct provider (bKash, Nagad, Stripe, etc.)
3. The agent's registered `AccountNumber` is used as the destination for payouts

## Error Handling

| HTTP Status | Reason | Resolution |
|-------------|--------|------------|
| `400` | Invalid input (empty account number, invalid method type) | Check request body |
| `409` | Duplicate `(AgentId, Provider, AccountNumber)` | Use a different account or update existing |
| `401` | Missing or invalid JWT | Re-authenticate |
| `404` | Agent not found or payment method not found | Verify IDs |

## Environment Variables / Configuration

### bKash (for payout processing, not registration)

```json
{
  "Bkash": {
    "AppKey": "your-app-key",
    "AppSecret": "your-app-secret",
    "Username": "your-username",
    "Password": "your-password",
    "BaseUrl": "https://tokenized.sandbox.bka.sh/v1.2.0-beta",
    "CallbackUrl": "https://your-domain.com/api/v1/webhooks/bkash",
    "WebhookSecret": "your-webhook-secret"
  }
}
```

### Nagad

```json
{
  "Nagad": {
    "MerchantId": "your-merchant-id",
    "SecretKey": "your-secret-key",
    "BaseUrl": "https://api-sandbox.nagad.com.bd",
    "CallbackUrl": "https://your-domain.com/api/v1/webhooks/nagad",
    "WebhookSecret": "your-webhook-secret"
  }
}
```

## Testing

Use the API endpoints with test data:

```bash
# Add test bKash method
POST /api/v1/agents/00000000-0000-0000-0000-000000000000/payment-methods
{
  "agentId": "00000000-0000-0000-0000-000000000000",
  "methodType": "Bkash",
  "provider": "Bkash",
  "accountNumber": "01700000000",
  "accountName": "Test Agent"
}
```

## Support

For issues with:
- **bKash API**: https://developer.bkash.com/
- **Nagad API**: https://nagad.com.bd/developer-portal
- **Payment Service**: Contact the platform engineering team