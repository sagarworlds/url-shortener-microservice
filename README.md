# URL Shortener

A microservices-based URL shortener built with **.NET 10** and **.NET Aspire**, designed as a hands-on learning project for distributed systems patterns: service discovery, event-driven architecture, transactional outbox, CQRS read models, API gateway, and JWT authentication.

## Architecture

```
                        +-----------+
                        |  Gateway  |  YARP reverse proxy
                        |  (Auth,   |  JWT validation
                        |  Rate     |  Rate limiting
                        |  Limit)   |  X-User-Id forwarding
                        +-----+-----+
                              |
              +---------------+---------------+
              |               |               |
        +-----+-----+  +-----+-----+  +------+------+
        | Links API  |  |  Redirect |  |  Analytics  |
        | CRUD ops   |  |  302 hot  |  |  Click stats|
        | Outbox     |  |  path     |  |  Event      |
        | publisher  |  |           |  |  consumer   |
        +-----+------+  +-----+----+  +------+------+
              |               |               |
         +----+----+     +----+----+     +----+----+
         |linksdb  |     |  Redis  |     |analytics|
         |Postgres |     |         |     |  db     |
         +---------+     +---------+     +---------+
              |                               |
              +--------> RabbitMQ <-----------+
                     link-events exchange
```

| Service | Responsibility | Data Store | Milestone |
|---------|---------------|------------|-----------|
| **Links API** | Create, read, delete short links; transactional outbox | Postgres `linksdb` | M1, M3 |
| **Redirect API** | `GET /{code}` returns 302; event-fed Redis read model | Redis | M1, M3 |
| **Analytics API** | Consume `LinkClicked` events, serve per-day click stats | Postgres `analyticsdb` | M2 |
| **Gateway** | YARP entry point, Keycloak JWT auth, per-user rate limiting | none | M4 |

Supporting projects:
- **Shortener.AppHost** -- Aspire orchestration (Postgres, Redis, RabbitMQ, Keycloak, all services)
- **Shortener.ServiceDefaults** -- OpenTelemetry, health checks, service discovery, resilience
- **Shortener.Contracts** -- Integration event records (`LinkCreated`, `LinkClicked`, `LinkDeleted`)

## Key Patterns Demonstrated

- **Database per service** -- each service owns its data store; no cross-service database access
- **Transactional outbox** (M3) -- link changes are atomically written with outbox messages, then published to RabbitMQ by a background dispatcher
- **Event-driven read model** (M3) -- Redirect consumes `LinkCreated`/`LinkDeleted` events to maintain a Redis read model, decoupling it from Links at runtime
- **Idempotent consumers** (M2) -- Analytics deduplicates events via a `processed_events` table in the same transaction as the aggregate update
- **API gateway with YARP** (M4) -- single entry point with config-based routing and Aspire service discovery destination resolution
- **JWT authentication** (M4) -- Keycloak issues tokens; Gateway validates and forwards `sub` as `X-User-Id`; backends enforce resource ownership
- **Per-user rate limiting** (M4) -- fixed-window policy (10 requests/min) on link creation
- **Cache-aside with graceful degradation** (M1) -- Redirect caches links in Redis; when Links is unreachable, cached codes still work, uncached return 503 (never a false 404)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Postgres, Redis, RabbitMQ, Keycloak)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)

```bash
dotnet workload install aspire
```

## Getting Started

1. **Clone the repository**

```bash
git clone https://github.com/your-username/url-shortener.git
cd url-shortener
```

2. **Start Docker Desktop**

3. **Run the system**

```bash
dotnet run --project Shortener.AppHost
```

This starts all services and infrastructure (Postgres, Redis, RabbitMQ, Keycloak) via Aspire. Open the dashboard URL printed in the terminal to see resource health, traces, and logs.

4. **Run the tests**

```bash
dotnet test
```

## API Reference

All endpoints are accessed through the Gateway (default: `http://localhost:5000`).

### Links

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/links` | Required | Create a short link |
| `GET` | `/api/links/{code}` | Required | Get link details |
| `DELETE` | `/api/links/{code}` | Required | Delete a link |

**Create a link:**
```bash
TOKEN=$(curl -s http://localhost:8080/realms/shortener/protocol/openid-connect/token \
  -d grant_type=password -d client_id=shortener-api \
  -d username=alice -d password=alice | jq -r .access_token)

curl -X POST http://localhost:5000/api/links \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com", "alias": "demo", "expiresAt": null}'
```

**Response (201):**
```json
{
  "code": "demo",
  "shortUrl": "http://localhost:5000/demo",
  "url": "https://example.com",
  "expiresAt": null
}
```

### Redirect

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/{code}` | None | 302 redirect to the destination URL |

