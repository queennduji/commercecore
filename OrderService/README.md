# OrderService

Checkout and order-lifecycle microservice for the CommerceCore ecommerce platform — the saga
orchestrator: it calls CartService and InventoryService synchronously to turn a cart into a
reserved, durable order, then drives that order through its lifecycle.

`Paid` is triggered directly through this service's own API and calls PaymentService
synchronously (real Stripe test-mode charges — see PaymentService's README). `Shipped`/`Delivered`
are driven by ShippingService instead: this service consumes `shipment.dispatched.v1` and
`shipment.delivered.v1` from Kafka and advances the order automatically — there's no manual
"ship"/"deliver" endpoint anymore. `Refund` remains a manual ops action, since no fulfillment
system owns that transition.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- PostgreSQL via EF Core — orders are durable business records, unlike CartService's ephemeral Redis carts
- Two synchronous HTTP calls (same pattern as CartService → CatalogService):
  - **CartService** — fetch the caller's own cart at checkout (`GET /api/carts/{userId}`, per CartService's deterministic-authenticated-cart convention), then clear it
  - **InventoryService** — look up per-location stock (`GET /api/inventory/{productId}`) to pick a fulfilling location, then reserve/release/commit against it as the order moves through its lifecycle
- Confluent Kafka + Schema Registry (Avro): publishes `order.created.v1`, `order.paid.v1` (carries `shippingAddress`, consumed by ShippingService), `order.shipped.v1`, `order.delivered.v1`, `order.cancelled.v1`, `order.refunded.v1`. Also **consumes** `shipment.dispatched.v1`/`shipment.delivered.v1` (owned by ShippingService) — the first Kafka consumers in this service, driving `Ship`/`Deliver` automatically instead of via an ops endpoint
- CQRS via MediatR, FluentValidation for request validation

Command/query handlers live in the **Application** layer (same as Catalog/Inventory/Cart).

## Checkout saga

`POST /api/orders/checkout` is a compensating-transaction saga, not a single database write:

1. Fetch the caller's own cart from CartService. Fail if empty.
2. For each line item, ask InventoryService for stock across all locations and pick the first with
   enough `Available`. If none qualify, **release every reservation already made in this attempt**
   and fail the whole checkout — no line item is left holding a stray reservation.
3. Reserve stock for the line at the chosen location.
4. Once every line is reserved, create the order (`Pending`), clear the cart, and publish
   `order.created.v1`.

## Order lifecycle

```
Pending --pay--> Paid --[shipment.dispatched.v1]--> Shipped --[shipment.delivered.v1]--> Delivered
   |                |
 cancel           cancel / refund
   |                |
   v                v
Cancelled        Refunded (also reachable from Shipped/Delivered)
```

- **Pay** calls PaymentService synchronously (real Stripe test-mode charge) before flipping status.
- **Reserve** (at checkout) only holds stock — `OnHand` is untouched.
- **Ship** (`ShipOrderCommandHandler`, now triggered by consuming `shipment.dispatched.v1`) commits
  every line's reservation — this is the point stock actually leaves the building (`OnHand`
  decrements). If a commit fails partway through a multi-item order, there's no compensating
  rollback (InventoryService's commit isn't reversible) — the order stays `Paid` and the error is
  logged for investigation, since there's no HTTP caller left to retry against.
- **Deliver** (`DeliverOrderCommandHandler`) is triggered by consuming `shipment.delivered.v1`.
- **Cancel** (only valid from `Pending`/`Paid`, i.e. pre-shipment) releases every line's reservation.
- **Refund** (valid from `Paid`/`Shipped`/`Delivered`) calls PaymentService synchronously (real
  Stripe test-mode refund) and deliberately does **not** touch inventory — restocking a
  post-shipment return needs its own workflow, out of scope for now.

### Ownership model

AuthenticationService's `Admin` role (see [its README](../AuthenticationService/README.md#roles))
is what gates actions to "fulfillment staff" vs. "customer" here — this used to be a known gap
("no role system exists yet, so Refund is just any authenticated caller") before that role existed;
it isn't anymore:

- **Checkout, Pay, Cancel, Get, list-my-orders** — ownership-checked against the caller's JWT
  subject, available to any authenticated customer acting on their own order. `Get`/list return
  "not found" rather than "forbidden" on a mismatch, so the endpoint doesn't leak whether an order
  id exists to someone who doesn't own it.
- **Refund** — requires the `Admin` role, no ownership check (an admin can refund any customer's
  order). Stands in for an action a back-office system would trigger, not the customer.
- **Ship, Deliver** — no longer HTTP endpoints at all; see Order lifecycle above.

## Local development

CartService and InventoryService must be running for checkout to succeed. PaymentService must be
running for Pay/Refund to succeed. ShippingService must be running (with its own Kafka consumer
active) for Ship/Deliver to happen at all, since there's no other trigger for them anymore. See
each service's own README.

```bash
# from commercecore/OrderService/
docker compose up -d order-postgres
dotnet ef database update --project src/OrderService.Infrastructure --startup-project src/OrderService.Api
dotnet run --project src/OrderService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

### Endpoints

All endpoints require a valid JWT bearer token (obtained from AuthenticationService's
`/api/auth/login`) — there's no anonymous order data.

- `POST /api/orders/checkout` — checks out the caller's own cart (body: `shippingAddress`)
- `GET /api/orders/{id}` — get an order (ownership-checked)
- `GET /api/orders/me` — the caller's own order history, paginated (`page`/`pageSize`)
- `POST /api/orders/{id}/pay` — `Pending` → `Paid` (ownership-checked; body: `paymentMethodId`; calls PaymentService)
- `POST /api/orders/{id}/cancel` — `Pending`/`Paid` → `Cancelled`, releases reservations (ownership-checked)
- `POST /api/orders/{id}/refund` — `Paid`/`Shipped`/`Delivered` → `Refunded` (`Admin` role required; calls PaymentService)

`Paid` → `Shipped` → `Delivered` happen automatically via Kafka — see ShippingService's README.

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` must
match AuthenticationService's values exactly (copied verbatim), same local-dev-only symmetric-key
sharing used by every other service. `CartService:BaseUrl`/`InventoryService:BaseUrl`/
`PaymentService:BaseUrl` need the same dual-addressing split as Minio (`http://cart-service:8080`
etc internal Docker aliases vs `http://localhost:8087` etc local dev). ShippingService isn't called
synchronously at all — it only shows up via the shared Kafka broker/schema registry config.

## Running everything in Docker

```bash
# from commercecore/ — shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/CartService/ and commercecore/InventoryService/ — OrderService's dependencies
docker compose up -d --build

# from commercecore/OrderService/ — this service itself
docker compose up -d --build
```

Brings up Postgres (host port 5436) and the API (host port 8089).
