namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of the MUGEN statistics service.
/// Tracks and retrieves match statistics and character performance.
/// </summary>
public class MugenStatsService : IMugenStatsService
{
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly SaveState.Core.Mugen.IMugenMatchHistoryRepository _matchHistoryRepository;

    public MugenStatsService(
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        SaveState.Core.Mugen.IMugenMatchHistoryRepository matchHistoryRepository)
    {
        _characterRepository = characterRepository;
        _matchHistoryRepository = matchHistoryRepository;
    }

    public async Task<Result<GlobalStats>> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var histories = await _matchHistoryRepository.GetMatchHistoriesAsync(pageNumber: 1, pageSize: 1000, ct: ct);
            var matches = histories.Items;

            if (!matches.Any())
            {
                return Result.Success(new GlobalStats(0, 0, 0, "None", 0f));
            }

            var totalMatches = matches.Count;
            var wins = matches.Count(m => m.Result == MatchResult.Player1Win || m.Result == MatchResult.Player2Win); // Total wins is just total matches if no draws
            var totalWins = matches.Count(m => m.Result == MatchResult.Player1Win); // Just a heuristic
            var totalLosses = totalMatches - totalWins;

            var charUsage = matches
                .SelectMany(m => new[] { m.Player1CharacterId, m.Player2CharacterId })
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            string? mostPlayedName = "Unknown";
            if (charUsage != null)
            {
                var charResult = await _characterRepository.GetByIdAsync(charUsage.Key, ct);
                mostPlayedName = charResult.IsSuccess ? charResult.Value?.DisplayName : "Unknown";
            }

            float highestWinRate = 0f;
            // Simplified high win rate check
            var stats = matches
                .SelectMany(m => new[] {
                    (Id: m.Player1CharacterId, Win: m.Result == MatchResult.Player1Win),
                    (Id: m.Player2CharacterId, Win: m.Result == MatchResult.Player2Win) })
                .GroupBy(x => x.Id)
                .Select(g => (float)g.Count(x => x.Win) / g.Count())
                .ToList();

            if (stats.Any()) highestWinRate = stats.Max();

