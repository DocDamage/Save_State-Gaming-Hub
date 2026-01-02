# ADR 006: Repository Pattern with EF Core

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

Data access needs abstraction for testability and to hide persistence details.

## Decision

Implement repository pattern with EF Core as the ORM. Generic repository with specific repositories for complex queries.

## Compliance Status (January 2, 2026)

| Rule | Status |
|------|--------|
| All repositories implement interfaces | ✅ Compliant |
| Repositories in Infrastructure layer | ✅ Compliant |
| Interfaces in Core layer | ✅ Compliant |
| Complex queries in specific repos | ✅ Compliant |

## Repositories Implemented

| Repository | Entity | Features |
|------------|--------|----------|
| `GameRepository` | Game | Pagination, filtering, soft delete |
| `PlatformRepository` | Platform | Lookup, seeding |
| `MugenCharacterRepository` | MugenCharacter | Stats, filtering |
| `MugenTournamentRepository` | MugenTournament | Bracket queries |
| `MugenMatchHistoryRepository` | MugenMatchHistory | Stats aggregation |

## Database Configuration

| Setting | Value |
|---------|-------|
| Provider | SQLite |
| ORM | EF Core 9.0 |
| Mode | WAL (Write-Ahead Logging) |
| Migrations | Code-first |

## Consequences

- ✅ Testable data access (easy to fake)
- ✅ Framework abstraction
- ✅ Query optimization per entity
- ✅ Supports pagination (mandatory)
- ⚠️ Additional abstraction layer

## Alternatives Considered

- Active Record pattern (tight coupling)
- Direct EF usage (no abstraction)
- Generic repository only (limited for complex domain needs)

## References

- Repository pattern documentation
- EF Core documentation
