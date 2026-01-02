# ADR 005: Strong Typing Over Primitive Obsession

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

Primitive types don't convey business meaning and allow invalid values. Domain concepts should be strongly typed.

## Decision

Use value objects and domain-specific types instead of primitives for business concepts.

## Compliance Status (January 2, 2026)

| Rule | Status |
|------|--------|
| Core entities use value objects | ✅ Compliant |
| Validation in value object constructors | ✅ Compliant |
| Immutable value objects | ✅ Compliant |
| Guard clauses for invariants | ✅ Compliant |

## Value Objects Implemented

| Value Object | Domain Concept | Validation |
|--------------|----------------|------------|
| `GameTitle` | Game name | Max 200 chars, non-empty |
| `FilePath` | File system path | Valid path characters |
| `PlaytimeMinutes` | Session duration | Non-negative |
| `ErrorType` | Error classification | Enum-based |
| `MatchResult` | Fight outcome | Win/Loss/Draw/Timeout |

## Consequences

- ✅ Business rules enforced at type level
- ✅ Self-documenting code
- ✅ Compile-time validation
- ✅ Prevents invalid state
- ⚠️ More classes to maintain

## Alternatives Considered

- Primitive types with validation (runtime errors)
- Strings for everything (no type safety)
- Enums for simple cases (limited for complex values)

## References

- Domain-Driven Design by Eric Evans
- "Primitive Obsession" code smell
