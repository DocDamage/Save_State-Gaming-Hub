# 🤖 AI Master Context: SaveState Reborn

**Status**: ✅ Active
**Last Updated**: December 31, 2025
**Maintained By**: Development Team
**Next Review**: January 15, 2026
**Related Documents**: [AI_PROJECT_INDEX.md](./AI_PROJECT_INDEX.md), [ENGINEERING_RULES.md](./ENGINEERING_RULES.md), [LESSONS_LEARNED.md](./LESSONS_LEARNED.md)

---

## Table of Contents

- [Technical Foundation](#-technical-foundation)
- [Core Project Structure](#-core-project-structure)
- [Coding Standards & Behavioral Handbook](#-coding-standards--behavioral-handbook)
- [Domain Truth & Invariants](#-domain-truth--invariants)
- [Gold Standard Examples](#-gold-standard-examples)
- [Decision History & "Scars"](#-decision-history--scars)
- [Architecture Decisions](#-architecture-decisions)
- [Codebase "Hotspots"](#-codebase-hotspots)
- [Anti-Patterns to Avoid](#-anti-patterns-to-avoid)
- [Current Status & Roadmap](#-current-status--roadmap)
- [Integration Setup](#-integration-setup)
- [Critical References](#-critical-references)

---

> [!IMPORTANT]
> This document is designed for AI models (Large Language Models) to quickly ingest the current state, architecture, and "sacred" rules of the SaveState Reborn project. Use this as your primary context source before suggesting or writing code.

---

## 🏗️ Technical Foundation

### 🏛️ Architecture Patterns

- **Clean Architecture**: Domain at the center (`Core`), Infrastructure (`Infrastructure`) on the outside.
- **CQRS**: Command-Query Responsibility Segregation via `IMediator`.
- **Result Pattern**: **MANDATORY**. Never return `null` or throw business exceptions. Use `Result<T>`.
- **Dependency Injection**: Registered in `src/SaveState.Infrastructure/DependencyInjection.cs`.

### 🛠️ Tech Stack

- **Backend**: .NET 9.0 (C#)
- **UI**: Avalonia UI (MVVM)
- **CLI**: Spectre.Console & System.CommandLine
- **Database**: SQLite (EF Core) with WAL enabled.
- **External**: IGDB, SteamGridDB, Discord, Groq/OpenAI.

---

## 🗺️ Core Project Structure

| Project | Purpose | Key Patterns |
| :--- | :--- | :--- |
| `SaveState.Core` | Entities, Value Objects, Interfaces. | Domain Events, Guard Clauses. |
| `SaveState.Application` | Use Cases, MediatR Handlers, DTOs. | FluentValidation. |
| `SaveState.Infrastructure` | Persistence, APIs, DI. | EF Core, Resilient HTTP Clients. |
| `SaveState.Presentation` | XAML Views, ViewModels. | ReactiveUI style. |
| `SaveState.CLI` | Power-user console app. | Complex Spectre.Console commands. |

---

## 📜 Coding Standards & Behavioral Handbook

### 1. The Result Pattern (MANDATORY)

NEVER return `null` for failures.

- **Success**: `Result<T>.Success(value)`
- **Failure**: `Result<T>.Failure(errorMessage, ErrorType)` (e.g., `ErrorType.NotFound`)
- **Validation**: Use `Guard.Against` (Ardalis) in constructors.

### 2. Async Safety

- **Avoid `async void`**: Use `async Task` everywhere.
- **Exception**: Top-level event handlers in UI can be `async void`, but **MUST** wrap in `try-catch` with structured logging.
- **UI Thread**: Use `Dispatcher.UIThread.InvokeAsync` for UI updates from background tasks.

### 3. Performance & Data Access

- **Pagination**: Mandatory for all "list" operations. No `GetAllAsync`.
- **N+1 Avoidance**: Perform aggregations (`Sum`, `Count`) at the DB level, not in memory.
- **HTTP**: Always use `IHttpClientFactory`. Manual instantiation is banned (socket exhaustion).

---

## 🏛️ Domain Truth & Invariants (The "Sacred" Rules)

- **Game Integrity**: A `Game` MUST have a `Title` (max 200 chars).
- **Status Lifecycle**: Use behavior methods (`MarkAsRunning()`, `MarkAsNotRunning()`) instead of raw property sets.
- **Soft Deletion**: Most entities use `ISoftDelete`. Do not hard delete unless explicitly requested.
- **Value Objects**: Use `GameTitle`, `FilePath`, etc., to encode rules at the type level.

---

## 🏆 Gold Standard Examples (Match This Style)

| Pattern | Reference File |
| :--- | :--- |
| **Aggregate Root** | [Game.cs](../src/SaveState.Core/GameLibrary/Entities/Game.cs) |
| **Read Handler** | [GetGameDetailsQueryHandler.cs](../src/SaveState.Application/GameLibrary/Queries/Handlers/GetGameDetailsQueryHandler.cs) |
| **Value Object** | [GameTitle.cs](../src/SaveState.Core/Common/ValueObjects/GameTitle.cs) |
| **DI Registration** | [DependencyInjection.cs](../src/SaveState.Infrastructure/DependencyInjection.cs) |

---

## 🕰️ Decision History & "Scars"

- **Why Polly?**: AI APIs have high transient failure rates; circuit breakers are critical.
- **Radioactive Zones**: `IHttpClientFactory` config in Infrastructure is tuned for socket reuse; do not refactor to manual `new HttpClient()`.
- **Lessons Learned**: Comprehensive history found in [LESSONS_LEARNED.md](./LESSONS_LEARNED.md).

---

## 🏛️ Architecture Decisions

Key decisions that shaped the codebase:

| Decision | File | Rationale | Alternatives Considered |
|:---------|:-----|:---------|:------------------------|
| **Clean Architecture** | LESSONS_LEARNED.md § 1 | Testability & scalability | Layered, Hexagonal |
| **CQRS via MediatR** | LESSONS_LEARNED.md § 2 | Separation of concerns | Simple CRUD, Command Bus |
| **Result<T> Pattern** | LESSONS_LEARNED.md § 4 | Type-safe error handling | Exceptions, Option types |
| **IHttpClientFactory** | LESSONS_LEARNED.md § 7 | Socket pooling | Manual HttpClient (rejected) |

**When to reference**: Before proposing architectural changes.

**Why**: Helps developers understand "why" before suggesting alternatives.

---

## 🔥 Codebase "Hotspots" - High-Change Areas

| Location | Reason | Caution |
|:---------|:-------|:--------|
| `src/SaveState.Core/GameLibrary/Entities/Game.cs` | Core domain model | Changes cascade widely |
| `src/SaveState.Infrastructure/DependencyInjection.cs` | All service registration | Keep in sync with new services |
| `src/SaveState.Application/GameLibrary/Commands/` | Primary business logic | Heavy test coverage required |
| `src/SaveState.Presentation/ViewModels/` | UI layer coupling | Avoid domain logic leakage |
| `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs` | Database schema | Requires migration management |

**Rule**: Changes to these files require architectural review + tests.

**Why**: Prevents accidental cascade failures.

---

## ❌ Anti-Patterns to Avoid

| Anti-Pattern | Example | Use Instead | Why |
|:---------|:---------|:---------|:---------|
| Null returns | `return null;` | `Result<T>.Failure("msg")` | Type safety |
| Async void | `async void OnClick()` | `async Task OnClick()` | Exception handling |
| Manual HttpClient | `new HttpClient()` | `IHttpClientFactory` | Socket exhaustion |
| Direct DB queries | `_context.Games.ToList()` | Pagination + projections | Memory efficiency |
| Silent catches | `catch (Exception) { }` | `catch (...) { _logger.Log...` | Debuggability |
| Hardcoded strings | `"12345"` | Constants, `nameof()` | Maintainability |

**Quick Check**: Before committing, search your code for these patterns.

**Why**: Improves code review efficiency.

---

## 🚦 Current Status & Roadmap (V2.1 Stabilization)

### Build Health
- Compilation: ✅ 0 Errors
- Tests: ✅ 290+/290 passing (100%)
- Code Coverage: 35%+
- Architecture Compliance: ✅ 100%

### Known Issues (In Priority Order)
1. **Game Memory Intelligence** (Phase 8) - Blocks tournament system
   - Effort: 8-10 hours
   - Status: Design complete, implementation pending
   - Risk: Windows-only initially

2. **MUGEN Tournament Features** (Phase 9) - Next major feature
   - Effort: 12-16 hours
   - Status: Interfaces designed, awaiting Phase 8 completion
   - Risk: Death match simulator needs AI integration testing

### Blockers (None Currently)
✅ All critical blockers resolved as of Dec 31, 2025

### Upcoming Milestones
- Jan 15: Phase 8 completion target
- Feb 1: Phase 9 completion target
- Feb 15: V3.0 release candidate

---

## 🔌 Integration Setup

- **Database**: `savestate.db` (SQLite) in the root.
- **Config**: Root `appsettings.json` contains API keys and validation policies.
- **Testing**: Run tests using `dotnet test`. Maintain 100% pass rate across 330+ tests.

---

## 🔗 Critical References

- [Engineering Rules](./ENGINEERING_RULES.md) (The "Must/Must Not" List)
- [Development Status](./DEVELOPMENT_STATUS.md)
- [V2 Feature Roadmap](./V2_FEATURE_ROADMAP.md)
- [Technical Debt Report](./technical_debt_scan_report.md)
