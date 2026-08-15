# NotificationService

Notification microservice for the CommerceCore ecommerce platform — **real Resend (email) and
Twilio (SMS) test-mode integrations**, not simulated/fake senders. It's also this platform's first
pure terminal consumer: it owns no topics of its own and publishes nothing — nothing downstream
needs to react to "a notification was sent" — it only ever consumes.

Every order-lifecycle notification is sent on both channels a user has on file: email (always
present) and SMS (only if the user supplied a phone number at registration). A user with both on
file gets two `Notification` rows per event — one per channel — not one row describing two outcomes.
The overall result succeeds if at least one channel succeeded.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- **Resend** (MIT license) against the real Resend Email API — real network calls, a real message
  lands in a real inbox, but Resend's free tier (3,000 emails/month, no card required) and
  zero-domain-verification sandbox sender make it low-friction to try
- **Twilio** (MIT-licensed SDK) against the real Twilio SMS API — same "real API, test mode" posture,
  using a free trial account
- PostgreSQL via EF Core
- Confluent Kafka + Schema Registry (Avro): **consumes eight topics across three other services**
  and publishes none. See "What triggers a notification" below.
- CQRS via MediatR, FluentValidation for request validation

## What triggers a notification

| Topic | Owned by | Notification |
|---|---|---|
| `auth.user-registered.v1` | AuthenticationService | *(none — populates the local email lookup, see below)* |
| `order.created.v1` | OrderService | Order received |
| `order.paid.v1` | OrderService | Payment received |
| `order.shipped.v1` | OrderService | Order shipped |
| `order.delivered.v1` | OrderService | Order delivered |
| `order.cancelled.v1` | OrderService | Order cancelled |
| `order.refunded.v1` | OrderService | Order refunded |
| `payment.failed.v1` | PaymentService | Payment failed |

Six of the seven notification-triggering topics are owned by OrderService, because Order is
already the single source of truth for the customer-facing lifecycle — Payment and Shipping's own
activity already funnels through OrderService's events (e.g. `order.shipped.v1` is only published
after OrderService itself consumes ShippingService's `shipment.dispatched.v1`), so consuming
OrderService's topics alone captures the whole happy path without this service needing to know
anything about Payment or Shipping internals. The one exception is `payment.failed.v1`: a declined
card leaves the order in `Pending` with no corresponding OrderService event to hang a "payment
failed, please retry" notification off, so that one topic is consumed directly from PaymentService.

### Why a local contact lookup instead of calling AuthenticationService

None of the order/payment events carry an email address or phone number, and threading them
through every other service's events just for this would be invasive. AuthenticationService
already publishes `auth.user-registered.v1` with `userId` + `email` + optional `phoneNumber` — so
this service instead builds and maintains its own small local `userId -> (email, phoneNumber)`
table by consuming that topic, and never calls AuthenticationService synchronously at all.

## ⚠️ You need your own Resend API key and Twilio credentials

Same posture as PaymentService's Stripe key and ShippingService's EasyPost key: real per-account
external credentials, **never committed anywhere in this repo**.

### Resend (email)

1. Create a free Resend account: https://resend.com/signup — no card required.
2. Grab your API key (starts with `re_`) from the dashboard's API Keys page.
3. Configure it locally — pick one:
   - **Running via `dotnet run`**: `dotnet user-secrets set "Resend:ApiKey" "re_..." --project src/NotificationService.Api`
   - **Running via Docker**: `cp .env.example .env` in this folder, then edit `.env` and set `RESEND_API_KEY=re_...`. `.env` is gitignored — `docker compose` reads it automatically.

