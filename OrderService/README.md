# OrderService

Checkout and order-lifecycle microservice for the CommerceCore ecommerce platform — the saga
orchestrator: it calls CartService and InventoryService synchronously to turn a cart into a
reserved, durable order, then drives that order through its lifecycle.

Payment and Shipping services don't exist yet, so `Paid`/`Shipped`/`Refunded` are triggered
directly through this service's own API for now rather than by real downstream services — the
lifecycle is built with its full shape so those services can plug into the existing transitions
later instead of requiring a rework.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- PostgreSQL via EF Core — orders are durable business records, unlike CartService's ephemeral Redis carts
- Two synchronous HTTP calls (same pattern as CartService → CatalogService):
  - **CartService** — fetch the caller's own cart at checkout (`GET /api/carts/{userId}`, per CartService's deterministic-authenticated-cart convention), then clear it
  - **InventoryService** — look up per-location stock (`GET /api/inventory/{productId}`) to pick a fulfilling location, then reserve/release/commit against it as the order moves through its lifecycle
- Confluent Kafka + Schema Registry (Avro): publishes `order.created.v1`, `order.paid.v1`, `order.shipped.v1`, `order.delivered.v1`, `order.cancelled.v1`, `order.refunded.v1` — no consumer yet, since nothing exists to react to (same "don't build ahead of a real consumer" reasoning used elsewhere in this project)
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
Pending --pay--> Paid --ship--> Shipped --deliver--> Delivered
   |                |
 cancel           cancel / refund
   |                |
   v                v
Cancelled        Refunded (also reachable from Shipped/Delivered)
```

- **Reserve** (at checkout) only holds stock — `OnHand` is untouched.
- **Ship** commits every line's reservation — this is the point stock actually leaves the building
  (`OnHand` decrements). If a commit fails partway through a multi-item order, there's no
  compensating rollback (InventoryService's commit isn't reversible) — the order stays `Paid` and
  the error is surfaced for a retry once the underlying issue is fixed.
- **Cancel** (only valid from `Pending`/`Paid`, i.e. pre-shipment) releases every line's reservation.
- **Refund** (valid from `Paid`/`Shipped`/`Delivered`) deliberately does **not** touch inventory —
  restocking a post-shipment return needs its own workflow, out of scope for now.

### Ownership model (known simplification)

AuthenticationService has no role system yet, so this service can't gate actions to "fulfillment
staff" vs. "customer." Instead:

- **Checkout, Pay, Cancel, Get, list-my-orders** — ownership-checked against the caller's JWT
  subject. `Get`/list return "not found" rather than "forbidden" on a mismatch, so the endpoint
  doesn't leak whether an order id exists to someone who doesn't own it.
- **Ship, Deliver, Refund** — any authenticated caller, no ownership check. These stand in for
  actions a back-office/fulfillment system would trigger, not the customer.

## Local development

CartService and InventoryService must be running for checkout to succeed — see their READMEs.

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
- `POST /api/orders/{id}/pay` — `Pending` → `Paid` (ownership-checked)
- `POST /api/orders/{id}/cancel` — `Pending`/`Paid` → `Cancelled`, releases reservations (ownership-checked)
- `POST /api/orders/{id}/ship` — `Paid` → `Shipped`, commits reservations (ops action)
- `POST /api/orders/{id}/deliver` — `Shipped` → `Delivered` (ops action)
- `POST /api/orders/{id}/refund` — `Paid`/`Shipped`/`Delivered` → `Refunded` (ops action)

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` must
match AuthenticationService's values exactly (copied verbatim), same local-dev-only symmetric-key
sharing used by every other service. `CartService:BaseUrl`/`InventoryService:BaseUrl` need the same
dual-addressing split as Minio (`http://cart-service:8080`/`http://inventory-service:8080` internal
Docker aliases vs `http://localhost:8087`/`http://localhost:8088` local dev).

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
