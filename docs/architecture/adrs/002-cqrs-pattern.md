# ADR 002: CQRS Pattern

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

The application needs to handle complex queries and commands with different optimization requirements. Read and write operations have different performance and consistency needs.

## Decision

Implement CQRS for write operations with direct queries for reads. Use MediatR for command/query separation.

## Compliance Status (January 2, 2026)

| Rule | Status |
|------|--------|
| All commands use MediatR handlers | ✅ Compliant |
| All queries use MediatR handlers | ✅ Compliant |
| Projections used for read operations | ✅ Compliant |
| No domain entities in query responses | ✅ Compliant |

## Current Metrics

| Metric | Value |
|--------|-------|
| Command handlers | 50+ |
| Query handlers | 30+ |
| Validation pipelines | FluentValidation |
| Caching | Semantic cache for AI |

## Consequences

- ✅ Optimized read and write paths
- ✅ Clear separation of concerns
- ✅ Better performance for complex queries
- ✅ Testable handlers in isolation
- ⚠️ Additional complexity in command/query handlers
- ⚠️ Eventual consistency considerations

## Alternatives Considered

- Full CQRS with separate read/write models (too complex for initial implementation)
- Repository pattern only (doesn't optimize for different query needs)
- Active Record pattern (too coupled to data layer)

## References

- CQRS pattern documentation
- MediatR library documentation
