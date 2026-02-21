# Architecture & Design Reference

This document provides visual architecture diagrams, event catalogs, validation rules, and debugging guides.

---

[← Back to README](./README.md) | [Common Infrastructure](./common-infrastructure.md)

---

## **🏛️ Architecture Diagrams**

### **Clean Architecture Layers**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PRESENTATION LAYER                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │  Views (XAML)  │  ViewModels  │  Converters  │  Navigation  │  Dialogs  ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                      │                                       │
│                                      ▼                                       │
│                              APPLICATION LAYER                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │  Commands  │  Queries  │  Handlers  │  Validators  │  DTOs  │  Mappers  ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                      │                                       │
│                                      ▼                                       │
│                                 CORE LAYER                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │  Entities  │  Value Objects  │  Domain Events  │  Domain Services       ││
│  │  Repository Interfaces  │  Specifications  │  Enums  │  Exceptions      ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                      │                                       │
│                                      ▼                                       │
│                            INFRASTRUCTURE LAYER                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │  EF Core  │  Repositories  │  External APIs  │  File System  │  Cache   ││
│  │  AI Providers  │  Resilience  │  Logging  │  Configuration              ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

### **Dependency Flow**

```
                    ┌─────────────────┐
                    │  Presentation   │
                    │    (UI/MVVM)    │
                    └────────┬────────┘
                             │ depends on
                             ▼
                    ┌─────────────────┐
                    │   Application   │
                    │  (Use Cases)    │
                    └────────┬────────┘
                             │ depends on
                             ▼
                    ┌─────────────────┐
                    │      Core       │
                    │ (Domain Logic)  │
                    └────────┬────────┘
                             │ abstracted by
                             ▼
                    ┌─────────────────┐
                    │ Infrastructure  │
                    │  (Externals)    │
                    └─────────────────┘

    ⚠️ RULE: Dependencies point INWARD only!
    ⚠️ Core NEVER references Infrastructure or Presentation!
```

### **Bounded Context Map**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SAVESTATE REBORN                                   │
│                                                                              │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐      │
│  │   GAME LIBRARY   │    │  ROM MANAGEMENT  │    │   AI ASSISTANT   │      │
│  │                  │    │                  │    │                  │      │
│  │  • Game          │    │  • RomFile       │    │  • ChatSession   │      │
│  │  • Platform      │    │  • Emulator      │    │  • Memory        │      │
│  │  • GameFile      │    │  • SaveState     │    │  • Provider      │      │
│  │  • Tag           │◄───┤  • Platform      │    │  • Request       │      │
│  │  • Collection    │    │                  │    │  • Response      │      │
│  └──────────────────┘    └──────────────────┘    └──────────────────┘      │
│            │                      │                      │                  │
│            └──────────────────────┼──────────────────────┘                  │
│                                   ▼                                         │
│                    ┌──────────────────────────┐                             │
│                    │      SHARED KERNEL       │                             │
│                    │                          │                             │
│                    │  • Entity<TId>           │                             │
│                    │  • ValueObject           │                             │
│                    │  • DomainEvent           │                             │
│                    │  • Result<T>             │                             │
│                    │  • AuditableEntity       │                             │
│                    └──────────────────────────┘                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### **AI Pipeline Flow**

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   REQUEST   │────▶│    CACHE    │────▶│ ORCHESTRATOR│────▶│  PROVIDER   │
│             │     │   CHECK     │     │             │     │  SELECT     │
└─────────────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘
                           │                   │                   │
                    ┌──────▼──────┐            │            ┌──────▼──────┐
                    │ CACHE HIT?  │            │            │   OPENAI    │
                    │  Return     │            │            │   GROQ      │
                    │  Cached     │            │            │   LOCAL     │
                    └─────────────┘            │            └──────┬──────┘
                                               │                   │
                                        ┌──────▼──────┐     ┌──────▼──────┐
                                        │  RESILIENCE │     │   CIRCUIT   │
                                        │   POLICY    │     │   BREAKER   │
                                        └──────┬──────┘     └──────┬──────┘
                                               │                   │
                                        ┌──────▼──────┐     ┌──────▼──────┐
                                        │   RETRY     │     │  FALLBACK   │
                                        │   LOGIC     │     │  PROVIDER   │
                                        └──────┬──────┘     └─────────────┘
                                               │
                                        ┌──────▼──────┐
                                        │   MEMORY    │
                                        │   STORE     │
                                        └──────┬──────┘
                                               │
                                        ┌──────▼──────┐
                                        │  RESPONSE   │
                                        │   CACHE     │
                                        └─────────────┘
