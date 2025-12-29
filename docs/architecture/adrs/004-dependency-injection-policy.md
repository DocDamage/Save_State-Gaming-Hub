# ADR 004: Zero Singletons Policy

## Status
Accepted

## Context
Singletons create tight coupling and make testing difficult. All dependencies should be injected.

## Decision
Zero singletons policy - all services must be registered in DI container and injected as interfaces.

## Consequences
- ✅ Easy to mock dependencies in tests
- ✅ Clear dependency graph
- ✅ Framework-independent code
- ⚠️ More verbose service registration

## Alternatives Considered
- Singleton pattern (makes testing impossible)
- Service locator (hides dependencies)
- Static classes (same issues as singletons)

## References
- Dependency Injection principles
