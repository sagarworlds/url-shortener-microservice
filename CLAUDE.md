# URL Shortener: .NET microservices learning project

This repo exists so its owner can get hands-on microservices experience with .NET 10 and Aspire. Work in small, reviewable steps and explain the *why* behind non-obvious choices in a sentence or two. Propose changes to the architecture or to the design decisions below before making them.

Milestone specs, status and definitions of done: @docs/roadmap.md
Update the status table there when a milestone is done.

## Architecture

| Service | Responsibility | Data | Milestone |
| --- | --- | --- | --- |
| Links API | Create, read and delete short links | Postgres `linksdb` | M1 |
| Redirect API | Hot path: `GET /{code}` returns 302 | Redis | M1 |
| Analytics API | Consume `LinkClicked` events, serve click stats | Postgres `analyticsdb` | M2 |
| Gateway | YARP entry point, Keycloak auth, rate limiting | none | M4 |

Supporting projects: `Shortener.AppHost` (Aspire orchestration for local dev), `Shortener.ServiceDefaults` (OpenTelemetry, health checks, service discovery, standard resilience), `Shortener.Contracts` (integration event records, from M2).

Boundaries that must always hold:
- Database per service. A service never touches another service's database.
- `Shortener.Contracts` holds plain event records only: no domain types, EF entities or logic.
- Services call each other through Aspire service discovery names (e.g. `https+http://links`), never hard-coded hosts or ports.
- Redirect must not `WaitFor(links)` in the AppHost. It has to start and serve cached links while Links is down.

## Repository layout

```text
Shortener.slnx
Directory.Build.props            net10.0, nullable, warnings as errors, XML docs
Directory.Packages.props         central package versions
Shortener.AppHost/
Shortener.ServiceDefaults/
src/
  Shortener.Contracts/           M2
  Shortener.Gateway/             M4
  Services/
    Links/Shortener.Links.Api/
    Redirect/Shortener.Redirect.Api/
    Analytics/Shortener.Analytics.Api/   M2
tests/
  Shortener.Links.UnitTests/
  Shortener.IntegrationTests/    Aspire.Hosting.Testing
```

Each service is one project with four folders:
- `Domain/`: entities and value objects (`ShortCode`, `DestinationUrl`). No EF, HTTP or framework types.
- `Application/`: use-case handlers plus the interfaces they depend on (`ILinkRepository`, `IShortCodeGenerator`, `ILinkResolver`).
- `Infrastructure/`: implementations (EF Core, Redis, typed HttpClients, RabbitMQ).
- `Endpoints/`: Minimal API route mapping only. No business logic.

The key seam is `ILinkResolver` in Redirect: `CacheAsideLinkResolver` in M1, `ReadModelLinkResolver` in M3. Swapping them must be a one-line DI change with no endpoint edits.

## Commands

- `aspire run`: start the whole system (Docker must be running), then open the dashboard URL it prints.
- `dotnet build` and `dotnet test` from the repo root.
- `dotnet ef migrations add <Name> --project src/Services/Links/Shortener.Links.Api` (needs the `dotnet-ef` tool).
- Prefer non-interactive commands, because prompts can't be answered from here: `dotnet new aspire-apphost` rather than `aspire init`, `dotnet add package` rather than `aspire add`.

## Git workflow (mandatory)

1. Never commit on `main`, `master` or `develop`. Before changing any code, create a branch from `main` named `SP/<type>/<short-description>`, where type is `feature`, `bugfix`, `refactor`, `test`, `docs` or `chore`. Example: `SP/feature/m1-links-redirect`.
2. Never add `Co-Authored-By:` trailers or "Generated with Claude Code" lines to commit messages.
3. Use Conventional Commits, e.g. `feat(links): add Base62 short code generator`.
4. Commit in small steps. `dotnet build` and `dotnet test` must pass before every commit.
5. Don't push, merge or open pull requests unless asked.

`.claude/hooks/guard-commits.sh` enforces rules 1 and 2. If it blocks a commit, fix the cause (switch branches or clean the message). Never edit or disable the hook.

## Coding standards

- **SOLID, strictly.**
  - Single responsibility: endpoints map HTTP, handlers orchestrate one use case, infrastructure does I/O. One reason to change per class.
  - Open/closed: extend by adding implementations (a new `ILinkResolver`), not by editing callers.
  - Liskov substitution: every implementation honours its interface's documented contract, including exceptions and null handling.
  - Interface segregation: small, role-specific interfaces. No catch-all repositories or services.
  - Dependency inversion: `Application` depends only on abstractions, `Infrastructure` implements them, and wiring happens in DI.
- **Error handling: no silent failures.**
  - Expected failures (invalid URL, alias taken, not found, expired) are returned as result objects from handlers and mapped to ProblemDetails with 400, 404, 409 or 410.
  - Unexpected failures go to one global `IExceptionHandler`, which logs them with context and returns a ProblemDetails 500.
  - Catch only specific exceptions you can handle, e.g. `PostgresException` where `SqlState == PostgresErrorCodes.UniqueViolation`. No empty catches and no `catch (Exception)` outside the global handler.
  - Log with structured context (short code, link id) using message templates. Never log secrets.
- **Documentation.** XML doc comments (`///`) on every public type and member: summary, `<param>`, `<returns>`, `<exception>`. Inline comments explain *why* (business rules, trade-offs, formulas) and never restate the code.
- **Clean code.** Small focused methods, descriptive names, readability over cleverness, pure functions where practical, no dead or commented-out code.
- **Tests.** xUnit. Unit tests for Domain and Application logic, integration tests through `Aspire.Hosting.Testing`. Name tests `Method_Scenario_ExpectedResult`.

## Design decisions (discuss before changing)

- Redirect answers **302 Found**, never 301. Browsers cache 301s permanently, which would hide clicks from analytics and make expiry and deletion unenforceable.
- Short codes are 7 Base62 characters from `RandomNumberGenerator.GetString`, so they can't be guessed or enumerated. A unique index backs them. On a unique violation, retry with a new code up to 3 times, then fail with a clear error. Custom aliases never retry: a collision is a 409.
- Only absolute `http` and `https` destination URLs are accepted.
- Redirect caches found links for min(24 h, time until expiry) and caches not-found results for about 60 s.
- When Links is unreachable (`HttpRequestException`, or Polly's `BrokenCircuitException` or `TimeoutRejectedException`), Redirect returns **503** and caches nothing. An outage must never become a 404.
- Losing an occasional click event is acceptable (M2 buffers clicks in memory). Losing a link is not (M3 adds a transactional outbox).
