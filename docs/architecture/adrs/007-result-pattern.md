# ADR 007: Result Pattern for Error Handling

## Status

Accepted

## Date

December 2025 (Original) | January 2, 2026 (Updated)

## Context

Exceptions should be for exceptional cases, not business rule violations. Business errors need structured handling.

## Decision

Use Result pattern with Success/Failure states for business operations.

## Compliance Status (January 2, 2026)

| Rule | Status | Violations |
|------|--------|------------|
| Service methods return Result<T> | ⚠️ Partial | 45+ `return null` |
| No exceptions for business errors | ✅ Compliant | 0 |
| Guard clauses for invalid arguments | ✅ Compliant | 0 |
| ErrorType categorization | ✅ Compliant | 0 |

## Error Types

| ErrorType | Usage |
|-----------|-------|
| `Validation` | Input doesn't meet business rules |
| `NotFound` | Requested entity doesn't exist |
| `Conflict` | Operation conflicts with existing state |
| `Unauthorized` | User lacks permission |
| `External` | Third-party service failure |
| `Internal` | Unexpected internal error |

## Technical Debt

**45+ `return null` statements** need conversion to `Result<T>.Failure()`:

| File | Count | Priority |
|------|-------|----------|
| GameMemoryReader.cs | 8 | Medium |
| PerformanceProfiler.cs | 4 | Medium |
| SmartCategorizationService.cs | 2 | Medium |
| Plugin files | 10+ | Low |

See [TECHNICAL_DEBT_AUDIT_2026-01-02.md](../../reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md)

## Consequences

- ✅ Railway-oriented programming
- ✅ Clear error handling
- ✅ No exception abuse
- ✅ Type-safe error information
- ⚠️ Different from typical .NET patterns
- ⚠️ Requires team training

## Alternatives Considered

- Exceptions for everything (performance issues)
- Return codes (less type-safe)
- Out parameters (ugly API)

## References

- Railway oriented programming
- [Result Pattern Implementation](../../../src/SaveState.Core/Common/Result.cs)
