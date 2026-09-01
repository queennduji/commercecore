# CatalogService

Products and categories microservice for the CommerceCore ecommerce platform. Full CRUD on both, publishes product lifecycle events to Kafka, and validates JWTs issued by AuthenticationService for write access.

## Stack

- .NET 10 (ASP.NET Core Web API), Clean Architecture (Domain / Application / Infrastructure / Api)
- PostgreSQL via EF Core
- Confluent Kafka + Schema Registry (Avro) for `catalog.product-created.v1`, `catalog.product-updated.v1`, `catalog.product-deleted.v1` events
- CQRS via MediatR, FluentValidation for request validation

Unlike AuthenticationService, command/query handlers live in the **Application** layer here rather than Infrastructure — they only depend on repository interfaces (`IProductRepository`, `ICategoryRepository`), not a framework-coupled type like `UserManager<T>`, so there's no forcing constraint pushing them into Infrastructure.

## Local development

Kafka + Schema Registry are shared platform infra — see the root [commercecore/README.md](../README.md) for starting them once.

```bash
# from commercecore/CatalogService/
docker compose up -d postgres
dotnet ef database update --project src/CatalogService.Infrastructure --startup-project src/CatalogService.Api
dotnet run --project src/CatalogService.Api
```

Swagger UI is available at `/swagger` in Development.

Build/test commands run from the repo root against [commercecore.slnx](../commercecore.slnx):

```bash
# from commercecore/
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```

### Endpoints

All `GET` endpoints are public. `POST`/`PUT`/`DELETE` on products, categories, and product images
require a JWT bearer token carrying the **`Admin`** role, not just any authenticated caller — see
[AuthenticationService/README.md](../AuthenticationService/README.md#roles) for how that role gets
assigned.

- `GET /api/categories` / `GET /api/categories/{id}` — list / get a category
- `POST /api/categories` — create a category
- `PUT /api/categories/{id}` — update a category
- `DELETE /api/categories/{id}` — delete a category (blocked with 400 if it still has products assigned)
- `GET /api/products` — list products, filterable by `categoryId`/`status`, paginated (`page`/`pageSize`)
- `GET /api/products/{id}` — get a product
- `POST /api/products` — create a product (starts in `Draft` status)
- `PUT /api/products/{id}` — update a product (name, description, price, status, category)
- `DELETE /api/products/{id}` — soft-delete: sets status to `Archived` rather than removing the row
- `POST /api/products/{productId}/images/upload-url` — get a MinIO presigned PUT URL to upload an
  image directly from the caller to object storage, bypassing this API for the actual bytes
- `POST /api/products/{productId}/images` — record an image against a product once its upload (via
  the presigned URL above) has completed
- `DELETE /api/products/{productId}/images/{imageId}` — remove a product image

### Configuration

Local defaults live in `appsettings.Development.json`. `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience` must match AuthenticationService's values exactly (copied verbatim) since this service only *validates* tokens issued elsewhere — it never issues its own. This symmetric-key sharing is only correct for local dev; a real deployment needs a shared secrets store or a move to asymmetric (RS256) signing so CatalogService only needs a public key.

## Running everything in Docker

```bash
# from commercecore/ — shared Kafka + Schema Registry, once
docker compose up -d

# from commercecore/CatalogService/ — this service's own Postgres + itself
docker compose up -d --build
```

Brings up Postgres (host port 5434) and the API (host port 8086). Host ports are intentionally non-default to avoid clashing with other local services on this machine, including AuthenticationService's own containers.
