# NotificationApp

A lightweight HTTP notification service built with ASP.NET Core 8. Receives notification payloads, evaluates severity, and forwards qualifying messages to Discord via webhook. Includes a sliding window rate limiter capped at 10 messages per minute.

---

## Tech stack

| Component | Choice |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core 8 |
| Test framework | xUnit |
| Mocking | Moq |
| Assertions | FluentAssertions |
| Integration testing | Microsoft.AspNetCore.Mvc.Testing |
| API docs | Swagger / Swashbuckle |

---

## Project structure

```
NotificationApp/
├── src/
│   ├── NotificationApp.Core/        # Business logic, models, interfaces
│   │   ├── Interfaces/
│   │   │   ├── IDiscordSender.cs
│   │   │   └── IRateLimiter.cs
│   │   ├── Models/
│   │   │   ├── NotificationLevel.cs
│   │   │   ├── NotificationRecord.cs
│   │   │   ├── NotificationRequest.cs
│   │   │   └── NotificationSettings.cs
│   │   └── Services/
│   │       ├── DiscordSender.cs
│   │       ├── MockDiscordSender.cs
│   │       ├── NotificationProcessor.cs
│   │       └── NotificationRateLimiter.cs
│   └── NotificationApp.Api/         # ASP.NET Web API, DI wiring
│       ├── Controllers/
│       │   └── NotificationsController.cs
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    └── NotificationApp.Tests/
        ├── UnitTests/
        │   ├── NotificationProcessorTests.cs
        │   └── NotificationRateLimiterTests.cs
        └── IntegrationTests/
            └── NotificationsEndpointTests.cs
```

Core has no dependency on the web layer. Api references Core. Tests reference both.

---

## Getting started

**Prerequisites:** .NET 8 SDK

```bash
git clone <repo-url>
cd NotificationApp
dotnet restore
dotnet build
```

Run the API:

```bash
cd src/NotificationApp.Api
dotnet run
```

Swagger UI opens at `http://localhost:<port>/` in development.

---

## Configuration

All settings live in `src/NotificationApp.Api/appsettings.json`:

```json
{
  "NotificationSettings": {
    "ForwardThreshold": "Warning",
    "RateLimitPerMinute": 10,
    "Discord": {
      "WebhookUrl": "https://discord.com/api/webhooks/YOUR_ID/YOUR_TOKEN",
      "Username": "Notification Bot"
    }
  }
}
```

`ForwardThreshold` accepts: `Info`, `Warning`, `Error`, `Critical`. Any notification at or above this level gets forwarded. The rest are logged locally only.

If `WebhookUrl` is empty the app automatically falls back to `MockDiscordSender`, which logs forwarded messages to the console instead of calling Discord. No other configuration needed for local development.

> In production, set the webhook URL via environment variable to avoid committing credentials:
> `NotificationSettings__Discord__WebhookUrl=https://discord.com/api/webhooks/...`

---

## API

### POST /api/notifications

Accepts a JSON notification payload.

**Request body:**

```json
{
  "title": "Disk usage high",
  "message": "Disk usage has exceeded 90% on server-01",
  "level": "Warning",
  "source": "MonitoringService",
  "timestamp": "2024-11-01T10:30:00Z"
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| title | string | yes 
| message | string | no 
| level | string | yes | Info / Warning / Error / Critical |
| source | string | no | Which service sent this |
| timestamp | datetime | no | Defaults to UTC now if omitted |

**Responses:**

| Status | Meaning |
|---|---|
| 200 OK | Received and logged — below forward threshold |
| 202 Accepted | Received and forwarded to Discord |
| 400 Bad Request | Missing required field or unrecognised level |
| 429 Too Many Requests | 10/min rate limit reached, try again later |
| 502 Bad Gateway | Notification received but Discord rejected it |

**Example — below threshold:**

```bash
curl -X POST http://localhost:5000/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"title":"Deploy done","message":"v1.2.3 deployed","level":"Info","source":"CI"}'
```

```json
{
  "status": "logged",
  "message": "Notification received and logged. Level is below the forward threshold."
}
```

**Example — forwarded:**

```bash
curl -X POST http://localhost:5000/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"title":"High memory","message":"Memory at 95%","level":"Error","source":"Monitor"}'
```

```json
{
  "status": "forwarded",
  "message": "Notification received and forwarded successfully."
}
```

**Example — rate limited:**

```json
{
  "status": "rate_limit_reached",
  "message": "Maximum of 10 notifications per minute reached. Please try again later."
}
```

---

## Rate limiting

Uses a sliding window algorithm. A `ConcurrentQueue<DateTime>` tracks timestamps of forwarded messages. On each request, timestamps older than 60 seconds are evicted from the front of the queue. If the queue length equals the configured limit, the request is rejected immediately with `429`.

The window is rolling — not a fixed 60s bucket. 10 messages sent at 00:59 blocks further messages until 01:59, not until 01:00.

To change the limit, update `RateLimitPerMinute` in `appsettings.json` — no code changes needed.

---

## Running tests

```bash
dotnet test
```

Or with output:

```bash
dotnet test --logger "console;verbosity=normal"
```

Tests cover:

- Processor routing: below threshold, forwarded, rate limited, invalid level, Discord failure
- Rate limiter: within limit returns true, exceeded returns false
- Integration: full HTTP pipeline via `WebApplicationFactory` — 200, 202, 400, 429 responses

---

## Discord integration

The app ships with two implementations of `IDiscordSender`:

- `MockDiscordSender` — logs to console, used when `WebhookUrl` is empty or in Test environment
- `DiscordSender` — posts a formatted embed to the Discord webhook URL

Switching between them is automatic based on configuration — no code changes needed. To get a webhook URL, create a server in Discord, right-click a channel → Edit Channel → Integrations → Webhooks → New Webhook → Copy Webhook URL.
