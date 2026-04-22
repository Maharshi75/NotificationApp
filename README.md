# NotificationApp

A lightweight HTTP notification service built with ASP.NET Core 8. Receives notification payloads and sends them to Discord via webhook. Includes a sliding window rate limiter capped at 10 messages per minute.

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
│   │   │   ├── NotificationLevelExtensions.cs
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
    "RateLimitPerMinute": 10,
    "Discord": {
      "WebhookUrl": "https://discord.com/api/webhooks/YOUR_ID/YOUR_TOKEN",
      "Username": "Notification Bot",
      "AvatarUrl": null
    }
  }
}
```

All levels (`Info`, `Warning`, `Error`, `Critical`) are sent to Discord. The only control is the rate limit — max 10 messages per 60-second rolling window.

If `WebhookUrl` is empty the app automatically falls back to `MockDiscordSender`, which logs sent messages to the console instead of calling Discord.

> In production, set the webhook URL via environment variable to avoid committing credentials:
> `NotificationSettings__Discord__WebhookUrl=https://discord.com/api/webhooks/...`

---

## API

### POST /api/notifications

Accepts a JSON notification payload and sends it to Discord.

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
| title | string | yes | Max 200 chars |
| message | string | no | Max 2000 chars |
| level | string | yes | Info / Warning / Error / Critical |
| source | string | no | Which service sent this |
| timestamp | datetime | no | Defaults to UTC now if omitted |
| metadata | object | no | Arbitrary key-value pairs |

**Responses:**

| Status | Meaning |
|---|---|
| 202 Accepted | Notification received and sent to Discord |
| 400 Bad Request | Missing required field or unrecognised level |
| 429 Too Many Requests | 10/min rate limit reached, try again later |
| 502 Bad Gateway | Notification received but Discord rejected it |

**Example — sent successfully:**

```bash
curl -X POST http://localhost:5000/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"title":"High memory","message":"Memory at 95%","level":"Error","source":"Monitor"}'
```

```json
{
  "status": "sent",
  "message": "Notification received and sent successfully."
}
```

**Example — rate limited:**

```json
{
  "status": "rate_limit_reached",
  "message": "Maximum of 10 notifications per minute reached. Please try again later."
}
```

**Example — invalid level:**

```json
{
  "status": "invalid_level",
  "message": "'SuperCritical' is not a recognised notification level. Valid values: Info, Warning, Error, Critical."
}
```

---

## Rate limiting

Uses a sliding window algorithm. A `ConcurrentQueue<DateTime>` tracks timestamps of sent messages. On each request, timestamps older than 60 seconds are evicted from the front of the queue. If the queue length equals the configured limit, the request is rejected immediately with `429`.

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

- Processor: meets threshold within limit, rate limited, invalid level, Discord send failure
- Rate limiter: within limit returns true, exceeded returns false
- Integration: full HTTP pipeline — 202 for all valid levels, 400, 429 responses

---

## Discord integration

The app ships with two implementations of `IDiscordSender`:

- `MockDiscordSender` — logs to console, used when `WebhookUrl` is empty or in Test environment
- `DiscordSender` — posts a formatted embed to the Discord webhook URL

Switching between them is automatic based on configuration. To get a webhook URL, create a server in Discord, right-click a channel → Edit Channel → Integrations → Webhooks → New Webhook → Copy Webhook URL.
