# SaveStateReborn Engineering Rules & Principles

**Status**: ✅ Active (Production + Development)
**Last Updated**: January 8, 2026 (Dialog System Complete v2.3.9)
**Maintained By**: Architecture Team
**Next Review**: January 15, 2026
**Related Documents**: [AI_MASTER_CONTEXT.md](../ai/AI_MASTER_CONTEXT.md), [LESSONS_LEARNED.md](../planning/LESSONS_LEARNED.md), [DECISIONS_LOG.md](DECISIONS_LOG.md)

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

**Version**: 2.3 (January 8, 2026 - Dialog System Complete)

These rules are derived from the lessons learned during the development and stabilization of V2.0+. They must be followed for all new feature development and refactoring.

---

## 📊 Current Compliance Status

> **January 8, 2026 Update**: Health Score 98/100. Build: **0 errors, 117 warnings** (88% reduction from 995). Dialog System Complete - all placeholder implementations eliminated. See [PROJECT_METRICS.md](../reports/PROJECT_METRICS.md) for full metrics.

### Rule Compliance Summary (January 8, 2026)

| Rule Category | Status | Violations |
|---------------|--------|------------|
| **Build compiles** | ✅ Compliant | 0 errors |
| **Placeholder implementations** | ✅ Compliant | 0 (v2.3.9) |
| **Sync-over-async (.Result)** | ⚠️ Violation | 3 (JwtTokenService) |
| **Async void forbidden** | ⚠️ Violation | 3 (ViewModels) |
| **Thread.Sleep forbidden** | ✅ Compliant | 0 |
| **Empty catch blocks** | ⚠️ Violation | 4 (needs logging) |
| **IHttpClientFactory** | ⚠️ Violation | 2 (plugins) |
| **Result pattern** | ⚠️ Violation | 30+ `return null` |
| **.Wait() forbidden** | ✅ Compliant | 0 |

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

**Current Violations**: 45+ `return null` statements (see [Tech Debt Audit](../reports/COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_15.md))

---

## 🤖 AI & Automation Rules

### 1. AI Orchestration

- **Must** route all AI requests through the `IAiOrchestrator`.
- **Must** implement **Resilience Policies** (Polly) for all AI calls:
  - Retry with exponential backoff.
  - Circuit breaker for 5XX errors.
  - Timeout policy (max 30s).
- **Must** use **Semantic Caching** for repetitive AI briefings/summaries.
- **Must** implement web search fallback when local knowledge is insufficient.
- **Must** auto-save search results to knowledge base for future queries.

### 2. Knowledge Base

- **Must** store all knowledge in Markdown format in `%LOCALAPPDATA%/SaveStateReborn/KnowledgeBase/`.
- **Must** use `IKnowledgeBaseService.SaveToKnowledgeBaseAsync` for persisting new information.
- **Must** immediately index new knowledge files for RAG availability.

### 3. Workflow Automation

- **Must** use the `IMacroManager` for keyboard/input recording.
- **Must Not** hardcode input delays; use configurable retry/wait intervals.

---

## 💻 CLI & Presentation Rules

### 1. Command Definitions

- **Must** keep `Program.cs` lean (<50 lines). ✅ **Compliant** (35 lines)
- **Must** use `ICommandGroup` interface for command organization.
- **Must Not** define duplicate command names at the root level.
- **Must** ensure `SetHandler` delegate signatures exactly match the argument/option count and order.

**Current Status**: 12/12 command groups implemented ✅

### 2. UI Stability (Avalonia)

- **Must Not** use `async void` except for Top-Level Event Handlers. ⚠️ **3 violations**
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

- **Must Not** use `.Result` to block on async operations. ⚠️ **3 violations**
- **Must Not** use `.Wait()` to block on async operations. ✅ **Compliant**
- **Must Not** use `GetAwaiter().GetResult()` except in Dispose methods.
- **Must** use `await` with `ConfigureAwait(false)` in library code.

**Current Violations**: 3 instances in `JwtTokenService.cs` (lines 29, 98, 118)

### 3. Logging & Diagnostics

- **Must** use **Structured Logging**. Include context parameters like `GameId`, `UserId`, or `ProviderName`.
- **Must Not** use silent `catch (Exception) {}` blocks. ⚠️ **4 violations**

**Silent Catch Violations**:

