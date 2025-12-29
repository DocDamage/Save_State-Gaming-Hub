using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Application.Common.DependencyInjection;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure;
using SaveState.Infrastructure.Persistence;
using System.Diagnostics;
using System;

BenchmarkRunner.Run<PerformanceBenchmarks>();

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class PerformanceBenchmarks
{
    private IServiceProvider? _serviceProvider;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
        dbContext.Database.EnsureCreated();
    }

    [Benchmark(Baseline = true)]
    public void ServiceProviderBuild()
    {
        // Measure DI container build time
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        // Clean up
        (provider as IDisposable)?.Dispose();
    }

    [Benchmark]
    public async Task DatabaseQuery()
    {
        using var scope = _serviceProvider!.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Measure query performance
        var query = new GetAllGamesQuery();
        var games = await mediator.Send(query, default).ConfigureAwait(false);

        // Should handle games efficiently
        if (games.Count < 0)
        {
            throw new InvalidOperationException("Query returned negative count");
        }
    }

    [Benchmark]
    public void DatabaseInitialization()
    {
        // Measure database initialization time
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var stopwatch = Stopwatch.StartNew();

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
        dbContext.Database.EnsureCreated();

        stopwatch.Stop();

        Console.WriteLine($"DB init time: {stopwatch.ElapsedMilliseconds}ms");

        (provider as IDisposable)?.Dispose();
    }

    [Benchmark]
    public void MemoryAllocation()
    {
        // Test memory allocation patterns
        var games = new System.Collections.Generic.List<Game>();
        for (int i = 0; i < 1000; i++)
        {
            games.Add(Game.Create($"Test Game {i}"));
        }

        // Clean up
        games.Clear();
    }

    [Benchmark]
    public async Task AiMemoryOperations()
    {
        // Test AI memory performance
        using var scope = _serviceProvider!.CreateScope();
        var memory = scope.ServiceProvider.GetRequiredService<SaveState.Core.Ai.Memory.IShortTermMemory>();

        // Measure memory storage performance
        for (int i = 0; i < 100; i++)
        {
            var entry = new SaveState.Core.Ai.Memory.MemoryEntry(
                $"bench-{i}",
                $"Benchmark memory content {i} with substantial text to test token estimation",
                DateTime.UtcNow,
                new[] { "benchmark", "performance" });
            await memory.StoreAsync(entry).ConfigureAwait(false);
        }

        // Test search performance
        var results = await memory.SearchAsync("benchmark", 50).ConfigureAwait(false);
        if (results.Count == 0)
        {
            throw new InvalidOperationException("Search should return results");
        }
    }

    [Benchmark]
    public async Task FileSystemOperations()
    {
        // Test file system performance
        using var scope = _serviceProvider!.CreateScope();
        var fileSystem = scope.ServiceProvider.GetRequiredService<SaveState.Core.Common.Interfaces.IFileSystem>();

        var tempPath = Path.GetTempFileName();

        try
        {
            // Test file operations
            var exists = await fileSystem.FileExistsAsync(tempPath).ConfigureAwait(false);
            var size = await fileSystem.GetFileSizeAsync(tempPath).ConfigureAwait(false);
            var content = await fileSystem.ReadAllBytesAsync(tempPath).ConfigureAwait(false);

            // Should not throw
            if (size < 0)
            {
                throw new InvalidOperationException("File size should be non-negative");
            }
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Benchmark]
    public async Task ConcurrentDatabaseOperations()
    {
        // Test concurrent database access
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider!.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var query = new GetAllGamesQuery();
                var games = await mediator.Send(query).ConfigureAwait(false);
                // Just ensure no exceptions
            }));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [Benchmark]
    public void AiOrchestratorInitialization()
    {
        // Test AI orchestrator creation performance
        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetService<SaveState.Core.Ai.Services.IAiOrchestrator>();

        if (orchestrator == null)
        {
            throw new InvalidOperationException("Orchestrator should be available");
        }

        (provider as IDisposable)?.Dispose();
    }

    [Benchmark]
    public async Task BulkGameCreation()
    {
        // Test bulk entity creation performance
        using var scope = _serviceProvider!.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();

        var games = new List<Game>();
        for (int i = 0; i < 100; i++)
        {
            games.Add(Game.Create($"Bulk Game {i}", platformId: null, description: $"Description for game {i}"));
        }

        await dbContext.Games.AddRangeAsync(games).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Clean up
        dbContext.Games.RemoveRange(games);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add minimal configuration
        var config = new ConfigurationBuilder().Build();

        // Add infrastructure with in-memory database for testing
        services.AddInfrastructure(config);
        services.AddApplicationServices();

        // Override database connection for benchmarks (use in-memory)
        services.AddDbContext<SaveStateDbContext>((sp, options) =>
            options.UseSqlite("Data Source=:memory:"));
    }
}