            return Result.Success(new GlobalStats(totalMatches, totalWins, totalLosses, mostPlayedName, highestWinRate));
        }
        catch (Exception ex)
        {
            return Result.Failure<GlobalStats>($"Failed to calculate global stats: {ex.Message}");
        }
    }

    public async Task<Result<CharacterStats>> GetCharacterStatsAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure || characterResult.Value is null)
                return Result.Failure<CharacterStats>("Character not found");
            var character = characterResult.Value;

            // Load actual match history from database
            var matches = await _matchHistoryRepository.GetByCharacterAsync(characterId, limit: 1000, ct);

            if (!matches.Any())
            {
                // Return empty stats if no matches
                var emptyStats = new CharacterStats(
                    characterId,
                    character.Name,
                    0, 0, 0, 0f, TimeSpan.Zero, null, null);
                return Result.Success<CharacterStats>(emptyStats);
            }

            // Calculate statistics
            var totalMatches = matches.Count;
            var wins = matches.Count(m =>
                (m.Player1CharacterId == characterId && m.Result == MatchResult.Player1Win) ||
                (m.Player2CharacterId == characterId && m.Result == MatchResult.Player2Win));
            var losses = totalMatches - wins;
            var winRate = totalMatches > 0 ? (float)wins / totalMatches : 0f;
            var totalPlaytime = matches.Aggregate(TimeSpan.Zero, (sum, m) => sum + m.MatchDuration);

            // Find best and worst matchups
            var opponentStats = matches
                .Where(m => m.Player1CharacterId == characterId || m.Player2CharacterId == characterId)
                .GroupBy(m => m.Player1CharacterId == characterId ? m.Player2CharacterId : m.Player1CharacterId)
                .Select(g => new
                {
                    OpponentId = g.Key,
                    OpponentMatches = g.ToList(),
                    Wins = g.Count(m =>
                        (m.Player1CharacterId == characterId && m.Result == MatchResult.Player1Win) ||
                        (m.Player2CharacterId == characterId && m.Result == MatchResult.Player2Win))
                })
                .Where(x => x.OpponentMatches.Count >= 3) // Minimum 3 matches for meaningful stats
                .ToList();

            var bestMatchup = opponentStats
                .OrderByDescending(x => (float)x.Wins / x.OpponentMatches.Count)
                .ThenByDescending(x => x.OpponentMatches.Count)
                .FirstOrDefault()?.OpponentId;

            var worstMatchup = opponentStats
                .OrderBy(x => (float)x.Wins / x.OpponentMatches.Count)
                .ThenByDescending(x => x.OpponentMatches.Count)
                .FirstOrDefault()?.OpponentId;

            var stats = new CharacterStats(
                characterId,
                character.Name,
                totalMatches,
                wins,
                losses,
                winRate,
                totalPlaytime,
                bestMatchup,
                worstMatchup);

            return Result.Success<CharacterStats>(stats);
        }
        catch (Exception ex)
        {
            return Result.Failure<CharacterStats>($"Failed to get character stats: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MatchupStats>>> GetMatchupStatsAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            // Load matchup statistics from database
            var matches = await _matchHistoryRepository.GetByCharacterAsync(characterId, limit: 1000, ct);

            if (!matches.Any())
            {
                return Result.Success<IReadOnlyList<MatchupStats>>(new List<MatchupStats>());
            }

            // Group by opponent and calculate stats
            var opponentStats = matches
                .Where(m => m.Player1CharacterId == characterId || m.Player2CharacterId == characterId)
                .GroupBy(m => m.Player1CharacterId == characterId ? m.Player2CharacterId : m.Player1CharacterId)
                .Select(async g =>
                {
                    var opponentId = g.Key;
                    var opponentMatches = g.ToList();
                    var wins = opponentMatches.Count(m =>
                        (m.Player1CharacterId == characterId && m.Result == MatchResult.Player1Win) ||
                        (m.Player2CharacterId == characterId && m.Result == MatchResult.Player2Win));
                    var losses = opponentMatches.Count - wins;
                    var winRate = opponentMatches.Count > 0 ? (float)wins / opponentMatches.Count : 0f;

                    // Get opponent character name
                    var opponentCharacterResult = await _characterRepository.GetByIdAsync(opponentId, ct);
                    var opponentName = opponentCharacterResult.IsSuccess && opponentCharacterResult.Value is not null 
                        ? opponentCharacterResult.Value.Name 
                        : $"Character {opponentId}";

                    return new MatchupStats(opponentId, opponentName, wins, losses, winRate);
                })
                .ToList();

            // Wait for all async operations to complete
            var matchupStats = await Task.WhenAll(opponentStats);

            // Sort by total matches (most played first), then by win rate
            var sortedStats = matchupStats
                .OrderByDescending(m => m.Wins + m.Losses)
                .ThenByDescending(m => m.WinRate)
                .ToList();

            return Result.Success<IReadOnlyList<MatchupStats>>(sortedStats);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MatchupStats>>($"Failed to get matchup stats: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MugenMatchHistory>>> GetRecentMatchesAsync(
        int count = 20,
        CancellationToken ct = default)
    {
        try
        {
            // Load actual match history from database
            var pagedResult = await _matchHistoryRepository.GetMatchHistoriesAsync(
                pageNumber: 1,
                pageSize: count,
                ct: ct);

            return Result.Success<IReadOnlyList<MugenMatchHistory>>(pagedResult.Items);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenMatchHistory>>($"Failed to get recent matches: {ex.Message}");
        }
    }

    public async Task<Result> RecordMatchAsync(
        MugenMatchHistory match,
        CancellationToken ct = default)
    {
        try
        {
            // Persist match to database
            var result = await _matchHistoryRepository.RecordMatchAsync(match, ct);
            if (result.IsFailure)
                return Result.Failure($"Failed to record match: {result.Error}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to record match: {ex.Message}");
        }
    }
}

