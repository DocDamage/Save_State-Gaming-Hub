# Architecture Decision Log

## ADR-001: Clean Architecture Adoption
**Date**: August 2025
**Status**: ✅ Accepted
**Context**: Needed testable, maintainable architecture
**Decision**: Adopted 4-layer Clean Architecture
**Consequences**:
- ✅ Easy to test business logic
- ✅ Easy to swap implementations
- ❌ More boilerplate initially
- ❌ Steeper learning curve

**Alternatives Considered**:
- Layered (rejected: tight coupling)
- Hexagonal (rejected: too abstract)