```

---

## **📊 Database Schema (ERD)**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              DATABASE SCHEMA                                 │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│    PLATFORMS    │       │      GAMES      │       │    GAME_FILES   │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ Id (PK)         │◄──────┤ Id (PK)         │◄──────┤ Id (PK)         │
│ Name            │       │ Title           │       │ GameId (FK)     │
│ ShortName       │       │ PlatformId (FK) │       │ Path            │
│ Type            │       │ Description     │       │ SizeBytes       │
│ IconPath        │       │ CoverImageUrl   │       │ Type            │
│ CreatedAt       │       │ InstallPath     │       │ Hash            │
│ UpdatedAt       │       │ Status          │       │ CreatedAt       │
└─────────────────┘       │ LastPlayed      │       └─────────────────┘
                          │ TotalPlayTime   │
                          │ IsHidden        │       ┌─────────────────┐
                          │ CreatedAt       │       │      TAGS       │
                          │ UpdatedAt       │       ├─────────────────┤
                          └────────┬────────┘       │ Id (PK)         │
                                   │                │ Name            │
                                   │                │ Color           │
                          ┌────────▼────────┐       │ CreatedAt       │
                          │   GAME_TAGS     │       └────────┬────────┘
                          ├─────────────────┤                │
                          │ GameId (FK)     │────────────────┘
                          │ TagId (FK)      │
                          └─────────────────┘

┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│   EMULATORS     │       │    ROM_FILES    │       │   SAVE_STATES   │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ Id (PK)         │◄──────┤ Id (PK)         │◄──────┤ Id (PK)         │
│ Name            │       │ Title           │       │ RomFileId (FK)  │
│ PlatformId (FK) │       │ PlatformId (FK) │       │ Name            │
│ ExecutablePath  │       │ EmulatorId (FK) │       │ FilePath        │
│ CommandArgs     │       │ FilePath        │       │ ScreenshotPath  │
│ RequiresBios    │       │ SizeBytes       │       │ CreatedAt       │
│ BiosPath        │       │ Hash            │       │ PlayTime        │
│ IsDefault       │       │ Region          │       └─────────────────┘
│ CreatedAt       │       │ IsVerified      │
└─────────────────┘       │ CreatedAt       │
                          └─────────────────┘

┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│   AI_MODELS     │       │  CHAT_SESSIONS  │       │  CHAT_MESSAGES  │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ Id (PK)         │◄──────┤ Id (PK)         │◄──────┤ Id (PK)         │
│ Name            │       │ AiModelId (FK)  │       │ SessionId (FK)  │
│ Provider        │       │ Title           │       │ Role            │
│ ModelId         │       │ CreatedAt       │       │ Content         │
│ Type            │       │ UpdatedAt       │       │ TokenCount      │
│ MaxTokens       │       │ TotalTokens     │       │ CreatedAt       │
│ IsDefault       │       └─────────────────┘       └─────────────────┘
│ EndpointUrl     │
│ CreatedAt       │
└─────────────────┘
```

---

## **📋 Domain Event Catalog**

All domain events in the system with their triggers and handlers.

### **Game Library Events**

| Event | Trigger | Handlers | Payload |
|:---|:---|:---|:---|
| `GameCreatedEvent` | Game.Create() | UpdateStats, NotifyUI | GameId, Title, PlatformId |
| `GameImportedEvent` | ImportService.Import() | EnrichMetadata, GenerateCover | GameId, Source, SourceId |
| `GameLaunchedEvent` | LaunchService.Launch() | UpdateLastPlayed, TrackPlayTime | GameId, StartTime |
| `GameStoppedEvent` | Process.Exited | UpdatePlayTime, SaveState | GameId, Duration |
| `GameDeletedEvent` | DeleteCommand | CleanupFiles, UpdateStats | GameId |
| `GameTaggedEvent` | TagCommand | UpdateIndex | GameId, TagId |
| `GameMetadataUpdatedEvent` | MetadataService | UpdateCache, RefreshUI | GameId, Metadata |

### **ROM Management Events**

| Event | Trigger | Handlers | Payload |
|:---|:---|:---|:---|
| `RomScannedEvent` | Scanner.Scan() | VerifyHash, UpdateDB | RomPath, Hash, Platform |
| `RomImportedEvent` | ImportCommand | Organize, GenerateArt | RomId, FilePath |
| `SaveStateCreatedEvent` | Emulator callback | CaptureScreenshot | RomId, StatePath, Time |
| `SaveStateLoadedEvent` | LoadCommand | UpdateMetrics | StateId, RomId |

### **AI Events**

