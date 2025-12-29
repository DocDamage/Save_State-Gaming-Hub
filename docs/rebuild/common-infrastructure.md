# Common Infrastructure & Conventions

This document defines shared infrastructure, conventions, and patterns used across all phases.

---

[← Back to README](./README.md)

---

## **📋 Exception Definitions**

All custom exceptions should be defined early and used consistently.

📁 Create: `src/SaveState.Core/Common/Exceptions/DomainExceptions.cs`

```csharp
namespace SaveState.Core.Common.Exceptions;

/// <summary>Base exception for all domain-level errors.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when an entity is not found.</summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found")
    {
        EntityName = entityName;
        EntityId = id;
    }
}

/// <summary>Thrown when validation fails.</summary>
public class ValidationException : DomainException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base($"Validation failed: {string.Join(", ", errors)}")
    {
        Errors = errors.ToList();
    }

    public ValidationException(string error) : this(new[] { error }) { }
}

/// <summary>Thrown when AI services are unavailable.</summary>
public class AiUnavailableException : DomainException
{
    public string? ProviderName { get; }

    public AiUnavailableException(string message, string? providerName = null)
        : base(message)
    {
        ProviderName = providerName;
    }
}

/// <summary>Thrown when memory capacity is exceeded.</summary>
public class MemoryCapacityExceededException : DomainException
{
    public int CurrentCount { get; }
    public int MaxCount { get; }

    public MemoryCapacityExceededException(string message, int current = 0, int max = 0)
        : base(message)
    {
        CurrentCount = current;
        MaxCount = max;
    }
}

/// <summary>Thrown when external API calls fail.</summary>
public class ExternalApiException : DomainException
{
    public string ApiName { get; }
    public int? StatusCode { get; }

    public ExternalApiException(string apiName, string message, int? statusCode = null)
        : base($"{apiName} API error: {message}")
    {
        ApiName = apiName;
        StatusCode = statusCode;
    }
}

/// <summary>Thrown when BIOS files are required but missing.</summary>
public class BiosRequiredException : DomainException
{
    public string EmulatorName { get; }
    public IReadOnlyList<string> MissingFiles { get; }

    public BiosRequiredException(string emulatorName, IEnumerable<string> missingFiles)
        : base($"BIOS files required for {emulatorName}: {string.Join(", ", missingFiles)}")
    {
        EmulatorName = emulatorName;
        MissingFiles = missingFiles.ToList();
    }
}
```

---

## **📋 Common DTOs**

📁 Create: `src/SaveState.Application/Common/DTOs/CommonDtos.cs`

```csharp
namespace SaveState.Application.Common.DTOs;

/// <summary>Standard result wrapper for all operations.</summary>
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> Failure(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors.ToList() };
}

/// <summary>Paged result for list queries.</summary>
public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>Progress reporting for long-running operations.</summary>
public record ProgressReport
{
    public string Stage { get; init; } = string.Empty;
    public int Current { get; init; }
    public int Total { get; init; }
    public string Message { get; init; } = string.Empty;
    public double PercentComplete => Total > 0 ? (double)Current / Total * 100 : 0;
}
```

---

## **📋 Logging Conventions**

All logging should follow these patterns for consistency and searchability.

### **Structured Logging Standards**

```csharp
// ✅ DO: Use structured logging with named properties
_logger.LogInformation("Importing game {GameTitle} from {Provider}", game.Title, provider.Name);

// ❌ DON'T: Use string interpolation
_logger.LogInformation($"Importing game {game.Title} from {provider.Name}");

// ✅ DO: Include correlation IDs for distributed tracing
_logger.LogDebug("Processing request {RequestId} with {TokenCount} tokens", request.Id, tokens);

// ✅ DO: Log timing for performance monitoring
var sw = Stopwatch.StartNew();
await DoWorkAsync();
_logger.LogInformation("Operation {OperationName} completed in {ElapsedMs}ms", "ImportGames", sw.ElapsedMilliseconds);

// ✅ DO: Log counts and metrics
_logger.LogInformation("Import completed: {GamesImported} imported, {GamesFailed} failed",
    result.Imported, result.Failed);

// ✅ DO: Log errors with exception details
_logger.LogError(ex, "Failed to import game {GameTitle} from {Provider}", game.Title, provider.Name);
```

