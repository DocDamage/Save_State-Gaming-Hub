# SaveStateReborn Engineering Rules & Principles

**Status**: ✅ Active
**Last Updated**: December 31, 2025
**Maintained By**: Architecture Team
**Next Review**: January 15, 2026
**Related Documents**: [AI_MASTER_CONTEXT.md](./AI_MASTER_CONTEXT.md), [LESSONS_LEARNED.md](./LESSONS_LEARNED.md)

---

## Table of Contents

- [Architecture Rules](#-architecture-rules)
- [AI & Automation Rules](#-ai--automation-rules)
- [CLI & Presentation Rules](#-cli--presentation-rules)
- [Infrastructure & Code Quality](#-infrastructure--code-quality)
- [Testing Rules](#-testing-rules)
- [Gaming & Performance Rules](#-gaming--performance-rules)
- [How These Rules Are Enforced](#-how-these-rules-are-enforced)
- [Rule Severity Levels](#-rule-severity-levels)
- [Exception Handling Policy](#-exception-handling-policy)

---

**Version**: 1.1 (Dec 31, 2025 - Stabilization Update)

These rules are derived from the lessons learned during the development and stabilization of V2.1. They must be followed for all new feature development and refactoring.

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

- **Must** keep `Program.cs` lean. Command handlers **must** only delegate to MediatR commands.
- **Must Not** define duplicate command names at the root level.
- **Must** ensure `SetHandler` delegate signatures exactly match the argument/option count and order.

### 2. UI Stability (Avalonia)

- **Must Not** use `async void` except for Top-Level Event Handlers.
- **Must** wrap all event-driven `async void` calls in a robust `try-catch` with logging.
- **Must** use `Dispatcher.UIThread.InvokeAsync` when updating UI components from background tasks.

---

## 🔧 Infrastructure & Code Quality

### 1. HTTP Communication

- **Must Always** use `IHttpClientFactory`.
- **Must Not** manually instantiate `new HttpClient()`.
- **Must** apply retry and circuit breaker policies to all named clients.

### 2. Logging & Diagnostics

- **Must** use **Structured Logging**. Include context parameters like `GameId`, `UserId`, or `ProviderName`.
- **Must Not** use silent `catch (Exception) {}` blocks. Every catch must log at least a warning.

### 3. Startup & Configuration

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

---

## 🎮 Gaming & Performance Rules

### 1. Data Access

- **Must** implement pagination for all "list" operations.
- **Must Not** implement `GetAllAsync()` or any method that loads an entire database table into memory.
- **Must** perform aggregations (Count, Sum, GroupBy) at the database level using EF Core.

### 2. Resource Management

- **Must** use `IDisposable` for any system hooks (Voice recognition, Process watchers).
- **Must** ensure background workers are properly cancelled via `CancellationToken` when the app exits.

---

## 🤖 How These Rules Are Enforced

| Rule | Enforcement | Tool | Auto-Fixable |
|:-----|:------------|:-----|:------------|
| Pagination required | Code review + test coverage | Manual | ❌ |
| Result pattern | Static analyzer | Roslyn analyzer | ⚠️ |
| IHttpClientFactory | IDE inspection | ReSharper | ✅ |
| Structured logging | Code review | SonarQube | ❌ |
| Async void forbidden | Static analyzer | StyleCop | ✅ |
| No silent catches | Automated scan | Custom analyzer | ⚠️ |

**How to enable**:
```bash
dotnet tool install -g dotnet-format
dotnet format --verify-no-changes
```

**Why**: Rules without enforcement are guidelines.

---

## 🚨 Rule Severity Levels

### 🔴 CRITICAL (Blocks Merge)
- No `async void` in business code
- No manual `new HttpClient()`
- Result pattern on all service methods
- No silent exception catches

### 🟡 HIGH (Code Review Required)
- Pagination on list operations
- Structured logging in catch blocks
- Configuration validation at startup
- N+1 query prevention

### 🟢 MEDIUM (Guideline)
- Prefer constants over magic strings
- Use guard clauses in constructors
- Document complex algorithms
- Keep methods under 30 lines

**CI/CD Integration**: Critical violations fail build. High violations require approval.

**Why**: Helps teams prioritize when rules conflict.

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

**Why**: Clarifies which failures are expected vs. bugs.

---

**Failure to adhere to these rules is considered a regression in project quality.**
