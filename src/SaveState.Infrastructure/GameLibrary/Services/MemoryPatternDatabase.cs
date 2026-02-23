using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Database for managing game memory signatures and pattern detection.
/// Provides JSON persistence and fuzzy pattern matching capabilities.
/// </summary>
public partial class MemoryPatternDatabase : IMemoryPatternDatabase
{
    private readonly ILogger<MemoryPatternDatabase> _logger;
    private readonly ConcurrentDictionary<string, List<GameMemorySignature>> _gameSignatures = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public int Count => _gameSignatures.Sum(g => g.Value.Count);

    public MemoryPatternDatabase(ILogger<MemoryPatternDatabase> logger)
    {
        _logger = logger;
        InitializeKnownPatterns();
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<GameMemorySignature>> GetSignaturesForGame(string gameTitle)
    {
        if (string.IsNullOrWhiteSpace(gameTitle))
        {
            return Result.Success<IReadOnlyList<GameMemorySignature>>(Array.Empty<GameMemorySignature>());
        }

        var results = new List<GameMemorySignature>();

        // Get exact match
        if (_gameSignatures.TryGetValue(gameTitle, out var signatures))
        {
            results.AddRange(signatures.Where(s => s.IsEnabled));
        }

        // Get universal patterns
        if (_gameSignatures.TryGetValue("*", out var universalSignatures))
        {
            results.AddRange(universalSignatures.Where(s => s.IsEnabled));
        }

        // Try fuzzy matching if no exact match
        if (results.Count == 0)
        {
            var similarGames = _gameSignatures.Keys
                .Where(title => title != "*" &&
                    (title.Contains(gameTitle, StringComparison.OrdinalIgnoreCase) ||
                     gameTitle.Contains(title, StringComparison.OrdinalIgnoreCase) ||
                     CalculateSimilarity(title, gameTitle) > 0.7))
                .ToList();

            if (similarGames.Any())
            {
                var bestMatch = similarGames.OrderByDescending(g => CalculateSimilarity(g, gameTitle)).First();
                if (_gameSignatures.TryGetValue(bestMatch, out signatures))
                {
                    _logger.LogInformation(
                        "Found signatures for similar game '{Similar}' when searching for '{Game}'",
                        bestMatch, gameTitle);
                    results.AddRange(signatures.Where(s => s.IsEnabled));
                }
            }
        }

        // Sort by priority
        return Result.Success<IReadOnlyList<GameMemorySignature>>(
            results.OrderByDescending(s => s.Priority).ToList());
    }

    /// <inheritdoc />
    public Result AddSignature(GameMemorySignature signature)
    {
        if (signature == null)
        {
            return Result.Failure("Signature cannot be null", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(signature.GameTitle))
        {
            return Result.Failure("Game title cannot be empty", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(signature.Name))
        {
            return Result.Failure("Signature name cannot be empty", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(signature.Pattern))
        {
            return Result.Failure("Pattern cannot be empty", ErrorType.Validation);
        }

        var signatures = _gameSignatures.GetOrAdd(signature.GameTitle, _ => new List<GameMemorySignature>());

        lock (signatures)
        {
            // Remove existing signature with same name if present
            signatures.RemoveAll(s => s.Name == signature.Name);
            signatures.Add(signature);
        }

        _logger.LogInformation("Added memory signature '{Signature}' for game '{Game}'",
            signature.Name, signature.GameTitle);

        return Result.Success();
    }

    /// <inheritdoc />
    public Result AddSignature(string gameTitle, GameMemorySignature signature)
    {
        if (signature == null)
        {
            return Result.Failure("Signature cannot be null", ErrorType.Validation);
        }

        signature.GameTitle = gameTitle;
        return AddSignature(signature);
    }

    /// <inheritdoc />
    public Result RemoveSignature(string gameTitle, string name)
    {
        if (_gameSignatures.TryGetValue(gameTitle, out var signatures))
        {
            lock (signatures)
            {
                var removed = signatures.RemoveAll(s => s.Name == name);
                if (removed > 0)
                {
                    _logger.LogInformation("Removed signature '{Name}' for game '{Game}'", name, gameTitle);
                    return Result.Success();
                }
            }
        }

        return Result.Failure($"Signature '{name}' not found for game '{gameTitle}'", ErrorType.NotFound);
    }

    /// <inheritdoc />
    public Result RemoveAllSignaturesForGame(string gameTitle)
    {
        if (_gameSignatures.TryRemove(gameTitle, out _))
        {
            _logger.LogInformation("Removed all signatures for game '{Game}'", gameTitle);
            return Result.Success();
        }

        return Result.Failure($"No signatures found for game '{gameTitle}'", ErrorType.NotFound);
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<string>> GetSupportedGames()
    {
        return Result.Success<IReadOnlyList<string>>(_gameSignatures.Keys.ToList());
    }

    /// <inheritdoc />
    public bool HasSignaturesForGame(string gameTitle)
    {
        return _gameSignatures.ContainsKey(gameTitle) ||
               _gameSignatures.Keys.Any(k =>
                   k.Contains(gameTitle, StringComparison.OrdinalIgnoreCase) ||
                   gameTitle.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<GameMemorySignature> GetAllSignatures()
    {
        return _gameSignatures.SelectMany(g => g.Value).ToList();
    }

    /// <inheritdoc />
    public Result UpdateSignature(string gameTitle, string name, GameMemorySignature updatedSignature)
    {
        if (updatedSignature == null)
        {
            return Result.Failure("Updated signature cannot be null", ErrorType.Validation);
        }

        if (_gameSignatures.TryGetValue(gameTitle, out var signatures))
        {
            lock (signatures)
            {
                var index = signatures.FindIndex(s => s.Name == name);
                if (index >= 0)
                {
                    updatedSignature.GameTitle = gameTitle;
                    signatures[index] = updatedSignature;
                    _logger.LogInformation("Updated signature '{Name}' for game '{Game}'", name, gameTitle);
                    return Result.Success();
                }
            }
        }

        return Result.Failure($"Signature '{name}' not found for game '{gameTitle}'", ErrorType.NotFound);
    }

    /// <inheritdoc />
    public Result Clear()
    {
        _gameSignatures.Clear();
        _logger.LogInformation("Cleared all signatures from database");
        return Result.Success();
    }

    /// <summary>
    /// Loads signatures from a JSON file.
    /// </summary>
    /// <param name="path">Path to the JSON file.</param>
    public async Task<Result> LoadFromFileAsync(string path)
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(path))
            {
                return Result.Failure($"File not found: {path}", ErrorType.NotFound);
            }

            var json = await File.ReadAllTextAsync(path);
            var signatures = JsonSerializer.Deserialize<List<GameMemorySignature>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (signatures == null)
            {
                return Result.Failure("Failed to deserialize signatures", ErrorType.Validation);
            }

            int addedCount = 0;
            foreach (var signature in signatures)
            {
                if (!string.IsNullOrWhiteSpace(signature.GameTitle) &&
                    !string.IsNullOrWhiteSpace(signature.Name) &&
                    !string.IsNullOrWhiteSpace(signature.Pattern))
                {
                    AddSignature(signature);
                    addedCount++;
                }
            }

            _logger.LogInformation("Loaded {Count} signatures from {Path}", addedCount, path);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading signatures from {Path}", path);
            return Result.Failure($"Error loading signatures: {ex.Message}", ErrorType.Internal);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Saves all signatures to a JSON file.
    /// </summary>
    /// <param name="path">Path to save the JSON file.</param>
    public async Task<Result> SaveToFileAsync(string path)
    {
        await _fileLock.WaitAsync();
        try
        {
            var signatures = GetAllSignatures();
            var json = JsonSerializer.Serialize(signatures, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, json);

            _logger.LogInformation("Saved {Count} signatures to {Path}", signatures.Count, path);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving signatures to {Path}", path);
            return Result.Failure($"Error saving signatures: {ex.Message}", ErrorType.Internal);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Matches a byte array against a pattern with wildcards.
    /// </summary>
    /// <param name="data">The byte data to match against.</param>
    /// <param name="offset">Starting offset in the data.</param>
    /// <param name="pattern">Hex pattern with wildcards (??).</param>
    /// <returns>True if the pattern matches.</returns>
    public bool MatchesPattern(byte[] data, int offset, string pattern)
    {
        var patternBytes = ParsePattern(pattern);
        if (patternBytes == null) return false;

        for (int i = 0; i < patternBytes.Length; i++)
        {
            if (offset + i >= data.Length) return false;
            if (patternBytes[i].HasValue && patternBytes[i].Value != data[offset + i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// Parses a hex pattern string into nullable bytes.
    /// Wildcards (??) become null values.
    /// </summary>
    /// <param name="pattern">Hex pattern like "A1 ?? ?? ?? ?? 8B".</param>
    /// <returns>Array of nullable bytes, or null if parsing fails.</returns>
    public byte?[]? ParsePattern(string pattern)
    {
        try
        {
            // Remove all whitespace
            pattern = Regex.Replace(pattern, @"\s+", "");

            if (string.IsNullOrEmpty(pattern) || pattern.Length % 2 != 0)
            {
                return null;
            }

            var bytes = new byte?[pattern.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var hexPair = pattern.Substring(i * 2, 2);
                if (hexPair == "??" || hexPair == "**")
                {
                    bytes[i] = null; // Wildcard
                }
                else
                {
                    bytes[i] = Convert.ToByte(hexPair, 16);
                }
            }
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse pattern: {Pattern}", pattern);
            return null;
        }
    }

    /// <summary>
    /// Converts a byte array to a hex pattern string.
    /// </summary>
    public string ByteArrayToPattern(byte[] bytes)
    {
        return string.Join(" ", bytes.Select(b => b.ToString("X2")));
    }

    /// <summary>
    /// Searches for a pattern in a byte array and returns all matching offsets.
    /// </summary>
    public List<int> FindPatternOffsets(byte[] data, string pattern)
    {
        var offsets = new List<int>();
        var patternBytes = ParsePattern(pattern);
        if (patternBytes == null || patternBytes.Length == 0) return offsets;

        for (int i = 0; i <= data.Length - patternBytes.Length; i++)
        {
            if (MatchesPatternAtOffset(data, i, patternBytes))
            {
                offsets.Add(i);
            }
        }

        return offsets;
    }

    private bool MatchesPatternAtOffset(byte[] data, int offset, byte?[] patternBytes)
    {
        for (int i = 0; i < patternBytes.Length; i++)
        {
            if (offset + i >= data.Length) return false;
            if (patternBytes[i].HasValue && patternBytes[i].Value != data[offset + i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// Calculates string similarity using Levenshtein distance.
    /// Returns value between 0 (no similarity) and 1 (identical).
    /// </summary>
    private static double CalculateSimilarity(string s1, string s2)
    {
        if (string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase)) return 1.0;

        int levenshteinDistance = ComputeLevenshteinDistance(s1.ToLowerInvariant(), s2.ToLowerInvariant());
        int maxLength = Math.Max(s1.Length, s2.Length);

        return maxLength == 0 ? 1.0 : 1.0 - ((double)levenshteinDistance / maxLength);
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    #region Async Methods

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<GameMemorySignature>>> GetSignaturesForGameAsync(
        string gameTitle,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = GetSignaturesForGame(gameTitle);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<string>>> GetAllGamesWithSignaturesAsync(
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var games = _gameSignatures.Keys
            .Where(k => k != "*") // Exclude universal patterns
            .OrderBy(k => k)
            .ToList();
        return Task.FromResult(Result.Success<IReadOnlyList<string>>(games));
    }

    #endregion
}

