# ADR 003: Event-Driven Communication

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

Components need to communicate changes without tight coupling. Domain events should be published and handled asynchronously.

## Decision

Use MediatR for in-process event publishing with domain events for cross-boundary communication.

## Compliance Status (January 2, 2026)

| Rule | Status |
|------|--------|
| Domain events inherit from base | ✅ Compliant |
| Events published via MediatR | ✅ Compliant |
| No direct cross-module calls | ✅ Compliant |
| Async handlers for long-running ops | ✅ Compliant |

## Event Categories

| Category | Examples |
|----------|----------|
| Game Library | GameImported, GameLaunched, GameCompleted |
| MUGEN | MatchCompleted, TournamentStarted |
| Analytics | SessionEnded, AchievementUnlocked |
| AI | KnowledgeUpdated, SearchCompleted |

## Consequences

- ✅ Loose coupling between components
- ✅ Asynchronous processing capabilities
- ✅ Domain event traceability
- ✅ Extensible via new handlers
- ⚠️ Eventual consistency complexity

## Alternatives Considered

- Direct method calls (tight coupling)
- Message queues (overkill for in-process communication)
- Observer pattern (less flexible than events)

## References

- Domain-Driven Design by Eric Evans
