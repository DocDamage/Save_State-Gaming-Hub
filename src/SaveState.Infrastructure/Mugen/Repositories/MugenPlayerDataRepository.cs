using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Repositories;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Infrastructure.Mugen.Repositories;

/// <summary>
/// Repository for storing and retrieving player skill data for ML analysis.
/// </summary>
public class MugenPlayerDataRepository : IPlayerDataRepository
{
    private readonly ILogger<MugenPlayerDataRepository> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, PlayerSkill> _playerSkills;

    public MugenPlayerDataRepository(ILogger<MugenPlayerDataRepository> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _playerSkills = InitializePlayerSkills();
    }

    public Task<Result<PlayerSkill>> GetPlayerSkillAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            if (_playerSkills.TryGetValue(playerId, out var skill))
            {
                _logger.LogInformation("Retrieved skill data for player {PlayerId}: Rating={Rating:F0}, Volatility={Volatility:F3}",
                    playerId, skill.Rating, skill.Volatility);
                return Task.FromResult(Result.Success(skill));
            }
            else
            {
                // Create default skill for new players
                var defaultSkill = new PlayerSkill(
                    playerId: playerId,
                    rating: 1500.0, // Default Elo rating
                    volatility: 0.06, // Default Glicko volatility
                    characterRatings: new Dictionary<string, double>(),
                    lastUpdated: _timeProvider.UtcNow);

                _playerSkills[playerId] = defaultSkill;

                _logger.LogInformation("Created default skill data for new player {PlayerId}", playerId);
                return Task.FromResult(Result.Success(defaultSkill));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving player skill for {PlayerId}", playerId);
            return Task.FromResult(Result.Failure<PlayerSkill>($"Failed to get player skill: {ex.Message}"));
        }
    }

    public Task<Result> SavePlayerSkillAsync(PlayerSkill skill, CancellationToken ct = default)
    {
        try
        {
            _playerSkills[skill.PlayerId] = skill;

            _logger.LogInformation("Saved skill data for player {PlayerId}: Rating={Rating:F0}",
                skill.PlayerId, skill.Rating);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving player skill for {PlayerId}", skill.PlayerId);
            return Task.FromResult(Result.Failure($"Failed to save player skill: {ex.Message}"));
        }
    }

    public Task<Result<IReadOnlyDictionary<string, PlayerSkill>>> GetTopPlayersAsync(int count, CancellationToken ct = default)
    {
        try
        {
            var topPlayers = _playerSkills
                .OrderByDescending(p => p.Value.Rating)
                .Take(count)
                .ToDictionary(p => p.Key, p => p.Value);

            _logger.LogInformation("Retrieved top {Count} players by rating", topPlayers.Count);
            return Task.FromResult(Result.Success<IReadOnlyDictionary<string, PlayerSkill>>(topPlayers));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top players");
            return Task.FromResult(Result.Failure<IReadOnlyDictionary<string, PlayerSkill>>($"Failed to get top players: {ex.Message}"));
        }
    }

    private Dictionary<string, PlayerSkill> InitializePlayerSkills()
    {
        // Initialize with some sample player data
        return new Dictionary<string, PlayerSkill>
        {
            ["player1"] = new PlayerSkill(
                playerId: "player1",
                rating: 1650.0,
                volatility: 0.045,
                characterRatings: new Dictionary<string, double>
                {
                    ["Ryu"] = 1700,
                    ["Ken"] = 1680,
                    ["Guile"] = 1550
                },
                lastUpdated: _timeProvider.UtcNow.AddHours(-2)),

            ["player2"] = new PlayerSkill(
                playerId: "player2",
                rating: 1580.0,
                volatility: 0.052,
                characterRatings: new Dictionary<string, double>
                {
                    ["Chun-Li"] = 1620,
                    ["Blanka"] = 1600,
                    ["Dhalsim"] = 1500
                },
                lastUpdated: _timeProvider.UtcNow.AddHours(-1)),

            ["player3"] = new PlayerSkill(
                playerId: "player3",
                rating: 1720.0,
                volatility: 0.038,
                characterRatings: new Dictionary<string, double>
                {
                    ["Zangief"] = 1750,
                    ["Sagat"] = 1700,
                    ["Ryu"] = 1680
                },
                lastUpdated: _timeProvider.UtcNow.AddMinutes(-30))
        };
    }
}
