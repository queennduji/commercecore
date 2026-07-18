# AuthenticationService

Identity/Auth microservice for the CommerceCore ecommerce platform. Issues JWT access + refresh tokens, publishes user lifecycle events to Kafka, and runs a Quartz.NET job to purge expired refresh tokens.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- PostgreSQL via EF Core + ASP.NET Core Identity
- Confluent Kafka + Schema Registry (Avro) for `auth.user-registered.v1` and `auth.user-logged-in.v1` events
- Quartz.NET for scheduled refresh-token cleanup

## Local development

Kafka + Schema Registry are shared platform infra (one broker for all of CommerceCore, not one per service), so they live in the root [commercecore/docker-compose.yml](../docker-compose.yml) and only need to be started once regardless of which service you're working on:

```bash
# from commercecore/
docker compose up -d

# from commercecore/AuthenticationService/
docker compose up -d postgres
dotnet ef database update --project src/AuthenticationService.Infrastructure --startup-project src/AuthenticationService.Api
dotnet run --project src/AuthenticationService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands now run from the repo root against [commercecore.slnx](../commercecore.slnx) (there's no longer a per-service solution file):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

### Endpoints

- `POST /api/auth/register` — create a user, returns access + refresh tokens
- `POST /api/auth/login` — authenticate, returns access + refresh tokens
- `POST /api/auth/refresh` — rotate a refresh token for a new access token
- `POST /api/auth/revoke` — revoke a refresh token (logout)

### Configuration

Local defaults live in `appsettings.Development.json`, including a dev-only JWT signing key. Never reuse that key outside local development — staging/production must supply their own `Jwt:Key`, `ConnectionStrings:AuthDatabase`, and `Kafka:*` values via environment variables or a secrets store.

## Running everything in Docker

```bash
# from commercecore/ — shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/AuthenticationService/ — this service's own Postgres + itself
docker compose up -d --build
```

Brings up Postgres (host port 5433), Kafka (host port 9092), Schema Registry (host port 8082), and the API (host port 8085). Host ports are intentionally non-default to avoid clashing with other local services on this machine. The service's containers join the shared `commercecore` Docker network created by the root compose file — start the root stack first.
