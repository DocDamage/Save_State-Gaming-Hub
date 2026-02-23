using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Infrastructure;
using SaveState.Application.Common.DependencyInjection;
using System.Diagnostics;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.GameLibrary.Enums;
using SaveState.EndToEndTests.Infrastructure;
using Splat;


namespace SaveState.EndToEndTests;

/// <summary>
/// Test fixture that provides a fully configured service provider
/// for end-to-end integration testing of advanced features.
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly string _dbPath;
    private bool _disposed;

    public IServiceProvider Services => _services;
    public Guid TestGameId { get; private set; }


    public IntegrationTestFixture()
    {
        // Generate a unique database name for this test run to avoid file locking
        _dbPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"savestate_test_{Guid.NewGuid():N}.db");

        // Build configuration with unique database path
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}"
            })
            .Build();

        // Build host with all services
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                // Register all application services (same as production)
                services.AddInfrastructure(configuration);
                services.AddApplicationServices();
                
                // Register all presentation services required for E2E tests
                services.AddPresentationServicesForE2E();

                // Override with test-specific services if needed
                services.AddSingleton<ILoggerFactory, TestLoggerFactory>();
            })
            .Build();
        
        // Configure Splat Locator for Avalonia (required by ViewModels)
        Locator.Current.SetServices(_host.Services);

        _services = _host.Services;

        // Initialize database and seed data
        InitializeTestData().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Cleans up any existing test database files to ensure a fresh schema.
    /// This prevents issues with stale database schemas from previous test runs.
    /// </summary>
    private void CleanupTestDatabase()
    {
        try
        {
            var basePath = Path.GetDirectoryName(_dbPath);
            var fileName = Path.GetFileNameWithoutExtension(_dbPath);
            var extension = Path.GetExtension(_dbPath);

            // Delete main database file
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
                Console.WriteLine($"Deleted existing test database: {_dbPath}");
            }

            // Delete SQLite auxiliary files (shm, wal)
            var shmPath = Path.Combine(basePath ?? ".", $"{fileName}.db-shm");
            var walPath = Path.Combine(basePath ?? ".", $"{fileName}.db-wal");

            if (File.Exists(shmPath)) File.Delete(shmPath);
            if (File.Exists(walPath)) File.Delete(walPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not clean up test database: {ex.Message}");
        }
    }

    private async Task InitializeTestData()
    {
        try
        {
            // Create database if needed
            var dbContext = _services.GetRequiredService<SaveState.Infrastructure.Persistence.SaveStateDbContext>();

            // Ensure fresh database schema - delete and recreate
            await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

            Console.WriteLine("Test database created successfully with fresh schema.");

            // Seed basic test data
            await SeedTestData(dbContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: Failed to initialize test data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task SeedTestData(SaveState.Infrastructure.Persistence.SaveStateDbContext dbContext)
    {
        // Seed minimal test data for integration tests
        var pcName = PlatformName.From("PC");
        var platform = await dbContext.Platforms.FirstOrDefaultAsync(p => p.Name == pcName).ConfigureAwait(false);
        if (platform == null)
        {
            platform = new Platform(
                PlatformName.From("PC"),
                PlatformShortName.From("PC"),
                PlatformType.Computer);

            await dbContext.Platforms.AddAsync(platform).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Title == "Test Game").ConfigureAwait(false);
        if (game == null)
        {
            game = Game.Create("Test Game", platform.Id);
            await dbContext.Games.AddAsync(game).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        TestGameId = game.Id;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _host?.Dispose();

                // Clean up test database files after tests complete
                CleanupTestDatabase();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Test logger factory for capturing test output.
/// </summary>
public class TestLoggerFactory : ILoggerFactory
{
    private readonly List<TestLogger> _loggers = new();

    public void AddProvider(ILoggerProvider provider)
    {
        // Not needed for tests
    }

    public ILogger CreateLogger(string categoryName)
    {
        var logger = new TestLogger(categoryName);
        _loggers.Add(logger);
        return logger;
    }

    public void Dispose()
    {
        foreach (var logger in _loggers)
        {
            logger.Dispose();
        }
        _loggers.Clear();
    }
}

/// <summary>
/// Test logger that captures log output for verification.
/// </summary>
public class TestLogger : ILogger, IDisposable
{
    private readonly string _categoryName;
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    public TestLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => this;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var entry = new LogEntry
        {
            Level = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception,
            Timestamp = DateTime.UtcNow
        };

        _entries.Add(entry);
    }

    public void Dispose()
    {
        _entries.Clear();
    }
}

/// <summary>
/// Represents a captured log entry.
/// </summary>
public class LogEntry
{
    public LogLevel Level { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}
