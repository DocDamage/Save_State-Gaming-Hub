namespace SaveState.Application.Mugen.Services.MatchAnalytics.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for calculating player statistics from match data.
/// </summary>
public class StatisticEngine
{
    private readonly ILogger<StatisticEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public StatisticEngine(ILogger<StatisticEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Calculates comprehensive player statistics from a collection of matches.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="matches">The matches to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Calculated player statistics.</returns>
    public async Task<PlayerStatistics> CalculatePlayerStatisticsAsync(
        Guid playerId,
        IReadOnlyList<MatchData> matches,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating statistics for player {PlayerId} from {MatchCount} matches",
            playerId, matches.Count);

        if (!matches.Any())
        {
            throw new ArgumentException("At least one match is required for statistics calculation", nameof(matches));
        }

        // Calculate basic stats
        var totalMatches = matches.Count;
        var wins = matches.Count(m => m.Rounds.LastOrDefault()?.WinnerId == playerId);
        var losses = totalMatches - wins;
        var winRate = totalMatches > 0 ? (decimal)wins / totalMatches * 100 : 0;

        // Calculate character stats
        var characterStats = CalculateCharacterStats(playerId, matches);

        // Calculate achievements
        var achievements = CalculateAchievements(playerId, matches, wins);

        // Calculate ranking info
        var ranking = CalculateRankingInfo(playerId, matches, wins, winRate);

        var statistics = new PlayerStatistics(
            PlayerId: playerId,
            TotalMatches: totalMatches,
            Wins: wins,
            Losses: losses,
            WinRate: winRate,
            CharacterStats: characterStats,
            Achievements: achievements,
            Ranking: ranking
        );

        _logger.LogInformation("Statistics calculated for player {PlayerId}: {Wins}W/{Losses}L ({WinRate:F1}% win rate)",
            playerId, wins, losses, winRate);

        await Task.CompletedTask; // Placeholder for potential async operations
        return statistics;
    }

    private IReadOnlyDictionary<string, CharacterStats> CalculateCharacterStats(Guid playerId, IReadOnlyList<MatchData> matches)
    {
        var characterData = new Dictionary<string, CharacterStatistics>();

        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;
            var characterName = isPlayer1 ? match.Player1Character : match.Player2Character;
            var opponentId = isPlayer1 ? match.Player2Id : match.Player1Id;

            if (!characterData.ContainsKey(characterName))
            {
                characterData[characterName] = new CharacterStatistics
                {
                    PlayerId = playerId,
                    CharacterName = characterName,
                    TotalMatches = 0,
                    Wins = 0,
                    Losses = 0,
                    TotalDamageDealt = 0,
                    TotalDamageReceived = 0,
                    TotalCombos = 0,
                    LongestCombo = 0
                };
            }

            var stats = characterData[characterName];
            stats.TotalMatches++;

            // Check if won
            var lastRound = match.Rounds.LastOrDefault();
            if (lastRound?.WinnerId == playerId)
            {
                stats.Wins++;
            }
            else
            {
                stats.Losses++;
            }

            // Calculate damage dealt/received
            foreach (var round in match.Rounds)
            {
                stats.TotalDamageDealt += round.Hits
                    .Where(h => h.AttackerId == playerId)
                    .Sum(h => h.Damage);

                stats.TotalDamageReceived += round.Hits
                    .Where(h => h.DefenderId == playerId)
                    .Sum(h => h.Damage);

                // Calculate combos
                var roundCombos = round.Combos.Where(c => c.PlayerId == playerId).ToList();
                stats.TotalCombos += roundCombos.Count;

                var longestInRound = roundCombos.Any() ? roundCombos.Max(c => c.Length) : 0;
                if (longestInRound > stats.LongestCombo)
                {
                    stats.LongestCombo = longestInRound;
                }
            }
        }

