# CommerceCore

Ecommerce microservices platform. Each service lives in its own top-level folder in this repo (one repo, one service per subfolder), built one service at a time.

## Stack

- .NET 10, Clean Architecture per service (Domain / Application / Infrastructure / Api), CQRS via MediatR
- Confluent Kafka + Schema Registry (Avro) as the shared event bus — one broker/registry for the whole platform, each service owns its own topics
- MinIO (S3-compatible) as shared object storage — one instance for the whole platform, each service gets its own bucket(s)
- PostgreSQL, one database per service
- Quartz.NET for background jobs

## Services

- [AuthenticationService](AuthenticationService/README.md) — Identity/Auth: JWT issuance, user registration/login, refresh token lifecycle
- [CatalogService](CatalogService/README.md) — Products and categories, full CRUD, validates JWTs issued by AuthenticationService for writes

## Local development

Shared infra (Kafka + Schema Registry + MinIO) is started once from the root, before starting any individual service:

```bash
docker compose up -d
```

Then follow the README in whichever service you're working on for its own database/run instructions.

## Solution

All services' projects are included in the root [commercecore.slnx](commercecore.slnx):

```bash
dotnet build commercecore.slnx
dotnet test commercecore.slnx
```
