# Email Provider Configuration

This guide walks through configuring the Email notification channel step by step.

## Supported Providers

The Notification Service uses **MailKit** for SMTP delivery. Any SMTP-compliant server works:

- Local dev: MailHog, Papercut, smtp4dev
- Cloud: SendGrid, Amazon SES, Postmark, Mailgun (via their SMTP endpoints)
- Enterprise: Exchange, Office 365

## Step 1: Choose Your SMTP Server

### Option A: Local Development (MailHog)

MailHog is an email testing tool that catches emails locally.

```bash
# Using Docker
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Web UI: http://localhost:8025
# SMTP port: 1025
```

### Option B: SendGrid

SendGrid provides a free tier for development.

1. Sign up at https://sendgrid.com
2. Create an API key or use SMTP credentials
3. SMTP host: `smtp.sendgrid.net`, port: `587`

### Option C: Amazon SES

1. Verify your domain/email in AWS SES
2. Generate SMTP credentials (not your AWS access keys)
3. SMTP host: `email-smtp.<region>.amazonaws.com`, port: `587`

### Option D: Office 365 / Exchange

1. Enable SMTP AUTH on your mailbox (or use app password)
2. SMTP host: `smtp.office365.com`, port: `587`

## Step 2: Configure appsettings.json

```json
{
  "Smtp": {
    "Host": "localhost",
    "Port": 1025,
    "UserName": "",
    "Password": "",
    "UseStartTls": false,
    "FromAddress": "no-reply@bus-ticketing.local",
    "FromDisplayName": "Bus Ticketing"
  }
}
```

### Field Reference

| Field | Purpose | Example |
|---|---|---|
| `Host` | SMTP server hostname | `smtp.sendgrid.net` |
| `Port` | SMTP port | `587` (STARTTLS), `465` (SSL), `1025` (no auth) |
| `UserName` | SMTP auth username | `apikey` (SendGrid), your SMTP user |
| `Password` | SMTP auth password | `SG.xxx...` (SendGrid), your SMTP password |
| `UseStartTls` | Enable STARTTLS | `true` for port 587, `false` for 1025 |
| `FromAddress` | Sender email address | `no-reply@yourdomain.com` |
| `FromDisplayName` | Sender display name | `Bus Ticketing` |

## Step 3: Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "Smtp": {
    "Host": "localhost",
    "Port": 1025,
    "UserName": "",
    "Password": "",
    "UseStartTls": false,
    "FromAddress": "dev@bus-ticketing.local",
    "FromDisplayName": "Bus Ticketing (Dev)"
  }
}
```

### Production (Environment Variables)

```bash
Smtp__Host=smtp.sendgrid.net
Smtp__Port=587
Smtp__UserName=apikey
Smtp__Password=SG.xxxxxxxx
Smtp__UseStartTls=true
Smtp__FromAddress=no-reply@yourdomain.com
Smtp__FromDisplayName=Bus Ticketing
```

### Production (User Secrets - Local Testing)

```bash
dotnet user-secrets set Smtp:Host smtp.sendgrid.net
dotnet user-secrets set Smtp:Port 587
dotnet user-secrets set Smtp:UserName apikey
dotnet user-secrets set Smtp:Password SG.xxxxxxxx
dotnet user-secrets set Smtp:UseStartTls true
dotnet user-secrets set Smtp:FromAddress no-reply@yourdomain.com
dotnet user-secrets set Smtp:FromDisplayName "Bus Ticketing"
```

## Step 4: Verify the Configuration

### Check 1: View OpenAPI/Scalar

Navigate to `http://localhost:5301/scalar` and verify the SMTP configuration is loaded.

### Check 2: Send a Test Notification

```bash
curl -X POST http://localhost:5301/api/v1/notifications \
  -H "Content-Type: application/json" \
  -d '{
    "recipient": "test@example.com",
    "channel": "Email",
    "subject": "Test",
    "body": "If you receive this, SMTP is working.",
    "priority": "Normal",
    "isTransactional": true
  }'
```

### Check 3: Verify Delivery

- **MailHog**: Open http://localhost:8025
- **SendGrid**: Check your SendGrid dashboard
- **Amazon SES**: Check your SES sending statistics

## Step 5: Monitor Delivery

### Logs

Check `logs/runtime-errors/runtime-error-<date>.txt` for SMTP errors:

```
Email send to user@example.com failed after retries. 
Root cause: SMTP dependency unavailable or rejected the message (host=smtp.sendgrid.net, port=587).
Possible solution: verify SMTP credentials...
```

### Common Issues

| Issue | Cause | Fix |
|---|---|---|
| `AuthenticationException` | Wrong username/password | Verify `Smtp:UserName` and `Smtp:Password` |
| `SmtpCommandException` (550) | Sender address not verified | Verify `FromAddress` in your SMTP provider |
| Connection timeout | Firewall or wrong host/port | Verify `Smtp:Host` and `Smtp:Port` are reachable |
| TLS/SSL error | Wrong `UseStartTls` setting | Port 587 → `true`, Port 465 → try `SecureSocketOptions.SslOnConnect` |

## Provider-Specific Notes

### SendGrid

- `UserName` must be `apikey` (literally)
- `Password` is your actual SendGrid API key
- Free tier: 100 emails/day

### Amazon SES

- SMTP credentials are different from AWS access keys
- Generate via IAM → SMTP Settings
- Sandbox mode: must verify recipient emails
- Production access: request via AWS Support

### Office 365

- App password required if MFA is enabled
- SMTP AUTH must be enabled on the mailbox
- Some Office 365 plans disable SMTP AUTH

### MailHog (Local Dev)

- No authentication required
- Port 1025 for SMTP, 8025 for web UI
- Perfect for local testing without real emails
