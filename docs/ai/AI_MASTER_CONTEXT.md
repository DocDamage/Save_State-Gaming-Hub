# 🤖 AI Master Context: SaveState Reborn

**Status**: 🚀 Production + Active Development (V2.3.9)
**Last Updated**: January 8, 2026 (Dialog System Complete - All Placeholders Eliminated)
**Maintained By**: Development Team
**Next Review**: February 1, 2026
**Related Documents**: [AI_PROJECT_INDEX.md](../AI_PROJECT_INDEX.md), [V2_3_0_PROGRESS.md](../status/V2_3_0_PROGRESS.md), [ENGINEERING_RULES.md](../architecture/ENGINEERING_RULES.md), [LESSONS_LEARNED.md](../planning/LESSONS_LEARNED.md)

---

## Table of Contents

- [Build Status & Critical Issues](#-build-status--critical-issues)
- [Technical Foundation](#-technical-foundation)
- [Core Project Structure](#-core-project-structure)
- [Codebase Metrics](#-codebase-metrics)
- [Coding Standards & Behavioral Handbook](#-coding-standards--behavioral-handbook)
- [Domain Truth & Invariants](#-domain-truth--invariants)
- [Gold Standard Examples](#-gold-standard-examples)
- [Current Technical Debt](#-current-technical-debt)
- [Architecture Decisions](#-architecture-decisions)
- [Codebase Hotspots](#-codebase-hotspots)
- [Anti-Patterns to Avoid](#-anti-patterns-to-avoid)
- [Current Status & Roadmap](#-current-status--roadmap)
- [Integration Setup](#-integration-setup)
- [Critical References](#-critical-references)

---

> [!NOTE]
> **UI Development Active**: Phases 1-6 (Core Shell, Dashboard, Library, Analytics, Tools, Terminal) completed. Game Detail Experience (Overview, Notes, Mods) fully surfaced. All 12 CLI command groups integrated via `CommandExecutor`. **Dialog System Complete** - All placeholder implementations eliminated. Health score **98/100** with **0 build errors** and 117 warnings. See [FEATURE_SURFACING_PLAN.md](../planning/FEATURE_SURFACING_PLAN.md) for remaining implementation roadmap.

---

## ✅ Build Status & Critical Issues

### Build Health

| Metric | Status |
|--------|--------|
| **Build Result** | ✅ **PASSING (0 errors, 117 warnings)** |
| **Full Metrics** | See [PROJECT_METRICS.md](../reports/PROJECT_METRICS.md) |
| **Tests Runnable** | ✅ Yes (300+ test methods, 100% passing) |
| **Health Score** | **98/100** ✅ (All build errors resolved) |

### V2.3.0 Feature Progress (January 8, 2026)

**Completed Phases**:

1. ✅ Phase 2.3: Analytics Export (CSV, JSON, HTML reports)
2. ✅ Phase 3: Smart Recommendations (Collaborative filtering, trending, backlog prioritization)
3. ✅ Phase 4 (60%): Social V2 - Community Challenges System
4. ✅ Phase 4 (100%): Challenge Progress Tracking (Real-time updates)
5. ✅ Phase 5.0 (100%): RetroArch Integration (Games, Cores, Import, Launch)
6. ✅ Phase 5.1 (100%): Game Detail V2 (Price History, Goals, Rich Text Notes, Templates)
7. ✅ Ecosystem Foundation: Plugin Marketplace, Export/Import, Mobile API Foundation
8. ✅ Social V2 UI Integration (Dashboard widget + Social tab)
9. ✅ **Dialog System Complete** (TextInputDialog, Branch dialogs, SaveStateSettings)

**Recent Implementation (January 8, 2026)**:

- **Dialog System Completion**:
  - `TextInputDialogViewModel` + View + Code-behind (complete implementation)
  - `BranchCreationDialog`, `BranchMergeDialog`, `SaveStateSettingsDialog` (fully wired)
  - Eliminated all placeholder implementations in `IDialogService`
  - Fixed `ExperimentalAcrylicBorder` XAML structure
- **Build Error Resolution** (15 → 0 errors):
  - Fixed `GameId` type conversions throughout ViewModels
  - Corrected `AutoSaveConfig` record constructor usage
  - Resolved `Application.Current` ambiguity in `ClipboardService`
  - Added missing service dependencies (`IBacklogService`, `IClipboardService`)
  - Fixed ViewModel constructor signatures
- **Type Safety Improvements**:
  - Proper nullable reference handling with `!` operator
  - Explicit type conversions for value objects
  - Avalonia-specific pattern corrections

### Recent Fixes (January 4, 2026)

1. ✅ Completed Mod Management Integration
2. ✅ Fixed `GameDetailViewModel` constructor dependencies
3. ✅ Resolved all build errors and ambiguities
4. ✅ Migrated 18 core files to zero-allocation logging (~744 warnings resolved)

### Known Issues (From January 4, 2026 Scan)

| Issue | Count | Severity |
|-------|-------|----------|
| `.Result` sync-over-async | **0** ✅ | 🔴 RESOLVED |
| `async void` methods | 3 | 🟠 HIGH |
| Silent exception handlers | 2 | 🟡 MEDIUM |
| TODO Comments | 110+ | 🟡 MEDIUM (Planned work) |
| `return null` statements | 30+ | 🟡 MEDIUM |

---

## 🏗️ Technical Foundation

### 🏛️ Architecture Patterns

- **Clean Architecture**: Domain at the center (`Core`), Infrastructure (`Infrastructure`) on the outside.
- **CQRS**: Command-Query Responsibility Segregation via `IMediator`.
- **Result Pattern**: **MANDATORY**. Never return `null` or throw business exceptions. Use `Result<T>`.
- **Dependency Injection**: Registered in `src/SaveState.Infrastructure/DependencyInjection.cs` (494 lines).

### 🧠 AI Architecture (The "Brain")

- **Orchestrator**: `AiOrchestrator` coordinates all AI tasks with RAG, BMAD, and Web Search.
- **Knowledge Base**: `SqliteVectorStore` (Semantic Kernel style) for long-term data + `MarkdownKnowledgeBaseService` for file-based knowledge.
- **Web Search**: `IWebSearchService` for internet fallback when local knowledge is insufficient.
- **Memory**: `EnhancedShortTermMemory` for context retention.
- **Resilience**: `AiResiliencePolicy` handles retries/circuit breakers (Polly).
- **Auto-Learning**: Search results are automatically saved to knowledge base as Markdown files.

### 🎮 Social V2 System (NEW)

- **Challenge System**: `IChallengeProgressService` for real-time progress tracking across multiple challenge types
- **Leaderboards**: `ICommunityRepository` for global, game-specific, and challenge-specific rankings
- **Progress Tracking**: Automatic updates on game sessions, achievements, and stat changes
- **Challenge Types**: Playtime, Achievement, HighScore, Speedrun, Daily, Weekly, Monthly, Custom, Event
- **UI Integration**: `SocialViewModel` with observable collections and `CommunityHighlightsWidget` for dashboard

### 🛠️ Tech Stack

- **Backend**: .NET 9.0 (C# 13)
- **UI**: Avalonia UI 11.x (MVVM with ReactiveUI)
- **CLI**: Spectre.Console & System.CommandLine
- **Database**: SQLite (EF Core 9.0) with WAL enabled
- **External**: IGDB, SteamGridDB, Discord, Groq/OpenAI, RetroAchievements
- **Testing**: xUnit, FluentAssertions, Moq, Bogus

---

## 🗺️ Core Project Structure

> For detailed file counts and line metrics, see [PROJECT_METRICS.md](../reports/PROJECT_METRICS.md).

| Project | Purpose | Key Patterns |
|---------|---------|--------------|
| `SaveState.Core` | 22 bounded contexts, Entities, Value Objects, Interfaces | Domain Events, Guard Clauses, Result Pattern |
| `SaveState.Application` | Use Cases, MediatR Handlers, DTOs | FluentValidation, 50+ Commands/Queries |
| `SaveState.Infrastructure` | Persistence, APIs, DI, 60+ services | EF Core, Resilient HTTP Clients, Polly |
| `SaveState.Presentation` | XAML Views, ViewModels, Big Picture Mode | ReactiveUI, MVVM |
| `SaveState.CLI` | Power-user console (15+ commands) | Spectre.Console, System.CommandLine |
| `SaveState.Plugins.*` | 19 specialized plugins | IPlugin interface, capability-based loading |

---

## 📊 Codebase Metrics

See [PROJECT_METRICS.md](../reports/PROJECT_METRICS.md) for a comprehensive, single source of truth for all project statistics, build health, and technical debt metrics.

---

## 📜 Coding Standards & Behavioral Handbook

### 1. The Result Pattern (MANDATORY)

NEVER return `null` for failures.

- **Success**: `Result<T>.Success(value)`
- **Failure**: `Result<T>.Failure(errorMessage, ErrorType)` (e.g., `ErrorType.NotFound`)
- **Validation**: Use `Guard.Against` (Ardalis) in constructors.

⚠️ **Current Violations**: ~45 `return null` statements need conversion (see Tech Debt Audit)

### 2. Async Safety

- **Avoid `async void`**: Use `async Task` everywhere. ⚠️ **3 violations in ViewModels**
- **Avoid `.Result`**: 3 instances need fixing in `JwtTokenService.cs`
- **Avoid `.Wait()`**: ✅ 0 violations
- **Avoid `GetAwaiter().GetResult()`**: 2 instances (in Dispose methods - acceptable)
- **Exception**: Top-level event handlers in UI can be `async void`, but **MUST** wrap in `try-catch` with structured logging.
- **UI Thread**: Use `Dispatcher.UIThread.InvokeAsync` for UI updates from background tasks.

### 3. Performance & Data Access

- **Pagination**: Mandatory for all "list" operations. No `GetAllAsync`.
- **N+1 Avoidance**: Perform aggregations (`Sum`, `Count`) at the DB level, not in memory.
- **HTTP**: Always use `IHttpClientFactory`. Manual instantiation is banned (2 violations in plugins).

---

## 🏛️ Domain Truth & Invariants (The "Sacred" Rules)

- **Game Integrity**: A `Game` MUST have a `Title` (max 200 chars).
- **Status Lifecycle**: Use behavior methods (`MarkAsRunning()`, `MarkAsNotRunning()`) instead of raw property sets.
- **Soft Deletion**: Most entities use `ISoftDelete`. Do not hard delete unless explicitly requested.
- **Value Objects**: Use `GameTitle`, `FilePath`, etc., to encode rules at the type level.

---

## 🏆 Gold Standard Examples (Match This Style)

| Pattern | Reference File |
|---------|----------------|
| **Aggregate Root** | [Game.cs](../src/SaveState.Core/GameLibrary/Entities/Game.cs) |
| **Service Coordinator** | [AiOrchestrator.cs](../src/SaveState.Infrastructure/Ai/AiOrchestrator.cs) |
| **Resilience Policy** | [AiResiliencePolicy.cs](../src/SaveState.Infrastructure/Ai/Resilience/AiResiliencePolicy.cs) |
| **Read Handler** | [GetGameDetailsQueryHandler.cs](../src/SaveState.Application/GameLibrary/Queries/Handlers/GetGameDetailsQueryHandler.cs) |
| **Value Object** | [GameTitle.cs](../src/SaveState.Core/Common/ValueObjects/GameTitle.cs) |
| **DI Registration** | [DependencyInjection.cs](../src/SaveState.Infrastructure/DependencyInjection.cs) |
| **Command Group** | [GameCommands.cs](../src/SaveState.CLI/Commands/GameCommands.cs) |
| **Web Search Service** | [WebSearchService.cs](../src/SaveState.Infrastructure/Ai/Services/WebSearchService.cs) |
| **Knowledge Base** | [MarkdownKnowledgeBaseService.cs](../src/SaveState.Infrastructure/Ai/Knowledge/MarkdownKnowledgeBaseService.cs) |

---

## 🔧 Current Technical Debt

| Issue | Location | Impact |
|-------|----------|--------|
| **None** | All critical debt resolved | ✅ Baseline stable |

### High Priority

| Issue | Count | Files |
|-------|-------|-------|
| `async void` methods | 3 | DashboardViewModel, StatusBarViewModel, TerminalViewModel |
| Silent exception handlers | 4 | RecommendationService, SmartCategorizationService, EmulatorRomScanner |

### Medium Priority

| Issue | Count | Files |
|-------|-------|-------|
| `return null` | 45+ | GameMemoryReader (8), PerformanceProfiler (4), etc. |
| TODO comments | 68+ | Mostly in Presentation layer ViewModels |
| Manual HttpClient | 2 | MugenManagerPlugin, ItchGameProviderPlugin |

### Low Priority

| Issue | Count | Notes |
|-------|-------|-------|
| CS1591 XML docs | ~1200 | Documentation gaps |
| `GetAwaiter().GetResult()` | 2 | In Dispose methods - acceptable |

**Full Details**: [TECHNICAL_DEBT_AUDIT_2026-01-02.md](../reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md)

---

## 🏛️ Architecture Decisions

Key decisions that shaped the codebase:

| Decision | Rationale | Alternatives Rejected |
|----------|-----------|----------------------|
| **Clean Architecture** | Testability & scalability | Layered, Hexagonal |
| **CQRS via MediatR** | Separation of concerns | Simple CRUD, Command Bus |
| **Result<T> Pattern** | Type-safe error handling | Exceptions, Option types |
| **IHttpClientFactory** | Socket pooling | Manual HttpClient |
| **Plugin Architecture** | Extensibility | Monolithic features |
| **AI Knowledge Base** | RAG + Web Search fallback | Static prompts only |

**When to reference**: Before proposing architectural changes.

---

## 🔥 Codebase Hotspots - High-Change Areas

| Location | Reason | Caution |
|----------|--------|---------|
| `src/SaveState.Infrastructure/Ai/AiOrchestrator.cs` | Core AI coordination | Changes affect all AI features |
| `src/SaveState.Core/GameLibrary/Entities/Game.cs` | Core domain model | Changes cascade widely |
| `src/SaveState.Infrastructure/DependencyInjection.cs` | 494 lines of registration | Keep in sync with services |
| `src/SaveState.Application/GameLibrary/Commands/` | Primary business logic | Heavy test coverage required |
| `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs` | Database schema | Requires migration management |
| `src/SaveState.Presentation/ViewModels/` | UI logic | Many TODOs remaining |
| `src/SaveState.Infrastructure/Social/ChallengeProgressService.cs` | **NEW** - Challenge tracking | Integration with game sessions |
| `src/SaveState.Core/Social/Entities/Challenge.cs` | **NEW** - Challenge domain | Progress tracking logic |

**Rule**: Changes to these files require architectural review + tests.

---

## ❌ Anti-Patterns to Avoid

| Anti-Pattern | Current Violations | Use Instead |
|--------------|-------------------|-------------|
| Null returns | 45+ | `Result<T>.Failure("msg")` |
| `.Result` sync-over-async | 3 | `await` with ConfigureAwait |
| `.Wait()` blocking | 0 ✅ | `await` with ConfigureAwait |
| Manual HttpClient | 2 (plugins) | `IHttpClientFactory` |
| async void | 3 | `async Task` with fire-and-forget |
| Silent catches | 4 | Logging required |
| Thread.Sleep | 0 ✅ | `Task.Delay` |

---

## 🚦 Current Status & Roadmap

### Build Health (January 4, 2026)

| Metric | Status |
|--------|--------|
| Compilation | ✅ 0 errors, 0 warnings |
| Tests | ✅ 529 test methods (100% passing) |
| Source Files | 763+ C# files |
| Test Projects | 13 |
| Architecture Compliance | ✅ 100% |
| UI Implementation | Phases 1-6 Complete |
| **Health Score** | **98/100** ⬆️ |

### AI System Status

| Component | Status |
|-----------|--------|
| AiOrchestrator | ✅ Production |
| RAG (Knowledge Base) | ✅ Active |
| BMAD (Short-term Memory) | ✅ Active |
| Web Search Fallback | ✅ Integrated |
| Auto-save to KB | ✅ Active |

### Plugin System Architecture

**Status**: **Plugin Loading System Integrated** - Automatic plugin discovery

**Plugin Categories (19 plugins)**:

- **Gaming Integration**: Steam, GOG, Epic, Itch.io, MUGEN (6 plugins)
- **MUGEN Ecosystem**: Training, Replay, Achievements, Network, Fusion, Manager (6 plugins)
- **Character Development Tools**: MugenHook, LuaSupernull, OpenMK, iguana, ikemenarmor, character packs (see [Character Development Integration Plan](planning/character_development_integration_plan.md))
- **Productivity**: Health & Wellness, Accessibility (2 plugins)
- **Content**: Themes, Screenshot Capture, Twitch Streaming (3 plugins)
- **Integration**: Discord, Google Drive Sync, Playnite (2 plugins)

### Character Development & Modification System

**Status**: 📋 Planning Phase - Integration Plan Complete

SaveStateReborn supports full character modification and development capabilities for MUGEN/Ikemen:

**Integrated Tools & Frameworks**:

- **MugenHook** - Engine enhancements (animated portraits, multiple characters per slot)
- **LuaSupernull** - Advanced character framework using Lua
- **OpenMK** - Mortal Kombat-style character development library
- **ikemenarmor** - Character armor mechanics system
- **iguana** - Movelist extraction and metadata automation
- **Character Packs** - NooksVsCrans and other character collections

**Visual Resources** (Implemented):

- **Elecbyte Screenpack** - Default UI resources
- **Round Transition Effects** - Custom round animations
- **Shader Collection** - Visual shader presets

**Full Details**: [Character Development Integration Plan](../planning/character_development_integration_plan.md) - 12-week implementation roadmap

### UI Implementation Progress

| Phase | Weeks | Focus | Status | Completion |
|-------|-------|-------|--------|------------|
| **1** | **1-2** | **Core Shell & Navigation** | ✅ **COMPLETE** | 100% |
| **2** | **3-4** | **Dashboard Hub** | ✅ **COMPLETE** | 100% |
| **3** | **5-6** | **Library Enhancement** | ✅ **COMPLETE** | 100% |
| **4** | **7-8** | **Analytics & Social** | ✅ **COMPLETE** | 100% |
| **5** | **9-10** | **Tools & Utilities** | ✅ **COMPLETE** | 100% |
| **6** | **11** | **Terminal & CLI** | ✅ **COMPLETE** | 100% |
| **7** | **12** | **RetroArch Hub** | ✅ **COMPLETE** | 100% |
| 8 | 13 | MUGEN Enhancement | 🏗️ IN PROGRESS | 5% |
| 9 | 14-15 | Big Picture Mode | 📋 PLANNED | 0% |
| 10 | 16 | Polish & Accessibility | 📋 PLANNED | 0% |

**Full Details**: [FEATURE_SURFACING_PLAN.md](../planning/FEATURE_SURFACING_PLAN.md)

---

## 🔌 Integration Setup

- **Database**: `savestate.db` (SQLite) in the root.
- **Knowledge Base**: `%LOCALAPPDATA%/SaveStateReborn/KnowledgeBase/` for Markdown files.
- **Config**: Root `appsettings.json` contains API keys and validation policies.
- **Plugins**: Automatic loading from `Plugins/` directory at application startup.
- **UI**: Main application launches with full shell (MainShell.axaml) featuring 7-tab navigation.
- **Testing**: Run tests using `dotnet test`.
- **AI**: Web search automatically falls back when local knowledge is insufficient.

---

## 🔗 Critical References

| Document | Purpose |
|----------|---------|
| [Engineering Rules](../architecture/ENGINEERING_RULES.md) | The "Must/Must Not" List |
| [Technical Debt Audit](../reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) | ⭐ **Latest debt scan** |
| [Technical Debt Remediation Plan](../reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md) | Remediation tracking |
| [Feature Surfacing Plan](../planning/FEATURE_SURFACING_PLAN.md) | UI implementation roadmap |
| [Development Status](../status/DEVELOPMENT_STATUS.md) | Overall project status |
| [V2 Feature Roadmap](../planning/V2_FEATURE_ROADMAP.md) | Feature planning |
| [Lessons Learned](../planning/LESSONS_LEARNED.md) | Historical decisions |

---

## 📝 Document Revision History

| Date | Version | Changes |
|------|---------|---------|
| Jan 8, 2026 | 3.0 | Game Detail Experience (V2) complete - Overview, Notes, Goals, Price Tracking |
| Jan 7, 2026 | 2.9 | Performance Logging Migration - Phases 1-4 mostly complete (18 files, ~750 warnings fixed) |
| Jan 5, 2026 | 2.8 | RetroArch Integration & Ecosystem Hub - UI, Navigation, Marketplace, Export/Import, and Mobile API Foundation complete |
| Jan 4, 2026 | 2.7 | Mod Management integration complete - GameModsTabViewModel fully connected, health score 98/100 |
| Jan 3, 2026 | 2.6 | UI Phase 5 & 6 completion - Tools and Terminal integration fully functional |
| Jan 3, 2026 | 2.5 | Character development integration planning - MugenHook, LuaSupernull, OpenMK, iguana, ikemenarmor, NooksVsCrans |
| Jan 2, 2026 | 2.4 | Technical debt audit, AI knowledge base integration, web search fallback |
| Jan 2, 2026 | 2.3 | Library UI stabilization, repository safety fixes |
| Jan 1, 2026 | 2.2 | UI development status - Phase 1 & 2 complete |
| Jan 1, 2026 | 2.1 | Added Feature Surfacing Plan reference |
| Jan 1, 2026 | 2.0 | Comprehensive audit, accurate metrics |
| Dec 31, 2025 | 1.0 | Initial creation |