| Event | Trigger | Handlers | Payload |
|:---|:---|:---|:---|
| `ChatMessageReceivedEvent` | User input | Process, Store | SessionId, Message |
| `AiResponseGeneratedEvent` | Provider response | CacheResponse, UpdateTokens | SessionId, Response, Tokens |
| `ProviderFailedEvent` | HTTP error | TriggerFallback, LogError | Provider, Error, RetryCount |
| `CircuitBreakerOpenedEvent` | Threshold breached | AlertUser, SwitchProvider | Provider, Duration |
| `MemoryPrunedEvent` | Capacity exceeded | LogPruned, UpdateMetrics | EntriesRemoved, NewCount |

### **Event Publishing Pattern**

```csharp
// Domain entity raises event
public class Game : AggregateRoot<GameId>
{
    public static Game Create(string title, Platform platform)
    {
        var game = new Game(GameId.New(), title, platform);
        game.AddDomainEvent(new GameCreatedEvent(game.Id, title, platform.Id));
        return game;
    }
}

// Handler subscribes to event
public class GameCreatedEventHandler : INotificationHandler<GameCreatedEvent>
{
    private readonly IStatisticsService _stats;
    private readonly IEventBus _eventBus;

    public async Task Handle(GameCreatedEvent notification, CancellationToken ct)
    {
        await _stats.IncrementGameCountAsync(notification.PlatformId, ct);
        await _eventBus.PublishAsync(new UiRefreshNeededEvent("Games"), ct);
    }
}
```

---

## **✅ Validation Rules Catalog**

All business validation rules in one place.

### **Game Entity Rules**

| Rule ID | Field | Rule | Error Message |
|:---|:---|:---|:---|
| G-001 | Title | Required, 1-200 chars | "Game title is required and must be 1-200 characters" |
| G-002 | Title | No control characters | "Game title contains invalid characters" |
| G-003 | Platform | Required | "Platform is required" |
| G-004 | InstallPath | Valid path if Installed | "Install path is required for installed games" |
| G-005 | InstallPath | Directory exists | "Install path does not exist" |
| G-006 | InstallPath | No path traversal | "Install path contains invalid characters" |
| G-007 | CoverImageUrl | Valid URL or null | "Cover image URL is invalid" |
| G-008 | Tags | Max 20 tags | "Game cannot have more than 20 tags" |

### **ROM File Rules**

| Rule ID | Field | Rule | Error Message |
|:---|:---|:---|:---|
| R-001 | FilePath | Required | "ROM file path is required" |
| R-002 | FilePath | File exists | "ROM file does not exist" |
| R-003 | FilePath | Valid extension | "File extension not supported for platform" |
| R-004 | SizeBytes | > 0 | "ROM file is empty" |
| R-005 | SizeBytes | < 4GB | "ROM file exceeds maximum size" |
| R-006 | Hash | Valid SHA256 | "ROM hash is invalid" |
| R-007 | Platform | Required | "Platform is required" |

### **AI Request Rules**

| Rule ID | Field | Rule | Error Message |
|:---|:---|:---|:---|
| A-001 | Message | Required, 1-10000 chars | "Message must be 1-10000 characters" |
| A-002 | Message | No injection patterns | "Message contains disallowed content" |
| A-003 | MaxTokens | 1-8192 | "MaxTokens must be between 1 and 8192" |
| A-004 | Temperature | 0.0-2.0 | "Temperature must be between 0.0 and 2.0" |
| A-005 | Model | Supported model | "Model is not supported" |

### **Validation Implementation**

```csharp
public class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator(IPlatformRepository platformRepo)
    {
        // G-001: Title required
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Game title is required")
            .MaximumLength(200).WithMessage("Game title must be 200 characters or less");

        // G-002: No control characters
        RuleFor(x => x.Title)
            .Must(t => !t.Any(char.IsControl))
            .WithMessage("Game title contains invalid characters");

        // G-003: Platform exists
        RuleFor(x => x.PlatformId)
            .MustAsync(async (id, ct) => await platformRepo.ExistsAsync(id, ct))
            .WithMessage("Platform does not exist");

        // G-006: No path traversal
        RuleFor(x => x.InstallPath)
            .Must(p => p is null || !p.Contains(".."))
            .WithMessage("Install path contains invalid characters");
    }
}
```

---

## **🔍 Debugging Guide**

### **Common Errors Decision Tree**

