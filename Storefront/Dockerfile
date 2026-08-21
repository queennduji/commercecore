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

FROM node:24-alpine AS runtime
WORKDIR /app

# output: "standalone" (next.config.ts) traces only the files actually needed at runtime - no
# node_modules copy needed here at all, unlike a plain `next start` deployment.
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY --from=builder /app/public ./public

# HOSTNAME=0.0.0.0 is required, not cosmetic - the standalone server.js binds to localhost only
# by default, which is unreachable from outside the container.
ENV HOSTNAME=0.0.0.0
ENV PORT=3000
EXPOSE 3000

CMD ["node", "server.js"]
