# Decisions Log - Why We Made These Choices

**Purpose**: Documents architectural decisions so AI/developers don't accidentally undo them.
**Last Updated**: January 8, 2026 (ADR-010: Dialog System Implementation)

---

## How to Read This Document

Each decision follows this format:

- **Context**: What problem we faced
- **Decision**: What we chose
- **Consequences**: Trade-offs accepted
- **Do NOT**: Patterns to avoid

---

## Architecture Decisions

### ADR-001: Clean Architecture with Vertical Slices

**Date**: November 2025
**Status**: ✅ Active

**Context**: Needed a scalable architecture for a complex gaming platform with multiple bounded contexts.

**Decision**: Adopt Clean Architecture with vertical slice organization within each layer.

**Structure**:

```
src/
├── SaveState.Core/           # Domain Layer (entities, value objects)
├── SaveState.Application/    # Application Layer (CQRS handlers)
├── SaveState.Infrastructure/ # Infrastructure Layer (EF Core, APIs)
├── SaveState.Presentation/   # Presentation Layer (Avalonia UI)
└── SaveState.CLI/            # CLI Alternative Presentation
```

**Consequences**:

- ✅ Clear dependency direction (outer → inner)
- ✅ Domain is framework-agnostic
- ✅ Easy to test in isolation
- ⚠️ More files/folders than simple architecture

**Do NOT**:

- Reference Infrastructure from Core
- Put business logic in ViewModels
- Skip the Application layer for "simple" features

---

### ADR-002: CQRS with MediatR

**Date**: November 2025
**Status**: ✅ Active

**Context**: Controllers/ViewModels were accumulating 10+ service dependencies.

**Decision**: Implement CQRS pattern using MediatR for all operations.

**Pattern**:

```csharp
// Command
public record CreateGameCommand(string Title) : IRequest<Result<GameId>>;

// Handler
public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Result<GameId>>
```

**Consequences**:

- ✅ Single dependency (IMediator) in consumers
- ✅ Easy to add cross-cutting concerns (logging, validation)
- ✅ Clear separation of read/write operations
- ⚠️ More boilerplate per feature

**Do NOT**:

- Inject services directly into ViewModels (use MediatR)
- Create "God handlers" that do multiple things
- Skip creating separate Command and Query types

---

### ADR-003: Result Pattern Instead of Exceptions

**Date**: November 2025
**Status**: ✅ Active

**Context**: Exceptions for control flow led to unclear error handling and hidden failures.

**Decision**: Use `Result<T>` pattern for all operations that can fail.

**Pattern**:

```csharp
public async Task<Result<Game>> GetGameAsync(GameId id)
{
    var game = await _repository.GetByIdAsync(id);
    if (game is null)
        return Result.Failure<Game>(GameErrors.NotFound(id));
    return Result.Success(game);
}
```

**Consequences**:

- ✅ Errors are explicit in type signatures
- ✅ Compiler enforces error handling
- ✅ No surprise exceptions
- ⚠️ 45+ legacy `return null` patterns still exist (technical debt)

**Do NOT**:

- Return `null` from methods (use `Result.Failure`)
- Throw exceptions for expected failures
- Ignore `Result.IsFailure` checks

---

### ADR-004: Avalonia for Cross-Platform UI

**Date**: November 2025
**Status**: ✅ Active

**Context**: Needed desktop UI that works on Windows, Linux, and macOS.

**Decision**: Use Avalonia 11.x instead of WPF.

**Consequences**:

- ✅ Single codebase for all platforms
- ✅ XAML-like syntax familiar to WPF developers
- ✅ Active community and development
- ⚠️ Some WPF patterns don't work (Triggers, certain behaviors)
- ⚠️ Designer support weaker than WPF

**Do NOT**:

- Use WPF-specific features (System.Windows namespace)
- Expect perfect designer preview
- Use `Trigger` (use `Styles` with `Selector` instead)

---

### ADR-005: Value Objects for Domain Primitives

**Date**: November 2025
**Status**: ✅ Active

