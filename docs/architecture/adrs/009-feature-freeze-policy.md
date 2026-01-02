# ADR 009: Feature Freeze Policy

## Status

Accepted (Modified)

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

Continuous feature addition prevents stabilization and performance optimization.

## Decision

Feature freeze after core functionality complete. Only bug fixes, performance improvements, and documentation allowed after freeze.

## Current Status (January 2, 2026)

| Phase | Status | Focus |
|-------|--------|-------|
| Core Features | ✅ Frozen | Stable |
| MUGEN Features | ✅ Frozen | Stable |
| AI Features | ✅ Frozen | Web search added |
| CLI Commands | ✅ Frozen | 12/12 complete |
| UI Development | 🔓 Active | Phase 3 in progress |
| Plugin System | 🔓 Active | Extensions allowed |

## Freeze Exceptions

Allowed post-freeze:

- ✅ Bug fixes (any severity)
- ✅ Performance optimizations
- ✅ Documentation updates
- ✅ UI enhancements (surfacing existing features)
- ✅ Plugin development (external to core)

Not allowed post-freeze:

- ❌ New domain entities
- ❌ New database schema changes
- ❌ New external API integrations
- ❌ Breaking interface changes

## Technical Debt During Freeze

| Category | Allowed? |
|----------|----------|
| Fix `return null` → Result | ✅ Yes |
| Fix `async void` | ✅ Yes |
| Add logging to catches | ✅ Yes |
| Refactor for complexity | ✅ Yes |
| Add new features | ❌ No |

## Consequences

- ✅ Stable codebase for optimization
- ✅ Clear milestone for completion
- ✅ Focus on quality over features
- ✅ Predictable release schedule
- ⚠️ Delayed feature delivery for V2

## Alternatives Considered

- No freeze (endless development)
- Early freeze (missing features)
- Phased freezes (complex management)

## References

- Software release management best practices
