FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:24-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# NEXT_PUBLIC_* vars are statically inlined into the client bundle at `next build` time - they
# must be build args (baked into the image), not runtime environment variables like API_BASE_URL
# below. See src/lib/env.ts for the full server/client addressing split this build arg is half of.
ARG NEXT_PUBLIC_API_BASE_URL
ARG NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=$NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY

RUN npm run build

FROM node:24-alpine AS prod-deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --omit=dev

FROM node:24-alpine AS runtime
WORKDIR /app

# next.config.ts deliberately has no `output: "standalone"` (it broke the real deployment target,
# Vercel - see that file's comment), so this is a plain `next start` runtime: needs the full
# .next build output, public/, package.json, and production-only node_modules (built separately
# above so devDependencies - eslint, typescript, etc. - don't bloat the image).
COPY --from=prod-deps /app/node_modules ./node_modules
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/package.json ./package.json

# HOSTNAME=0.0.0.0 is required, not cosmetic - `next start` binds to localhost only by default,
# which is unreachable from outside the container.
ENV HOSTNAME=0.0.0.0
ENV PORT=3000
EXPOSE 3000

CMD ["npm", "start"]
