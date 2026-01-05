# Patterns Cookbook - Copy-Paste Code Examples

**Purpose**: Provides copy-paste code patterns from the **actual SaveState Reborn codebase**. Use these exactly.
**Last Updated**: January 2, 2026
**Source**: Extracted from `src/` directory

---

## Table of Contents

1. [Result Pattern (Actual Implementation)](#1-result-pattern-actual-implementation)
2. [CQRS Commands (Real Examples)](#2-cqrs-commands-real-examples)
3. [CQRS Queries (Real Examples)](#3-cqrs-queries-real-examples)
4. [Value Objects (Real Examples)](#4-value-objects-real-examples)
5. [Plugin System (Real Examples)](#5-plugin-system-real-examples)
6. [AI Integration (Real Examples)](#6-ai-integration-real-examples)
7. [MUGEN/Fighting Game Patterns](#7-mugenfighting-game-patterns)
8. [Repository Pattern](#8-repository-pattern)
9. [Dependency Injection Registration](#9-dependency-injection-registration)
10. [Async Patterns](#10-async-patterns)
11. [Avalonia MVVM (Real Examples)](#11-avalonia-mvvm-real-examples)
12. [Testing Patterns](#12-testing-patterns)
13. [Anti-Patterns to Avoid](#13-anti-patterns-to-avoid)

---

## 1. Result Pattern (Actual Implementation)

**Source**: `src/SaveState.Core/Common/Result.cs`

### The Actual Result Class

```csharp
// FROM: SaveState.Core/Common/Result.cs
namespace SaveState.Core.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, string? error = null, ErrorType errorType = ErrorType.None)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, error, errorType);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value = default, string? error = null, ErrorType errorType = ErrorType.None)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Validation) =>
        new(false, default, error, errorType);
}

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Internal,
    ExternalService,
    NotImplemented
}
```

### Usage Pattern (From AiOrchestrator.cs)

```csharp
// FROM: SaveState.Infrastructure/Ai/AiOrchestrator.cs
public async Task<Result<string>> GenerateTextAsync(string prompt, CancellationToken ct = default)
{
    try
    {
        var request = new AiRequest(Type: AiRequestType.Completion, Prompt: prompt);
        var response = await ProcessRequestAsync(request, ct).ConfigureAwait(false);

        if (response.IsSuccessful)
        {
            return Result<string>.Success(response.Content);
        }
        else
        {
            return Result<string>.Failure(response.Error ?? "AI generation failed");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to generate text for prompt: {Prompt}", prompt);
        return Result<string>.Failure($"Text generation failed: {ex.Message}");
    }
}
```

### ✅ Correct Usage

```csharp
// Returning success with value
return Result<Game>.Success(game);

// Returning failure with error type
return Result<Game>.Failure("Game not found", ErrorType.NotFound);

// Checking result
var result = await GetGameAsync(id);
if (result.IsFailure)
{
    _logger.LogWarning("Failed: {Error}", result.Error);
    return Result<GameDto>.Failure(result.Error!, result.ErrorType);
}
var game = result.Value!;
```

### ❌ Never Do This

```csharp
// WRONG: Returning null
return null;  // Use Result<T>.Failure instead

// WRONG: Ignoring IsFailure
var value = result.Value;  // May be null! Always check IsFailure first
```

---

## 2. CQRS Commands (Real Examples)

### Simple Command (From CreateGameCommand.cs)

**Source**: `src/SaveState.Application/GameLibrary/Commands/CreateGameCommand.cs`

```csharp
namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

public record CreateGameCommand(string Title, string? CoverImagePath) : IRequest<int>;

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, int>
{
    private readonly IGameRepository _repository;

    public CreateGameCommandHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var game = Game.Create(request.Title, null, null, request.CoverImagePath);
        await _repository.AddAsync(game, cancellationToken).ConfigureAwait(false);
        return 1; // Return affected rows
    }
}
```

### Command with Result Pattern (From GetRecommendationsCommand.cs)

**Source**: `src/SaveState.Application/Recommendations/Commands/GetRecommendationsCommand.cs`

```csharp
using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.Services;

namespace SaveState.Application.Recommendations.Commands;

public sealed record GetRecommendationsCommand(int Count = 10)
    : IRequest<Result<IReadOnlyList<GameRecommendation>>>;

public sealed class GetRecommendationsCommandHandler
    : IRequestHandler<GetRecommendationsCommand, Result<IReadOnlyList<GameRecommendation>>>
{
    private readonly IRecommendationService _recommendationService;

    public GetRecommendationsCommandHandler(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<Result<IReadOnlyList<GameRecommendation>>> Handle(
        GetRecommendationsCommand request,
        CancellationToken ct)
    {
        return await _recommendationService.GetRecommendationsAsync(request.Count, ct);
    }
}
```

### MUGEN-Specific Command (From ScanMugenCharactersCommand.cs)

**Source**: `src/SaveState.Application/Mugen/Commands/ScanMugenCharactersCommand.cs`

```csharp
namespace SaveState.Application.Mugen.Commands;

using MediatR;

/// <summary>
/// Command to scan a directory for MUGEN characters and add them to the library.
/// </summary>
public record ScanMugenCharactersCommand(
    string DirectoryPath,
    bool IncludeSubdirectories = true,
    bool OverwriteExisting = false
) : IRequest<Unit>;
```

### All 69 Commands in the Codebase

Use these as templates for new commands:

| Domain | Commands |
|--------|----------|
| **GameLibrary** | CreateGame, DeleteGame, UpdateGame, LaunchGame, ImportGame, FetchCoverArt, AddToBacklog |
| **MUGEN** | ScanMugenCharacters, LaunchIkemenVersus, CreateMugenTournament, RunDeathMatchSimulation |
| **AI/Recommendations** | GetRecommendations, GetSimilarGames, AskAssistant, GetQuickTips |
| **Analytics** | CreateGoal, UpdateGoalProgress, CancelGoal |
| **Social** | CreateCollection, AddGameToCollection, ShareCollection |
| **Plugins** | LoadPlugin, DiscoverPlugins |
| **Voice** | StartVoiceListening, StopVoiceListening, GetRegisteredVoiceCommands |

---

## 3. CQRS Queries (Real Examples)

### Query Pattern

**Naming Convention**: `Get{Entity}Query.cs` or `List{Entity}Query.cs`

```csharp
// FROM: SaveState.Application/GameLibrary/Queries/GetGameByIdQuery.cs
public sealed record GetGameByIdQuery(Guid GameId) : IRequest<Result<GameDetailsDto>>;

public sealed class GetGameByIdQueryHandler
    : IRequestHandler<GetGameByIdQuery, Result<GameDetailsDto>>
{
    private readonly IGameRepository _repository;

    public GetGameByIdQueryHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GameDetailsDto>> Handle(
        GetGameByIdQuery request,
        CancellationToken ct)
    {
        var game = await _repository.GetByIdAsync(request.GameId, ct);

        if (game is null)
            return Result<GameDetailsDto>.Failure("Game not found", ErrorType.NotFound);

        return Result<GameDetailsDto>.Success(GameDetailsDto.FromDomain(game));
    }
}
```

### All 40 Queries in the Codebase

| Domain | Queries |
|--------|---------|
| **GameLibrary** | GetGameById, GetAllGames, SearchGames, GetBacklog, GetGamingHeatmap, GetLibraryStatistics |
| **MUGEN** | GetMugenCharacters |
| **Analytics** | GetActiveGoals, GetCompletedGoals |
| **Social** | GetReviews, GetFriendActivity, GetSharedCollections, GetLeaderboard |
| **SaveStates** | GetSaveStates, GetSaveStateTimeline |
| **Plugins** | GetPlugins |

---

## 4. Value Objects (Real Examples)

### GameTitle (Validated Value Object)

**Source**: `src/SaveState.Core/Common/ValueObjects/GameTitle.cs`

```csharp
using SaveState.Core.Common.Base;

namespace SaveState.Core.Common.ValueObjects;

public sealed class GameTitle : ValueObject
{
    public string Value { get; }

    private GameTitle(string value)
    {
        Value = Guard.Against.NullOrWhiteSpace(value, "title")
            .Trim();
        if (Value.Length < 1 || Value.Length > 200)
            throw new ArgumentException("Game title must be 1-200 characters", "title");
    }

    public static GameTitle From(string value) => new(value);

    public static implicit operator string(GameTitle title) => title.Value;
    public static explicit operator GameTitle(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant(); // Case-insensitive comparison
    }

    public override string ToString() => Value;
}
```

### Value Object Pattern Template

```csharp
public sealed class {Name} : ValueObject
{
    public {Type} Value { get; }

    private {Name}({Type} value)
    {
        // Validation here
        Value = value;
    }

    public static {Name} From({Type} value) => new(value);

    // Optional: implicit/explicit conversions
    public static implicit operator {Type}({Name} obj) => obj.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
```

---

## 5. Plugin System (Real Examples)

### IPlugin Interface

**Source**: `src/SaveState.Core/Plugins/IPlugin.cs`

```csharp
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string? Description { get; }
    PluginCapabilities Capabilities { get; }

    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}
```

### Plugin Capabilities Flags

```csharp
[Flags]
public enum PluginCapabilities
{
    None = 0,
    GameProvider = 1 << 0,       // Can provide games from external sources
    MetadataScraper = 1 << 1,    // Can scrape metadata
    ThemeProvider = 1 << 2,      // Provides UI themes
    Importer = 1 << 3,           // Can import data
    Exporter = 1 << 4,           // Can export data
    UIExtension = 1 << 5,        // Provides UI extensions
    AIService = 1 << 6,          // Provides AI features
    CloudStorage = 1 << 7,       // Cloud storage integration
    SocialFeatures = 1 << 8,     // Social features
    InputProvider = 1 << 9,      // Input/controller features
    PerformanceMonitor = 1 << 10,// Performance monitoring
    SaveStateProvider = 1 << 11, // Save state management
    SystemOptimization = 1 << 12,// System optimization
    LaunchExperience = 1 << 13,  // Launch experience enhancements
    MacroSystem = 1 << 14,       // Macro recording
    SteamDeckIntegration = 1 << 15,
    BatteryOptimization = 1 << 16,
    TouchControls = 1 << 17,
    CloudGaming = 1 << 18,
    MemoryIntelligence = 1 << 19
}
```

### Plugin Context Interface

```csharp
public interface IPluginContext
{
    IServiceProvider Services { get; }
    ILogger Logger { get; }
    string DataDirectory { get; }
    string PluginDirectory { get; }

    Task<bool> RegisterMenuItemAsync(PluginMenuItem item);
    Task<bool> RegisterGameProviderAsync(IGameProvider provider);
    Task<bool> RegisterMetadataScraperAsync(IMetadataScraper scraper);
    Task<bool> RegisterThemeAsync(ITheme theme);

    void ReportProgress(string message, float progress);
    void HandleEvent(PluginEventType eventType, object? data = null);
}
```

### Creating a New Plugin

```csharp
public class MyCustomPlugin : IPlugin
{
    public string Id => "my-custom-plugin";
    public string Name => "My Custom Plugin";
    public string Version => "1.0.0";
    public string Author => "Developer Name";
    public string? Description => "Adds custom functionality";
    public PluginCapabilities Capabilities => PluginCapabilities.GameProvider;

    private ILogger? _logger;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _logger = context.Logger;
        _logger.LogInformation("Initializing {Plugin}", Name);

        // Register capabilities
        await context.RegisterGameProviderAsync(new MyGameProvider());
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down {Plugin}", Name);
        return Task.CompletedTask;
    }
}
```

---

## 6. AI Integration (Real Examples)

### AiOrchestrator Pattern

**Source**: `src/SaveState.Infrastructure/Ai/AiOrchestrator.cs`

```csharp
public class AiOrchestrator : IAiOrchestrator
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly ICacheService _cache;
    private readonly ILogger<AiOrchestrator> _logger;
    private readonly IShortTermMemory _memory;
    private readonly IWebSearchService _searchService;
    private readonly IKnowledgeBaseService _kbService;

    public AiOrchestrator(
        IEnumerable<ILlmProvider> providers,
        ICacheService cache,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger,
        IShortTermMemory memory,
        IWebSearchService searchService,
        IKnowledgeBaseService kbService)
    {
        _providers = providers;
        _cache = cache;
        _logger = logger;
        _memory = memory;
        _searchService = searchService;
        _kbService = kbService;
    }

    public async Task<Result<AiResponse>> ExecutePromptAsync(
        string sessionId,
        string prompt,
        CancellationToken ct = default)
    {
        try
        {
            var request = new AiRequest(
                Type: AiRequestType.Chat,
                Prompt: prompt,
                MaxTokens: 1000,
                Temperature: 0.7f);

            var response = await ProcessRequestWithContextAsync(sessionId, request, ct)
                .ConfigureAwait(false);

            if (response.IsSuccessful)
            {
                return Result<AiResponse>.Success(response);
            }
            else
            {
                return Result<AiResponse>.Failure(response.Error ?? "AI execution failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute prompt: {Prompt}", prompt);
            return Result<AiResponse>.Failure($"Prompt execution failed: {ex.Message}");
        }
    }

    // Provider selection with fallback
    private ILlmProvider? SelectProvider(string? preferredProvider)
    {
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            var preferred = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase)
                && p.IsAvailable);
            if (preferred is not null) return preferred;
        }

        return _providers.FirstOrDefault(p => p.IsAvailable);
    }
}
```

### Calling AI from a Handler

```csharp
public sealed class AskAssistantCommandHandler
    : IRequestHandler<AskAssistantCommand, Result<AssistantResponse>>
{
    private readonly IAiOrchestrator _orchestrator;

    public AskAssistantCommandHandler(IAiOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<Result<AssistantResponse>> Handle(
        AskAssistantCommand request,
        CancellationToken ct)
    {
        var result = await _orchestrator.ExecutePromptAsync(
            request.SessionId,
            request.Question,
            ct);

        if (result.IsFailure)
            return Result<AssistantResponse>.Failure(result.Error!);

        return Result<AssistantResponse>.Success(
            new AssistantResponse(result.Value!.Content));
    }
}
```

---

## 7. MUGEN/Fighting Game Patterns

### MUGEN Character Scanning

```csharp
public record ScanMugenCharactersCommand(
    string DirectoryPath,
    bool IncludeSubdirectories = true,
    bool OverwriteExisting = false
) : IRequest<Unit>;
```

### MUGEN Tournament System

```csharp
public record CreateMugenTournamentCommand(
    string Name,
    TournamentFormat Format,
    IReadOnlyList<string> ParticipantIds
) : IRequest<Result<TournamentId>>;

public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Swiss
}
```

### Death Match Simulation

```csharp
public record RunDeathMatchSimulationCommand(
    string Character1Id,
    string Character2Id,
    int MatchCount = 1000
) : IRequest<Result<DeathMatchResult>>;
```

---

## 8. Repository Pattern

### Interface

```csharp
public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Game game, CancellationToken ct = default);
    Task UpdateAsync(Game game, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> SearchAsync(string query, CancellationToken ct = default);
}
```

### EF Core Implementation

```csharp
public sealed class GameRepository : IGameRepository
{
    private readonly SaveStateDbContext _context;

    public GameRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Games
            .Include(g => g.Achievements)
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Game game, CancellationToken ct = default)
    {
        await _context.Games.AddAsync(game, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
```

---

## 9. Dependency Injection Registration

### Service Registration Pattern

**Source**: `src/SaveState.Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Database
    services.AddDbContext<SaveStateDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

    // Repositories (Scoped)
    services.AddScoped<IGameRepository, GameRepository>();
    services.AddScoped<IAchievementRepository, AchievementRepository>();

    // Services (Scoped)
    services.AddScoped<IAiOrchestrator, AiOrchestrator>();
    services.AddScoped<IRecommendationService, RecommendationService>();

    // Singletons (Shared state)
    services.AddSingleton<ICacheService, MemoryCacheService>();
    services.AddSingleton<IShortTermMemory, BoundedMemoryStore>();

    // HTTP Clients (HttpClientFactory)
    services.AddHttpClient<ISteamGridDbService, SteamGridDbService>();
    services.AddHttpClient<IOpenAiProvider, OpenAiProvider>();

    // MediatR
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(CreateGameCommand).Assembly));

    return services;
}
```

---

## 10. Async Patterns

### ✅ Correct Patterns

```csharp
// Always use ConfigureAwait(false) in libraries
var result = await _service.GetAsync(id, ct).ConfigureAwait(false);

// Always pass CancellationToken
public async Task<Result<T>> GetAsync(int id, CancellationToken ct = default)

// Fire-and-forget in ViewModels (acknowledged)
_ = InitializeAsync(); // In constructor
```

### ❌ Never Do This

```csharp
// BLOCKS THREAD - CAUSES DEADLOCKS
var result = asyncMethod().Result;              // ❌
var result = asyncMethod().GetAwaiter().GetResult(); // ❌

// LOSES EXCEPTIONS
async void BadMethod() { }  // ❌

// MISSING CANCELLATION
await LongRunningAsync(); // ❌ Pass CancellationToken!
```

---

## 11. Avalonia MVVM (Real Examples)

### ViewModel with Commands

```csharp
public partial class GameLibraryViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private ObservableCollection<GameViewModel> _games = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public GameLibraryViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _mediator.Send(new GetAllGamesQuery());
            if (result.IsSuccess)
            {
                Games = new ObservableCollection<GameViewModel>(
                    result.Value.Select(g => new GameViewModel(g)));
            }
            else
            {
                ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### Navigation Commands (Async Pattern)

**Source**: `src/SaveState.Presentation/ViewModels/Library/GameCardViewModel.cs`

```csharp
public partial class GameCardViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILogger<GameCardViewModel> _logger;

    public GameId GameId { get; }

    [RelayCommand]
    private async Task OpenGame()
    {
        // Navigate to game detail view with GameId parameter
        await _navigationService.NavigateTo("Library", GameId);
        _logger.LogInformation("Navigating to game detail: {Title} ({GameId})", Title, GameId);
    }
}
```

**Key Points**:
- ✅ Always use `async Task` for navigation commands (never `async void`)
- ✅ Always `await` `NavigateTo` calls (supports `INavigationAware` ViewModels)
- ✅ Use `_ =` pattern only for fire-and-forget in constructors, not for navigation

---

## 12. Testing Patterns

### Unit Test with Mock

```csharp
public class CreateGameCommandHandlerTests
{
    private readonly Mock<IGameRepository> _repositoryMock;
    private readonly CreateGameCommandHandler _handler;

    public CreateGameCommandHandlerTests()
    {
        _repositoryMock = new Mock<IGameRepository>();
        _handler = new CreateGameCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsGameToRepository()
    {
        // Arrange
        var command = new CreateGameCommand("Test Game", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

---

## 14. Entity Pattern (Real Examples)

### Game Entity (Aggregate Root)

**Source**: `src/SaveState.Core/GameLibrary/Entities/Game.cs`

```csharp
namespace SaveState.Core.GameLibrary.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary.Enums;

public class Game : EntityBase, ISoftDelete
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? CoverImagePath { get; private set; }
    public string? InstallPath { get; private set; }
    public GameStatus Status { get; private set; }
    public Guid? PlatformId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastPlayedAt { get; private set; }
    public TimeSpan TotalPlayTime { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<GameFile> Files { get; private set; } = new List<GameFile>();

    private Game() { } // EF Core constructor

    // Factory method - ALWAYS use this, never new Game()
    public static Game Create(
        string title,
        Guid? platformId = null,
        string? description = null,
        string? coverImagePath = null)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)),
            PlatformId = platformId,
            Description = description,
            CoverImagePath = coverImagePath,
            Status = GameStatus.NotInstalled,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Behavior methods - encapsulate state changes
    public void SetInstallPath(string installPath)
    {
        InstallPath = Guard.Against.NullOrWhiteSpace(installPath, nameof(installPath));
        Status = GameStatus.Installed;
    }

    public void MarkAsRunning()
    {
        if (Status == GameStatus.Installed)
            Status = GameStatus.Running;
    }

    public void MarkAsDeleted()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
```

### EntityBase Class

**Source**: `src/SaveState.Core/Common/Base/EntityBase.cs`

```csharp
public abstract class EntityBase : IEntity, IAggregateRoot
{
    public virtual Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IEvent> _domainEvents = new();
    public IReadOnlyCollection<IEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

### SaveState Entity

**Source**: `src/SaveState.Core/SaveStates/Entities/SaveState.cs`

```csharp
public class SaveState : EntityBase
{
    public Guid GameId { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public string? ThumbnailPath { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public TimeSpan PlaytimeAtSave { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsAutoSave { get; private set; }
    public long FileSizeBytes { get; private set; }

    private SaveState() { }

    public static SaveState Create(
        Guid gameId,
        string filePath,
        TimeSpan playtimeAtSave,
        bool isAutoSave = false)
    {
        return new SaveState
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            FilePath = Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath)),
            CreatedAt = DateTime.UtcNow,
            PlaytimeAtSave = playtimeAtSave,
            IsAutoSave = isAutoSave
        };
    }

    public void ToggleFavorite() => IsFavorite = !IsFavorite;
    public void SetThumbnail(string path) => ThumbnailPath = path;
}
```

---

## 15. Domain Events (Real Examples)

### Event Base Class

**Source**: `src/SaveState.Core/Common/Events/EventBase.cs`

```csharp
namespace SaveState.Core.Common.Events;

public abstract class EventBase : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### Domain Event Definition

**Source**: `src/SaveState.Core/GameLibrary/Events/GameImportedEvent.cs`

```csharp
namespace SaveState.Core.GameLibrary.Events;

using SaveState.Core.Common.Events;

public class GameImportedEvent : EventBase
{
    public Guid GameId { get; }
    public string Source { get; }
    public string? SourceId { get; }
    public DateTime ImportedAt { get; }

    public GameImportedEvent(Guid gameId, string source, string? sourceId = null)
    {
        GameId = gameId;
        Source = source;
        SourceId = sourceId;
        ImportedAt = DateTime.UtcNow;
    }
}
```

### Event Handler (MediatR Notification)

**Source**: `src/SaveState.Application/GameLibrary/EventHandlers/GameImportedEventHandler.cs`

```csharp
namespace SaveState.Application.GameLibrary.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Events;

public class GameImportedEventHandler : INotificationHandler<GameImportedEvent>
{
    private readonly ILogger<GameImportedEventHandler> _logger;

    public GameImportedEventHandler(ILogger<GameImportedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(GameImportedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Game imported: {GameId} from {Source} ({SourceId}) at {ImportedAt}",
            notification.GameId,
            notification.Source,
            notification.SourceId ?? "N/A",
            notification.ImportedAt);

        // Could also:
        // - Update search indexes
        // - Send notifications
        // - Trigger background processing

        return Task.CompletedTask;
    }
}
```

### Publishing Events from Entity

```csharp
// In your entity
public void Import(string source, string? sourceId)
{
    Source = source;
    SourceId = sourceId;

    // Add domain event
    AddDomainEvent(new GameImportedEvent(Id, source, sourceId));
}

// In your handler/service after saving
await _repository.AddAsync(game, ct);
foreach (var domainEvent in game.DomainEvents)
{
    await _mediator.Publish(domainEvent, ct);
}
game.ClearDomainEvents();
```

---

## 16. Configuration Options Pattern (Real Examples)

### OpenAI Configuration

**Source**: `src/SaveState.Core/Configuration/OpenAiOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class OpenAiOptions
{
    public const string Section = "OpenAi";

    [Required(ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    [Required(ErrorMessage = "ApiKey is required")]
    [MinLength(1, ErrorMessage = "ApiKey cannot be empty")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "DefaultModel is required")]
    public string DefaultModel { get; set; } = "gpt-4";
}
```

### MUGEN Configuration

**Source**: `src/SaveState.Core/Configuration/MugenOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class MugenOptions
{
    public const string SectionName = "Mugen";

    [Required]
    public string ExecutablePath { get; set; } = "engines/ikemen/Ikemen_GO.exe";

    [Range(0, 10000)]
    public int ProcessStartupDelayMs { get; set; } = 500;

    public string[] CharacterDirectories { get; set; } = new[]
    {
        "data/characters/streetfighter",
        "data/characters/mvc2",
        "data/characters/builtin"
    };
}
```

### Registering Options

```csharp
// In DependencyInjection.cs
services.AddOptions<OpenAiOptions>()
    .Bind(configuration.GetSection(OpenAiOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddOptions<MugenOptions>()
    .Bind(configuration.GetSection(MugenOptions.SectionName))
    .ValidateDataAnnotations();
```

### Using Options

```csharp
public class OpenAiProvider : ILlmProvider
{
    private readonly OpenAiOptions _options;

    public OpenAiProvider(IOptions<OpenAiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl}chat/completions";
        // Use _options.ApiKey, _options.DefaultModel, etc.
    }
}
```

### All 19 Options Classes

| Category | Options Class | Section Name |
|----------|---------------|--------------|
| **AI** | `OpenAiOptions`, `GroqOptions`, `AiOptions` | `OpenAi`, `Groq`, `Ai` |
| **Gaming** | `MugenOptions`, `SteamOptions`, `GogOptions`, `EpicOptions` | `Mugen`, `Steam`, `Gog`, `Epic` |
| **Metadata** | `IgdbOptions`, `SteamGridDbOptions` | `Igdb`, `SteamGridDb` |
| **Infrastructure** | `DatabaseOptions`, `RateLimitingOptions`, `MemoryOptions` | Various |
| **Auth** | `JwtOptions`, `AuthenticationOptions` | `Jwt`, `Authentication` |
| **App** | `ApplicationOptions`, `LocalizationOptions` | `Application`, `Localization` |

---

## 17. Logging Patterns

### Structured Logging

```csharp
// ✅ CORRECT - Use structured logging with named parameters
_logger.LogInformation(
    "Game imported: {GameId} from {Source} at {ImportedAt}",
    game.Id,
    source,
    DateTime.UtcNow);

// ✅ CORRECT - Include exception
_logger.LogError(ex,
    "Failed to import game {Title} from {Source}",
    title,
    source);

// ❌ WRONG - String interpolation loses structure
_logger.LogInformation($"Game imported: {game.Id} from {source}");
```

### Log Levels

| Level | When to Use | Example |
|-------|-------------|---------|
| `Trace` | Very detailed, rarely on | Loop iterations |
| `Debug` | Diagnostic info | Cache hit/miss |
| `Information` | Normal operations | "Game launched" |
| `Warning` | Unusual but handled | "Retry attempt 2" |
| `Error` | Failures with exceptions | "Database connection failed" |
| `Critical` | App cannot continue | "Config file missing" |

### Scoped Logging

```csharp
using (_logger.BeginScope("Processing game {GameId}", gameId))
{
    _logger.LogInformation("Starting import");
    // ... operations
    _logger.LogInformation("Import complete");
}
// All logs in scope include GameId
```

---

## 18. HTTP Client Patterns

### Typed HttpClient

```csharp
public class SteamGridDbService : ISteamGridDbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SteamGridDbService> _logger;

    public SteamGridDbService(HttpClient httpClient, ILogger<SteamGridDbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<CoverArt>> GetCoverArtAsync(string gameTitle, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"search/autocomplete/{gameTitle}", ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<SteamGridDbResponse>(ct);
            return Result<CoverArt>.Success(content.ToCoverArt());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "SteamGridDB request failed for {Title}", gameTitle);
            return Result<CoverArt>.Failure("External service unavailable", ErrorType.ExternalService);
        }
    }
}
```

### Registration with Base Address

```csharp
services.AddHttpClient<ISteamGridDbService, SteamGridDbService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<SteamGridDbOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

---

## 19. Guard Clauses

### Using Ardalis.GuardClauses

```csharp
// Null/empty checks
Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));

// Range checks
Guard.Against.OutOfRange(count, nameof(count), 1, 100);

// Negative
Guard.Against.Negative(amount, nameof(amount));

// Custom validation
if (title.Length > 200)
    throw new ArgumentException("Title too long", nameof(title));
```

### In Entity Factory Methods

```csharp
public static Game Create(string title, Guid? platformId = null)
{
    return new Game
    {
        Id = Guid.NewGuid(),
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)).Trim(),
        PlatformId = platformId,
        CreatedAt = DateTime.UtcNow
    };
}
```

---

## 20. File/Folder Structure Patterns

### Where to Put New Code

```
src/
├── SaveState.Core/                    # Domain Layer
│   ├── {BoundedContext}/
│   │   ├── Entities/                  # Domain entities
│   │   ├── ValueObjects/              # Value objects
│   │   ├── Events/                    # Domain events
│   │   ├── Enums/                     # Domain enums
│   │   ├── Interfaces/                # Repository interfaces
│   │   └── Services/                  # Domain services (interfaces)
│   ├── Common/
│   │   ├── Base/                      # EntityBase, ValueObject
│   │   ├── Events/                    # EventBase, IEvent
│   │   ├── Interfaces/                # IEntity, IAggregateRoot
│   │   └── ValueObjects/              # Shared value objects
│   └── Configuration/                 # Options classes
│
├── SaveState.Application/             # Application Layer
│   ├── {BoundedContext}/
│   │   ├── Commands/                  # CQRS Commands + Handlers
│   │   ├── Queries/                   # CQRS Queries + Handlers
│   │   ├── EventHandlers/             # Domain event handlers
│   │   └── Dtos/                      # Data transfer objects
│   └── Common/                        # Shared application services
│
├── SaveState.Infrastructure/          # Infrastructure Layer
│   ├── Persistence/                   # EF Core, DbContext
│   ├── Repositories/                  # Repository implementations
│   ├── ExternalServices/              # API clients
│   ├── Ai/                            # AI provider implementations
│   └── DependencyInjection.cs         # Service registration
│
└── SaveState.Presentation/            # UI Layer (Avalonia)
    ├── ViewModels/                    # MVVM ViewModels
    ├── Views/                         # XAML Views
    └── Services/                      # UI-specific services
```

### Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Verb}{Noun}Command.cs` | `CreateGameCommand.cs` |
| Query | `Get{Noun}Query.cs` | `GetGameByIdQuery.cs` |
| Handler | `{Command}Handler.cs` | `CreateGameCommandHandler.cs` |
| Event | `{Noun}{Verb}Event.cs` | `GameImportedEvent.cs` |
| Repository | `I{Entity}Repository.cs` | `IGameRepository.cs` |
| Options | `{Feature}Options.cs` | `OpenAiOptions.cs` |
| ViewModel | `{View}ViewModel.cs` | `GameLibraryViewModel.cs` |

---

## 21. Complete New Feature Checklist

When adding a new feature, create these files:

### 1. Domain Layer (Core)

```
☐ Entity: src/SaveState.Core/{Context}/Entities/{Name}.cs
☐ Value Objects: src/SaveState.Core/{Context}/ValueObjects/{Name}.cs
☐ Domain Event: src/SaveState.Core/{Context}/Events/{Name}Event.cs
☐ Repository Interface: src/SaveState.Core/{Context}/Interfaces/I{Name}Repository.cs
```

### 2. Application Layer

```
☐ Command: src/SaveState.Application/{Context}/Commands/{Action}{Name}Command.cs
☐ Query: src/SaveState.Application/{Context}/Queries/Get{Name}Query.cs
☐ DTO: src/SaveState.Application/{Context}/Dtos/{Name}Dto.cs
☐ Event Handler: src/SaveState.Application/{Context}/EventHandlers/{Name}EventHandler.cs
```

### 3. Infrastructure Layer

```
☐ Repository: src/SaveState.Infrastructure/Repositories/{Name}Repository.cs
☐ EF Config: src/SaveState.Infrastructure/Persistence/Configurations/{Name}Configuration.cs
☐ DI Registration: Update DependencyInjection.cs
```

### 4. Presentation Layer

```
☐ ViewModel: src/SaveState.Presentation/ViewModels/{Name}ViewModel.cs
☐ View: src/SaveState.Presentation/Views/{Name}View.axaml
```

### 5. Tests

```
☐ Unit Tests: tests/SaveState.Application.Tests/{Context}/{Command}HandlerTests.cs
☐ Integration Tests: tests/SaveState.Infrastructure.Tests/{Context}/{Repository}Tests.cs
```

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────────┐
│                    SAVESTATE PATTERNS                       │
├─────────────────────────────────────────────────────────────┤
│ New Command:    record XxxCommand(...) : IRequest<Result<T>>│
│ New Query:      record GetXxxQuery(...) : IRequest<Result<T>>│
│ New Handler:    class XxxHandler : IRequestHandler<Xxx, T>  │
│ New Entity:     class Xxx : EntityBase { static Create() }  │
│ New Event:      class XxxEvent : EventBase { ... }          │
│ New Options:    class XxxOptions { const string Section }   │
│ Return Success: Result<T>.Success(value)                    │
│ Return Failure: Result<T>.Failure("msg", ErrorType.X)       │
│ Check Result:   if (result.IsFailure) return Failure(...)   │
│ Value Object:   class Xxx : ValueObject { ... }             │
│ Plugin:         class Xxx : IPlugin { ... }                 │
│ Async:          await x.ConfigureAwait(false)               │
│ CancellationToken: ALWAYS pass it                           │
│ Logging:        _logger.LogX("{Named}", value)              │
│ Guard:          Guard.Against.NullOrWhiteSpace(x, nameof)   │
└─────────────────────────────────────────────────────────────┘
```

---

**This cookbook uses real code from SaveState Reborn. When in doubt, copy exactly from here.**
