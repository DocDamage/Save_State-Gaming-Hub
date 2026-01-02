# SaveStateReborn Engineering Rules & Principles

**Status**: ✅ Active (v1.0.0 Released - Production)
**Last Updated**: January 1, 2026 (v1.0.0 Release)
**Maintained By**: Architecture Team
**Next Review**: January 15, 2026
**Related Documents**: [AI_MASTER_CONTEXT.md](./AI_MASTER_CONTEXT.md), [LESSONS_LEARNED.md](planning/LESSONS_LEARNED.md), [TECHNICAL_DEBT_REMEDIATION_PLAN.md](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md)

---

## Table of Contents

- [Current Compliance Status](#-current-compliance-status)
- [Architecture Rules](#-architecture-rules)
- [AI & Automation Rules](#-ai--automation-rules)
- [CLI & Presentation Rules](#-cli--presentation-rules)
- [Infrastructure & Code Quality](#-infrastructure--code-quality)
- [Testing Rules](#-testing-rules)
- [Gaming & Performance Rules](#-gaming--performance-rules)
- [How These Rules Are Enforced](#-how-these-rules-are-enforced)
- [Rule Severity Levels](#-rule-severity-levels)
- [Exception Handling Policy](#-exception-handling-policy)
- [Current Violations](#-current-violations)

---

**Version**: 2.1 (January 1, 2026 - Post-Compilation Fix Update)

These rules are derived from the lessons learned during the development and stabilization of V2.1. They must be followed for all new feature development and refactoring.

---

## 📊 Current Compliance Status

> [!NOTE]
> **Phase 0 & 1 Complete**: All compilation errors and sync-over-async violations resolved on January 1, 2026.

### Rule Compliance Summary (January 1, 2026 Audit)

| Rule Category | Status | Violations |
|---------------|--------|------------|
| **Build compiles** | ✅ Compliant | 0 errors |
| **Sync-over-async** | ✅ Compliant | 0 (Fixed!) |
| **Async void forbidden** | ✅ Compliant | 0 |
| **Thread.Sleep forbidden** | ✅ Compliant | 0 |
| **Empty catch blocks** | ✅ Compliant | 0 |
| **IHttpClientFactory** | ✅ Compliant | 0 (Fixed!) |
| **Result pattern** | ✅ Compliant | 0 (Fixed!) |

---

## 🏗️ Architecture Rules

### 1. Layers & Dependencies

- **Must** maintain a strict 4-layer separation: `Core` → `Application` → `Infrastructure` → `Presentation`.
- **Must Not** allow `Core` or `Application` to depend on `Infrastructure` or `Presentation`.
- **Must** use the **Adapter Pattern** for all third-party gaming providers (Steam, GOG, RetroAchievements, etc.).

### 2. CQRS & MediatR

- **Must** use MediatR for all cross-layer operations.
- **Must** separate Read and Write models. Use **Projections** (Records) for data retrieval to reduce memory footprint.
- **Must Not** use heavy Domain Entities for simple UI listings.

### 3. Result Pattern

- **Must** return `Result<T>` or `Result` for all service and command methods.
- **Must Not** return `null` to indicate failure or "not found".
- **Must Not** use exceptions for expected validation errors.

**Current Violations**: 50+ `return null` statements (see [Tech Debt Report](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md))

---

## 🤖 AI & Automation Rules

### 1. AI Orchestration

- **Must** route all AI requests through the `IAiOrchestrator`.
- **Must** implement **Resilience Policies** (Polly) for all AI calls:
  - Retry with exponential backoff.
  - Circuit breaker for 5XX errors.
  - Timeout policy (max 30s).
- **Must** use **Semantic Caching** for repetitive AI briefings/summaries.

### 2. Workflow Automation

- **Must** use the `IMacroManager` for keyboard/input recording.
- **Must Not** hardcode input delays; use configurable retry/wait intervals.

---

## 💻 CLI & Presentation Rules

### 1. Command Definitions

- **Must** keep `Program.cs` lean (<50 lines). ✅ **Compliant** (35 lines)
- **Must** use `ICommandGroup` interface for command organization.
- **Must Not** define duplicate command names at the root level.
- **Must** ensure `SetHandler` delegate signatures exactly match the argument/option count and order.

**Current Status**: 3/12 command groups implemented

### 2. UI Stability (Avalonia)

- **Must Not** use `async void` except for Top-Level Event Handlers. ✅ **Compliant**
- **Must** wrap all event-driven `async void` calls in a robust `try-catch` with logging.
- **Must** use `Dispatcher.UIThread.InvokeAsync` when updating UI components from background tasks.
- **Must Not** use `$parent[vm:ViewModelType]` in XAML bindings.
  - **Reason**: Avalonia's `$parent` syntax only supports **Control types** (e.g., `UserControl`, `Window`, `views:MyView`). Using ViewModel types causes runtime `ArgumentException: Unable to resolve type`.
  - **Correct Pattern**: `{Binding $parent[views:MyView].DataContext.MyCommand}`.

---

## 🔧 Infrastructure & Code Quality

### 1. HTTP Communication

- **Must Always** use `IHttpClientFactory`.
- **Must Not** manually instantiate `new HttpClient()`.
- **Must** apply retry and circuit breaker policies to all named clients.

**Current Violations**: 2 instances in plugins

- `MugenManagerPlugin.cs:36`
- `ItchGameProviderPlugin.cs:32`

### 2. Async Best Practices

- **Must Not** use `.Result` to block on async operations.
- **Must Not** use `.Wait()` to block on async operations.
- **Must Not** use `GetAwaiter().GetResult()` except in Dispose methods.
- **Must** use `await` with `ConfigureAwait(false)` in library code.

**Current Violations**: 14 instances (see [Tech Debt Report Phase 1](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md))

### 3. Logging & Diagnostics

- **Must** use **Structured Logging**. Include context parameters like `GameId`, `UserId`, or `ProviderName`.
- **Must Not** use silent `catch (Exception) {}` blocks. ✅ **Compliant**

### 4. Startup & Configuration

- **Must** use `.ValidateOnStart()` for all configuration options.
- **Must Not** allow the app to start with missing API keys or invalid URLs.

---

## 🧪 Testing Rules

### 1. Isolation

- **Must** ensure every test class gets a unique, isolated environment (e.g., unique SQLite file/in-memory name).
- **Must Not** share state (static variables) between integration tests.

### 2. Test Doubles

- **Must** prefer **Fakes** (thin implementations) over complex **Mocks** for Infrastructure components (Storage, Cache).
- **Must Not** mock extension methods (like `IMemoryCache.TryGetValue`). Wrap them in an interface if needed.

### 3. Reliability

- **Must** use seeded data for performance tests to ensure reproducibility.
- **Must Not** rely on `new Guid()` for entity lookups in tests.

### Current Test Metrics

| Metric | Value |
|--------|-------|
| Test Projects | 13 |
| Test Files | 148 |
| Test Methods | 529 |
| Test LOC | 11,056 |
| Tests Runnable | ❌ Blocked by build |

---

## 🎮 Gaming & Performance Rules

### 1. Data Access

- **Must** implement pagination for all "list" operations.
- **Must Not** implement `GetAllAsync()` or any method that loads an entire database table into memory.
- **Must** perform aggregations (Count, Sum, GroupBy) at the database level using EF Core.

### 2. Resource Management

- **Must** use `IDisposable` for any system hooks (Voice recognition, Process watchers).
- **Must** ensure background workers are properly cancelled via `CancellationToken` when the app exits.

### 3. Thread Safety

- **Must** use appropriate synchronization for shared state.
- **Should** prefer `ConcurrentDictionary` over `lock()` for simple key-value scenarios.

**Current Status**: 21 `lock()` statements in monitoring/state management (appropriate usage)

---

## 🤖 How These Rules Are Enforced

| Rule | Enforcement | Tool | Current Status |
|:-----|:------------|:-----|:---------------|
| Build compiles | CI/CD | dotnet build | ❌ 11 errors |
| Pagination required | Code review | Manual | ✅ Compliant |
| Result pattern | Static analyzer | Roslyn | ⚠️ 50+ violations |
| IHttpClientFactory | IDE inspection | ReSharper | ⚠️ 2 violations |
| Structured logging | Code review | SonarQube | ✅ Compliant |
| Async void forbidden | Static analyzer | StyleCop | ✅ Compliant |
| No silent catches | Automated scan | Custom | ✅ Compliant |
| No .Result/.Wait() | Code review | Manual | ❌ 14 violations |

**Enforcement setup**:

```bash
dotnet tool install -g dotnet-format
dotnet format --verify-no-changes
```

---

## 🚨 Rule Severity Levels

### 🔴 CRITICAL (Blocks Merge)

| Rule | Status |
|------|--------|
| Build must compile | ❌ 11 errors |
| No `async void` in business code | ✅ Compliant |
| No manual `new HttpClient()` | ⚠️ 2 violations |
| Result pattern on all service methods | ❌ 50+ violations |
| No silent exception catches | ✅ Compliant |

### 🟡 HIGH (Code Review Required)

| Rule | Status |
|------|--------|
| Pagination on list operations | ✅ Compliant |
| Structured logging in catch blocks | ✅ Compliant |
| Configuration validation at startup | ✅ Compliant |
| N+1 query prevention | ✅ Compliant |
| No sync-over-async | ❌ 14 violations |

### 🟢 MEDIUM (Guideline)

| Rule | Status |
|------|--------|
| Prefer constants over magic strings | ✅ Mostly compliant |
| Use guard clauses in constructors | ✅ Compliant |
| Document complex algorithms | ⚠️ Some TODOs |
| Keep methods under 30 lines | ⚠️ DependencyInjection.cs is 479 lines |

**CI/CD Integration**: Critical violations fail build. High violations require approval.

---

## 📋 Exception Handling Policy

### When to use Result<T>

- Service methods (all public methods)
- Command handlers
- Query handlers
- API client calls

### When to throw exceptions

- ✅ Invalid method arguments (ArgumentException)
- ✅ Unexpected internal state (InvalidOperationException)
- ✅ Programming errors (NotImplementedException)
- ❌ Expected validation failures (use Result instead)
- ❌ Business rule violations (use Result instead)

### Logging on Exception

```csharp
catch (HttpRequestException ex) when (ex is { InnerException: TimeoutException })
{
    _logger.LogWarning(ex,
        "Timeout calling {ServiceName} for {EntityType} {EntityId}. " +
        "Attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms",
        serviceName, typeof(T).Name, id, attempt, maxAttempts, delayMs);
}
```

---

## 📍 Current Violations

### Critical (None)

 All critical architectural violations have been resolved as of January 2, 2026.

### High Priority (None)

 All high-priority sync-over-async and null-returning violations have been resolved.

### Medium Priority

| Category | Count | Reference |
|----------|-------|-----------|
| `return null` | 50+ | [Tech Debt Report](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md) |
| TODO comments | 32 | 17 files |
| Incomplete CLI groups | 9 | [Tech Debt Report Phase 3](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md) |

---

## 📊 Compliance Trend

| Date | Health Score | Critical Violations | High Violations |
|------|--------------|---------------------|-----------------|
| Dec 31, 2025 | 86/100 | 0 | Unknown |
| Jan 1, 2026 | 65/100 | 11 (compilation) | 14 (sync-over-async) |

**Target**: 95/100 by January 19, 2026

---

**Failure to adhere to these rules is considered a regression in project quality.**

*Last Audit*: January 1, 2026 (Comprehensive codebase scan)
*Audit Method*: grep, file analysis, build verification