```
Error Occurred
     │
     ├── Build Error (CS0XXX)?
     │        │
     │        ├── CS0246: Type not found
     │        │     └── Add missing `using` statement or NuGet package
     │        │
     │        ├── CS0535: Does not implement
     │        │     └── Check interface signature matches implementation
     │        │
     │        ├── CS1061: Does not contain definition
     │        │     └── Check method name spelling and parameter types
     │        │
     │        └── CS0103: Name does not exist
     │              └── Check variable scope and initialization
     │
     ├── Runtime Error?
     │        │
     │        ├── NullReferenceException
     │        │     ├── Check all injected dependencies
     │        │     ├── Check async methods await properly
     │        │     └── Use null-conditional operators (?.)
     │        │
     │        ├── InvalidOperationException
     │        │     ├── Check entity state before operations
     │        │     └── Check circuit breaker state
     │        │
     │        ├── HttpRequestException
     │        │     ├── Check API key configuration
     │        │     ├── Check network connectivity
     │        │     └── Check rate limiting status
     │        │
     │        └── SqliteException
     │              ├── Check connection string
     │              ├── Run migrations
     │              └── Check file permissions
     │
     └── Test Failure?
              │
              ├── Mock not returning expected value
              │     └── Verify Setup() matches actual call parameters
              │
              ├── Async test deadlock
              │     └── Use ConfigureAwait(false) or async all the way
              │
              └── DbContext disposed
                    └── Use fresh scope per test, don't share contexts
```

### **Debugging Checklist**

```markdown
## Pre-Debug Checklist

- [ ] Is the project building without errors?
- [ ] Are all NuGet packages restored?
- [ ] Is the database migrated to latest?
- [ ] Are user secrets configured for API keys?
- [ ] Is the correct launch profile selected?

## Runtime Debug Checklist

- [ ] Check Output window for exceptions
- [ ] Check logs in `logs/` folder
- [ ] Enable detailed errors in appsettings.Development.json
- [ ] Use breakpoints on exception handlers
- [ ] Check DI registration for missing services

## Test Debug Checklist

- [ ] Run test in isolation (not parallel)
- [ ] Check test output for detailed errors
- [ ] Verify mock setups match actual calls
- [ ] Check async/await patterns
- [ ] Use Test Explorer to debug single tests
```

### **Logging Investigation Commands**

```bash
# View recent errors (PowerShell)
Get-Content logs/savestate-*.log -Tail 100 | Select-String "Error|Exception"

# Filter by component
Get-Content logs/savestate-*.log | Select-String "AiOrchestrator"

# View structured logs as JSON
Get-Content logs/savestate-*.log | ConvertFrom-Json | Where-Object Level -eq "Error"

# Count errors by type
Get-Content logs/savestate-*.log | Select-String "Exception" | Group-Object | Sort-Object Count -Descending
```

---

## **🔄 State Machine Diagrams**

### **Game Status State Machine**

```
                    ┌─────────────┐
                    │   UNKNOWN   │
                    └──────┬──────┘
                           │ Scan/Import
                           ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  UNINSTALL  │◄────│  NOT_OWNED  │────▶│   WISHLST   │
└─────────────┘     └──────┬──────┘     └─────────────┘
                           │ Purchase/Add
                           ▼
                    ┌─────────────┐
          ┌────────▶│    OWNED    │◄────────┐
          │         └──────┬──────┘         │
          │                │ Install        │ Uninstall
          │                ▼                │
          │         ┌─────────────┐         │
          │         │  INSTALLING │─────────┤
          │         └──────┬──────┘         │
          │                │ Complete       │
          │                ▼                │
          │         ┌─────────────┐         │
          └─────────│  INSTALLED  │─────────┘
                    └──────┬──────┘
                           │ Launch
                           ▼
                    ┌─────────────┐
                    │   RUNNING   │
                    └──────┬──────┘
                           │ Exit
                           ▼
                    ┌─────────────┐
                    │  INSTALLED  │
                    └─────────────┘
```

### **Circuit Breaker State Machine**

```
                    ┌─────────────┐
                    │   CLOSED    │◄──────────────────┐
                    │ (Normal)    │                   │
                    └──────┬──────┘                   │
                           │                          │
                           │ Failure count            │ Success in
                           │ >= threshold             │ half-open
                           ▼                          │
                    ┌─────────────┐                   │
                    │    OPEN     │                   │
                    │ (Blocking)  │                   │
                    └──────┬──────┘                   │
                           │                          │
                           │ Duration                 │
                           │ elapsed                  │
                           ▼                          │
                    ┌─────────────┐                   │
                    │  HALF-OPEN  │───────────────────┘
                    │ (Testing)   │
                    └──────┬──────┘
                           │
                           │ Failure in half-open
                           ▼
                    ┌─────────────┐
                    │    OPEN     │
                    └─────────────┘
```

---

## **📖 Domain Glossary**

