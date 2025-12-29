# ADR 006: Repository Pattern with EF Core

## Status
Accepted

## Context
Data access needs abstraction for testability and to hide persistence details.

## Decision
Implement repository pattern with EF Core as the ORM. Generic repository with specific repositories for complex queries.

## Consequences
- ✅ Testable data access
- ✅ Framework abstraction
- ✅ Query optimization
- ⚠️ Additional abstraction layer

## Alternatives Considered
- Active Record pattern (tight coupling)
- Direct EF usage (no abstraction)
- Generic repository only (limited for complex domain needs)

## References
- Repository pattern documentation
- EF Core documentation