### **Log Levels Guide**

| Level | Use For | Example |
|:---|:---|:---|
| `Trace` | Detailed debugging | `Entering method {Method} with {ParamCount} parameters` |
| `Debug` | Diagnostic info | `Cache hit for key {CacheKey}` |
| `Information` | Normal operations | `User {UserId} logged in` |
| `Warning` | Recoverable issues | `Retry {Attempt} for {Provider} after {Delay}ms` |
| `Error` | Failures | `Failed to import game {Title}` |
| `Critical` | System failures | `Database connection failed` |

---

## **📋 Configuration Schema**

### **Master Configuration Structure**

📁 Create: `src/SaveState.App/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SaveState.db"
  },
  "Database": {
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "CommandTimeout": 30
  },
  "Resilience": {
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationMs": 60000,
    "MaxRetries": 3,
    "InitialRetryDelayMs": 1000,
    "RetryBackoffMultiplier": 2.0,
    "DefaultTimeoutMs": 30000
  },
  "Ai": {
    "DefaultProvider": "OpenAI",
    "DefaultModel": "gpt-4",
    "DefaultMaxTokens": 1000,
    "DefaultTemperature": 0.7,
    "CacheTtlMinutes": 60,
    "EnableFallback": true
  },
  "OpenAi": {
    "BaseUrl": "https://api.openai.com/v1/",
    "DefaultModel": "gpt-4"
  },
  "Groq": {
    "BaseUrl": "https://api.groq.com/openai/v1/",
    "DefaultModel": "mixtral-8x7b-32768"
  },
  "Memory": {
    "MaxEntries": 500,
    "MaxTokens": 50000,
    "PruneBatchSize": 50,
    "MaxTotalMemoryBytes": 104857600
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": { "path": "logs/savestate-.log", "rollingInterval": "Day" }
      }
    ]
  }
}
```

### **User Secrets (for API keys)**

```bash
# Initialize user secrets
cd src/SaveState.App
dotnet user-secrets init

# Store API keys securely (NEVER in appsettings.json!)
dotnet user-secrets set "OpenAi:ApiKey" "sk-your-api-key"
dotnet user-secrets set "Groq:ApiKey" "gsk_your-api-key"
dotnet user-secrets set "Steam:ApiKey" "your-steam-api-key"
dotnet user-secrets set "Igdb:ClientId" "your-igdb-client-id"
dotnet user-secrets set "Igdb:ClientSecret" "your-igdb-secret"
```

---

## **📋 DI Registration Patterns**

### **Infrastructure DI Extensions**

📁 Create: `src/SaveState.Infrastructure/DependencyInjection.cs`

```csharp
namespace SaveState.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<SaveStateDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ISaveStateDbContext>(sp => sp.GetRequiredService<SaveStateDbContext>());
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        // Repositories
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlatformRepository, PlatformRepository>();
        services.AddScoped<IRomFileRepository, RomFileRepository>();
        services.AddScoped<IEmulatorRepository, EmulatorRepository>();

        // Game Providers
        services.AddScoped<IGameProvider, SteamProvider>();
        services.AddScoped<IGameProvider, GogProvider>();
        services.AddScoped<IGameProvider, EpicProvider>();

        // ROM Management
        services.AddScoped<IRomScannerService, RomScannerService>();
        services.AddScoped<IEmulatorService, EmulatorService>();

        // AI Services
        services.AddSingleton<AiResiliencePolicy>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        services.AddScoped<IShortTermMemory, EnhancedShortTermMemory>();

        // AI Providers (with HttpClient)
        services.AddHttpClient<ILlmProvider, OpenAiProvider>(client =>
        {
            var options = configuration.GetSection("OpenAi").Get<OpenAiOptions>()!;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddHttpClient<ILlmProvider, GroqProvider>(client =>
        {
            var options = configuration.GetSection("Groq").Get<GroqOptions>()!;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        // Configuration
        services.Configure<ResilienceConfig>(configuration.GetSection("Resilience"));
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));
        services.Configure<GroqOptions>(configuration.GetSection("Groq"));
        services.Configure<MemoryConfig>(configuration.GetSection("Memory"));

        return services;
    }
}
```