| Term | Definition | Context |
|:---|:---|:---|
| **Game** | A playable software title with metadata and files | Core entity in Game Library |
| **Platform** | A gaming system (PC, NES, PS5, etc.) | Categorizes Games and ROMs |
| **ROM** | Read-Only Memory image of a game cartridge/disc | Used with emulators |
| **Emulator** | Software that mimics hardware to run ROMs | Infrastructure service |
| **Save State** | Snapshot of emulator memory at a point in time | Associated with ROMs |
| **Provider** | External service for game discovery (Steam, GOG) | Infrastructure service |
| **LLM** | Large Language Model for AI assistance | AI subsystem |
| **Circuit Breaker** | Resilience pattern to prevent cascade failures | AI resilience |
| **Bounded Context** | Self-contained domain with its own models | DDD concept |
| **Aggregate Root** | Entity that controls access to related entities | DDD concept |
| **Value Object** | Immutable object defined by its values | DDD concept |
| **Domain Event** | Notification that something happened in domain | Event-driven architecture |
| **CQRS** | Command Query Responsibility Segregation | Architecture pattern |
| **DTO** | Data Transfer Object for layer boundaries | Application layer |

---

## **📁 File Dependency Map**

Which files depend on which for impact analysis.

```
SaveState.Core (No dependencies)
├── Entities/Game.cs
│   ├── Used by: GameRepository, GameService, CreateGameHandler
│   └── Uses: Platform, GameFile, Tag, GameId
├── Entities/Platform.cs
│   ├── Used by: PlatformRepository, Game, RomFile
│   └── Uses: PlatformId
├── Services/IGameValidationService.cs
│   ├── Used by: CreateGameHandler, UpdateGameHandler
│   └── Uses: Game
└── Events/GameCreatedEvent.cs
    ├── Used by: Game.Create()
    └── Handled by: GameCreatedEventHandler

SaveState.Application (Depends on: Core)
├── Commands/CreateGameCommand.cs
│   ├── Used by: GameController, ViewModel
│   └── Uses: (none)
├── Handlers/CreateGameCommandHandler.cs
│   ├── Used by: MediatR
│   └── Uses: IGameRepository, IGameValidationService, Game
└── DTOs/GameSummaryDto.cs
    ├── Used by: GetGamesQueryHandler, ViewModel
    └── Uses: (none)

SaveState.Infrastructure (Depends on: Core, Application)
├── Persistence/SaveStateDbContext.cs
│   ├── Used by: All Repositories
│   └── Uses: All Entities
├── Repositories/GameRepository.cs
│   ├── Used by: Handlers
│   └── Uses: SaveStateDbContext, Game
└── External/SteamProvider.cs
    ├── Used by: GameImportService
    └── Uses: ISteamApiClient, GameInfo

SaveState.Presentation (Depends on: Application)
├── ViewModels/GameListViewModel.cs
│   ├── Used by: GameListView.xaml
│   └── Uses: GetGamesQuery, IMediator
└── Views/GameListView.xaml
    ├── Used by: MainWindow navigation
    └── Uses: GameListViewModel
```

---

## **🎯 Code Review Checklist**

Use this checklist when reviewing PRs or self-reviewing code.

### **Architecture & Design**

- [ ] Dependencies flow inward (toward Core)
- [ ] No direct references from Core to Infrastructure
- [ ] Interfaces defined in Core, implementations in Infrastructure
- [ ] DTOs used at layer boundaries
- [ ] Value objects used instead of primitives where appropriate

### **Code Quality**

- [ ] Classes < 200 lines
- [ ] Methods < 30 lines
- [ ] Cyclomatic complexity < 10
- [ ] No magic numbers/strings (use constants)
- [ ] No code duplication
- [ ] Meaningful names for all identifiers

### **Error Handling**

- [ ] All exceptions caught and logged
- [ ] Custom exceptions used for domain errors
- [ ] Validation before operations
- [ ] Null checks or null-object pattern
- [ ] Result pattern for operations that can fail

### **Testing**

- [ ] Unit tests for all public methods
- [ ] Edge cases covered
- [ ] Mocks used for external dependencies
- [ ] Assertions use FluentAssertions
- [ ] Test names describe behavior

### **Security**

- [ ] No secrets in code
- [ ] Input validated and sanitized
- [ ] File paths checked for traversal
- [ ] SQL injection prevented (use parameterized queries)
- [ ] Sensitive data not logged

### **Performance**

- [ ] Async/await used for I/O
- [ ] No N+1 query problems
- [ ] Appropriate use of caching
- [ ] No unnecessary allocations in hot paths
- [ ] Pagination for large result sets

---

**This document should be referenced alongside phase documents for architecture decisions and debugging.**
