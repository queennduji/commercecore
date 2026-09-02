# InventoryService

Stock and reservations microservice for the CommerceCore ecommerce platform. Tracks on-hand/reserved quantity per product **per location** (multi-warehouse), supports a reserve/release/commit workflow for holding stock against pending orders, and auto-provisions a zero-stock record for every new product by consuming CatalogService's Kafka events – no direct HTTP coupling between the two services.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- PostgreSQL via EF Core
- Confluent Kafka + Schema Registry (Avro):
  - Publishes `inventory.stock-adjusted.v1`, `inventory.stock-reserved.v1`, `inventory.reservation-released.v1`, `inventory.reservation-committed.v1`
  - Consumes CatalogService's `catalog.product-created.v1` (via a `BackgroundService`) to provision inventory for new products at every active location
- CQRS via MediatR, FluentValidation for request validation

Command/query handlers live in the **Application** layer (same as CatalogService) – they only depend on repository interfaces, no framework-coupled type forcing them into Infrastructure.

## Domain model

- **Location** – a warehouse/fulfillment location (`Code` is unique). Deactivating a location (`DELETE /api/locations/{id}`) is blocked if it still holds on-hand or reserved stock.
- **InventoryItem** – `(ProductId, LocationId)` unique pair, with `OnHand`, `Reserved`, and a computed `Available = OnHand - Reserved`.
- **StockReservation** – a hold against available stock (`Active` → `Released` or `Committed`). `Reserve` decrements `Available` (via `Reserved`) without touching `OnHand`; `Commit` is what actually removes stock from the building (decrements both `OnHand` and `Reserved`); `Release` gives the hold back without any stock leaving.

## Local development

Kafka + Schema Registry are shared platform infra – see the root [commercecore/README.md](../README.md) for starting them once.

```bash
# from commercecore/InventoryService/
docker compose up -d inventory-postgres
dotnet ef database update --project src/InventoryService.Infrastructure --startup-project src/InventoryService.Api
dotnet run --project src/InventoryService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

### Endpoints

All `GET` endpoints are public. Writes split into two tiers, not one:

- **Location CRUD and `/adjust`** (manual stock correction/restocking) require a JWT carrying the
  **`Admin`** role – see [AuthenticationService/README.md](../AuthenticationService/README.md#roles).
  These are back-office actions.
- **Reserve/release/commit** only require *any* valid JWT, no `Admin` role – they're called by
  OrderService as part of the checkout saga on a customer's behalf, not by staff, so gating them to
  admins would break checkout entirely.

- `GET /api/locations` / `GET /api/locations/{id}` – list / get a location
- `POST /api/locations` – create a location (Admin)
- `PUT /api/locations/{id}` – update a location (name, code, active flag) (Admin)
- `DELETE /api/locations/{id}` – deactivate a location (blocked with 400 if it still holds stock) (Admin)
- `GET /api/inventory` – list inventory records, filterable by `productId`/`locationId`, paginated (`page`/`pageSize`)
- `GET /api/inventory/{productId}` – stock for one product across every location
- `GET /api/inventory/{productId}/{locationId}` – stock for one product at one location
- `POST /api/inventory/adjust` – adjust on-hand stock (`delta` positive for restock, negative for correction/damage); upserts the inventory record if one doesn't exist yet, fails if the adjustment would go negative (Admin)
- `POST /api/inventory/reservations` – reserve stock at a location (fails if `Available < quantity`), returns the reservation
- `GET /api/inventory/reservations/{id}` – get a reservation
- `POST /api/inventory/reservations/{id}/release` – release an active reservation (stock becomes available again)
- `POST /api/inventory/reservations/{id}/commit` – commit an active reservation (stock actually leaves – decrements on-hand)

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` must match AuthenticationService's values exactly (copied verbatim), same local-dev-only symmetric-key sharing used by CatalogService.

## Running everything in Docker

```bash
# from commercecore/ – shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/InventoryService/ – this service's own Postgres + itself
docker compose up -d --build
```

Brings up Postgres (host port 5435) and the API (host port 8088). Host ports are intentionally non-default to avoid clashing with AuthenticationService's and CatalogService's own containers.
