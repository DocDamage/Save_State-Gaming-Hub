# ADR 004: Zero Singletons Policy

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

Singletons create tight coupling and make testing difficult. All dependencies should be injected.

## Decision

Zero singletons policy - all services must be registered in DI container and injected as interfaces.

## Compliance Status (January 2, 2026)

| Rule | Status |
|------|--------|
| No static service classes | ✅ Compliant |
| All services registered in DI | ✅ Compliant |
| Dependencies injected as interfaces | ✅ Compliant |
| No service locator pattern | ✅ Compliant (except ViewLocator for XAML) |

## DI Registration Summary

| Location | Purpose | Lines |
|----------|---------|-------|
| `DependencyInjection.cs` | Infrastructure services | 494 |
| `Program.cs` (Presentation) | UI services | 50 |
| `Program.cs` (CLI) | CLI services | 35 |

## Service Lifetimes

| Lifetime | Usage |
|----------|-------|
| Singleton | DbContext factory, Configuration |
| Scoped | Repositories, Unit of Work |
| Transient | Handlers, Validators |

## Consequences

- ✅ Easy to mock dependencies in tests
- ✅ Clear dependency graph
- ✅ Framework-independent code
- ✅ Supports multiple scopes
- ⚠️ More verbose service registration (494+ lines)

## Alternatives Considered

- Singleton pattern (makes testing impossible)
- Service locator (hides dependencies)
- Static classes (same issues as singletons)

## References

- Dependency Injection principles
- Microsoft.Extensions.DependencyInjection
