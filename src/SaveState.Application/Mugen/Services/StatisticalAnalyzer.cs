using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Comprehensive statistical analyzer for MUGEN match data.
/// Provides detailed statistics on player performance, character matchups, and game patterns.
/// </summary>
public class StatisticalAnalyzer
{
    private readonly ILogger<StatisticalAnalyzer> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, CharacterStatistics> _characterStats = new();

    public StatisticalAnalyzer(ILogger<StatisticalAnalyzer> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerStatistics> CalculatePlayerStatisticsAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating statistics for player {PlayerId} from {MatchCount} matches",
                playerId, matches.Count);

            if (!matches.Any())
            {
                return CreateEmptyStatistics(playerId);
            }

            // Calculate basic win/loss statistics
            var wins = matches.Count(m => m.Rounds.Last().WinnerId == playerId);
            var losses = matches.Count - wins;
            var winRate = matches.Any() ? (decimal)wins / matches.Count : 0;

            var currentStreak = CalculateCurrentStreak(playerId, matches);

            // Calculate character-specific statistics
            var characterStats = await CalculateCharacterStatisticsAsync(playerId, matches, ct);

            // Calculate achievements
            var achievements = await CalculateAchievementsAsync(playerId, matches, characterStats, ct);

            // Calculate ranking information
            var ranking = await CalculatePlayerRankingAsync(playerId, matches, characterStats, ct);