### **Application DI Extensions**

📁 Create: `src/SaveState.Application/DependencyInjection.cs`

```csharp
namespace SaveState.Application;

using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Application services
        services.AddScoped<IGameImportService, GameImportService>();
        services.AddScoped<IMetadataService, IgdbMetadataService>();

        return services;
    }
}
```

---

## **📋 Migration Commands**

### **Creating Migrations**

```bash
# Navigate to Infrastructure project
cd src/SaveState.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../SaveState.App

# Create subsequent migrations
dotnet ef migrations add AddGameTags --startup-project ../SaveState.App
dotnet ef migrations add AddAiModels --startup-project ../SaveState.App

# Apply migrations
dotnet ef database update --startup-project ../SaveState.App
```

### **Rollback Migrations**

```bash
# Rollback to specific migration
dotnet ef database update AddGameTags --startup-project ../SaveState.App

# Remove last migration (if not applied)
dotnet ef migrations remove --startup-project ../SaveState.App

# Reset database completely
rm SaveState.db
dotnet ef database update --startup-project ../SaveState.App
```

### **Migration Script for Production**

```bash
# Generate SQL script for production deployment
dotnet ef migrations script --startup-project ../SaveState.App -o migrations.sql
```

---

## **📋 Performance Expectations**

### **Target Metrics**

| Operation | Target | Critical Threshold |
|:---|:---|:---|
| Application Startup | < 200ms | < 500ms |
| Database Query (single) | < 10ms | < 50ms |
| Database Query (list 100) | < 50ms | < 200ms |
| AI Chat Request | < 5000ms | < 10000ms |
| AI Completion Request | < 3000ms | < 8000ms |
| Memory Search (500 entries) | < 10ms | < 50ms |
| ROM Scan (1000 files) | < 5000ms | < 10000ms |
| Game Import (single) | < 200ms | < 500ms |
| UI Response (button click) | < 50ms | < 100ms |

### **Benchmark Template**

📁 Create: `tools/SaveState.Benchmarks/BenchmarkTemplate.cs`

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
[SimpleJob]
public class OperationBenchmarks
{
    private IServiceProvider _services = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // Configure services...
        _services = services.BuildServiceProvider();
    }

    [Benchmark(Baseline = true)]
    public async Task<IReadOnlyList<Game>> GetAllGames()
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        return await repo.GetAllAsync(default);
    }

    [Benchmark]
    public async Task<Game?> GetGameById()
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        return await repo.GetByIdAsync(_testGameId, default);
    }
}
```

---

## **📋 Security Considerations**

### **API Key Management**

```markdown
⚠️ NEVER store API keys in:
- appsettings.json
- Source code
- Git repository

✅ DO store API keys in:
- User secrets (development)
- Environment variables (production)
- Azure Key Vault / AWS Secrets Manager (cloud)
```

### **Input Validation**

```csharp
// ✅ Always validate file paths
public void ValidateFilePath(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        throw new ValidationException("Path cannot be empty");

    // Prevent path traversal attacks
    var fullPath = Path.GetFullPath(path);
    if (!fullPath.StartsWith(_allowedBasePath, StringComparison.OrdinalIgnoreCase))
        throw new ValidationException("Invalid path: access denied");

    // Check for invalid characters
    if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        throw new ValidationException("Path contains invalid characters");
}

// ✅ Sanitize user input before database
public GameTitle CreateGameTitle(string input)
{
    // Remove control characters
    var sanitized = new string(input.Where(c => !char.IsControl(c)).ToArray());

    // Trim and limit length
    sanitized = sanitized.Trim();
    if (sanitized.Length > 200)
        sanitized = sanitized[..200];

    return new GameTitle(sanitized);
}
```

### **Rate Limiting**

```csharp
// ✅ Rate limit AI requests to prevent cost overruns
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai-requests", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 10;
    });
});
```

---

## **📋 Rollback Procedures**

### **Per-Phase Rollback**

```bash
# Create checkpoint before starting phase
git tag pre-phase-X-$(date +%Y%m%d)