```bash
curl -L http://localhost:5000/demo
```

### Analytics

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/analytics/{code}` | Required | Click stats for a link (owner only) |

```bash
curl http://localhost:5000/api/analytics/demo \
  -H "Authorization: Bearer $TOKEN"
```

**Response (200):**
```json
{
  "code": "demo",
  "totalClicks": 3,
  "dailyClicks": [
    { "date": "2026-01-15", "count": 3 }
  ]
}
```

### Error Responses

All errors use [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457):

| Status | Meaning |
|--------|---------|
| 400 | Invalid input (bad URL, invalid alias) |
| 401 | Missing or invalid JWT |
| 404 | Link not found or not owned by caller |
| 409 | Alias already taken |
| 410 | Link has expired |
| 429 | Rate limit exceeded (10 creates/min/user) |
| 503 | Upstream service unavailable |

## Project Structure

```
Shortener.slnx
Directory.Build.props               net10.0, nullable, warnings-as-errors
Directory.Packages.props            central package management
Shortener.AppHost/                  Aspire orchestration
Shortener.ServiceDefaults/          shared OpenTelemetry, health, resilience
src/
  Shortener.Contracts/              integration event records
  Shortener.Gateway/                YARP + auth + rate limiting
  Services/
    Links/Shortener.Links.Api/
    Redirect/Shortener.Redirect.Api/
    Analytics/Shortener.Analytics.Api/
tests/
  Shortener.Links.UnitTests/
  Shortener.Redirect.UnitTests/
  Shortener.Analytics.UnitTests/
  Shortener.IntegrationTests/       end-to-end via Aspire.Hosting.Testing
```

Each service follows a consistent four-folder layout:

| Folder | Responsibility | Dependencies |
|--------|---------------|--------------|
| `Domain/` | Entities, value objects | None (no framework types) |
| `Application/` | Use-case handlers, interfaces | Domain only |
| `Infrastructure/` | EF Core, Redis, RabbitMQ, HTTP clients | Application interfaces |
| `Endpoints/` | Minimal API route mapping | Application handlers |

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 |
| Orchestration | .NET Aspire 13.5 |
| API Gateway | YARP 2.3 |
| Auth | Keycloak + JWT Bearer |
| Databases | PostgreSQL (via EF Core) |
| Cache / Read Model | Redis |
| Messaging | RabbitMQ |
| Observability | OpenTelemetry (traces, metrics, logs) |
| Resilience | Microsoft.Extensions.Http.Resilience (Polly) |
| Testing | xUnit, Aspire.Hosting.Testing |

## Milestones

| Milestone | Scope | Status |
|-----------|-------|--------|
| **M1** | Links + Redirect over HTTP, cache-aside with Redis | Done |
| **M2** | Analytics service, click events over RabbitMQ, idempotent consumers | Done |
| **M3** | Transactional outbox, event-fed Redis read model, Redirect decoupled from Links | Done |
| **M4** | YARP gateway, Keycloak auth, per-user rate limiting, resource ownership | Done |
| **M5** | CI, container images, deployment | Not started |

See [docs/roadmap.md](docs/roadmap.md) for detailed specs, "done when" criteria, and experiment observations for each milestone.

## Design Decisions

- **302 Found, never 301** -- browsers cache 301 permanently, which would hide clicks from analytics and make expiry/deletion unenforceable
- **7-char Base62 codes** from `RandomNumberGenerator` -- cryptographically random, not guessable; unique index with 3 retry attempts on collision
- **503 on upstream failure, never 404** -- when Links is unreachable, Redirect returns 503 and caches nothing, so an outage is never mistaken for a missing link
- **Nullable ownership** -- `OwnerId` is nullable everywhere so pre-auth links skip ownership checks; the system is fully backward compatible
- **Database per service** -- Analytics projects link ownership from events into its own `link_owners` table rather than querying Links' database

## Testing

**69 unit tests** covering domain logic, value object validation, handler behavior, and ownership enforcement.

**7 integration tests** (require Docker) proving end-to-end flows:
- Link creation through the full pipeline
- Redirect via the event-fed read model
- Click analytics event propagation
- Gateway auth (401 without token)
- Anonymous redirects through the gateway
- Cross-user ownership isolation (404 for another user's analytics)
- Rate limiting (429 on burst)

```bash
# Unit tests only (no Docker needed)
dotnet test --filter "FullyQualifiedName!~IntegrationTests"

# All tests (Docker required)
dotnet test
```

## License

This project is for educational purposes.