            return new PlayerStatistics(
                PlayerId: playerId,
                TotalMatches: matches.Count,
                Wins: wins,
                Losses: losses,
                WinRate: winRate,
                CharacterStats: characterStats,
                Achievements: achievements,
                Ranking: ranking
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for player {PlayerId}", playerId);
            return CreateEmptyStatistics(playerId);
        }
    }

    public async Task UpdateModelsAsync(MatchRecording matchData, CancellationToken ct = default)
    {
        try
        {
            // Update character statistics models
            await UpdateCharacterStatisticsAsync(matchData.Player1Id, matchData.Player1Character, matchData, ct);
            await UpdateCharacterStatisticsAsync(matchData.Player2Id, matchData.Player2Character, matchData, ct);

            _logger.LogDebug("Updated statistical models with match {MatchId}", matchData.MatchId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating statistical models for match {MatchId}", matchData.MatchId);
        }
    }

    private async Task<Dictionary<string, CharacterAnalyticsStats>> CalculateCharacterStatisticsAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct)
    {
        var characterStats = new Dictionary<string, CharacterAnalyticsStats>();

        // Group matches by character used by this player
        var characterMatches = new Dictionary<string, List<MatchRecording>>();

        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;
            var character = isPlayer1 ? match.Player1Character : match.Player2Character;

            if (!characterMatches.ContainsKey(character))
            {
                characterMatches[character] = new List<MatchRecording>();
            }

            characterMatches[character].Add(match);
        }

        // Calculate statistics for each character
        foreach (var (character, charMatches) in characterMatches)
        {
            var stats = await CalculateCharacterStatsAsync(playerId, character, charMatches, ct);
            characterStats[character] = stats;
        }

        return characterStats;
    }

    private async Task<CharacterAnalyticsStats> CalculateCharacterStatsAsync(
        Guid playerId,
        string character,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct)
    {
        var wins = matches.Count(m => m.Rounds.Last().WinnerId == playerId);
        var losses = matches.Count - wins;
        var winRate = matches.Any() ? (decimal)wins / matches.Count : 0;

        // Calculate damage statistics
        var totalDamageDealt = 0;
        var totalDamageReceived = 0;
        var longestCombo = 0;
        var moveUsage = new Dictionary<string, int>();

        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;

            foreach (var round in match.Rounds)
            {
                // Damage dealt
                totalDamageDealt += round.Hits
                    .Where(h => h.AttackerId == playerId)
                    .Sum(h => h.Damage);

                // Damage received
                totalDamageReceived += round.Hits
                    .Where(h => h.DefenderId == playerId)
                    .Sum(h => h.Damage);

                // Longest combo
                var playerCombos = round.Combos.Where(c => c.PlayerId == playerId);
                if (playerCombos.Any())
                {
                    longestCombo = Math.Max(longestCombo, playerCombos.Max(c => c.Length));
                }

                // Move usage statistics
                foreach (var hit in round.Hits.Where(h => h.AttackerId == playerId))
                {
                    if (!moveUsage.ContainsKey(hit.MoveName))
                    {
                        moveUsage[hit.MoveName] = 0;
                    }
                    moveUsage[hit.MoveName]++;
                }
            }
        }

        // Calculate average combo length
        var allCombos = matches.SelectMany(m => m.Rounds.SelectMany(r => r.Combos.Where(c => c.PlayerId == playerId)));
        var averageComboLength = allCombos.Any() ? (int)allCombos.Average(c => c.Length) : 0;

        // Get most used moves
        var mostUsedMoves = moveUsage
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp => kvp.Key)
            .ToList();

        return new CharacterAnalyticsStats(
            CharacterName: character,
            MatchesPlayed: matches.Count,
            Wins: wins,
            Losses: losses,
            WinRate: winRate,
            TotalDamageDealt: totalDamageDealt,
            AverageComboLength: averageComboLength,
            MostUsedMoves: mostUsedMoves
        );
    }

    private async Task<IReadOnlyList<Achievement>> CalculateAchievementsAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        IReadOnlyDictionary<string, CharacterAnalyticsStats> characterStats,
        CancellationToken ct)
    {
        var achievements = new List<Achievement>();

        // First Victory achievement
        if (matches.Any(m => m.Rounds.Last().WinnerId == playerId))
        {
            achievements.Add(new Achievement(
                Name: "First Victory",
                Description: "Won your first match",
                UnlockedAt: matches.First(m => m.Rounds.Last().WinnerId == playerId).EndTime,
                Rarity: AchievementRarity.Common
            ));
        }

        // Combo Master achievement
        var hasLongCombos = matches.Any(m => m.Rounds.Any(r =>
            r.Combos.Any(c => c.PlayerId == playerId && c.Length >= 10)));

        if (hasLongCombos)
        {
            achievements.Add(new Achievement(
                Name: "Combo Master",
                Description: "Executed a 10+ hit combo",
                UnlockedAt: matches.First(m => m.Rounds.Any(r =>
                    r.Combos.Any(c => c.PlayerId == playerId && c.Length >= 10))).EndTime,
                Rarity: AchievementRarity.Rare
            ));
        }

        // Character Collector achievement
        if (characterStats.Count >= 5)
        {
            achievements.Add(new Achievement(
                Name: "Character Collector",
                Description: "Played 5 different characters",
                UnlockedAt: _timeProvider.UtcNow,
                Rarity: AchievementRarity.Rare
            ));
        }

        // High Win Rate achievement
        var overallWinRate = matches.Any() ? matches.Count(m => m.Rounds.Last().WinnerId == playerId) / (decimal)matches.Count : 0;
        if (overallWinRate >= 0.8m && matches.Count >= 10)
        {
            achievements.Add(new Achievement(
                Name: "Winning Streak",
                Description: "Achieved 80%+ win rate with 10+ matches",
                UnlockedAt: _timeProvider.UtcNow,
                Rarity: AchievementRarity.Epic
            ));
        }

        // Damage Dealer achievement
        var totalDamageDealt = matches.Sum(m => m.Rounds.Sum(r =>
            r.Hits.Where(h => h.AttackerId == playerId).Sum(h => h.Damage)));

        if (totalDamageDealt >= 10000)
        {
            achievements.Add(new Achievement(
                Name: "Damage Dealer",
                Description: "Dealt 10,000+ total damage",
                UnlockedAt: _timeProvider.UtcNow,
                Rarity: AchievementRarity.Rare
            ));
        }

        return achievements;
    }

    private async Task<PlayerRanking> CalculatePlayerRankingAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        IReadOnlyDictionary<string, CharacterAnalyticsStats> characterStats,
        CancellationToken ct)
    {
        // Simplified ranking calculation
        // In a real implementation, this would use Elo ratings and global rankings

        var totalMatches = matches.Count;
        var wins = matches.Count(m => m.Rounds.Last().WinnerId == playerId);
        var winRate = totalMatches > 0 ? (decimal)wins / totalMatches : 0;

        // Calculate rating based on win rate and match count
        var baseRating = 1000;
        var rating = baseRating + (int)((winRate - 0.5m) * 400) + Math.Min(totalMatches * 5, 200);

        var tier = GetTierFromRating(rating);

        // Calculate ranked stats for different game modes
        var rankedStats = new List<RankedStats>
        {
            new RankedStats(
                GameMode: "All Matches",
                Rank: CalculateGlobalRank(rating),
                Rating: rating,
                Wins: wins,
                Losses: totalMatches - wins,
                WinRate: winRate
            )
        };

        return new PlayerRanking(
            GlobalRank: CalculateGlobalRank(rating),
            RegionalRank: CalculateRegionalRank(rating),
            Rating: rating,
            Tier: tier,
            RankedStats: rankedStats
        );
    }

    private int CalculateCurrentStreak(Guid playerId, IReadOnlyList<MatchRecording> matches)
    {
        var orderedMatches = matches.OrderByDescending(m => m.EndTime).ToList();
        var streak = 0;

        foreach (var match in orderedMatches)
        {
            var won = match.Rounds.Last().WinnerId == playerId;
            if (won)
            {
                streak++;
            }
            else
            {
                streak--;
                break; // Streak broken
            }
        }

        return streak;
    }

    private async Task UpdateCharacterStatisticsAsync(
        Guid playerId,
        string character,
        MatchRecording matchData,
        CancellationToken ct)
    {
        var key = $"{playerId}_{character}";

        if (!_characterStats.ContainsKey(key))
        {
            _characterStats[key] = new CharacterStatistics
            {
                PlayerId = playerId,
                CharacterName = character
            };
        }

        var stats = _characterStats[key];

        // Update match count
        stats.TotalMatches++;

        // Update win/loss
        var won = matchData.Rounds.Last().WinnerId == playerId;
        if (won) stats.Wins++;
        else stats.Losses++;

        // Update damage stats
        foreach (var round in matchData.Rounds)
        {
            stats.TotalDamageDealt += round.Hits
                .Where(h => h.AttackerId == playerId)
                .Sum(h => h.Damage);

            stats.TotalDamageReceived += round.Hits
                .Where(h => h.DefenderId == playerId)
                .Sum(h => h.Damage);
        }

        // Update combo stats
        var playerCombos = matchData.Rounds.SelectMany(r => r.Combos.Where(c => c.PlayerId == playerId));
        if (playerCombos.Any())
        {
            stats.TotalCombos += playerCombos.Count();
            stats.LongestCombo = Math.Max(stats.LongestCombo, playerCombos.Max(c => c.Length));
        }
    }

    private string GetTierFromRating(int rating) => rating switch
    {
        >= 3000 => "Grandmaster",
        >= 2800 => "Master",
        >= 2600 => "Diamond",
        >= 2400 => "Platinum",
        >= 2200 => "Gold",
        >= 2000 => "Silver",
        >= 1800 => "Bronze",
        _ => "Unranked"
    };

    private int CalculateGlobalRank(int rating)
    {
        // Simplified global ranking calculation
        // In reality, this would be based on comparison with all players
        return Math.Max(1, 10000 - rating / 10);
    }

    private int CalculateRegionalRank(int rating)
    {
        // Simplified regional ranking
        return Math.Max(1, 1000 - rating / 20);
    }

    private PlayerStatistics CreateEmptyStatistics(Guid playerId)
    {
        return new PlayerStatistics(
            PlayerId: playerId,
            TotalMatches: 0,
            Wins: 0,
            Losses: 0,
            WinRate: 0,
            CharacterStats: new Dictionary<string, CharacterAnalyticsStats>(),
            Achievements: Array.Empty<Achievement>(),
            Ranking: new PlayerRanking(
                GlobalRank: 0,
                RegionalRank: 0,
                Rating: 1000,
                Tier: "Unranked",
                RankedStats: Array.Empty<RankedStats>()
            )
        );
    }

    private class CharacterStatistics
    {
        public Guid PlayerId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalDamageReceived { get; set; }
        public int TotalCombos { get; set; }
        public int LongestCombo { get; set; }
    }
}
