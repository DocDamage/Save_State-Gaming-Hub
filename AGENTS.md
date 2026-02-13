# SaveStateReborn - Agent Guidelines

**Project:** SaveState Reborn  
**Description:** A comprehensive gaming management platform with AI-powered features, cross-platform cloud gaming, voice controls, and extensive plugin system.  
**Version:** 2.4.0  
**Last Updated:** February 13, 2026  

---

## 🎯 Quick Reference

| Category | Guideline |
|----------|-----------|
| **Language** | C# 13 (.NET 9) |
| **UI Framework** | Avalonia UI 11.2.6 |
| **Architecture** | Clean Architecture + CQRS + MediatR |
| **DI Container** | Microsoft.Extensions.DependencyInjection + Splat |
| **Testing** | xUnit + Moq + FluentAssertions + Bogus |
| **Database** | SQLite with Entity Framework Core 9 |
| **Build Status** | ✅ 0 errors, 0 warnings |
| **Test Status** | ✅ 600+ tests passing (100% pass rate) |

---

## 📋 Project Overview

SaveStateReborn is an enterprise-grade gaming management platform built with .NET 9 following Clean Architecture principles. The application provides:

- **Universal Game Library Management**: Steam, Epic, GOG, Origin, UPlay, and custom games
- **AI-Powered Gaming Intelligence**: Voice commands, smart recommendations, strategy assistance
- **Advanced Save State Management**: Tree-based branching, intelligent auto-save, timeline visualization
- **Cloud Gaming Integration**: Unified interface for GeForce Now, Xbox Cloud, Amazon Luna
- **MUGEN/IKEMEN Fighting Game Platform**: Complete fighting game engine with character management
- **Plugin System**: 60+ plugins for extensibility (themes, cloud sync, analytics, etc.)
- **Big Picture Mode**: 10-foot UI for living room gaming with controller support

### Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Core Framework** | .NET 9, C# 13, Native AOT ready |
| **UI Framework** | Avalonia UI 11.2.6, ReactiveUI, Fluent Theme |
| **Architecture** | Clean Architecture, CQRS with MediatR 14.0 |
| **Database** | EF Core 9.0.2, SQLite, In-Memory (tests) |
| **AI/ML** | OpenAI GPT, Whisper for voice, Semantic Caching |
| **Resilience** | Polly 8.6.5 for retry/circuit breaker |
| **Logging** | Serilog with structured logging |
| **Validation** | FluentValidation 11.11.0 |
| **CLI** | Spectre.Console, System.CommandLine |
| **Testing** | xUnit 2.9.2, Moq 4.20.72, FluentAssertions 6.12.1, Bogus 35.5.1 |
| **Containerization** | Docker with multi-environment support |

---

## 🏗️ Architecture

### Clean Architecture Layers

```
src/
├── SaveState.Core/                 # Domain layer - entities, value objects, interfaces
│   ├── Common/                     # Shared primitives (Result, ValueObject, EntityBase)
│   ├── GameLibrary/                # Game management bounded context
│   ├── Mugen/                      # MUGEN/IKEMEN fighting game context
│   ├── SaveStates/                 # Save state branching context
│   ├── Sync/                       # Cloud sync and network quality context
│   ├── Social/                     # Reviews, collections, friends
│   ├── Plugins/                    # Plugin system interfaces
│   └── ...                         # 22 bounded contexts total
├── SaveState.Application/          # Application layer - CQRS handlers
│   ├── GameLibrary/Commands/       # Write operations (MediatR)
│   ├── GameLibrary/Queries/        # Read operations (MediatR)
│   └── ...                         # Cross-cutting concerns
├── SaveState.Infrastructure/       # Infrastructure layer - implementations
│   ├── Persistence/                # EF Core, repositories
│   ├── External/                   # API clients (IGDB, SteamGridDB)
│   └── ...                         # Service implementations
├── SaveState.Presentation/         # UI layer - Avalonia MVVM
│   ├── Views/                      # XAML views
│   ├── ViewModels/                 # MVVM view models
│   └── Services/                   # UI services
├── SaveState.CLI/                  # Command-line interface
│   └── Commands/                   # Spectre.Console commands
└── SaveState.Plugins.*/            # 60+ plugin projects
```

### Dependency Direction

```
Presentation → Application → Core ← Infrastructure
     ↑              ↑         ↑
     └──────────────┴─────────┘
              Plugins
```

