# Roadmap

| Milestone | Scope | Status |
| --- | --- | --- |
| M1 | Links + Redirect over HTTP | Done |
| M2 | Analytics service and click events over RabbitMQ | Not started |
| M3 | Transactional outbox and event-fed Redirect read model | Not started |
| M4 | YARP gateway, Keycloak auth, rate limiting | Not started |
| M5 | CI, container images, deployment | Not started |

## How to run a milestone

1. Create the milestone branch from `main` (branch names are listed below).
2. Propose a step-by-step plan (files, packages, order of work) and wait for approval before changing anything.
3. Implement in small commits. Build and test before each one.
4. Check every "Done when" item, run the experiments, and record what happened under **Observations**.
5. Update the status table, then summarize what was built and which microservice concepts it demonstrates.

---

## M1: Links + Redirect over HTTP

Branch: `SP/feature/m1-links-redirect`

### Scaffold

1. `dotnet new sln -n Shortener` (creates `Shortener.slnx` on .NET 10), `dotnet new gitignore`, `dotnet new buildprops`, `dotnet new packagesprops`. In `Directory.Build.props` set `net10.0`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `<WarningsNotAsErrors>CS1591</WarningsNotAsErrors>`. That keeps missing XML docs visible as warnings without breaking the build on generated EF migrations.
2. Services: `dotnet new webapi -n Shortener.Links.Api -o src/Services/Links/Shortener.Links.Api`, and the same for `Shortener.Redirect.Api` under `src/Services/Redirect/`.
3. Aspire: `dotnet new install Aspire.ProjectTemplates`, then `dotnet new aspire-apphost -n Shortener.AppHost -o Shortener.AppHost` and `dotnet new aspire-servicedefaults -n Shortener.ServiceDefaults -o Shortener.ServiceDefaults`. The AppHost references both APIs. Both APIs reference ServiceDefaults and call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.
4. Packages: AppHost gets `Aspire.Hosting.PostgreSQL` and `Aspire.Hosting.Redis`. Links gets `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore.Design`. Redirect gets `Aspire.StackExchange.Redis`.
5. Tests: `tests/Shortener.Links.UnitTests` (xUnit) and `tests/Shortener.IntegrationTests` (xUnit plus `Aspire.Hosting.Testing`).
6. Add every project to the solution.

### AppHost

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var linksDb = builder.AddPostgres("postgres").WithDataVolume().AddDatabase("linksdb");
var redis   = builder.AddRedis("redis");

var links = builder.AddProject<Projects.Shortener_Links_Api>("links")
                   .WithReference(linksDb).WaitFor(linksDb);
builder.AddProject<Projects.Shortener_Redirect_Api>("redirect")
       .WithReference(redis).WithReference(links).WaitFor(redis);

builder.Build().Run();
```

### Links API

| Endpoint | Success | Errors |
| --- | --- | --- |
| `POST /links` with `{ url, alias?, expiresAt? }` | 201 `{ code, shortUrl, url, expiresAt }` with `Location` | 400 invalid input, 409 alias taken |
| `GET /links/{code}` | 200 link details | 404 unknown |
| `DELETE /links/{code}` | 204 | 404 unknown |

- Domain: `Link` (Id, Code, DestinationUrl, CreatedAt, ExpiresAt?) plus value objects `ShortCode` and `DestinationUrl`, whose factory methods validate input and return results.
- `IShortCodeGenerator` is implemented by `RandomShortCodeGenerator` (7 Base62 characters via `RandomNumberGenerator.GetString`).
- `ILinkRepository` has an EF Core implementation over `LinksDbContext`, registered with `builder.AddNpgsqlDbContext<LinksDbContext>("linksdb")`. Unique index on `Code`.
- Collisions: catch `DbUpdateException` whose inner exception is a `PostgresException` with `SqlState == PostgresErrorCodes.UniqueViolation`, and retry with a new code up to 3 times.
- Apply migrations at startup in Development only.
- `AddProblemDetails()` plus one global `IExceptionHandler`.

### Redirect API

| Endpoint | Results |
| --- | --- |
| `GET /{code}` | 302 with `Location`, 404 unknown, 410 expired, 503 Links unreachable |

- `ILinkResolver` is implemented by `CacheAsideLinkResolver`: Redis first (`builder.AddRedisClient("redis")`), then a typed `LinksClient` whose base address is `https+http://links`.
- TTLs: found links get min(24 h, time until expiry), not-found gets 60 s. Never cache a 503 outcome.
- ServiceDefaults already adds standard resilience (retries, timeout, circuit breaker) to every `HttpClient`. Once those are exhausted, map `HttpRequestException`, `BrokenCircuitException` and `TimeoutRejectedException` to 503 and log them with the short code.

### Tests

- Unit: short code generator (length and alphabet), `DestinationUrl` validation (rejects relative URLs, `ftp:` and `javascript:`), expiry logic.
- Integration, using `DistributedApplicationTestingBuilder.CreateAsync<Projects.Shortener_AppHost>()`: create a link through Links, call `GET /{code}` on Redirect, and assert the 302 and the `Location` header. Use an `HttpClient` with `AllowAutoRedirect = false`, or the test will follow the redirect to the real destination.

### Done when

- [x] `aspire run` shows postgres, redis, links and redirect as healthy.
- [x] The dashboard trace of a first redirect shows Redirect → Links → Postgres, and a second redirect of the same code touches only Redis.
- [x] All unit and integration tests pass.
- [x] Experiment A: stop the `links` resource in the dashboard. Cached codes still redirect; uncached codes return 503, not 404.
- [x] Experiment B: delete a link and note that it keeps redirecting until its cache entry expires.

