using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Infrastructure.Mugen.Repositories;

/// <summary>
/// Repository for storing and retrieving match data for ML training.
/// </summary>
public class MugenMatchDataRepository : IMatchDataRepository
{
    private readonly ILogger<MugenMatchDataRepository> _logger;
    private readonly List<MLMatchResult> _matchHistory = new();

    public MugenMatchDataRepository(ILogger<MugenMatchDataRepository> logger)
    {
        _logger = logger;
        // Initialize with some sample data
        InitializeSampleData();
    }

    public Task<Result<IReadOnlyList<MLMatchResult>>> GetRecentMatchesAsync(int count, CancellationToken ct = default)
    {
        try
        {
            var recentMatches = _matchHistory
                .OrderByDescending(m => m.Duration) // Using duration as a proxy for recency, should be PlayedAt ideally but MLMatchResult doesn't have it.
                .Take(count)
                .ToList();

            _logger.LogInformation("Retrieved {Count} recent matches", recentMatches.Count);
            return Task.FromResult(Result.Success<IReadOnlyList<MLMatchResult>>(recentMatches));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent matches");
            return Task.FromResult(Result.Failure<IReadOnlyList<MLMatchResult>>($"Failed to get matches: {ex.Message}"));
        }
    }

    public Task<Result> SaveMatchResultAsync(MLMatchResult result, CancellationToken ct = default)
    {
        try
        {
            _matchHistory.Add(result);

            // Keep only recent matches (last 1000)
            if (_matchHistory.Count > 1000)
            {
                _matchHistory.RemoveAt(0);
            }

            _logger.LogInformation("Saved match result: {Winner} defeated {Loser}",
                result.WinnerCharacter, result.LoserCharacter);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving match result");
            return Task.FromResult(Result.Failure($"Failed to save match: {ex.Message}"));
        }
    }

    private void InitializeSampleData()
    {
        // Add some sample match data for ML training
        var sampleMatches = new[]
        {
            new MLMatchResult(
                WinnerId: "player1",
                LoserId: "player2",
                WinnerCharacter: "Ryu",
                LoserCharacter: "Ken",
                Duration: TimeSpan.FromSeconds(85),
                RoundsWonByWinner: 2,
                RoundsWonByLoser: 0,
                Outcome: MatchResult.Player1Win),

            new MLMatchResult(
                WinnerId: "player2",
                LoserId: "player1",
                WinnerCharacter: "Guile",
                LoserCharacter: "Ryu",
                Duration: TimeSpan.FromSeconds(120),
                RoundsWonByWinner: 2,
                RoundsWonByLoser: 1,
                Outcome: MatchResult.Player2Win),

            new MLMatchResult(
                WinnerId: "player1",
                LoserId: "player3",
                WinnerCharacter: "Chun-Li",
                LoserCharacter: "Blanka",
                Duration: TimeSpan.FromSeconds(95),
                RoundsWonByWinner: 2,
                RoundsWonByLoser: 0,
                Outcome: MatchResult.Player1Win),

            new MLMatchResult(
                WinnerId: "player3",
                LoserId: "player2",
                WinnerCharacter: "Zangief",
                LoserCharacter: "Dhalsim",
                Duration: TimeSpan.FromSeconds(110),
                RoundsWonByWinner: 2,
                RoundsWonByLoser: 1,
                Outcome: MatchResult.Player1Win)
        };

        _matchHistory.AddRange(sampleMatches);
        _logger.LogInformation("Initialized with {Count} sample matches", sampleMatches.Length);
    }
}