**Context**: Primitive obsession led to bugs (wrong Guid passed to wrong parameter).

**Decision**: Wrap domain primitives in value objects.

**Pattern**:

```csharp
public sealed record GameId(Guid Value);
public sealed record GameTitle
{
    public string Value { get; }
    private GameTitle(string value) => Value = value;
    public static Result<GameTitle> Create(string title) { ... }
}
```

**Consequences**:

- ✅ Type safety (can't pass GameId where UserId expected)
- ✅ Validation at creation time
- ✅ Self-documenting code
- ⚠️ More types to create

**Do NOT**:

- Pass raw `Guid` for entity IDs
- Pass raw `string` for validated fields
- Skip validation in value object constructors

---

### ADR-006: Event-Driven Communication Between Contexts

**Date**: December 2025
**Status**: ✅ Active

**Context**: Bounded contexts needed to communicate without tight coupling.

**Decision**: Use domain events via MediatR notifications.

**Pattern**:

```csharp
public record GameCreatedEvent(GameId Id, string Title) : INotification;

// Publisher
await _mediator.Publish(new GameCreatedEvent(game.Id, game.Title.Value));

// Handler (different context)
public class UpdateAnalyticsOnGameCreated : INotificationHandler<GameCreatedEvent>
```

**Consequences**:

- ✅ Loose coupling between contexts
- ✅ Easy to add new reactions to events
- ✅ Audit trail via event handlers
- ⚠️ Async nature can make debugging harder

**Do NOT**:

- Call services from other contexts directly
- Create circular event chains
- Put business logic in event publishers

---

### ADR-007: Bounded Memory for AI Conversations

**Date**: January 2026
**Status**: ✅ Active

**Context**: AI conversation memory was unbounded, risking memory exhaustion.

**Decision**: Implement `BoundedMemoryStore` with max 500 entries, 50K tokens.

**Implementation**:

```csharp
public class BoundedMemoryStore
{
    private const int MaxEntries = 500;
    private const int MaxTokens = 50_000;
    private readonly ConcurrentDictionary<string, ConversationThread> _threads;
}
```

**Consequences**:

- ✅ Predictable memory usage
- ✅ Automatic pruning of old conversations
- ✅ Thread-safe via ConcurrentDictionary
- ⚠️ Very old context may be lost

**Do NOT**:

- Store unbounded conversation history
- Use non-thread-safe collections for shared state
- Exceed token limits without pruning

---

### ADR-008: Dual AI Provider Strategy

**Date**: January 2026
**Status**: ✅ Active

**Context**: Single AI provider dependency was a reliability risk.

**Decision**: Support OpenAI as primary, Groq as fallback.

**Pattern**:

```csharp
try
{
    return await _openAiProvider.ChatAsync(request, ct);
}
catch (Exception)
{
    _logger.LogWarning("OpenAI failed, falling back to Groq");
    return await _groqProvider.ChatAsync(request, ct);
}
```

**Consequences**:

- ✅ Higher availability
- ✅ Cost optimization options
- ⚠️ Must maintain two provider implementations

**Do NOT**:

- Assume single provider will always be available
- Hard-code provider URLs without configuration

---

### ADR-009: Plugin Architecture for Extensibility

**Date**: December 2025
**Status**: ✅ Active

**Context**: Needed to support community extensions without modifying core.

**Decision**: Dynamic plugin loading via `IPlugin` interface.

**Pattern**:

```csharp
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    void Initialize(IServiceCollection services);
    void Configure(IApplicationBuilder app);
}
```

**Consequences**:

- ✅ Community can extend functionality
- ✅ Core remains stable
- ✅ 19 plugins implemented
- ⚠️ Plugin compatibility testing needed

**Do NOT**:

- Put plugin-specific code in core
- Skip plugin sandboxing for untrusted plugins
- Load plugins without version checking

---

### ADR-010: Complete Dialog Implementations Over Placeholders

**Date**: January 2026
**Status**: ✅ Active

**Context**: Placeholder dialog implementations were accumulating technical debt, causing build errors and poor user experience.

**Decision**: Eliminate all placeholder implementations by creating complete dialog systems with proper ViewModels, Views, and code-behinds.

**Pattern**:

```csharp
// ❌ BEFORE: Placeholder
public async Task<string?> ShowInputDialogAsync(string title, string message)
{
    _logger.LogWarning("Using placeholder implementation");
    var result = await ShowNoteEditorAsync(null, message); // Hacky workaround
    return result?.Content;
}

// ✅ AFTER: Complete Implementation
public async Task<string?> ShowInputDialogAsync(string title, string message, string? placeholder = null)
{
    var vm = new TextInputDialogViewModel
    {
        Title = title,
        Message = message,
        Placeholder = placeholder ?? "Enter text..."
    };

    var dialog = new TextInputDialog { DataContext = vm };
    var mainWindow = GetMainWindow();
    return await dialog.ShowDialog<string?>(mainWindow);
}
```

**Implementation (January 8, 2026)**:

- Created `TextInputDialog` (ViewModel + View + Code-behind)
- Created `BranchCreationDialog` with proper constructor signatures
- Created `BranchMergeDialog` for save state operations
- Fixed `SaveStateSettingsDialog` to use record constructors correctly
- Resolved 15 build errors related to incomplete implementations

**Consequences**:

- ✅ Zero placeholder implementations remaining
- ✅ Build errors reduced from 15 → 0
- ✅ Warnings reduced from 995 → 117 (88% reduction)
- ✅ Type-safe dialog interactions
- ✅ Proper Avalonia patterns (`ExperimentalAcrylicBorder.Material`)
- ⚠️ More upfront development time (30 min vs 5 min placeholder)

**Key Learnings**:

1. **Type Safety**: `GameId` is a value object, use `!` operator not `.Value`
2. **Constructor Signatures**: Records use positional parameters, not property initializers
3. **Avalonia Patterns**: Fully qualify `Avalonia.Application.Current` to avoid ambiguity
4. **Dependency Injection**: Track dependencies through entire call chain

**Do NOT**:

- Create placeholder implementations with TODO comments
- Use hacky workarounds (e.g., repurposing `ShowNoteEditorAsync` for generic input)
- Skip proper ViewModel/View/Code-behind structure
- Ignore constructor signature mismatches

---

## Technology Decisions

### TDR-001: .NET 9.0 with Native AOT

**Date**: November 2025
**Consequence**: Fast startup (~200ms), single executable deployment.

### TDR-002: Entity Framework Core 9.0

**Date**: November 2025
**Consequence**: Full LINQ support, good SQLite performance.

### TDR-003: Polly for Resilience

**Date**: December 2025
**Consequence**: Retry, circuit breaker, timeout policies for external calls.

### TDR-004: Serilog for Logging

**Date**: November 2025
**Consequence**: Structured logging, multiple sinks, enrichers.

### TDR-005: CommunityToolkit.Mvvm

**Date**: November 2025
**Consequence**: Source generators for MVVM boilerplate reduction.

---

## Anti-Patterns to Avoid

| Pattern | Why It's Bad | What to Do Instead |
|---------|--------------|-------------------|
| `async void` | Exceptions lost | Use `async Task` |
| `.Result` / `.GetAwaiter().GetResult()` | Deadlocks | Use `await` |
| `return null` | NullReferenceException | Use `Result.Failure` |
| Silent `catch { }` | Hidden failures | Log and handle |
| Direct service injection in VMs | 10+ dependencies | Use MediatR |
| Primitive types for IDs | Wrong ID passed | Use value objects |
| Throwing exceptions for expected cases | Control flow abuse | Use Result pattern |
| **Placeholder implementations** | **Technical debt compounds** | **Complete implementation** |

---

**This log is authoritative. If code contradicts these decisions, the code is wrong (or these decisions need updating).**
**Last Updated**: January 8, 2026 (ADR-010: Dialog System Implementation)
