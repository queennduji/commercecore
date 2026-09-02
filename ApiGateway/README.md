# ApiGateway

Single public entry point for the CommerceCore platform – a **YARP reverse proxy**, not a
hand-rolled forwarder. Routes every `/api/*` request to the right backend service by path prefix,
so a client only ever needs to know one base URL (`http://localhost:8080` locally) instead of each
service's individual port.

Deliberately the thinnest service in this platform: no Domain/Application/Infrastructure layers,
no database, no Kafka – just routing config and an auth check. There's no business logic here to
warrant Clean Architecture's usual layering; the "BFF" option (response aggregation across
services) was considered and explicitly deferred – see the root plan/decision history – so this is
a pure reverse-proxy gateway for now.

## Stack

- .NET 10 (ASP.NET Core), minimal hosting (no MVC controllers – YARP owns the entire request
  pipeline)
- [YARP](https://microsoft.github.io/reverse-proxy/) (`Yarp.ReverseProxy`), Microsoft's own
  reverse-proxy toolkit – config-driven routing via `appsettings.json`'s `ReverseProxy` section
- JWT bearer auth (same shared key/issuer/audience every other service uses) for the gateway's own
  route-level enforcement – see below
- `Microsoft.AspNetCore.RateLimiting` (built into the shared framework, no extra package) – a
  global fixed-window limiter, per client IP – see below

## Routing

| Path prefix | Routes to | Backend host port |
|---|---|---|
| `/api/auth/*` | AuthenticationService | 8085 |
| `/api/products/*`, `/api/categories/*` | CatalogService | 8086 |
| `/api/inventory/*`, `/api/locations/*` | InventoryService | 8088 |
| `/api/carts/*` | CartService | 8087 |
| `/api/orders/*` | OrderService | 8089 |
| `/api/payments/*` | PaymentService | 8090 |
| `/api/shipments/*` | ShippingService | 8091 |
| `/api/notifications/*` | NotificationService | 8092 |

## Auth enforcement – why it's per-route, not blanket

The gateway validates JWTs itself and rejects invalid/missing tokens with `401` **before**
proxying – but only on the four routes whose backend controllers are blanket `[Authorize]` with no
anonymous actions: `/api/orders`, `/api/payments`, `/api/shipments`, `/api/notifications`.

The other four route groups (`/api/auth`, `/api/products` + `/api/categories`,
`/api/inventory` + `/api/locations`, `/api/carts`) are left **pass-through** at the gateway – no
`AuthorizationPolicy` set on those routes – because their backend controllers mix
`[AllowAnonymous]` and `[Authorize]` on individual actions (e.g. browsing products/categories,
checking inventory, guest cart operations, and registering/logging in are all intentionally
anonymous today; only writes like creating a category or product require a token). Blanket gateway
enforcement on those routes would incorrectly reject requests that are legitimately anonymous.

Every downstream service **still validates independently** either way – this policy only decides
whether the *gateway* rejects before proxying; it's defense-in-depth, not a replacement for each
service's own `[Authorize]`/`[AllowAnonymous]` attributes.

## Rate limiting

A single global limiter applies to **every** route, including `/health` and unauthenticated
requests to protected routes – abuse doesn't stop at the auth check, so the limiter runs ahead of
`UseAuthentication` in the pipeline. Fixed-window, partitioned by client IP
(`HttpContext.Connection.RemoteIpAddress`): each IP gets its own independent budget.

Configured via the `RateLimiting` section (`appsettings.json`):

```json
"RateLimiting": {
  "PermitLimit": 100,
  "WindowSeconds": 60
}
```

100 requests per 60-second window per IP by default. Exceeding it returns `429 Too Many Requests`
with a `Retry-After` header (seconds until the window's next replenishment); `QueueLimit` is `0`
– rejected requests fail immediately rather than queueing, so a client never sits waiting on a
gateway that's about to say no anyway.

## Local development

```bash
# from commercecore/ – shared Kafka + Schema Registry + MinIO + Redis, once
docker compose up -d

# start whichever backend services you want reachable through the gateway (see their READMEs)

# from commercecore/ApiGateway/
dotnet run --project src/ApiGateway.Api
```

`appsettings.Development.json` points every cluster at `localhost:<port>` for local `dotnet run`
use, matching each service's own `dotnet run` port. There's no Swagger UI here (no controllers to
document) – `GET /health` is available for a liveness check, and everything else is proxied as-is.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

## Running everything in Docker

```bash
# from commercecore/ – shared Kafka + Schema Registry + MinIO + Redis, once
docker compose up -d

# from commercecore/ApiGateway/
docker compose up -d --build
```

In Docker, `docker-compose.yml` overrides every cluster destination to the backend's Docker network
service name on its internal container port (e.g. `http://authentication-service:8080`) instead of
the host-mapped ports used for local `dotnet run` – see the `ReverseProxy__Clusters__*` environment
variables in that file. Bring up whichever backend services you want reachable; the gateway itself
has no dependency on any of them being up (routes to a down service just return a proxy error for
that path, same as hitting the service directly).

Exposed on host port 8080.
