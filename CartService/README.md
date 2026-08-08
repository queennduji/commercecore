# CartService

Shopping cart microservice for the CommerceCore ecommerce platform. Unlike every other service so
far, **Redis is the primary store here, not a cache** — there's no Postgres database, no EF Core,
no migrations. Each cart is a single JSON-serialized Redis key whose TTL is refreshed on every
write, so an abandoned cart simply expires instead of needing a cleanup job.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- Redis (StackExchange.Redis, raw client) as the sole data store
- A synchronous HTTP call to CatalogService's public `GET /api/products/{id}` — the one place this
  service reaches across a service boundary directly instead of via Kafka, used to validate a
  product exists/is active and to snapshot its current name/sku/price at add-to-cart time
- CQRS via MediatR, FluentValidation for request validation
- No Kafka publishing yet — cart mutations are too frequent/low-value to broadcast, and there's no
  consumer for a "cart" event until Order/Checkout exists (same "don't build ahead of an actual
  consumer" reasoning used to defer Catalog's Variants and Inventory's original single-location
  option)

Command/query handlers live in the **Application** layer (same as Catalog/Inventory) — they only
depend on `ICartRepository`/`ICatalogServiceClient` interfaces, no framework-coupled dependency.

## Cart identity

- **Guest cart**: `POST /api/carts` mints a random `Guid` Id, no auth required. The client
  (browser/mobile) is responsible for remembering this Id (e.g. in a cookie) and passing it back
  on every subsequent call.
- **Authenticated cart**: deterministic — the cart Id **is** the user's Id. `GET /api/carts/me`
  (JWT required) gets-or-creates the caller's own cart with no separate lookup needed.
- **Merge on login**: `POST /api/carts/me/merge` (JWT required, body `{ "sourceCartId": "..." }`)
  merges a guest cart's items into the caller's own cart (summing quantities for duplicate
  products) and deletes the guest cart. This is the only place besides `/me` that reads identity
  off the JWT — every other endpoint just operates on whatever cart Id is in the URL, guest or
  user, since a Redis key lookup doesn't care which kind of Id it is.

## Local development

Redis is shared platform infra — see the root [commercecore/README.md](../README.md) for starting
it once. CatalogService must also be running for `AddItem` calls to succeed (it's the price/name
source of truth).

```bash
# from commercecore/CartService/
dotnet run --project src/CartService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

### Endpoints

`GET`/`POST`/`PUT`/`DELETE` on `/api/carts/{id}...` are all public (guest carts have no identity to
check). `/api/carts/me` and `/api/carts/me/merge` require a valid JWT bearer token (obtained from
AuthenticationService's `/api/auth/login`).

- `POST /api/carts` — create a new guest cart
- `GET /api/carts/{id}` — get a cart
- `DELETE /api/carts/{id}` — delete a cart entirely
- `POST /api/carts/{id}/items` — add a product (body: `productId`, `quantity`); increments quantity
  if the product is already in the cart; fails if the product doesn't exist or isn't `Active` in
  CatalogService
- `PUT /api/carts/{id}/items/{productId}` — set a line item's exact quantity
- `DELETE /api/carts/{id}/items/{productId}` — remove a line item
- `GET /api/carts/me` — get-or-create the caller's own cart
- `POST /api/carts/me/merge` — merge a guest cart into the caller's own cart, deleting the guest cart

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` must
match AuthenticationService's values exactly (copied verbatim), same local-dev-only symmetric-key
sharing used by every other service. `CatalogService:BaseUrl` points at CatalogService's own
address (`localhost:8086` locally, `http://catalog-service:8080` inside Docker).

## Running everything in Docker

```bash
# from commercecore/ — shared Redis (+ Kafka/Schema Registry/MinIO for the other services), once
docker compose up -d

# from commercecore/CatalogService/ — CartService needs a live CatalogService to add items against
docker compose up -d --build

# from commercecore/CartService/ — this service itself
docker compose up -d --build
```

Brings up the API on host port 8087. No own Postgres — nothing to bring up but the API container
itself.
