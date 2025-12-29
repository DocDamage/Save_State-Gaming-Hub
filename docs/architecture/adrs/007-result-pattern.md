# ADR 007: Result Pattern for Error Handling

## Status
Accepted

## Context
Exceptions should be for exceptional cases, not business rule violations. Business errors need structured handling.

## Decision
Use Result pattern with Success/Failure states for business operations.

## Consequences
- ✅ Railway-oriented programming
- ✅ Clear error handling
- ✅ No exception abuse
- ⚠️ Different from typical .NET patterns

## Alternatives Considered
- Exceptions for everything (performance issues)
- Return codes (less type-safe)
- Out parameters (ugly API)

## References
- Railway oriented programming