        // Convert to immutable CharacterStats records
        return characterData.ToDictionary(
            kvp => kvp.Key,
            kvp => new CharacterStats(
                CharacterName: kvp.Value.CharacterName,
                MatchesPlayed: kvp.Value.TotalMatches,
                Wins: kvp.Value.Wins,
                Losses: kvp.Value.Losses,
                WinRate: kvp.Value.TotalMatches > 0 ? (decimal)kvp.Value.Wins / kvp.Value.TotalMatches * 100 : 0,
                TotalDamageDealt: kvp.Value.TotalDamageDealt,
                AverageComboLength: kvp.Value.TotalCombos > 0 ? (decimal)kvp.Value.TotalCombos / kvp.Value.TotalMatches : 0,
                MostUsedMoves: ExtractMostUsedMoves(playerId, matches, kvp.Key)
            ));
    }

    private IReadOnlyList<string> ExtractMostUsedMoves(Guid playerId, IReadOnlyList<MatchData> matches, string characterName)
    {
        var moveCounts = new Dictionary<string, int>();

        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;
            var playerCharacter = isPlayer1 ? match.Player1Character : match.Player2Character;

            if (playerCharacter != characterName)
                continue;

            foreach (var round in match.Rounds)
            {
                foreach (var hit in round.Hits.Where(h => h.AttackerId == playerId))
                {
                    if (!moveCounts.ContainsKey(hit.MoveName))
                    {
                        moveCounts[hit.MoveName] = 0;
                    }
                    moveCounts[hit.MoveName]++;
                }

                foreach (var special in round.SpecialMoves.Where(sm => sm.PlayerId == playerId))
                {
                    if (!moveCounts.ContainsKey(special.MoveName))
                    {
                        moveCounts[special.MoveName] = 0;
                    }
                    moveCounts[special.MoveName]++;
                }
            }
        }

        return moveCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private IReadOnlyList<AchievementData> CalculateAchievements(Guid playerId, IReadOnlyList<MatchData> matches, int wins)
    {
        var achievements = new List<AchievementData>();

        // First win achievement
        if (wins >= 1)
        {
            var firstWinMatch = matches.FirstOrDefault(m => m.Rounds.LastOrDefault()?.WinnerId == playerId);
            if (firstWinMatch != null)
            {
                achievements.Add(new AchievementData(
                    Name: "First Victory",
                    Description: "Win your first match",
                    UnlockedAt: firstWinMatch.EndTime,
                    Rarity: AchievementRarity.Common
                ));
            }
        }

        // 10 wins achievement
        if (wins >= 10)
        {
            var tenthWinMatch = matches
                .Where(m => m.Rounds.LastOrDefault()?.WinnerId == playerId)
                .Skip(9)
                .FirstOrDefault();

            if (tenthWinMatch != null)
            {
                achievements.Add(new AchievementData(
                    Name: "Getting Started",
                    Description: "Win 10 matches",
                    UnlockedAt: tenthWinMatch.EndTime,
                    Rarity: AchievementRarity.Common
                ));
            }
        }

        // 50 wins achievement
        if (wins >= 50)
        {
            var fiftiethWinMatch = matches
                .Where(m => m.Rounds.LastOrDefault()?.WinnerId == playerId)
                .Skip(49)
                .FirstOrDefault();

            if (fiftiethWinMatch != null)
            {
                achievements.Add(new AchievementData(
                    Name: "Veteran Fighter",
                    Description: "Win 50 matches",
                    UnlockedAt: fiftiethWinMatch.EndTime,
                    Rarity: AchievementRarity.Rare
                ));
            }
        }

        // Combo master achievement
        var hasLongCombo = matches.Any(m =>
            m.Rounds.SelectMany(r => r.Combos)
                .Any(c => c.PlayerId == playerId && c.Length >= 10));

        if (hasLongCombo)
        {
            achievements.Add(new AchievementData(
                Name: "Combo Master",
                Description: "Execute a 10+ hit combo",
                UnlockedAt: _timeProvider.UtcNow, // Use current time as we don't track exact moment
                Rarity: AchievementRarity.Rare
            ));
        }

        // Perfect round achievement (no damage received in a won round)
        var hasPerfectRound = matches.Any(m =>
            m.Rounds.Any(r =>
                r.WinnerId == playerId &&
                !r.Hits.Any(h => h.DefenderId == playerId)));

        if (hasPerfectRound)
        {
            achievements.Add(new AchievementData(
                Name: "Flawless Victory",
                Description: "Win a round without taking damage",
                UnlockedAt: _timeProvider.UtcNow,
                Rarity: AchievementRarity.Epic
            ));
        }

        // Comeback achievement (won match after losing multiple rounds)
        var hasComeback = matches.Any(m =>
        {
            var roundsLost = m.Rounds.Count(r => r.WinnerId != playerId && r.WinnerId != Guid.Empty);
            var wonMatch = m.Rounds.LastOrDefault()?.WinnerId == playerId;
            return roundsLost >= 2 && wonMatch;
        });

        if (hasComeback)
        {
            achievements.Add(new AchievementData(
                Name: "Never Give Up",
                Description: "Win a match after losing 2+ rounds",
                UnlockedAt: _timeProvider.UtcNow,
                Rarity: AchievementRarity.Epic
            ));
        }

        return achievements;
    }

    private RankingInfo CalculateRankingInfo(Guid playerId, IReadOnlyList<MatchData> matches, int wins, decimal winRate)
    {
        // Calculate a simple rating based on wins and win rate
        var rating = wins * 10 + (int)(winRate * 2);

        // Determine tier based on rating
        var tier = rating switch
        {
            >= 1000 => "S",
            >= 800 => "A",
            >= 600 => "B",
            >= 400 => "C",
            >= 200 => "D",
            _ => "E"
        };

        // Calculate global rank (simulated - in real implementation would query leaderboard)
        var globalRank = Math.Max(1, 10000 - rating + new Random(playerId.GetHashCode()).Next(1, 100));
        var regionalRank = Math.Max(1, globalRank / 10);

        // Calculate ranked stats by game mode
        var rankedStats = matches
            .GroupBy(m => m.Metadata.GameMode)
            .Select(g =>
            {
                var modeMatches = g.ToList();
                var modeWins = modeMatches.Count(m => m.Rounds.LastOrDefault()?.WinnerId == playerId);
                var modeLosses = modeMatches.Count - modeWins;
                var modeWinRate = modeMatches.Count > 0 ? (decimal)modeWins / modeMatches.Count * 100 : 0;
                var modeRating = modeWins * 10 + (int)(modeWinRate * 2);

                return new RankedStats(
                    GameMode: g.Key,
                    Rank: Math.Max(1, 1000 - modeRating),
                    Rating: modeRating,
                    Wins: modeWins,
                    Losses: modeLosses,
                    WinRate: modeWinRate
                );
            })
            .ToList();

        return new RankingInfo(
            GlobalRank: globalRank,
            RegionalRank: regionalRank,
            Rating: rating,
            Tier: tier,
            RankedStats: rankedStats
        );
    }
}
