# Storefront

Customer-facing storefront for CommerceCore - Next.js (App Router) + TypeScript, talking to the
[ApiGateway](../ApiGateway/README.md) as its only backend dependency.

**Phase 1**: anonymous, read-only product browsing - home page, product listing with category filtering
and pagination, product detail with images and live stock.

**Phase 2**: registration/login and a shopping cart that works for both guests and signed-in users, with a
guest cart merging into the user's cart on login.

**Phase 3**: checkout - shipping address, order creation, and a real Stripe test-mode card payment via
Stripe Elements, ending on an order confirmation page (`/orders/[id]`).

**Phase 4**: order history (`/orders`) and shipment tracking on the order detail page - this closes out
the planned phases. **Docker packaging** (current scope): its own `Dockerfile` + `docker-compose.yml`,
matching every other service in this repo - see "Run with Docker" below.

## Stack

- Next.js 16 (App Router), TypeScript
- Tailwind CSS v4 + [shadcn/ui](https://ui.shadcn.com/) (component primitives copied into `src/components/ui`,
  not a runtime dependency)
- [TanStack Query](https://tanstack.com/query) for cart/order state (client-side cache over each service's
  own source of truth) and [sonner](https://sonner.emilkowal.ski/) for toasts
- [Stripe Elements](https://stripe.com/docs/js) (`@stripe/stripe-js` + `@stripe/react-stripe-js`) for
  checkout - a real (test-mode) `CardElement` creates a Stripe PaymentMethod client-side, handed to
  OrderService's `/pay` endpoint. PaymentService charges with `Confirm=true, OffSession=true`, so this is
  fully synchronous - no 3D Secure/`requires_action` handling needed
- Product/category/inventory reads happen server-side (React Server Components) directly against the
  ApiGateway. Cart, checkout, and order reads/writes happen **client-side**, straight to the gateway (its
  anonymous-by-design and `[Authorize]` routes alike needed a CORS allow - see
  `ApiGateway/src/ApiGateway.Api/Program.cs`)
- Auth is a thin BFF: `app/api/auth/*` Route Handlers proxy AuthenticationService and hold the refresh
  token in an **httpOnly cookie** the client never touches; the short-lived access token lives in a React
  Context (`AuthProvider`), refreshed silently ~1 minute before it expires

## Local development

Requires the shared infra, **CatalogService**, **InventoryService**, **CartService**,
**AuthenticationService**, **OrderService**, **PaymentService**, **ShippingService**, and **ApiGateway**
running (see the [root README](../README.md) and each service's own README). PaymentService additionally
needs its own Stripe test-mode secret key, and ShippingService its own EasyPost test-mode key - see their
READMEs. Without ShippingService running, order detail pages just show "Preparing your order for
shipment" indefinitely instead of real tracking info - everything else still works.

```bash
# from commercecore/ - shared Kafka + Schema Registry + MinIO + Redis, once
docker compose up -d

# start CatalogService, InventoryService, CartService, AuthenticationService, OrderService,
# PaymentService, ShippingService, and ApiGateway per their own READMEs
```

Then, from `Storefront/`:

```bash
npm install
cp .env.example .env.local
npm run dev
```

Open [http://localhost:3000](http://localhost:3000). Seed at least one category and a couple of products
with images via CatalogService's Postman collection or Swagger UI - nothing renders without real catalog
data.

### Run with Docker

Storefront has its own `Dockerfile` + `docker-compose.yml` like every other service in this repo,
joining the external `commercecore` network. ApiGateway must already be running as **its own**
`docker compose` service (not just `dotnet run`) for the internal `api-gateway:8080` DNS name this
container relies on to resolve.

```bash
# from Storefront/
cp .env.example .env   # separate from .env.local - Compose reads .env for build.args
docker compose up -d --build
```

Two env vars behave differently here than under plain `npm run dev`, because "the server" (this
container) and "the browser" (your host machine) are no longer the same machine - see
`src/lib/env.ts` for the full reasoning:

- `NEXT_PUBLIC_API_BASE_URL`/`NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` are **build args** (from
  `Storefront/.env`), baked into the client bundle at image-build time - changing them means
  rebuilding (`docker compose up -d --build`), not just restarting.
- `API_BASE_URL` (no `NEXT_PUBLIC_` prefix, set directly in `docker-compose.yml`) points
  server-side code (Server Components, `app/api/auth/*` Route Handlers) at the Docker-internal
  gateway address instead - a true runtime value, changeable without a rebuild.

### Testing checkout

Use [Stripe's test cards](https://docs.stripe.com/testing#cards) at the payment step - any future
expiry date and any 3-digit CVC work:

| Card number | Result |
|---|---|
| `4242 4242 4242 4242` | Succeeds |
| `4000 0000 0000 0002` | Always declines |
| `4000 0000 0000 9995` | Declines with insufficient funds |

## Environment variables

| Variable | Purpose | Local default |
|---|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | Base URL of the ApiGateway, as reached from the **browser**. Public on purpose - cart/checkout calls go straight from the browser to the gateway, not just from Storefront's own server | `http://localhost:8080` |
| `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` | Stripe test-mode publishable key (`pk_test_...`), same Stripe account as PaymentService's secret key - see [dashboard.stripe.com/test/apikeys](https://dashboard.stripe.com/test/apikeys) | - (required to reach `/checkout`; the rest of the site works without it) |
| `API_BASE_URL` | Base URL of the ApiGateway, as reached from **server-side code** (Server Components, `app/api/auth/*`). Docker only - under plain `npm run dev` this is unset and server-side code falls back to `NEXT_PUBLIC_API_BASE_URL` | `http://api-gateway:8080` (Docker only) |

## Scripts

```bash
npm run dev     # start the dev server
npm run build   # production build (also catches type errors)
npm run start   # run a production build
npm run lint    # eslint
```