**Rules:**
- Core has NO external dependencies
- Application depends only on Core
- Infrastructure implements Core interfaces
- Presentation depends on Application
- No circular dependencies allowed

---

## 🧩 Key Patterns

### 1. Result Pattern (MANDATORY)

**Always use Result<T> for operations that can fail:**

```csharp
// ✅ CORRECT - Return Result<T>
public async Task<Result<Game>> GetGameAsync(int id)
{
    var game = await _repository.GetByIdAsync(id);
    if (game is null)
        return Result<Game>.Failure($"Game {id} not found", ErrorType.NotFound);
    
    return Result<Game>.Success(game);
}

// ✅ CORRECT - Check result before using Value
var result = await _gameService.GetGameAsync(id);
if (result.IsFailure)
{
    _logger.LogWarning("Failed: {Error}", result.Error);
    return Result<GameDto>.Failure(result.Error!, result.ErrorType);
}
var game = result.Value; // Safe to use after check

// ❌ WRONG - Never return null for failure
return null;  // DON'T DO THIS

// ❌ WRONG - Never use result.Value without checking IsFailure
var game = result.Value;  // May be null!
```

### 2. CQRS with MediatR

**Commands (Write operations):**

```csharp
// Command definition
public sealed record CreateGameCommand(string Title, string? CoverImagePath) 
    : IRequest<int>;

// Handler implementation
public sealed class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, int>
{
    private readonly IGameRepository _repository;
    
    public CreateGameCommandHandler(IGameRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<int> Handle(CreateGameCommand request, CancellationToken ct)
    {
        var game = Game.Create(request.Title, null, null, request.CoverImagePath);
        await _repository.AddAsync(game, ct).ConfigureAwait(false);
        return game.Id;
    }
}
```

**Queries (Read operations):**

```csharp
// Query definition
public sealed record GetGameQuery(int Id) : IRequest<Result<GameDto>>;

// Handler with Result pattern
public sealed class GetGameQueryHandler : IRequestHandler<GetGameQuery, Result<GameDto>>
{
    private readonly IGameRepository _repository;
    
    public GetGameQueryHandler(IGameRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Result<GameDto>> Handle(GetGameQuery request, CancellationToken ct)
    {
        var game = await _repository.GetByIdAsync(request.Id, ct);
        if (game is null)
            return Result<GameDto>.Failure($"Game {request.Id} not found", ErrorType.NotFound);
        
        return Result<GameDto>.Success(new GameDto(game));
    }
}
```

### 3. ITimeProvider Pattern (CRITICAL)

**Always use `ITimeProvider` instead of `DateTime.Now`:**

```csharp
// ✅ CORRECT - Use injected ITimeProvider
public class MyService
{
    private readonly ITimeProvider _timeProvider;
    
    public MyService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void DoWork()
    {
        var now = _timeProvider.Now;
        var utcNow = _timeProvider.UtcNow;
        var today = _timeProvider.Today;
    }
}

// ❌ WRONG - Never use DateTime.Now directly
public class MyService
{
    public void DoWork()
    {
        var now = DateTime.Now; // DON'T DO THIS
    }
}
```

**For Plugins:**

```csharp
public class MyPlugin : IPlugin
{
    private ITimeProvider _timeProvider = null!;
    
    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();
    }
}
```

**For Tests:**

```csharp
// Use SystemTimeProvider for real time
var service = new MyService(new SystemTimeProvider());

// Or mock for deterministic testing
var timeProviderMock = new Mock<ITimeProvider>();
timeProviderMock.Setup(tp => tp.Now).Returns(new DateTime(2026, 2, 12));
var service = new MyService(timeProviderMock.Object);
```

### 4. Value Objects

```csharp
// ✅ CORRECT - Use record for value objects
public record GameId(Guid Value)
{
    public static GameId New() => new(Guid.NewGuid());
}

public record PlatformName
{
    public string Value { get; }
    
    private PlatformName(string value) => Value = value;
    
    public static Result<PlatformName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PlatformName>.Failure("Platform name cannot be empty");
        
        return Result<PlatformName>.Success(new PlatformName(value.Trim()));
    }
}
```

---

## 🔧 Build & Run Commands

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- Git
- Windows 10/11, Linux, or macOS

### Build Commands

```bash
# Build entire solution
dotnet build SaveStateReborn.sln

# Build specific project
dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj

# Build with Release configuration
dotnet build -c Release
```

### Run Commands

