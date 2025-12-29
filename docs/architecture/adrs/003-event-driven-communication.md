# ADR 003: Event-Driven Communication

## Status
Accepted

## Context
Components need to communicate changes without tight coupling. Domain events should be published and handled asynchronously.

## Decision
Use MediatR for in-process event publishing with domain events for cross-boundary communication.

## Consequences
- ✅ Loose coupling between components
- ✅ Asynchronous processing capabilities
- ✅ Domain event traceability
- ⚠️ Eventual consistency complexity

## Alternatives Considered
- Direct method calls (tight coupling)
- Message queues (overkill for in-process communication)
- Observer pattern (less flexible than events)

## References
- Domain-Driven Design by Eric Evans
