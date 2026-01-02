# ADR 001: Clean Architecture

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

We need a maintainable, testable architecture that separates concerns and allows independent development of layers. The application will grow over time and needs to support multiple platforms and deployment scenarios.

## Decision

Adopt Clean Architecture with 4 primary layers plus a plugin system:

| Layer | Project | Purpose |
|-------|---------|---------|
| Core | `SaveState.Core` | Domain entities, value objects, interfaces |
| Application | `SaveState.Application` | Use cases, MediatR handlers, DTOs |
| Infrastructure | `SaveState.Infrastructure` | Persistence, APIs, external services |
| Presentation | `SaveState.Presentation` + `SaveState.CLI` | UI (Avalonia) and CLI |
| Plugins | `SaveState.Plugins.*` | 19 extensibility modules |

## Compliance Status (January 2, 2026)

| Rule | Status |
|------|--------|
| Core has no external dependencies | ✅ Compliant |
| Application depends only on Core | ✅ Compliant |
| Infrastructure implements Core interfaces | ✅ Compliant |
| Presentation depends on Application | ✅ Compliant |
| No circular dependencies | ✅ Compliant |

## Consequences

- ✅ Clear separation of concerns
- ✅ Easy to test each layer independently
- ✅ Framework-independent core business logic
- ✅ 22 bounded contexts in Core layer
- ⚠️ More files and boilerplate initially (763+ source files)
- ⚠️ Learning curve for new team members

## Current Metrics

| Metric | Value |
|--------|-------|
| Core files | 262 |
| Application files | 221 |
| Infrastructure files | 180 |
| Presentation files | 38 |
| CLI files | 10 |
| Plugin files | 52 |

## Alternatives Considered

- Traditional layered architecture (too coupled to frameworks)
- Hexagonal architecture (similar but more complex for our needs)
- Onion architecture (essentially the same as Clean Architecture)

## References

- "Clean Architecture" by Robert C. Martin
- <https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html>
