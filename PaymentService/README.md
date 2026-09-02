# PaymentService

Payment processing microservice for the CommerceCore ecommerce platform – a **real Stripe
test-mode integration**, not a simulated/fake gateway. Called synchronously by OrderService's
`/pay` and `/refund` transitions, the same orchestrated-saga pattern already used for
Cart/Inventory.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- **Stripe.net** (Apache-2.0) against the real Stripe PaymentIntents/Refunds API, in Stripe's test
  mode – real network calls, real (test) card behavior, but zero real money ever moves
- PostgreSQL via EF Core – payments are durable financial records, same as Orders
- Confluent Kafka + Schema Registry (Avro): publishes `payment.succeeded.v1`, `payment.failed.v1`,
  `payment.refunded.v1` – `payment.failed.v1` is consumed by NotificationService (a declined card
  leaves the order `Pending` with no OrderService event of its own to hang a "payment failed,
  please retry" notification off); the other two currently have no consumer
- CQRS via MediatR, FluentValidation for request validation

Command/query handlers live in the **Application** layer, same as every other service. The
Application layer depends only on `IPaymentGateway` – an abstraction Stripe sits behind – so unit
tests and integration tests never need a real Stripe account; only a live Docker smoke test does.

## ⚠️ You need your own Stripe test-mode secret key

This is the **first credential in the project you supply yourself** – unlike the shared JWT
signing key (an internal secret trusted across all services, safe to commit for local dev), a
Stripe key is a real per-account external credential and is **never committed anywhere in this
repo**.

1. Create a free Stripe account: https://dashboard.stripe.com/register (no business verification
   needed to use test mode).
2. Grab your test secret key (starts with `sk_test_`): https://dashboard.stripe.com/test/apikeys
3. Configure it locally – pick one:
   - **Running via `dotnet run`**: `dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/PaymentService.Api` (keeps it out of any file in this repo entirely).
   - **Running via Docker**: `cp .env.example .env` in this folder, then edit `.env` and set `STRIPE_SECRET_KEY=sk_test_...`. `.env` is gitignored – `docker compose` reads it automatically.

Without a key configured, the service still starts and every non-payment endpoint works fine –
only `POST /api/payments/charge`/`refund` will fail (Stripe rejects the request).

### Test cards (PaymentMethod ids)

Since this is a backend-only demo with no Stripe.js/Elements frontend collecting real card
numbers, `paymentMethodId` in charge requests uses Stripe's own built-in test PaymentMethod ids –
the officially documented way to exercise PaymentIntents server-to-server
(https://docs.stripe.com/testing):

| PaymentMethod id | Result |
|---|---|
| `pm_card_visa` | Always succeeds |
| `pm_card_visa_chargeDeclined` | Always declines (generic decline) |
| `pm_card_visa_chargeDeclinedInsufficientFunds` | Declines with `insufficient_funds` |

## Domain model

`Payment` – Id, OrderId, UserId, Amount, Currency, Status (`Pending`/`Succeeded`/`Failed`/
`Refunded`), `ProviderReference` (Stripe's PaymentIntent/Refund id – what you'd look this up by in
the Stripe dashboard), `FailureReason`. A row is recorded for **every** charge attempt, succeeded
or declined, so there's always an audit trail – not just of successful payments.

### Guarding against a duplicate charge

A retried checkout request (network blip, a double-click, OrderService's own resilience handler
retrying a slow call) shouldn't ever charge a customer twice for the same order. Three
independent layers, not one:

1. **Stripe idempotency key**, keyed by `OrderId` – Stripe itself dedupes an identical retried
   charge request before it becomes a second real charge.
2. **A Postgres advisory lock**, held for the duration of `ChargeCommandHandler.Handle` – serializes
   concurrent charge attempts for the same order rather than letting them race.
3. **A partial unique index** on `OrderId` (`WHERE "Status" = 'Succeeded'`) – the last-resort
   database-level backstop if the first two somehow both failed to prevent it; a second attempt
   that races past the lock hits this constraint and converges to the same success response instead
   of a raw SQL error.

Refunds use the same idempotency-key approach, keyed by the original charge's `ProviderReference`.

## Local development

```bash
# from commercecore/PaymentService/
docker compose up -d payment-postgres
dotnet ef database update --project src/PaymentService.Infrastructure --startup-project src/PaymentService.Api
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/PaymentService.Api
dotnet run --project src/PaymentService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

`dotnet test` never touches the real Stripe API – integration tests swap in a fake `IPaymentGateway`,
same as CartService/OrderService fake out CatalogService/CartService/InventoryService in their own
integration tests.

### Endpoints

All endpoints require a valid JWT bearer token (obtained from AuthenticationService's
`/api/auth/login`) – there's no anonymous payment data. `refund` additionally requires the
**`Admin`** role – see [AuthenticationService/README.md](../AuthenticationService/README.md#roles).

- `POST /api/payments/charge` – charge a card (body: `orderId`, `amount`, `currency`, `paymentMethodId`); records a Payment either way, fails the request if the charge was declined
- `POST /api/payments/refund` – refund the most recent Succeeded payment for an order (body: `orderId`); fails if there's no successful payment on record (Admin)
- `GET /api/payments/{id}` – get a payment (ownership-checked)
- `GET /api/payments/order/{orderId}` – list payments for an order (filtered to ones the caller owns)

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` must
match AuthenticationService's values exactly (copied verbatim), same local-dev-only symmetric-key
sharing used by every other service. `Stripe:SecretKey` is deliberately left empty in every
committed file – see the credential setup section above.

## Running everything in Docker

```bash
# from commercecore/ – shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/PaymentService/ – set up your .env first (see above), then:
docker compose up -d --build
```

Brings up Postgres (host port 5437) and the API (host port 8090). `docker compose up` will refuse
to start `payment-service` with a clear error if `STRIPE_SECRET_KEY` isn't set in your shell or a
`.env` file – that's intentional, not a bug.
