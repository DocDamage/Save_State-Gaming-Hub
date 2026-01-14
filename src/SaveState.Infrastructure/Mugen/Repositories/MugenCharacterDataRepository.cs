using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Repositories;

namespace SaveState.Infrastructure.Mugen.Repositories;

/// <summary>
/// Repository for storing and retrieving character statistics for ML analysis.
/// </summary>
public class MugenCharacterDataRepository : ICharacterDataRepository
{
    private readonly ILogger<MugenCharacterDataRepository> _logger;
    private readonly Dictionary<string, SaveState.Core.Mugen.ValueObjects.CharacterAttributes> _characterStats;

    public MugenCharacterDataRepository(ILogger<MugenCharacterDataRepository> logger)
    {
        _logger = logger;
        _characterStats = InitializeCharacterStats();
    }

    public Task<Result<SaveState.Core.Mugen.ValueObjects.CharacterAttributes>> GetCharacterStatsAsync(string characterName, CancellationToken ct = default)
    {
        try
        {
            if (_characterStats.TryGetValue(characterName, out var stats))
            {
                _logger.LogInformation("Retrieved stats for character {Character}: Health={Health}, Attack={Attack}, Defense={Defense}",
                    characterName, stats.Health, stats.Attack, stats.Defense);
                return Task.FromResult(Result.Success(stats));
            }
            else
            {
                // Return default stats for unknown characters
                var defaultStats = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1000, 800, 800);
                _logger.LogWarning("Character {Character} not found, returning default stats", characterName);
                return Task.FromResult(Result.Success(defaultStats));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving character stats for {Character}", characterName);
            return Task.FromResult(Result.Failure<SaveState.Core.Mugen.ValueObjects.CharacterAttributes>($"Failed to get character stats: {ex.Message}"));
        }
    }

    public Task<Result> UpdateCharacterStatsAsync(string characterName, SaveState.Core.Mugen.ValueObjects.CharacterAttributes stats, CancellationToken ct = default)
    {
        try
        {
            _characterStats[characterName] = stats;
            _logger.LogInformation("Updated stats for character {Character}", characterName);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character stats for {Character}", characterName);
            return Task.FromResult(Result.Failure($"Failed to update character stats: {ex.Message}"));
        }
    }

    private Dictionary<string, SaveState.Core.Mugen.ValueObjects.CharacterAttributes> InitializeCharacterStats()
    {
        // Initialize with Street Fighter-style character stats
        return new Dictionary<string, SaveState.Core.Mugen.ValueObjects.CharacterAttributes>
        {
            // Shoto characters (Ryu, Ken) - Balanced all-around
            ["Ryu"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1000, 900, 900),
            ["Ken"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1000, 950, 850),

            // Zoner (Guile) - Good range, lower health
            ["Guile"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(950, 800, 1000),

            // Rushdown (Chun-Li) - Fast, good normals
            ["Chun-Li"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(900, 950, 900),

            // Grappler (Zangief) - High damage, low speed
            ["Zangief"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1200, 1100, 600),

            // Beast (Blanka) - High damage, unpredictable
            ["Blanka"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1100, 1050, 700),

            // Stretchy (Dhalsim) - Good range, low health
            ["Dhalsim"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(800, 850, 950),

            // Big Boobs (Sagat) - High damage, slow
            ["Sagat"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1100, 1000, 800),

            // Default stats for unknown characters
            ["Default"] = new SaveState.Core.Mugen.ValueObjects.CharacterAttributes(1000, 800, 800)
        };
    }
}