```bash
# Run the desktop application
dotnet run --project src/SaveState.Presentation

# Run the CLI
dotnet run --project src/SaveState.CLI -- [command]

# Example CLI commands
dotnet run --project src/SaveState.CLI -- list
dotnet run --project src/SaveState.CLI -- search "zelda"
```

### Docker Commands

```bash
# Development environment with hot reload
docker-compose -f docker-compose.dev.yml up --build

# Production deployment
docker-compose -f docker-compose.prod.yml --profile nginx up --build -d

# Build Docker image
docker build -t savestate-reborn .
```

### Database Migrations

```bash
# Update database
cd src/SaveState.Infrastructure
dotnet ef database update --startup-project ../SaveState.Presentation

# Add new migration
dotnet ef migrations add [MigrationName] --startup-project ../SaveState.Presentation
```

---

## 🧪 Testing Guidelines

### Test Structure

```csharp
public class MyServiceTests
{
    private readonly Mock<IDependency> _dependencyMock;
    private readonly MyService _service;
    
    public MyServiceTests()
    {
        _dependencyMock = new Mock<IDependency>();
        _service = new MyService(_dependencyMock.Object, new SystemTimeProvider());
    }
    
    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        _dependencyMock.Setup(d => d.GetAsync())
            .ReturnsAsync(Result.Success(data));
        
        // Act
        var result = await _service.DoWorkAsync();
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SaveState.Core.Tests
dotnet test tests/SaveState.Application.Tests

# Run with verbosity
dotnet test --verbosity normal

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~MyServiceTests"
```

### Test Projects

| Project | Purpose | Location |
|---------|---------|----------|
| Core Tests | Domain entities, value objects | `tests/SaveState.Core.Tests/` |
| Application Tests | Command/query handlers | `tests/SaveState.Application.Tests/` |
| Infrastructure Tests | Repositories, external APIs | `tests/SaveState.Infrastructure.Tests/` |
| Integration Tests | Database integration | `tests/SaveState.IntegrationTests/` |
| E2E Tests | Full application flows | `tests/SaveState.EndToEndTests/` |
| Cross-Platform Tests | OS compatibility | `tests/SaveState.CrossPlatform.Tests/` |
| Accessibility Tests | WCAG compliance | `tests/SaveState.Accessibility.Tests/` |
| Load Tests | Performance under load | `tests/SaveState.LoadTests/` |

### Test Conventions

- Use `Bogus` for fake data generation
- Use `FluentAssertions` for readable assertions
- Use `Moq` for mocking dependencies
- Always inject `ITimeProvider` with `SystemTimeProvider` in tests
- Use `IAsyncLifetime` for async test setup/teardown

---

## 📝 Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `GameDetailViewModel` |
| Interfaces | PascalCase + I prefix | `IGameRepository` |
| Methods | PascalCase | `GetByIdAsync` |
| Private fields | _camelCase with underscore | `_timeProvider` |
| Constants | PascalCase | `MaxRetries` |
| Records | PascalCase | `CreateGameCommand` |
| Enums | PascalCase | `GameStatus` |

### Async/Await Best Practices

```csharp
// ✅ Always use async/await
var result = await _service.GetAsync();

// ✅ Name async methods with Async suffix
public async Task<Result<Data>> GetDataAsync() { }

// ✅ Use ConfigureAwait(false) in library code
await _repository.SaveAsync(entity).ConfigureAwait(false);

// ❌ Don't block on async code
var result = _service.GetAsync().Result; // Deadlock risk!

// ❌ Don't use async void (except for event handlers)
async void OnClick() { } // DON'T DO THIS

// ❌ Don't use .Wait()
_service.GetAsync().Wait(); // DON'T DO THIS
```

### Null Safety

```csharp
// ✅ Use null-forgiving operator only when absolutely necessary
var value = result.Value!; // Avoid this

// ✅ Prefer explicit null checks
if (result.Value is not null)
{
    var value = result.Value;
}

// ✅ Use null-coalescing for defaults
var name = game.Title ?? "Unknown";

// ✅ Use pattern matching
if (entity is Game game) { }
```

### File Organization

```csharp
// 1. Usings (global usings in GlobalUsings.cs)
using System;
using MediatR;

// 2. Namespace
namespace SaveState.Application.GameLibrary.Commands;

// 3. Using static for common imports (optional)
using static SaveState.Core.Common.Result;

// 4. Type declaration
public sealed record CreateGameCommand(string Title) : IRequest<int>;

// 5. Handler in same file or separate file
public sealed class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, int>
{
    // Implementation
}
```

