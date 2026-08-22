# Deploying CommerceCore's backend

Runs the backend (everything except Storefront, which deploys separately to Vercel - see
`Storefront/README.md`) as one Docker Compose stack on a VM, alongside whatever else is already
running there. See the plan this was built from for the full reasoning; this file is just the
how-to.

**Not deployed yet**: NotificationService (needs your own Resend/Twilio credentials - it's an
async Kafka consumer, nothing in the Storefront UI calls it, so skipping it doesn't break
anything) and ShippingService (needs an EasyPost API key - once you have one, see "Adding
ShippingService later" below). Order detail pages already handle ShippingService being absent -
they show a static "Preparing your order for shipment" message instead of live tracking.

## One-time setup on the VM

1. **Get the repo onto the VM** (clone it, or `git pull` if already there):
   ```bash
   git clone <this-repo-url> commercecore && cd commercecore
   ```

2. **Create `deploy/.env.prod`** from the template, filled with real values - this file is
   gitignored and must never be committed:
   ```bash
   cp deploy/.env.prod.example deploy/.env.prod
   openssl rand -base64 32   # run 1x, paste as JWT_KEY
   openssl rand -hex 20      # run 5x, paste as each *_DB_PASSWORD and MINIO_ROOT_PASSWORD
   ```
   For `MINIO_ROOT_USER`, any short identifier is fine (e.g. `commercecore-prod`). For
   `STRIPE_SECRET_KEY`, reuse the existing test-mode key in `PaymentService/.env`
   (`sk_test_...`) - same Stripe account as the `pk_test_...` key already wired into Storefront.

3. **Start the stack**:
   ```bash
   docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod up -d --build
   ```
   First build takes a while (9 .NET images + Redpanda + Postgres pulls). Watch it come up:
   ```bash
   docker compose -f deploy/docker-compose.prod.yml ps
   docker compose -f deploy/docker-compose.prod.yml logs -f cc-api-gateway
   ```

4. **Smoke-test locally on the VM**, before any public DNS points here:
   ```bash
   curl http://localhost:8080/health          # -> {"status":"healthy"}
   curl -I http://localhost:9000/minio/health/live   # -> HTTP/1.1 200 OK
   ```

5. **Add the two nginx server blocks** (adjust paths if this VM's nginx layout differs from
   Debian/Ubuntu's default `sites-available`/`sites-enabled`):
   ```bash
   sudo cp deploy/nginx/api.commercecore.app.conf /etc/nginx/sites-available/
   sudo cp deploy/nginx/media.commercecore.app.conf /etc/nginx/sites-available/
   sudo ln -s /etc/nginx/sites-available/api.commercecore.app.conf /etc/nginx/sites-enabled/
   sudo ln -s /etc/nginx/sites-available/media.commercecore.app.conf /etc/nginx/sites-enabled/
   sudo nginx -t && sudo systemctl reload nginx
   ```

6. **Point DNS at this VM** (in Vercel's DNS panel for `commercecore.app`, since that's where the
   domain is registered): add A records for `api` and `media`, both to this VM's public IP. Wait
   for propagation (`dig api.commercecore.app` should return the VM's IP).

7. **Provision HTTPS certs** (only after DNS has propagated - certbot verifies domain ownership
   over HTTP first):
   ```bash
   sudo certbot --nginx -d api.commercecore.app -d media.commercecore.app
   ```
   This rewrites both server blocks in place to add the `listen 443 ssl` + certificate
   directives and set up auto-renewal - no need to hand-edit them afterward.

8. **Verify from outside the VM**: `https://api.commercecore.app/health` and
   `https://media.commercecore.app/minio/health/live` should both respond over HTTPS.

## Redeploying after a code change

```bash
git pull
docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod up -d --build
```
Only changed services rebuild; the rest keep running unaffected. Volumes (Postgres data, MinIO
objects, Redpanda topics) persist across this.

## Adding ShippingService later

Once you have a real EasyPost API key:
1. Add a `shipping-postgres` + `shipping-service` block to `deploy/docker-compose.prod.yml`,
   copying the pattern from `order-postgres`/`order-service` above (own DB password, same
   `Jwt__Key`/Kafka/Otel treatment) plus `EasyPost__ApiKey: "${EASYPOST_API_KEY}"` - see
   `ShippingService/docker-compose.yml` for its exact env var names.
2. Add `EASYPOST_API_KEY=` to `deploy/.env.prod` (and `.env.prod.example`).
3. Redeploy with the same `up -d --build` command above - `cc-api-gateway`'s
   `shipping-cluster` route is already configured to find it at `http://shipping-service:8080`
   once that container exists.

## Notes

- The `commercecore` Docker network here is created **by this compose file** (not `external:
  true` like the per-service dev compose files) since this compose project creates it first - see
  the comment at the top of `docker-compose.prod.yml`. If this VM also runs a separate project
  behind its own nginx (as it did when this was built - see atlas-bank's own compose/nginx setup),
  that nginx needs to join this network too (as an `external: true` reference, since this file
  creates it) to reach `cc-api-gateway`/`minio` by name for reverse-proxying.
- The ApiGateway service here is named `cc-api-gateway`, not the more natural `api-gateway` -
  a sibling project on the same box had already claimed that exact name in its own compose file.
  A container joined to both networks doing a DNS lookup for a name both networks alias
  differently is genuinely ambiguous (Docker's embedded DNS resolution order across multiple
  networks isn't something to rely on), so this avoids the collision outright rather than hoping
  it resolves to the right one. Check any other project sharing this box for name clashes before
  reusing bare/common service names (`redis`, `postgres`, etc.) here too.
- `Otel__TracesEndpoint`/`Otel__LogsEndpoint` are left pointed at `jaeger:4317`/
  `otel-collector:4317` even though nothing is listening there - this is intentional (see the
  compose file's top comment), not a leftover to clean up. Never blank these out.
- If `catalog-service`'s presigned-upload calls to `media.commercecore.app` fail from inside its
  own container (a same-host request going out to the public domain and back in), that's a
  loopback/hairpin-NAT quirk some networks have - check `docker compose logs catalog-service` for
  MinIO connection errors first if product image uploads misbehave after deploy.
