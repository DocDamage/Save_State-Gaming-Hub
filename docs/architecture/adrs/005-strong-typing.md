# ADR 005: Strong Typing Over Primitive Obsession

## Status
Accepted

## Context
Primitive types don't convey business meaning and allow invalid values. Domain concepts should be strongly typed.

## Decision
Use value objects and domain-specific types instead of primitives for business concepts.

## Consequences
- ✅ Business rules enforced at type level
- ✅ Self-documenting code
- ✅ Compile-time validation
- ⚠️ More classes to maintain

## Alternatives Considered
- Primitive types with validation (runtime errors)
- Strings for everything (no type safety)
- Enums for simple cases (limited for complex values)

## References
- Domain-Driven Design by Eric Evans
