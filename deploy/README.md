# Deploying CommerceCore's backend

Runs the backend (everything except Storefront, which deploys separately to Vercel - see
`Storefront/README.md`) as one Docker Compose stack on a VM, alongside whatever else is already
running there. See the plan this was built from for the full reasoning; this file is just the
how-to.

**Not deployed yet**: NotificationService (needs your own Resend/Twilio credentials - it's an
async Kafka consumer, nothing in the Storefront UI calls it, so skipping it doesn't break
anything). ShippingService is deployed (see "ShippingService" below) - order detail pages still
handle it being briefly unreachable during a redeploy the same way they always did: a static
"Preparing your order for shipment" message instead of live tracking.

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
   For `ADMIN_EMAIL`, see "Becoming an admin" below - can be left blank and set later.

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

5. **Add the two nginx server blocks.** This VM doesn't run a standalone host-installed
   nginx — see "Sharing nginx with another project on the same VM" below instead, which is
   what's actually in use. For a genuinely standalone box with nginx installed directly on
   the host (adjust paths if its layout differs from Debian/Ubuntu's default
   `sites-available`/`sites-enabled`):
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

## ShippingService

Runs the same as every other service (own `shipping-postgres`, `SHIPPING_DB_PASSWORD` in
`deploy/.env.prod`), plus `EASYPOST_API_KEY` - a real EasyPost test-mode key, reusing the one
already in `ShippingService/.env` unless you'd rather keep this deployment on its own. It only
ever creates shipments reactively (consuming `order.paid.v1` off Redpanda), so there's no
synchronous `*Service__BaseUrl` wiring for it anywhere - `cc-api-gateway`'s `shipping-cluster`
route (`http://shipping-service:8080`) exists purely for the Storefront's own read-only tracking
lookups on the order detail page.

## Sharing nginx with another project on the same VM

This is the actual current setup (as of when this was written, this VM also runs atlas-bank),
not the standalone host-nginx path described in step 5 above. Instead of installing nginx
directly on the host, the other project's own dockerized nginx picks up CommerceCore's server
blocks via a bind-mounted `conf.d` directory:

1. The other project's nginx service joins this stack's `commercecore` network as an
   `external: true` reference (see the Notes section below) - that's what lets it reverse-proxy
   to `cc-api-gateway`/`minio` by container name.
2. Its nginx service bind-mounts this repo's `deploy/nginx/conf.d/` directory onto its own
   `/etc/nginx/conf.d/`, and its main `nginx.conf` has `include /etc/nginx/conf.d/*.conf;`
   inside the `http {}` block to pick it up. `deploy/nginx/conf.d/` (not the top-level
   `deploy/nginx/*.conf` files used by step 5) is the version meant for this scenario - it
   proxies by Docker container name instead of `127.0.0.1:<port>`, since nginx and
   `cc-api-gateway`/`minio` are all containers here, not host processes.
3. HTTPS certs for `api.commercecore.app`/`media.commercecore.app` are provisioned and renewed
   by the OTHER project's certbot container, not one of this project's own services - the two
   projects share one `certbot-conf` volume. See that project's `nginx/init-letsencrypt.sh`
   (or equivalent) for the actual issuance command; `certbot renew`'s periodic loop picks up
   both projects' certs automatically since they all live under the same mounted
   `/etc/letsencrypt/live/`.
4. Redeploying CommerceCore's own services (`up -d --build`, as above) does **not** touch
   nginx at all - nginx is entirely owned and run by the other project's compose stack. If you
   change anything under `deploy/nginx/conf.d/`, the other project's nginx container needs to
   be recreated (not just reloaded) to pick up the new bind-mounted files, since Docker bind
   mounts are set at container-create time.

## Becoming an admin

Product/category/inventory writes and refunds (CatalogService's Products/Categories/
ProductImages controllers, InventoryService's Locations controller and the `/adjust` endpoint,
OrderService's `/refund`, PaymentService's `/refund`, ShippingService's `/dispatch` and
`/refresh-tracking`) all require the `Admin` role - a normal registered customer account can't
reach them. There's no admin-management UI; the only way to grant it is:

1. Set `ADMIN_EMAIL` in `deploy/.env.prod` to the email you'll register (or already registered)
   with.
2. Redeploy `authentication-service` so it picks up the new env var:
   ```bash
   docker compose -f deploy/docker-compose.prod.yml --env-file deploy/.env.prod up -d authentication-service
   ```
3. If that email is **already registered**, the role is assigned automatically the moment
   `authentication-service` starts (its `AdminRoleSeeder` runs once at startup) - no action
   needed beyond the redeploy above. Log out and back in afterward (or just wait for your access
   token to naturally refresh) so your JWT picks up the new role claim.
4. If that email **hasn't registered yet**, register normally through the Storefront - the role
   is assigned immediately as part of registration, no restart needed.

`ADMIN_EMAIL` currently supports exactly one address (`Admin__Emails__0` under the hood). For
more than one admin, add `Admin__Emails__1`, `Admin__Emails__2`, etc. as additional `environment:`
entries on `authentication-service` in `docker-compose.prod.yml`, following the same pattern.

## Notes

- The `commercecore` Docker network here is created **by this compose file** (not `external:
  true` like the per-service dev compose files) since this compose project creates it first - see
  the comment at the top of `docker-compose.prod.yml`. If this VM also runs a separate project
  behind its own nginx, see "Sharing nginx with another project on the same VM" above - that
  nginx needs to join this network too (as an `external: true` reference, since this file
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