The default sender (`onboarding@resend.dev`, Resend's sandbox address) can send to the email
address you signed up with immediately, with no domain verification step — that's the account
you'll see real notification emails land in during local testing.

### Twilio (SMS)

1. Create a free Twilio trial account: https://www.twilio.com/try-twilio — no card required. Your
   Account SID and Auth Token are on the console dashboard; a trial phone number (your
   `FromPhoneNumber`) is provisioned for you automatically.
2. **Trial accounts can only send SMS to phone numbers you've verified** in the console under
   Phone Numbers → Verified Caller IDs — the same class of restriction hit with Resend's
   sandbox-only-verified-recipient limit and EasyPost's test-account signup. Verify your own
   number there before attempting a live smoke test.
3. Phone numbers must be in **E.164 format** (`+` followed by country code and number, e.g.
   `+15551234567`) — that's what Twilio expects for both the `FromPhoneNumber` config and what
   AuthenticationService's registration endpoint validates against.
4. Configure it locally — pick one:
   - **Running via `dotnet run`**: `dotnet user-secrets set "Twilio:AccountSid" "AC..." --project src/NotificationService.Api` (and similarly for `Twilio:AuthToken`, `Twilio:FromPhoneNumber`)
   - **Running via Docker**: same `.env` file as Resend above — set `TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN`, `TWILIO_FROM_PHONE_NUMBER`.

A user only gets SMS notifications if they supplied a phone number when registering via
AuthenticationService's `/api/auth/register` — it's an optional field; existing/older accounts
have none until they re-register (no retrofit flow).

## Domain model

`Notification` — one row per channel per attempt, recorded whether it succeeded or failed (same
audit-trail reasoning as PaymentService's Payment / ShippingService's Shipment). UserId, Channel
(`Email` / `Sms`), Recipient (email address or E.164 phone number depending on channel), Type
(`OrderCreated` / `OrderPaid` / `OrderShipped` / `OrderDelivered` / `OrderCancelled` /
`OrderRefunded` / `PaymentFailed`), Subject (empty for SMS — it has none), Body, Status (`Sent` /
`Failed`), ProviderMessageId (Resend's email id or Twilio's message SID), FailureReason.

`UserContact` — the local `userId -> (email, phoneNumber)` table described above.

## Local development

```bash
# from commercecore/NotificationService/
docker compose up -d notification-postgres
dotnet ef database update --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api
dotnet user-secrets set "Resend:ApiKey" "re_..." --project src/NotificationService.Api
dotnet user-secrets set "Twilio:AccountSid" "AC..." --project src/NotificationService.Api
dotnet user-secrets set "Twilio:AuthToken" "..." --project src/NotificationService.Api
dotnet user-secrets set "Twilio:FromPhoneNumber" "+1..." --project src/NotificationService.Api
dotnet run --project src/NotificationService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

`dotnet test` never touches the real Resend or Twilio APIs — integration tests swap in fake
`IEmailGateway`/`ISmsGateway` implementations, same as every other real-integration service in this
project.

### Endpoints

All endpoints require a valid JWT bearer token (obtained from AuthenticationService's
`/api/auth/login`).

- `GET /api/notifications/me` — the caller's own notification history, paginated (`page`/`pageSize`)
- `GET /api/notifications/{id}` — get a notification (ownership-checked)

There's no `POST` endpoint — notifications are only ever created by the eight Kafka consumers,
never directly via HTTP.

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` match
AuthenticationService's values exactly, same local-dev-only symmetric-key sharing used by every
other service. `Resend:ApiKey` and the three `Twilio:*` settings are deliberately left empty in
every committed file.

## Running everything in Docker

```bash
# from commercecore/ — shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/NotificationService/ — set up your .env first (see above), then:
docker compose up -d --build
```

Brings up Postgres (host port 5439) and the API (host port 8092). `docker compose up` will refuse
to start `notification-service` with a clear error if `RESEND_API_KEY`, `TWILIO_ACCOUNT_SID`,
`TWILIO_AUTH_TOKEN`, or `TWILIO_FROM_PHONE_NUMBER` aren't set — that's intentional.
AuthenticationService, OrderService, and PaymentService should also be running so there's something
real for this service to consume and react to.
