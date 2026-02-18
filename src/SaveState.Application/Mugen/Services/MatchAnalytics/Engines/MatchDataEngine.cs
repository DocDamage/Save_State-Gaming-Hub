namespace SaveState.Application.Mugen.Services.MatchAnalytics.Engines;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for managing match data storage, retrieval, and validation.
/// </summary>
public class MatchDataEngine
{
    private readonly ILogger<MatchDataEngine> _logger;
    private readonly ConcurrentDictionary<string, MatchData> _matches = new();
    private readonly Dictionary<Guid, List<string>> _playerMatches = new();

    public MatchDataEngine(ILogger<MatchDataEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates match data for completeness and correctness.
    /// </summary>
    /// <param name="matchData">The match data to validate.</param>
    /// <returns>Validation result with IsValid flag and list of errors.</returns>
    public MatchValidationResult ValidateMatchData(MatchData matchData)
    {
        var errors = new List<string>();

        if (matchData.MatchId == Guid.Empty)
        {
            errors.Add("MatchId cannot be empty");
        }

        if (matchData.Player1Id == Guid.Empty)
        {
            errors.Add("Player1Id cannot be empty");
        }

        if (matchData.Player2Id == Guid.Empty)
        {
            errors.Add("Player2Id cannot be empty");
        }

        if (matchData.Player1Id == matchData.Player2Id)
        {
            errors.Add("Player1 and Player2 cannot be the same");
        }

        if (string.IsNullOrWhiteSpace(matchData.Player1Character))
        {
            errors.Add("Player1Character cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(matchData.Player2Character))
        {
            errors.Add("Player2Character cannot be empty");
        }

        if (matchData.StartTime == default)
        {
            errors.Add("StartTime must be specified");
        }

        if (matchData.EndTime == default)
        {
            errors.Add("EndTime must be specified");
        }

        if (matchData.EndTime < matchData.StartTime)
        {
            errors.Add("EndTime cannot be earlier than StartTime");
        }

        if (matchData.Rounds == null || !matchData.Rounds.Any())
        {
            errors.Add("At least one round must be recorded");
        }

        if (matchData.InputEvents == null)
        {
            errors.Add("InputEvents cannot be null");
        }

        if (matchData.Metadata == null)
        {
            errors.Add("Metadata cannot be null");
        }

        var isValid = errors.Count == 0;

        if (!isValid)
        {
            _logger.LogWarning("Match data validation failed for match {MatchId}: {Errors}",
                matchData.MatchId, string.Join(", ", errors));
        }

        return new MatchValidationResult(isValid, errors);
    }

    /// <summary>
    /// Records a match in the storage system.
    /// </summary>
    /// <param name="matchData">The match data to record.</param>
    public void RecordMatch(MatchData matchData)
    {
        var matchKey = matchData.MatchId.ToString();

        // Store in matches dictionary
        _matches[matchKey] = matchData;

        // Update player indexes
        lock (_playerMatches)
        {
            // Index for Player1
            if (!_playerMatches.ContainsKey(matchData.Player1Id))
            {
                _playerMatches[matchData.Player1Id] = new List<string>();
            }
            if (!_playerMatches[matchData.Player1Id].Contains(matchKey))
            {
                _playerMatches[matchData.Player1Id].Add(matchKey);
            }

            // Index for Player2
            if (!_playerMatches.ContainsKey(matchData.Player2Id))
            {
                _playerMatches[matchData.Player2Id] = new List<string>();
            }
            if (!_playerMatches[matchData.Player2Id].Contains(matchKey))
            {
                _playerMatches[matchData.Player2Id].Add(matchKey);
            }
        }

        _logger.LogInformation("Recorded match {MatchId} between players {Player1Id} and {Player2Id}",
            matchData.MatchId, matchData.Player1Id, matchData.Player2Id);
    }

    /// <summary>
    /// Finds a match by its unique identifier.
    /// </summary>
    /// <param name="matchId">The match ID to search for.</param>
    /// <returns>The match data if found, null otherwise.</returns>
    public MatchData? FindMatch(Guid matchId)
    {
        var matchKey = matchId.ToString();

        if (_matches.TryGetValue(matchKey, out var matchData))
        {
            return matchData;
        }

        _logger.LogDebug("Match {MatchId} not found", matchId);
        return null;
    }

    /// <summary>
    /// Gets all matches for a specific player.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="limit">Maximum number of matches to return (default 50).</param>
    /// <returns>List of match data for the player.</returns>
    public IReadOnlyList<MatchData> GetPlayerMatches(Guid playerId, int limit = 50)
    {
        lock (_playerMatches)
        {
            if (!_playerMatches.TryGetValue(playerId, out var matchKeys))
            {
                return Array.Empty<MatchData>();
            }

            var matches = matchKeys
                .Take(limit)
                .Select(key => _matches.TryGetValue(key, out var match) ? match : null)
                .Where(m => m != null)
                .Cast<MatchData>()
                .ToList();

            return matches;
        }
    }

    /// <summary>
    /// Gets matches for a player within a specific date range.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="start">Start date/time.</param>
    /// <param name="end">End date/time.</param>
    /// <returns>List of match data within the date range.</returns>
    public IReadOnlyList<MatchData> GetMatchesInRange(Guid playerId, DateTime start, DateTime end)
    {
        lock (_playerMatches)
        {
            if (!_playerMatches.TryGetValue(playerId, out var matchKeys))
            {
                return Array.Empty<MatchData>();
            }

            var matches = matchKeys
                .Select(key => _matches.TryGetValue(key, out var match) ? match : null)
                .Where(m => m != null)
                .Cast<MatchData>()
                .Where(m => m.StartTime >= start && m.StartTime <= end)
                .OrderBy(m => m.StartTime)
                .ToList();

            return matches;
        }
    }

    /// <summary>
    /// Gets the most recent matches for a player.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="count">Number of recent matches to retrieve.</param>
    /// <returns>List of recent match data.</returns>
    public IReadOnlyList<MatchData> GetRecentPlayerMatches(Guid playerId, int count)
    {
        lock (_playerMatches)
        {
            if (!_playerMatches.TryGetValue(playerId, out var matchKeys))
            {
                return Array.Empty<MatchData>();
            }

            var matches = matchKeys
                .Select(key => _matches.TryGetValue(key, out var match) ? match : null)
                .Where(m => m != null)
                .Cast<MatchData>()
                .OrderByDescending(m => m.StartTime)
                .Take(count)
                .ToList();

            return matches;
        }
    }
}
