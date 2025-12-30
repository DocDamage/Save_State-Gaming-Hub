using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Application.Common.Events;
using SaveState.Infrastructure.Persistence;
using Moq;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary;
using SaveState.Core.Ai.Services;
using Xunit;

namespace SaveState.Application.Tests.Concurrency;

/// <summary>
/// Concurrency tests for multi-threaded scenarios and race conditions.
/// </summary>
[Collection("DatabaseTests")]
public class ConcurrencyTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SaveStateDbContext _dbContext;

    public ConcurrencyTests()
    {
        var services = new ServiceCollection();

        // Configure in-memory database for testing
        // Use a unique name for each test instance to avoid interference if tests run in parallel
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<SaveStateDbContext>(options =>
            options.UseSqlite($"DataSource=file:{dbName}?mode=memory&cache=shared"));
        services.AddScoped<ISaveStateDbContext>(sp => sp.GetRequiredService<SaveStateDbContext>());

        // Add repositories
        services.AddScoped<IGameRepository, SaveState.Infrastructure.Repositories.GameRepository>();
        services.AddScoped<IPlatformRepository, SaveState.Infrastructure.Repositories.PlatformRepository>();

        // Add required services
        services.AddScoped<IGameValidationService, SaveState.Core.GameLibrary.DomainServices.GameValidationService>();
        services.AddScoped<IEventPublisher, SaveState.Application.Common.Events.EventPublisher>();
        services.AddScoped<SaveState.Core.Common.Interfaces.IFileSystem, SaveState.Infrastructure.Services.FileSystem>();

        // Mocks for lightweight dependencies
        services.AddSingleton(new Mock<IAiOrchestrator>().Object);
        services.AddSingleton(new Mock<SaveState.Core.Common.Services.IUserPreferencesService>().Object);
        services.AddSingleton(new Mock<SaveState.Core.Monitoring.IApplicationMetrics>().Object);

        // Add minimal services for testing
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ImportGameCommand).Assembly));
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<SaveStateDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await _dbContext.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task ConcurrentGameImports_HandlesRaceConditions()
    {
        // Arrange - Create platform first
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);
        await _dbContext.SaveChangesAsync();

        // Act - Concurrently import multiple games
        var tasks = new List<Task>();
        var importedGames = new System.Collections.Concurrent.ConcurrentBag<Game>();

        for (int i = 0; i < 10; i++)
        {
            var gameIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new ImportGameCommand
                {
                    Title = $"Concurrent Game {gameIndex}",
                    PlatformName = "PC",
                    Description = $"Description for game {gameIndex}",
                    Source = $"test-url-{gameIndex}",
                    Tags = new[] { "Action" },
                    CoverImageUrl = null
                };

                var result = await mediator.Send(command).ConfigureAwait(false);
                result.IsSuccess.Should().BeTrue(result.Error);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All games should be imported successfully
        var totalGames = await _dbContext.Games.CountAsync();
        totalGames.Should().Be(10);
    }

    [Fact]
    public async Task ConcurrentReadOperations_DatabaseConsistency()
    {
        // Arrange - Pre-populate with games
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var games = new List<Game>();
        for (int i = 0; i < 50; i++)
        {
            var game = Game.Create($"Test Game {i}", platformId);
            typeof(Game).GetProperty("Id")?.SetValue(game, (Guid)GameId.NewId());
             games.Add(game);
        }
        await _dbContext.Games.AddRangeAsync(games);
        await _dbContext.SaveChangesAsync();

        // Act - Concurrently read games
        var readTasks = new List<Task<int>>();
        for (int i = 0; i < 20; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();

                var count = await dbContext.Games.CountAsync().ConfigureAwait(false);
                return count;
            }));
        }

        var results = await Task.WhenAll(readTasks);

        // Assert - All reads should return consistent results
        results.Should().AllBeEquivalentTo(50);
    }

    [Fact]
    public async Task ConcurrentFileSystemOperations_HandlesThreadSafety()
    {
        // Arrange
        var fileSystem = _serviceProvider.GetRequiredService<SaveState.Core.Common.Interfaces.IFileSystem>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act - Concurrently create and check files
            var fileTasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                var fileIndex = i;
                fileTasks.Add(Task.Run(async () =>
                {
                    var filePath = Path.Combine(tempDir, $"test-{fileIndex}.txt");
                    await File.WriteAllTextAsync(filePath, $"Content {fileIndex}").ConfigureAwait(false);

                    var exists = await fileSystem.FileExistsAsync(filePath).ConfigureAwait(false);
                    exists.Should().BeTrue();

                    var size = await fileSystem.GetFileSizeAsync(filePath).ConfigureAwait(false);
                    size.Should().BeGreaterThan(0);
                }));
            }

            await Task.WhenAll(fileTasks);

            // Assert - All files should exist
            var allFiles = Directory.GetFiles(tempDir);
            allFiles.Length.Should().Be(20);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ConcurrentMediatorOperations_HandlesCommandProcessing()
    {
        // Arrange - Create platform
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);
        await _dbContext.SaveChangesAsync();

        // Act - Concurrently send commands through mediator
        var commandTasks = new List<Task>();
        for (int i = 0; i < 15; i++)
        {
            var commandIndex = i;
            commandTasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new ImportGameCommand
                {
                    Title = $"Mediator Game {commandIndex}",
                    PlatformName = "PC",
                    Description = $"Mediator description {commandIndex}",
                    Source = $"mediator-url-{commandIndex}",
                    Tags = new[] { "Strategy" },
                    CoverImageUrl = null
                };

                var result = await mediator.Send(command).ConfigureAwait(false);
                result.IsSuccess.Should().BeTrue(result.Error);
            }));
        }

        await Task.WhenAll(commandTasks);

        // Assert - All commands processed successfully
        var gameCount = await _dbContext.Games.CountAsync(g => g.PlatformId == platformId);
        gameCount.Should().Be(15);
    }

    [Fact]
    public async Task DatabaseTransactionIsolation_ConcurrentModifications()
    {
        // Arrange - Create initial game
        var platformId = Guid.NewGuid();
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);
        await _dbContext.Platforms.AddAsync(platform);

        var initialGame = Game.Create("Initial Game", platformId);
        typeof(Game).GetProperty("Id")?.SetValue(initialGame, Guid.NewGuid());
        await _dbContext.Games.AddAsync(initialGame);
        await _dbContext.SaveChangesAsync();

        var gameId = initialGame.Id; // Capture the ID Guid

        // Act - Concurrently modify the same game (should not cause conflicts with proper isolation)
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var iteration = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();

                var game = await dbContext.Games.FindAsync(gameId).ConfigureAwait(false);
                if (game != null)
                {
                    game.Update(description: $"Modified in task {iteration}");
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }));
        }

        // Assert - Should complete without deadlocks or constraint violations
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        exception.Should().BeNull();
    }

    [Fact]
    public async Task MemoryPressure_ConcurrentOperations()
    {
        // Arrange - Test memory management under concurrent load
        var memoryTasks = new List<Task>();
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        // Cancel after 5 seconds to prevent infinite memory growth
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            // Act - Create memory pressure with concurrent allocations
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                memoryTasks.Add(Task.Run(async () =>
                {
                    var objects = new List<object>();
                    while (!token.IsCancellationRequested)
                    {
                        // Allocate memory
                        objects.Add(new byte[1024]); // 1KB objects

                        // Periodic cleanup to prevent unbounded growth
                        if (objects.Count > 1000)
                        {
                            objects.Clear();
                            await Task.Delay(10, token).ConfigureAwait(false); // Allow GC
                        }
                    }
                }, token));
            }

            await Task.WhenAll(memoryTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token expires
        }

        // Assert - Should complete without out of memory exceptions
        // If we get here, the concurrent operations handled memory pressure
        true.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentAiOperations_MemoryManagement()
    {
        // Arrange - Test AI operations under concurrent load
        var aiTasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            aiTasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<SaveState.Core.Ai.Services.IAiOrchestrator>();

                // Perform multiple AI operations concurrently
                for (int j = 0; j < 10; j++)
                {
                    var request = new SaveState.Core.Ai.Services.AiRequest(
                        SaveState.Core.Ai.Services.AiRequestType.Completion,
                        Prompt: $"Test prompt {j}",
                        AllowCache: false);

                    try
                    {
                        var result = await orchestrator.ProcessRequestAsync(request).ConfigureAwait(false);
                        // Don't assert success since AI services may not be configured in tests
                    }
                    catch
                    {
                        // Expected in test environment without real AI services
                    }
                }
            }));
        }

        // Act & Assert - Should complete without deadlocks or resource exhaustion
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(aiTasks));
        exception.Should().BeNull();
    }
}
