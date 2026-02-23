// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Diagnostics;
using Bogus;
using FluentAssertions;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Performance.Tests;

/// <summary>
/// Startup performance tests for application initialization.
/// </summary>
public class StartupPerformanceTests
{
    [Fact]
    public void ServiceCollection_Registration()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate service registration
        var services = new List<ServiceDescriptor>();
        RegisterCoreServices(services);
        RegisterApplicationServices(services);
        RegisterInfrastructureServices(services);

        stopwatch.Stop();

        // Assert - Should register all services in under 100ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        services.Count.Should().BeGreaterThan(50);
    }

    [Fact]
    public void ConfigurationLoading_Defaults()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate configuration loading
        var config = new Dictionary<string, object>
        {
            ["GameLibrary:DefaultPageSize"] = 50,
            ["GameLibrary:MaxRecentGames"] = 20,
            ["CloudSync:Enabled"] = true,
            ["CloudSync:SyncInterval"] = TimeSpan.FromMinutes(5),
            ["UI:Theme"] = "Dark",
            ["UI:Language"] = "en-US",
            ["Performance:EnableHardwareAcceleration"] = true,
            ["Performance:MaxCacheSize"] = 1024 * 1024 * 1024L,
            ["Memory:MaxUndoStack"] = 50,
            ["Memory:GarbageCollectionThreshold"] = 512 * 1024 * 1024L
        };

        stopwatch.Stop();

        // Assert - Should load in under 10ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void GameLibraryCache_Warmup()
    {
        // Arrange
        var games = GenerateGames(5000);
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate cache warmup
        var cache = new GameLibraryCache();
        foreach (var game in games)
        {
            cache.AddOrUpdate(game);
        }
        cache.BuildIndexes();

        stopwatch.Stop();

        // Assert - Should warmup in under 200ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void DatabaseConnection_PoolInitialization()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate connection pool initialization
        SimulateConnectionPoolInitialization(minPoolSize: 5, maxPoolSize: 100);

        stopwatch.Stop();

        // Assert - Should initialize in under 100ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void PluginDiscovery_Scanning()
    {
        // Arrange
        var pluginAssemblies = GeneratePluginList(50);
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate plugin discovery
        var discoveredPlugins = pluginAssemblies
            .Where(p => p.IsEnabled)
            .Select(p => new DiscoveredPlugin
            {
                Name = p.Name,
                Version = p.Version,
                Type = Type.GetType(p.TypeName) ?? typeof(object)
            })
            .ToList();

        stopwatch.Stop();

        // Assert - Should scan in under 50ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(50));
        discoveredPlugins.Count.Should().Be(50);
    }

    [Fact]
    public void ResourceDictionaryLoading()
    {
        // Arrange
        var resources = GenerateResourceDictionaries(10);
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate resource loading
        var mergedResources = new Dictionary<string, object>();
        foreach (var dict in resources)
        {
            foreach (var kvp in dict)
            {
                mergedResources[kvp.Key] = kvp.Value;
            }
        }

        stopwatch.Stop();

        // Assert - Should load in under 20ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public void LoggerConfiguration_Initialization()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate logger configuration
        var loggers = new List<LoggerConfig>
        {
            new() { Name = "Default", Level = "Information" },
            new() { Name = "Microsoft", Level = "Warning" },
            new() { Name = "System", Level = "Warning" },
            new() { Name = "SaveState", Level = "Debug" }
        };

        var sinks = new List<LogSink>
        {
            new() { Type = "Console", Enabled = true },
            new() { Type = "File", Enabled = true, Path = "/logs/app.log" },
            new() { Type = "Debug", Enabled = true }
        };

        stopwatch.Stop();

        // Assert - Should configure in under 10ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void ThemeLoading_ApplicationStartup()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate theme loading
        var theme = LoadTheme("Dark");
        ApplyTheme(theme);

        stopwatch.Stop();

        // Assert - Should load and apply in under 50ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void LocalizationResourceLoading()
    {
        // Arrange
        var supportedLocales = new[] { "en-US", "de-DE", "fr-FR", "es-ES", "ja-JP" };
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate loading all localization resources
        var resources = new Dictionary<string, Dictionary<string, string>>();
        foreach (var locale in supportedLocales)
        {
            resources[locale] = LoadLocalizationResources(locale);
        }

        stopwatch.Stop();

        // Assert - Should load all locales in under 100ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
        resources.Count.Should().Be(5);
    }

    [Fact]
    public void InitialDataValidation()
    {
        // Arrange
        var games = GenerateGames(1000);
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate data validation
        var validationResults = games
            .AsParallel()
            .Select(g => ValidateGame(g))
            .ToList();

        stopwatch.Stop();

        // Assert - Should validate in under 200ms
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        validationResults.Count.Should().Be(1000);
    }

    #region Helper Methods

    private static void RegisterCoreServices(List<ServiceDescriptor> services)
    {
        // Simulate core service registration
        services.AddRange(new[]
        {
            new ServiceDescriptor { ServiceType = "IGameRepository", ImplementationType = "GameRepository", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "ISaveStateRepository", ImplementationType = "SaveStateRepository", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "IPlatformRepository", ImplementationType = "PlatformRepository", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "ITimeProvider", ImplementationType = "SystemTimeProvider", Lifetime = "Singleton" },
            new ServiceDescriptor { ServiceType = "ICacheService", ImplementationType = "MemoryCacheService", Lifetime = "Singleton" },
            new ServiceDescriptor { ServiceType = "IEventBus", ImplementationType = "InMemoryEventBus", Lifetime = "Singleton" }
        });
    }

    private static void RegisterApplicationServices(List<ServiceDescriptor> services)
    {
        // Simulate application service registration
        services.AddRange(new[]
        {
            new ServiceDescriptor { ServiceType = "IGameLibraryService", ImplementationType = "GameLibraryService", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "ISaveStateService", ImplementationType = "SaveStateService", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "ICloudSyncService", ImplementationType = "CloudSyncService", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "ISearchService", ImplementationType = "SearchService", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "IMetadataService", ImplementationType = "MetadataService", Lifetime = "Scoped" }
        });
    }

    private static void RegisterInfrastructureServices(List<ServiceDescriptor> services)
    {
        // Simulate infrastructure service registration
        services.AddRange(new[]
        {
            new ServiceDescriptor { ServiceType = "IDatabaseContext", ImplementationType = "ApplicationDbContext", Lifetime = "Scoped" },
            new ServiceDescriptor { ServiceType = "ISteamApiClient", ImplementationType = "SteamApiClient", Lifetime = "Singleton" },
            new ServiceDescriptor { ServiceType = "IFileSystem", ImplementationType = "PhysicalFileSystem", Lifetime = "Singleton" },
            new ServiceDescriptor { ServiceType = "IProcessService", ImplementationType = "ProcessService", Lifetime = "Singleton" }
        });
    }

    private static void SimulateConnectionPoolInitialization(int minPoolSize, int maxPoolSize)
    {
        // Simulate creating minimum connections
        var connections = new List<object>(minPoolSize);
        for (int i = 0; i < minPoolSize; i++)
        {
            connections.Add(new { Id = i, State = "Ready" });
        }
    }

    private static List<PluginInfo> GeneratePluginList(int count)
    {
        var faker = new Faker();
        return Enumerable.Range(0, count)
            .Select(i => new PluginInfo
            {
                Name = $"Plugin{i}",
                Version = $"1.{faker.Random.Int(0, 9)}.{faker.Random.Int(0, 9)}",
                TypeName = $"SaveState.Plugins.Plugin{i}.Plugin{i}Plugin",
                IsEnabled = faker.Random.Bool(0.9f)
            })
            .ToList();
    }

    private static List<Dictionary<string, object>> GenerateResourceDictionaries(int count)
    {
        var dictionaries = new List<Dictionary<string, object>>(count);
        for (int i = 0; i < count; i++)
        {
            var dict = new Dictionary<string, object>
            {
                [$"Resource{i}_Color"] = $"#FF{i:X2}{i:X2}{i:X2}",
                [$"Resource{i}_Size"] = i * 10,
                [$"Resource{i}_Font"] = "Segoe UI"
            };
            dictionaries.Add(dict);
        }
        return dictionaries;
    }

    private static Theme LoadTheme(string themeName)
    {
        return new Theme
        {
            Name = themeName,
            Colors = new Dictionary<string, string>
            {
                ["Background"] = themeName == "Dark" ? "#1E1E1E" : "#FFFFFF",
                ["Foreground"] = themeName == "Dark" ? "#FFFFFF" : "#000000",
                ["Accent"] = "#0078D4"
            },
            Fonts = new Dictionary<string, string>
            {
                ["Default"] = "Segoe UI",
                ["Header"] = "Segoe UI Light"
            }
        };
    }

    private static void ApplyTheme(Theme theme)
    {
        // Simulate theme application
        _ = theme.Colors.Count + theme.Fonts.Count;
    }

    private static Dictionary<string, string> LoadLocalizationResources(string locale)
    {
        var resources = new Dictionary<string, string>
        {
            ["App.Title"] = "SaveState Reborn",
            ["Menu.File"] = "File",
            ["Menu.Edit"] = "Edit",
            ["Menu.View"] = "View",
            ["Menu.Help"] = "Help",
            ["Library.Title"] = "Game Library",
            ["Library.AddGame"] = "Add Game",
            ["Library.Search"] = "Search",
            ["SaveState.Create"] = "Create Save State",
            ["SaveState.Load"] = "Load Save State",
            ["Settings.Title"] = "Settings",
            ["Settings.General"] = "General",
            ["Settings.Appearance"] = "Appearance"
        };

        return resources;
    }

    private static ValidationResult ValidateGame(Game game)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(game.Title))
            errors.Add("Title is required");

        if (game.Title.Length > 200)
            errors.Add("Title too long");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    private static List<Game> GenerateGames(int count)
    {
        var faker = new Faker();
        var games = new List<Game>(count);

        for (int i = 0; i < count; i++)
        {
            var game = Game.Create(
                title: $"Game {faker.Random.Word()} {i}",
                platformId: Guid.NewGuid());
            games.Add(game);
        }

        return games;
    }

    #endregion

    #region Helper Classes

    private class ServiceDescriptor
    {
        public string ServiceType { get; set; } = string.Empty;
        public string ImplementationType { get; set; } = string.Empty;
        public string Lifetime { get; set; } = string.Empty;
    }

    private class PluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    private class DiscoveredPlugin
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Type Type { get; set; } = typeof(object);
    }

    private class LoggerConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }

    private class LogSink
    {
        public string Type { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    private class Theme
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Colors { get; set; } = new();
        public Dictionary<string, string> Fonts { get; set; } = new();
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private class GameLibraryCache
    {
        private readonly Dictionary<Guid, Game> _games = new();
        private readonly Dictionary<string, List<Game>> _titleIndex = new(StringComparer.OrdinalIgnoreCase);

        public void AddOrUpdate(Game game)
        {
            _games[game.Id] = game;
        }

        public void BuildIndexes()
        {
            _titleIndex.Clear();
            foreach (var game in _games.Values)
            {
                var words = game.Title.Split(' ');
                foreach (var word in words)
                {
                    if (!_titleIndex.TryGetValue(word, out var list))
                    {
                        list = new List<Game>();
                        _titleIndex[word] = list;
                    }
                    list.Add(game);
                }
            }
        }
    }

    #endregion
}