---

## 🔌 Plugin Development

### Plugin Interface

```csharp
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}

public interface IPluginContext
{
    IServiceProvider Services { get; }
    IConfiguration Configuration { get; }
    ILoggerFactory LoggerFactory { get; }
}
```

### Example Plugin

```csharp
public class MyPlugin : IPlugin
{
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string Description => "My awesome plugin";
    
    private ITimeProvider _timeProvider = null!;
    private ILogger<MyPlugin> _logger = null!;
    
    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();
        _logger = context.LoggerFactory.CreateLogger<MyPlugin>();
        
        _logger.LogInformation("Plugin initialized at {Time}", _timeProvider.Now);
        return Task.CompletedTask;
    }
    
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Plugin shutting down");
        return Task.CompletedTask;
    }
}
```

---

## 🐳 Deployment

### Docker Configuration

The project includes multi-environment Docker support:

- `Dockerfile` - Production build
- `Dockerfile.dev` - Development with hot reload
- `docker-compose.yml` - Base configuration
- `docker-compose.dev.yml` - Development overrides
- `docker-compose.prod.yml` - Production configuration
- `docker-compose.ci.yml` - CI/CD configuration

### Health Checks

The application includes built-in health checks:

```bash
# Check application health
curl http://localhost:8080/health

# Check specific services
curl http://localhost:8080/health/database
curl http://localhost:8080/health/external-apis
```

---

## 🚨 Security Considerations

### Input Validation

```csharp
// ✅ Always validate inputs
public static Result<GameTitle> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result<GameTitle>.Failure("Title cannot be empty");
    
    if (value.Length > 200)
        return Result<GameTitle>.Failure("Title too long (max 200 chars)");
    
    return Result<GameTitle>.Success(new GameTitle(value.Trim()));
}

// ✅ Use GuardClauses for preconditions
public void ProcessGame(GameId id)
{
    Guard.Against.Null(id, nameof(id));
    // Process...
}
```

### Secrets Management

- Never commit secrets to source control
- Use `appsettings.Production.json` for production settings
- Use environment variables for sensitive configuration
- Use `Microsoft.Extensions.Configuration.UserSecrets` for development

### API Keys

```csharp
// ✅ Use Options pattern
public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

// ✅ Register in DI
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));
```

---

## ✅ Pre-Commit Checklist

Before committing code, ensure:

- [ ] Code builds with 0 errors, 0 warnings
- [ ] All unit tests passing (`dotnet test`)
- [ ] `ITimeProvider` used instead of `DateTime.Now`
- [ ] Result pattern used for failure-prone operations
- [ ] Proper null checks (no unnecessary `!` operators)
- [ ] Async methods named with `Async` suffix
- [ ] No `.Result` or `.Wait()` blocking calls
- [ ] No `async void` (except event handlers)
- [ ] XML documentation for public APIs
- [ ] No secrets or API keys in code

---

## 📚 Key Documentation

| Document | Purpose |
|----------|---------|
| `README.md` | Project overview, quick start |
| `docs/guides/AI_QUICK_START.md` | 30-second briefing for AI assistants |
| `docs/architecture/PATTERNS_COOKBOOK.md` | Copy-paste code patterns |
| `docs/architecture/DECISIONS_LOG.md` | Architecture decision records |
| `docs/architecture/ENGINEERING_RULES.md` | Engineering principles and rules |
| `docs/guides/PLUGIN_SDK.md` | Plugin development guide |
| `TECHNICAL_DEBT_AUDIT_2026-02-01.md` | Current technical debt status |

---

## 🆘 Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Missing ITimeProvider | Check DI registration in `App.axaml.cs` |
| Plugin build errors | Ensure `Microsoft.Extensions.DependencyInjection` referenced |
| Test failures | Check unique DB file isolation in tests |
| EF Core migrations fail | Verify startup project path |
| Avalonia design-time errors | Check DataContext bindings |

### Getting Help

1. Check `docs/guides/AI_QUICK_START.md` for immediate guidance
2. Review `docs/architecture/PATTERNS_COOKBOOK.md` for code examples
3. See `docs/architecture/ENGINEERING_RULES.md` for coding standards
4. Check `TECHNICAL_DEBT_AUDIT_2026-02-01.md` for known issues

---

*This file is maintained by the development team. Update when patterns change.*