# If phase fails, rollback
git reset --hard pre-phase-X-20251228
git clean -fd

# Or rollback to last successful phase
git reset --hard rebuild-phase1-complete
```

### **Per-Task Rollback**

```bash
# If a single task breaks the build:

# 1. Identify the files changed
git diff --name-only HEAD~1

# 2. Revert specific files
git checkout HEAD~1 -- src/SaveState.Infrastructure/Ai/

# 3. Or revert the entire commit
git revert HEAD --no-commit
git commit -m "Revert: T-3.1.2 LLM Provider (build failure)"

# 4. Document what failed
echo "T-3.1.2: HttpClient registration failed" >> docs/rebuild/failures.md
```

### **Database Rollback**

```bash
# Rollback to previous migration
dotnet ef database update PreviousMigrationName --startup-project ../SaveState.App

# Full database reset
rm SaveState.db
dotnet ef database update --startup-project ../SaveState.App
```

---

## **📋 Test Patterns**

### **Unit Test Template**

```csharp
namespace SaveState.Core.Tests.GameLibrary;

using FluentAssertions;
using Moq;
using Xunit;

public class GameProviderTests
{
    private readonly Mock<ISteamApiClient> _mockClient;
    private readonly Mock<ILogger<SteamProvider>> _mockLogger;
    private readonly SteamProvider _sut;

    public GameProviderTests()
    {
        _mockClient = new Mock<ISteamApiClient>();
        _mockLogger = new Mock<ILogger<SteamProvider>>();
        _sut = new SteamProvider(_mockClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetInstalledGamesAsync_ReturnsGames_WhenApiSucceeds()
    {
        // Arrange
        var expectedGames = new List<SteamGame>
        {
            new() { AppId = 220, Name = "Half-Life 2" },
            new() { AppId = 400, Name = "Portal" }
        };
        _mockClient.Setup(x => x.GetOwnedGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGames);

        // Act
        var result = await _sut.GetInstalledGamesAsync(default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "Half-Life 2");
        result.Should().Contain(g => g.Title == "Portal");
    }

    [Fact]
    public async Task GetInstalledGamesAsync_ReturnsEmpty_WhenApiFails()
    {
        // Arrange
        _mockClient.Setup(x => x.GetOwnedGamesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SteamApiException("API unavailable"));

        // Act
        var result = await _sut.GetInstalledGamesAsync(default);

        // Assert
        result.Should().BeEmpty();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetGameMetadataAsync_ThrowsValidation_WhenGameIdInvalid(string? gameId)
    {
        // Act
        var act = () => _sut.GetGameMetadataAsync(gameId!, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
```

### **Integration Test Template**

```csharp
namespace SaveState.IntegrationTests.Repositories;

using Microsoft.EntityFrameworkCore;
using Xunit;

public class GameRepositoryTests : IAsyncLifetime
{
    private SaveStateDbContext _context = null!;
    private GameRepository _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SaveStateDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _sut = new GameRepository(_context);

        // Seed test data
        var platform = new Platform("PC", "PC", PlatformType.PC);
        _context.Platforms.Add(platform);

        var games = Enumerable.Range(1, 10)
            .Select(i => Game.Create($"Test Game {i}", platform));
        _context.Games.AddRange(games);

        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllGames()
    {
        var result = await _sut.GetAllAsync(default);
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsGame_WhenExists()
    {
        var existingGame = await _context.Games.FirstAsync();

        var result = await _sut.GetByIdAsync(existingGame.Id, default);

        result.Should().NotBeNull();
        result!.Title.Should().Be(existingGame.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);
        result.Should().BeNull();
    }
}
```

---

**This document should be referenced by all phase documents for common patterns and infrastructure.**
