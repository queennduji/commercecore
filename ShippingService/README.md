# ShippingService

Fulfillment/shipping microservice for the CommerceCore ecommerce platform – a **real EasyPost
test-mode integration**, not a simulated/fake carrier. It's also the first service in this
platform where a downstream service drives another service's state purely through events:
ShippingService both consumes from and publishes to Kafka, and OrderService's own Paid → Shipped →
Delivered transitions are now driven entirely by what happens here, not by manual ops calls.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- **EasyPost-Official** (MIT license) against the real EasyPost Tracker API, in EasyPost's test
  mode – real network calls, real tracker lifecycle behavior, but no real label is ever purchased
  and no real money moves
- PostgreSQL via EF Core
- Confluent Kafka + Schema Registry (Avro): **consumes** `order.paid.v1` (owned by OrderService) to
  auto-create a shipment, and **publishes** `shipment.dispatched.v1` / `shipment.delivered.v1` /
  `shipment.exception.v1` – the first two are consumed back by OrderService
- CQRS via MediatR, FluentValidation for request validation

## How this connects to OrderService

```
OrderService (Pay)  --publishes-->  order.paid.v1  --consumed by-->  ShippingService
                                                                       (creates Shipment,
                                                                        AwaitingFulfillment)

ShippingService (Dispatch, ops)  --publishes-->  shipment.dispatched.v1  --consumed by-->  OrderService
                                                                                             (Status: Shipped)

ShippingService (RefreshTracking, ops)  --publishes-->  shipment.delivered.v1  --consumed by-->  OrderService
                                                                                                    (Status: Delivered)
```

OrderService no longer exposes `POST /api/orders/{id}/ship` or `/deliver` – those ops actions are
now genuinely driven by fulfillment activity here instead of a human calling an endpoint by hand.
`ShipOrderCommand`/`DeliverOrderCommandHandler` still exist in OrderService unchanged; only their
trigger moved from an HTTP controller action to a Kafka consumer.

### Why polling, not a webhook

EasyPost can push tracker updates to a webhook, but there's no public HTTPS endpoint in this
local-dev setup for it to call. Instead, `POST /api/shipments/{id}/refresh-tracking` pulls the
latest status from EasyPost on demand – call it after dispatching to see the tracker progress
through its test-mode states.

### Why no real label purchase

This platform doesn't model parcel weight/dimensions or structured (validated) addresses anywhere
– `Order.ShippingAddress` is a free-text string. Rather than bolt that on just to exercise a label
purchase, this service uses EasyPost's own **test tracking codes**, which simulate a real carrier's
full tracking lifecycle without needing a real shipment behind them – the same reasoning as
PaymentService using Stripe's built-in test PaymentMethod ids instead of a card-collection frontend.

## ⚠️ You need your own EasyPost test-mode API key

Same posture as PaymentService's Stripe key: a real per-account external credential, **never
committed anywhere in this repo**.

1. Create a free EasyPost account: https://www.easypost.com/signup
2. Grab your test API key (starts with `EZAK`) from your dashboard's API Keys page.
3. Configure it locally – pick one:
   - **Running via `dotnet run`**: `dotnet user-secrets set "EasyPost:ApiKey" "EZAK..." --project src/ShippingService.Api`
   - **Running via Docker**: `cp .env.example .env` in this folder, then edit `.env` and set `EASYPOST_API_KEY=EZAK...`. `.env` is gitignored – `docker compose` reads it automatically.

### Test tracking codes

Used as the `trackingCode` when dispatching a shipment – EasyPost's own documented test codes
(carrier must be `USPS`):

| Tracking code | Simulated status |
|---|---|
| `EZ1000000001` | pre_transit (maps to `Dispatched` here) |
| `EZ2000000002` | in_transit |
| `EZ3000000003` | out_for_delivery |
| `EZ4000000004` | delivered |
| `EZ5000000005` | return_to_sender (maps to `Exception`) |
| `EZ6000000006` | failure (maps to `Exception`) |
| `EZ7000000007` | unknown (no-op – status left unchanged) |

## Domain model

`Shipment` – one per order (`OrderId` is a unique index). Id, OrderId, UserId, ShippingAddress
(snapshotted from `order.paid.v1`, display-only), Status (`AwaitingFulfillment` / `Dispatched` /
`InTransit` / `Delivered` / `Exception`), CarrierName, TrackingNumber, ProviderTrackerId (EasyPost's
`trk_...` id – what refresh-tracking polls by), ExceptionReason.

## Local development

```bash
# from commercecore/ShippingService/
docker compose up -d shipping-postgres
dotnet ef database update --project src/ShippingService.Infrastructure --startup-project src/ShippingService.Api
dotnet user-secrets set "EasyPost:ApiKey" "EZAK..." --project src/ShippingService.Api
dotnet run --project src/ShippingService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

`dotnet test` never touches the real EasyPost API – integration tests swap in a fake
`IShippingCarrierGateway`, same as every other real-integration service in this project.

### Endpoints

All endpoints require a valid JWT bearer token (obtained from AuthenticationService's
`/api/auth/login`). `dispatch` and `refresh-tracking` additionally require the **`Admin`** role –
see [AuthenticationService/README.md](../AuthenticationService/README.md#roles) – they're ops
actions gated to fulfillment/admin staff via role, not an ownership check, unlike `Get`/`GetByOrder`
below.

- `GET /api/shipments/{id}` – get a shipment (ownership-checked)
- `GET /api/shipments/order/{orderId}` – get the shipment for an order (ownership-checked)
- `POST /api/shipments/{id}/dispatch` – ops action, Admin (body: `carrier`, `trackingCode`); creates the EasyPost tracker and publishes `shipment.dispatched.v1`
- `POST /api/shipments/{id}/refresh-tracking` – ops action, Admin; re-polls EasyPost and publishes `shipment.delivered.v1`/`shipment.exception.v1` on a genuine status transition

There's no `POST /api/shipments` – shipments are only ever created by the `order.paid.v1` consumer,
never directly via HTTP.

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` match
AuthenticationService's values exactly, same local-dev-only symmetric-key sharing used by every
other service. `EasyPost:ApiKey` is deliberately left empty in every committed file.

## Running everything in Docker

```bash
# from commercecore/ – shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/ShippingService/ – set up your .env first (see above), then:
docker compose up -d --build
```

Brings up Postgres (host port 5438) and the API (host port 8091). `docker compose up` will refuse
to start `shipping-service` with a clear error if `EASYPOST_API_KEY` isn't set – that's intentional.
