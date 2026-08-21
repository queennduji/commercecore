/**
 * Fails fast at import time (rather than surfacing as a confusing "fetch failed:
 * undefined/api/products" deep inside a component) if required env vars are missing.
 *
 * It must be referenced as a static `process.env.NEXT_PUBLIC_...` member expression below (not a
 * dynamic `process.env[name]` lookup) — that's the literal pattern Next's compiler statically
 * replaces; a dynamic lookup would stay `undefined` in the browser even though the var exists.
 */
function required(name: string, value: string | undefined): string {
  if (!value) {
    throw new Error(
      `Missing required environment variable "${name}". Copy .env.example to .env.local and set it.`,
    );
  }
  return value;
}

/**
 * Server code (Server Components, `app/api/auth/*` Route Handlers) and browser code need
 * different addresses for the same gateway once Storefront runs in Docker: server-side code runs
 * *inside* the Storefront container and needs the Docker-internal address (`API_BASE_URL`, e.g.
 * `http://api-gateway:8080` — deliberately not `NEXT_PUBLIC_`-prefixed, so it's a real runtime
 * `process.env` lookup, settable via docker-compose.yml's `environment:` with no rebuild, and
 * never inlined into the client bundle); the browser isn't on that Docker network at all and
 * needs the host-published `NEXT_PUBLIC_API_BASE_URL` (e.g. `http://localhost:8080`), baked in at
 * `next build` time as a build arg. Under plain `npm run dev` (no Docker — server and browser are
 * the same machine) `API_BASE_URL` is simply unset, so the server side falls back to the same
 * `NEXT_PUBLIC_API_BASE_URL` the browser uses, unchanged from before this split existed.
 *
 * `typeof window === "undefined"` is the standard isomorphic-code idiom for this — safe here
 * because each branch only ever executes in the environment it was written for (the `API_BASE_URL`
 * branch is unreachable in the browser, where it would read `undefined` since it's not
 * `NEXT_PUBLIC_`-prefixed and therefore never inlined).
 */
function resolveApiBaseUrl(): string {
  if (typeof window === "undefined") {
    return process.env.API_BASE_URL ?? required("NEXT_PUBLIC_API_BASE_URL", process.env.NEXT_PUBLIC_API_BASE_URL);
  }
  return required("NEXT_PUBLIC_API_BASE_URL", process.env.NEXT_PUBLIC_API_BASE_URL);
}

export const env = {
  apiBaseUrl: resolveApiBaseUrl(),
};

// Validated lazily (see lib/stripe/client.ts), NOT bundled into the eager `env` object above:
// unlike apiBaseUrl, every page needs, the Stripe key is only needed once someone reaches
// checkout — failing the entire app at startup just because Stripe isn't configured yet would
// take out Phases 1–2 along with it.
export function requiredStripePublishableKey(): string {
  return required("NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY", process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY);
}