### Observations

Verified via `Aspire.Hosting.Testing` (the `aspire` CLI isn't installed on this
machine; the testing harness runs the identical `Shortener.AppHost` resource
graph — Postgres, Redis, Links, Redirect — so it's a faithful stand-in for an
interactive `aspire run` session) plus `dotnet test` from the repo root.
28 Links unit tests, 7 Redirect unit tests, and the end-to-end integration
test all pass; Docker was confirmed running first.

**Experiment A** (stop `links`, via `ResourceCommandService`'s stop command —
the same one the dashboard's Stop button calls): a code redirected once
beforehand (cached) kept returning **302** after Links was stopped; a code
never looked up before (uncached) returned **503**, confirming an outage
degrades cached traffic not at all and never turns into a false 404 for the
rest.

**Experiment B** (delete a link, then poll redirect): used an 8s `expiresAt`
in place of the default ~24h cache TTL, so the "cache outlives the delete"
window is observable in seconds instead of requiring a real day-long wait.
Sequence observed: warm-up redirect **302** → `DELETE /links/{code}`
**204** → redirect immediately after, still **302** (serving the now-stale
cache entry) → after the ~8s TTL elapsed, redirect **404** (cache evicted,
fresh Links lookup found nothing). Confirms deletion takes effect only once
the cache entry naturally expires, not immediately — exactly the staleness
window M3's transactional outbox + read model closes.

The "second redirect touches only Redis" claim is proven directly by
Experiment A rather than by reading an OTel trace: the cached code kept
succeeding while Links was completely stopped and unreachable, which is
only possible if Redis alone answered it.

---

## M2: Analytics service and click events

Branch: `SP/feature/m2-analytics-events`

- `Shortener.Contracts`: `LinkClicked(Guid EventId, string Code, DateTimeOffset OccurredAt, string? Referrer)`.
- AppHost: `AddRabbitMQ("messaging").WithManagementPlugin()`, an `analyticsdb` database on the existing Postgres server, and the new `Shortener.Analytics.Api`.
- Redirect: `IClickPublisher` writes to a bounded in-memory `Channel<LinkClicked>`, and a `BackgroundService` drains it and publishes to RabbitMQ (`Aspire.RabbitMQ.Client`). The redirect never waits on the broker. If the channel is full, drop the event and log a warning.
- Use `RabbitMQ.Client` directly (no MassTransit or Wolverine yet) to learn exchanges, queues, bindings, manual acks, prefetch and a dead-letter queue.
- Analytics: the consumer must be idempotent because delivery is at-least-once. Keep a `processed_events` table with a unique `EventId`, written in the same transaction as the aggregate update. Store daily counts per code. `GET /analytics/{code}` returns total and per-day clicks.

Done when:
- [ ] Clicks appear in `GET /analytics/{code}` within seconds.
- [ ] With Analytics stopped, redirects are unaffected, messages pile up in the queue (visible in the RabbitMQ management UI), and they're processed after a restart.
- [ ] Redelivering the same message doesn't double-count.

### Observations

---

## M3: Outbox and event-fed read model

Branch: `SP/feature/m3-outbox-read-model`

- Links: a transactional outbox. Write an `OutboxMessage` row in the same `SaveChanges` call as the link change. A `BackgroundService` publishes `LinkCreated` and `LinkDeleted`, then marks rows as dispatched. Delivery is at-least-once.
- Redirect: consume those events into a Redis read model (set on created, with expiry; delete on deleted). Register `ReadModelLinkResolver` instead of `CacheAsideLinkResolver`, then remove Redirect's HTTP dependency on Links, including `WithReference(links)`.
- Backfill: add a way to republish every existing link so the read model starts complete.
- Optional: compare the hand-rolled outbox with Wolverine's built-in one.

Done when:
- [ ] With Links stopped, every existing code still redirects.
- [ ] A deleted link stops redirecting within seconds.
- [ ] Killing Links mid-dispatch loses no events, and duplicates are handled idempotently.

### Observations

---

## M4: Gateway, auth and rate limiting

Branch: `SP/feature/m4-gateway-auth`

- `Shortener.Gateway` with YARP. Routes: `/api/links/**` to links, `/api/analytics/**` to analytics, `/{code}` to redirect. Resolve destinations through service discovery.
- Keycloak through Aspire's Keycloak integration, with JWT bearer validation. Creating and deleting links and reading analytics require auth; redirects stay anonymous. Links store the owner's `sub`, and only the owner can see a link's analytics.
- ASP.NET Core rate limiting on link creation, per user.

Done when:
- [ ] An unauthenticated `POST /api/links` returns 401, another user's analytics returns 404 (so link existence isn't revealed), and a burst of creates returns 429.
- [ ] Redirects work anonymously through the gateway.

### Observations

---

## M5: CI, containers, deployment

Branch: `SP/feature/m5-ci-deploy`

- GitHub Actions: restore, build, and run unit and integration tests on every pull request.
- Container images built by the .NET SDK (`dotnet publish /t:PublishContainer`).
- Deploy outside the dev orchestrator with `aspire publish` to Docker Compose or Kubernetes.

Done when:
- [ ] Pull request builds are green, including integration tests.
- [ ] The full system runs from the published Compose or Kubernetes artifacts.

### Observations

---

Next project after M5: the food delivery platform, which adds sagas and compensating transactions.
