# SMS Provider Configuration

This guide walks through configuring the SMS notification channel step by step.

## Supported Providers

The Notification Service supports two SMS providers:

1. **Twilio** — direct REST API (no SDK dependency)
2. **Generic HTTP** — any SMS provider with a REST API (bearer token auth)

## Step 1: Choose Your SMS Provider

### Option A: Twilio (Recommended for Production)

Twilio is the most reliable global SMS provider.

1. Sign up at https://www.twilio.com
2. Get a phone number from Twilio Console
3. Find your Account SID and Auth Token from the Twilio Console Dashboard

### Option B: Generic HTTP Provider

Any provider with a REST API (e.g., AWS SNS, MessageBird, Plivo, custom gateway).

You need:
- API endpoint URL
- API key or bearer token
- Request/response format documentation

## Step 2: Configure Twilio

### appsettings.json

```json
{
  "Sms": {
    "Provider": "Twilio",
    "FromNumber": "+1234567890",
    "TwilioAccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "TwilioAuthToken": "your-auth-token"
  }
}
```

### Field Reference

| Field | Purpose | Example |
|---|---|---|
| `Provider` | SMS provider selection | `Twilio` or `GenericHttp` |
| `FromNumber` | Your Twilio phone number (E.164 format) | `+1234567890` |
| `TwilioAccountSid` | Twilio Account SID | `ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` |
| `TwilioAuthToken` | Twilio Auth Token | `your-auth-token` |

### Environment Variables (Production)

```bash
Sms__Provider=Twilio
Sms__FromNumber=+1234567890
Sms__TwilioAccountSid=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Sms__TwilioAuthToken=your-auth-token
```

### Step 2a: Verify Twilio Credentials

```bash
# Test with curl (replace with your actual SID and token)
curl -X GET https://api.twilio.com/2010-04-01/Accounts/ACxxxxxxxx/Messages.json \
  -u ACxxxxxxxx:your-auth-token
```

## Step 3: Configure Generic HTTP Provider

### appsettings.json

```json
{
  "Sms": {
    "Provider": "GenericHttp",
    "FromNumber": "BusTicketing",
    "GenericHttpEndpoint": "https://api.yoursmsprovider.com/v1/send",
    "GenericHttpApiKey": "your-api-key"
  }
}
```

### Field Reference

| Field | Purpose | Example |
|---|---|---|
| `Provider` | Must be `GenericHttp` | `GenericHttp` |
| `FromNumber` | Sender ID (depends on provider) | `BusTicketing` or `+1234567890` |
| `GenericHttpEndpoint` | Provider's SMS API URL | `https://api.yoursmsprovider.com/v1/send` |
| `GenericHttpApiKey` | Bearer token or API key | `your-api-key` |

### Environment Variables (Production)

```bash
Sms__Provider=GenericHttp
Sms__FromNumber=BusTicketing
Sms__GenericHttpEndpoint=https://api.yoursmsprovider.com/v1/send
Sms__GenericHttpApiKey=your-api-key
```

### Step 3a: Generic HTTP Request Format

The `GenericHttpSmsSender` sends:

```http
POST /v1/send HTTP/1.1
Authorization: Bearer your-api-key
Content-Type: application/json

{
  "to": "+8801712345678",
  "from": "BusTicketing",
  "body": "Your OTP is 123456"
}
```

**Important**: Adjust `GenericHttpSmsSender.cs` if your provider uses a different request/response format.

## Step 4: Send a Test SMS

### Via REST API

```bash
curl -X POST http://localhost:5301/api/v1/notifications \
  -H "Content-Type: application/json" \
  -d '{
    "recipient": "+8801712345678",
    "channel": "Sms",
    "body": "Test SMS from Notification Service",
    "priority": "Normal",
    "isTransactional": true
  }'
```

### Via gRPC

```csharp
var response = await client.SendNotificationAsync(new SendNotificationRequest
{
    Recipient = "+8801712345678",
    Channel = GrpcChannel.Sms,
    Body = "Test SMS from Notification Service",
    Priority = GrpcPriority.Normal,
    IsTransactional = true
});
```

## Step 5: Verify Delivery

### Twilio Console

1. Go to https://console.twilio.com
2. Navigate to **Monitor** → **Logs** → **Messages**
3. Find your message and check status

### Generic HTTP Provider

Check your provider's dashboard or API logs.

## Step 6: Monitor and Troubleshoot

### Logs

Check `logs/runtime-errors/runtime-error-<date>.txt`:

```
SMS send to +8801712345678 via Twilio failed after retries. 
Root cause: gateway rejected the request or is unreachable. 
Possible solution: verify Sms:TwilioAccountSid/TwilioAuthToken/FromNumber...
```

### Common Issues

| Issue | Cause | Fix |
|---|---|---|
| `401 Unauthorized` | Wrong Account SID or Auth Token | Verify Twilio credentials |
| `400 Bad Request` (21610) | Recipient opted out | Check `RecipientPreference` or Twilio opt-out list |
| `400 Bad Request` (21211) | Invalid phone number | Ensure E.164 format (`+8801712345678`) |
| Connection timeout | Firewall blocking outbound | Ensure port 443 outbound is open |
| `AuthenticationFailureException` (RabbitMQ) | Unrelated — RabbitMQ issue | Fix RabbitMQ separately |

## Cost Considerations

| Provider | Cost per SMS (approx) | Notes |
|---|---|---|
| Twilio | $0.0079 - $0.15 | Varies by destination country |
| AWS SNS | $0.00645 - $0.10 | Requires Amazon SNS setup |
| MessageBird | €0.05 - €0.15 | European-focused |
| Generic HTTP | Varies | Depends on your provider |

## Production Checklist

- [ ] Use environment variables or secret manager for credentials
- [ ] Enable retry (already configured via `Retry:MaxAttempts`)
- [ ] Set up delivery receipt webhooks if provider supports them
- [ ] Monitor costs — SMS costs add up quickly at scale
- [ ] Verify phone number formats (E.164 recommended)
- [ ] Test with real phone numbers before production
