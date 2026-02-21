using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Represents the complete game memory signature database.
/// </summary>
public sealed class GameMemoryDatabase
{
    /// <summary>
    /// Database version for migration purposes.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Last update date in ISO format (YYYY-MM-DD).
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = "";

    /// <summary>
    /// Collection of games with their memory signatures.
    /// </summary>
    [JsonPropertyName("games")]
    public List<GameMemoryEntry> Games { get; set; } = [];
}

/// <summary>
/// Represents a single game entry with its memory signatures.
/// </summary>
public sealed class GameMemoryEntry
{
    /// <summary>
    /// Unique identifier for the game (lowercase, no spaces).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display title of the game.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Game platform (PC, Steam, Epic, etc.).
    /// </summary>
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "PC";

    /// <summary>
    /// List of process names this game may use.
    /// </summary>
    [JsonPropertyName("processNames")]
    public List<string> ProcessNames { get; set; } = [];

    /// <summary>
    /// Game category/genre for organization.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "Action";

    /// <summary>
    /// Memory signatures for this game.
    /// </summary>
    [JsonPropertyName("signatures")]
    public List<MemorySignature> Signatures { get; set; } = [];
}

/// <summary>
/// Represents a single memory signature pattern for a game value.
/// </summary>
public sealed class MemorySignature
{
    /// <summary>
    /// Human-readable name of the value (e.g., "Health", "Gold").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category of the signature (e.g., "Combat", "Currency", "Progress").
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    /// <summary>
    /// Hex byte pattern with wildcards (??) for scanning.
    /// Format: "8B 45 ?? 89 45 ??"
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Byte offset from pattern match to the actual value.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Data type of the value (int8, int16, int32, int64, float, double, bool).
    /// </summary>
    [JsonPropertyName("valueType")]
    public string ValueType { get; set; } = "int32";

    /// <summary>
    /// Description of what this signature represents.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this signature is validated and known to work.
    /// </summary>
    [JsonPropertyName("validated")]
    public bool Validated { get; set; }

    /// <summary>
    /// Game version this signature was tested against.
    /// </summary>
    [JsonPropertyName("gameVersion")]
    public string? GameVersion { get; set; }

