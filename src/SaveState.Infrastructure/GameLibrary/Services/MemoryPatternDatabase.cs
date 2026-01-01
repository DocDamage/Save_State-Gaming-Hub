using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class MemoryPatternDatabase
{
    private readonly ILogger<MemoryPatternDatabase> _logger;
    private readonly ConcurrentDictionary<string, List<GameMemorySignature>> _gameSignatures = new();

    public MemoryPatternDatabase(ILogger<MemoryPatternDatabase> logger)
    {
        _logger = logger;
        InitializeKnownPatterns();
    }

    public Result<IReadOnlyList<GameMemorySignature>> GetSignaturesForGame(string gameTitle)
    {
        if (_gameSignatures.TryGetValue(gameTitle, out var signatures))
        {
            return Result<IReadOnlyList<GameMemorySignature>>.Success(signatures);
        }

        // Try fuzzy matching
        var similarGames = _gameSignatures.Keys
            .Where(title => title.Contains(gameTitle, StringComparison.OrdinalIgnoreCase) ||
                          gameTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (similarGames.Any())
        {
            var bestMatch = similarGames.First();
            if (_gameSignatures.TryGetValue(bestMatch, out signatures))
            {
                _logger.LogInformation("Found signatures for similar game '{Similar}' when searching for '{Game}'",
                    bestMatch, gameTitle);
                return Result<IReadOnlyList<GameMemorySignature>>.Success(signatures);
            }
        }

        return Result<IReadOnlyList<GameMemorySignature>>.Success(Array.Empty<GameMemorySignature>());
    }

    public Result AddSignature(string gameTitle, GameMemorySignature signature)
    {
        var signatures = _gameSignatures.GetOrAdd(gameTitle, _ => new List<GameMemorySignature>());
        signatures.Add(signature);

        _logger.LogInformation("Added memory signature '{Signature}' for game '{Game}'",
            signature.Name, gameTitle);

        return Result.Success();
    }

    public Result<IReadOnlyList<string>> GetSupportedGames()
    {
        return Result<IReadOnlyList<string>>.Success(_gameSignatures.Keys.ToList());
    }

    private void InitializeKnownPatterns()
    {
        // Initialize with some common game patterns
        // These are example patterns - in practice, these would be crowd-sourced or learned

        // Example: Celeste
        AddSignature("Celeste", new GameMemorySignature
        {
            Name = "Player Health",
            Pattern = "83 F8 64 7E ?? 8B", // Example signature
            Offset = 0x0,
            ValueType = "int32",
            Description = "Current player health value"
        });

        AddSignature("Celeste", new GameMemorySignature
        {
            Name = "Current Level",
            Pattern = "A1 ?? ?? ?? ?? 83 F8 01",
            Offset = 0x4,
            ValueType = "int32",
            Description = "Current level number"
        });

        // Example: Hollow Knight
        AddSignature("Hollow Knight", new GameMemorySignature
        {
            Name = "Geo Count",
            Pattern = "8B 0D ?? ?? ?? ?? 83 C1 01",
            Offset = 0x0,
            ValueType = "int32",
            Description = "Current geo (currency) amount"
        });

        // Example: Stardew Valley
        AddSignature("Stardew Valley", new GameMemorySignature
        {
            Name = "Gold Amount",
            Pattern = "A3 ?? ?? ?? ?? 83 3D ?? ?? ?? ?? 00",
            Offset = 0x0,
            ValueType = "int32",
            Description = "Current gold amount"
        });

        _logger.LogInformation("Initialized memory pattern database with {Count} game signatures",
            _gameSignatures.Sum(g => g.Value.Count));
    }
}

public class GameMemorySignature
{
    public string Name { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty; // Hex pattern to search for
    public int Offset { get; init; } // Offset from pattern start
    public string ValueType { get; init; } = "int32"; // int32, float, etc.
    public string Description { get; init; } = string.Empty;

    public override string ToString() => $"{Name}: {Description}";
}