- `RecommendationService.cs:318`
- `EmulatorRomScanner.cs:154`
- `SmartCategorizationService.cs:236, 262`

### 4. Startup & Configuration

- **Must** use `.ValidateOnStart()` for all configuration options.
- **Must Not** allow the app to start with missing API keys or invalid URLs.

### 5. Code Warnings

- **Must** strive for Zero Warnings in the build output.
- **Exception**: Missing XML Documentation (`CS1591`) is **SUPPRESSED** globally to reduce noise. Documentation is encouraged but not enforced via warnings.
- **Must Fix**: All nullable reference warnings (`CS8600`-`CS8604`) and obsolete usage (`CS0618`).

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
| Tests Runnable | ✅ Yes |

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

---

## 🤖 How These Rules Are Enforced

| Rule | Enforcement | Tool | Current Status |
|:-----|:------------|:-----|:---------------|
| Build compiles | CI/CD | dotnet build | ✅ 0 errors |
| Pagination required | Code review | Manual | ✅ Compliant |
| Result pattern | Static analyzer | Roslyn | ⚠️ 45+ violations |
| IHttpClientFactory | IDE inspection | ReSharper | ⚠️ 2 violations |
| Structured logging | Code review | SonarQube | ✅ Compliant |
| Async void forbidden | Static analyzer | StyleCop | ⚠️ 3 violations |
| No silent catches | Automated scan | Custom | ⚠️ 4 violations |
| No .Result/.Wait() | Code review | Manual | ⚠️ 3 violations |

---

## 🚨 Rule Severity Levels

### 🔴 CRITICAL (Blocks Merge)

| Rule | Status |
|------|--------|
| Build must compile | ✅ Compliant |
| No `async void` in business code | ⚠️ 3 violations |
| No manual `new HttpClient()` | ⚠️ 2 violations |
| Result pattern on all service methods | ⚠️ 45+ violations |
| No silent exception catches | ⚠️ 4 violations |

### 🟡 HIGH (Code Review Required)

| Rule | Status |
|------|--------|
| Pagination on list operations | ✅ Compliant |
| Structured logging in catch blocks | ✅ Compliant |
| Configuration validation at startup | ✅ Compliant |
| N+1 query prevention | ✅ Compliant |
| No sync-over-async | ⚠️ 3 violations |

### 🟢 MEDIUM (Guideline)

| Rule | Status |
|------|--------|
| Prefer constants over magic strings | ✅ Mostly compliant |
| Use guard clauses in constructors | ✅ Compliant |
| Document complex algorithms | ⚠️ 68+ TODOs |
| Keep methods under 30 lines | ✅ Mostly compliant |

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

### Critical (3)

| Issue | File | Line |
|-------|------|------|
| `.Result` sync-over-async | `JwtTokenService.cs` | 29, 98, 118 |

### High Priority (7)

| Category | Count | Files |
|----------|-------|-------|
| `async void` methods | 3 | DashboardViewModel, StatusBarViewModel, TerminalViewModel |
| Silent catch blocks | 4 | RecommendationService, SmartCategorizationService (2), EmulatorRomScanner |

### Medium Priority

| Category | Count | Reference |
|----------|-------|-----------|
| `return null` | 45+ | [Tech Debt Audit](../reports/COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_15.md) |
| TODO comments | 68+ | Presentation layer |
| Manual HttpClient | 2 | Plugin projects |

---

## 📊 Compliance Trend

| Date | Health Score | Critical Violations | High Violations | Warnings |
|------|--------------|---------------------|-----------------|----------|
| Dec 31, 2025 | 86/100 | 0 | Unknown | 4,746 |
| Jan 1, 2026 | 98/100 | 0 | 0 | ~1,000 |
| Jan 2, 2026 | 91/100 | 3 | 7 | 995 |
| Jan 7, 2026 | 91/100 | 3 | 7 | 995 |
| Jan 8, 2026 | **98/100** | 0 | 3 | **117** ✅ |

**Target**: 95/100 by January 15, 2026 ✅ **ACHIEVED**
**Warnings Reduction**: 4,746 → 117 (98% reduction) ✅ **MAJOR IMPROVEMENT**

---

**Failure to adhere to these rules is considered a regression in project quality.**

*Last Audit*: January 8, 2026 (Dialog System Complete v2.3.9)
*Audit Method*: grep, file analysis, build verification, dialog implementation review