    /// <summary>
    /// Additional notes or warnings about this signature.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// Loads and queries the game memory signature database.
/// </summary>
public sealed class GameMemoryDatabaseLoader
{
    private readonly string _databasePath;
    private readonly ILogger<GameMemoryDatabaseLoader>? _logger;
    private readonly ITimeProvider? _timeProvider;
    private GameMemoryDatabase? _cachedDatabase;
    private DateTime _lastLoadTime;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Creates a new instance of the database loader.
    /// </summary>
    /// <param name="databasePath">Path to the JSON database file.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="timeProvider">Optional time provider.</param>
    public GameMemoryDatabaseLoader(string databasePath, ILogger<GameMemoryDatabaseLoader>? logger = null, ITimeProvider? timeProvider = null)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Loads the complete database from disk.
    /// Uses caching to avoid repeated file reads.
    /// </summary>
    /// <param name="forceReload">Force reload from disk even if cache is valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the loaded database or error.</returns>
    public async Task<Result<GameMemoryDatabase>> LoadAsync(
        bool forceReload = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache
            if (!forceReload && _cachedDatabase is not null)
            {
                var cacheAge = (_timeProvider?.UtcNow ?? DateTime.UtcNow) - _lastLoadTime;
                if (cacheAge < _cacheDuration)
                {
                    _logger?.LogDebug("Returning cached database (age: {CacheAge}s)", cacheAge.TotalSeconds);
                    return Result<GameMemoryDatabase>.Success(_cachedDatabase);
                }
            }

            // Verify file exists
            if (!File.Exists(_databasePath))
            {
                _logger?.LogError("Database file not found: {Path}", _databasePath);
                return Result<GameMemoryDatabase>.Failure(
                    $"Database file not found: {_databasePath}",
                    ErrorType.NotFound);
            }

            // Read and deserialize
            _logger?.LogInformation("Loading game memory database from {Path}", _databasePath);
            var json = await File.ReadAllTextAsync(_databasePath, cancellationToken).ConfigureAwait(false);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var database = JsonSerializer.Deserialize<GameMemoryDatabase>(json, options);

            if (database is null)
            {
                _logger?.LogError("Failed to deserialize database from {Path}", _databasePath);
                return Result<GameMemoryDatabase>.Failure(
                    "Failed to deserialize database",
                    ErrorType.Internal);
            }

            // Update cache
            _cachedDatabase = database;
            _lastLoadTime = _timeProvider?.UtcNow ?? DateTime.UtcNow;

            _logger?.LogInformation(
                "Loaded database v{Version} with {GameCount} games (last updated: {LastUpdated})",
                database.Version,
                database.Games.Count,
                database.LastUpdated);

            return Result<GameMemoryDatabase>.Success(database);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON parsing error in database file");
            return Result<GameMemoryDatabase>.Failure(
                $"Invalid JSON format: {ex.Message}",
                ErrorType.Validation);
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "IO error reading database file");
            return Result<GameMemoryDatabase>.Failure(
                $"Failed to read database: {ex.Message}",
                ErrorType.Internal);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error loading database");
            return Result<GameMemoryDatabase>.Failure(
                $"Unexpected error: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Synchronous version of Load for compatibility.
    /// </summary>
    /// <param name="forceReload">Force reload from disk.</param>
    /// <returns>Result containing the loaded database or error.</returns>
    [Obsolete("Use LoadAsync instead to avoid blocking. This method may be removed in a future version.")]
    public Result<GameMemoryDatabase> Load(bool forceReload = false)
    {
        return LoadAsync(forceReload).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets all signatures for a specific process name.
    /// </summary>
    /// <param name="processName">Process name to search for (e.g., "Celeste.exe").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing matching signatures or error.</returns>
    public async Task<Result<List<MemorySignature>>> GetSignaturesForProcessAsync(
        string processName,
        CancellationToken cancellationToken = default)
    {
        var databaseResult = await LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (databaseResult.IsFailure)
        {
            return Result<List<MemorySignature>>.Failure(databaseResult.Error!, databaseResult.ErrorType);
        }

        var database = databaseResult.Value;
        var normalizedProcessName = processName.ToLowerInvariant();

        // Find game by process name
        var game = database.Games.FirstOrDefault(g =>
            g.ProcessNames.Any(p => p.ToLowerInvariant() == normalizedProcessName));

        if (game is null)
        {
            _logger?.LogDebug("No signatures found for process: {ProcessName}", processName);
            return Result<List<MemorySignature>>.Success([]);
        }

        _logger?.LogDebug(
            "Found {Count} signatures for {GameTitle} ({ProcessName})",
            game.Signatures.Count,
            game.Title,
            processName);

        return Result<List<MemorySignature>>.Success(game.Signatures);
    }

    /// <summary>
    /// Gets game entry by its unique ID.
    /// </summary>
    /// <param name="gameId">Game identifier (e.g., "celeste").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the game entry or not found error.</returns>
    public async Task<Result<GameMemoryEntry>> GetGameByIdAsync(
        string gameId,
        CancellationToken cancellationToken = default)
    {
        var databaseResult = await LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (databaseResult.IsFailure)
        {
            return Result<GameMemoryEntry>.Failure(databaseResult.Error!, databaseResult.ErrorType);
        }

        var game = databaseResult.Value.Games.FirstOrDefault(g =>
            g.Id.Equals(gameId, StringComparison.OrdinalIgnoreCase));

        if (game is null)
        {
            return Result<GameMemoryEntry>.Failure(
                $"Game '{gameId}' not found in database",
                ErrorType.NotFound);
        }

        return Result<GameMemoryEntry>.Success(game);
    }

    /// <summary>
    /// Searches for games by title substring.
    /// </summary>
    /// <param name="searchTerm">Search term for game title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing matching game entries.</returns>
    public async Task<Result<List<GameMemoryEntry>>> SearchGamesAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var databaseResult = await LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (databaseResult.IsFailure)
        {
            return Result<List<GameMemoryEntry>>.Failure(databaseResult.Error!, databaseResult.ErrorType);
        }

        var normalizedSearch = searchTerm.ToLowerInvariant();
        var matches = databaseResult.Value.Games
            .Where(g => g.Title.ToLowerInvariant().Contains(normalizedSearch))
            .ToList();

        return Result<List<GameMemoryEntry>>.Success(matches);
    }

    /// <summary>
    /// Gets all games in a specific category.
    /// </summary>
    /// <param name="category">Category to filter by (e.g., "AAA", "Indie", "Multiplayer").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing games in the category.</returns>
    public async Task<Result<List<GameMemoryEntry>>> GetGamesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var databaseResult = await LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (databaseResult.IsFailure)
        {
            return Result<List<GameMemoryEntry>>.Failure(databaseResult.Error!, databaseResult.ErrorType);
        }

        var matches = databaseResult.Value.Games
            .Where(g => g.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result<List<GameMemoryEntry>>.Success(matches);
    }

    /// <summary>
    /// Gets all validated signatures across all games.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing tuple of (game, signature) pairs.</returns>
    public async Task<Result<List<(GameMemoryEntry Game, MemorySignature Signature)>>> GetAllValidatedSignaturesAsync(
        CancellationToken cancellationToken = default)
    {
        var databaseResult = await LoadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (databaseResult.IsFailure)
        {
            return Result<List<(GameMemoryEntry, MemorySignature)>>.Failure(
                databaseResult.Error!,
                databaseResult.ErrorType);
        }

        var validated = databaseResult.Value.Games
            .SelectMany(g => g.Signatures
                .Where(s => s.Validated)
                .Select(s => (g, s)))
            .ToList();

        return Result<List<(GameMemoryEntry, MemorySignature)>>.Success(validated);
    }

    /// <summary>
    /// Clears the in-memory cache, forcing the next load to read from disk.
    /// </summary>
    public void ClearCache()
    {
        _cachedDatabase = null;
        _lastLoadTime = DateTime.MinValue;
        _logger?.LogDebug("Database cache cleared");
    }
}

/// <summary>
/// Extension methods for IServiceCollection to register the database loader.
/// </summary>
public static class GameMemoryDatabaseLoaderExtensions
{
    /// <summary>
    /// Adds the GameMemoryDatabaseLoader to the service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="databasePath">Path to the JSON database file. If null, uses default path.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddGameMemoryDatabaseLoader(
        this IServiceCollection services,
        string? databasePath = null)
    {
        services.AddSingleton<GameMemoryDatabaseLoader>(provider =>
        {
            var path = databasePath ?? GetDefaultDatabasePath();
            var logger = provider.GetService<ILogger<GameMemoryDatabaseLoader>>();
            return new GameMemoryDatabaseLoader(path, logger);
        });

        return services;
    }

    private static string GetDefaultDatabasePath()
    {
        // Look in the assembly directory first, then in common data locations
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;

        var possiblePaths = new[]
        {
            Path.Combine(assemblyDir, "Data", "GameMemoryDatabase.json"),
            Path.Combine(assemblyDir, "GameMemoryDatabase.json"),
            Path.Combine(AppContext.BaseDirectory, "Data", "GameMemoryDatabase.json"),
            Path.Combine(AppContext.BaseDirectory, "GameMemoryDatabase.json")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Return the first path as default even if it doesn't exist
        return possiblePaths[0];
    }
}
