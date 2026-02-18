namespace SaveState.Application.Mugen.Services.MatchAnalytics.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for analyzing match patterns and identifying player behaviors.
/// </summary>
public class PatternEngine
{
    private readonly ILogger<PatternEngine> _logger;
    private readonly List<PatternDefinition> _patternDefinitions;

    public PatternEngine(ILogger<PatternEngine> logger)
    {
        _logger = logger;
        _patternDefinitions = InitializePatternDefinitions();
    }

    /// <summary>
    /// Analyzes a single match for patterns.
    /// </summary>
    /// <param name="matchData">The match data to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task AnalyzeMatchAsync(MatchData matchData, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing patterns for match {MatchId}", matchData.MatchId);

        // Analyze patterns for Player 1
        await AnalyzePlayerPatternsInMatchAsync(matchData.Player1Id, matchData, cancellationToken);

        // Analyze patterns for Player 2
        await AnalyzePlayerPatternsInMatchAsync(matchData.Player2Id, matchData, cancellationToken);

        _logger.LogInformation("Pattern analysis completed for match {MatchId}", matchData.MatchId);
    }

    /// <summary>
    /// Identifies patterns for a player across multiple matches.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <param name="matches">The list of matches to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of detected player patterns.</returns>
    public async Task<IReadOnlyList<PlayerPattern>> IdentifyPatternsAsync(
        Guid playerId,
        IReadOnlyList<MatchData> matches,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Identifying patterns for player {PlayerId} across {MatchCount} matches",
            playerId, matches.Count);

        var patterns = new List<PlayerPattern>();

        foreach (var definition in _patternDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var confidence = await definition.AnalysisFunction(playerId, matches, cancellationToken);

                if (confidence >= definition.Threshold)
                {
                    var associatedMoves = ExtractAssociatedMoves(playerId, matches, definition.PatternType);
                    var impact = DeterminePatternImpact(definition.PatternType, confidence);

                    var pattern = new PlayerPattern(
                        PatternType: definition.PatternType,
                        Description: definition.Description,
                        Frequency: (decimal)(confidence / 100.0),
                        AssociatedMoves: associatedMoves,
                        Impact: impact
                    );

                    patterns.Add(pattern);
                    _logger.LogDebug("Detected pattern '{PatternType}' for player {PlayerId} with confidence {Confidence:F2}%",
                        definition.PatternType, playerId, confidence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing pattern '{PatternType}' for player {PlayerId}",
                    definition.PatternType, playerId);
            }
        }

        _logger.LogInformation("Identified {PatternCount} patterns for player {PlayerId}",
            patterns.Count, playerId);

        return patterns;
    }

    private async Task AnalyzePlayerPatternsInMatchAsync(Guid playerId, MatchData matchData, CancellationToken ct)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Analyze combo patterns
        var playerCombos = matchData.Rounds
            .SelectMany(r => r.Combos)
            .Where(c => c.PlayerId == playerId)
            .ToList();

        if (playerCombos.Any(c => c.Length >= 5))
        {
            _logger.LogDebug("Player {PlayerId} demonstrated combo-heavy pattern in match {MatchId}",
                playerId, matchData.MatchId);
        }

        // Analyze special move usage
        var specialMoves = matchData.Rounds
            .SelectMany(r => r.SpecialMoves)
            .Where(sm => sm.PlayerId == playerId)
            .ToList();

        if (specialMoves.Count >= 10)
        {
            _logger.LogDebug("Player {PlayerId} demonstrated special spam pattern in match {MatchId}",
                playerId, matchData.MatchId);
        }

        // Analyze defensive behavior
        var defensiveInputs = matchData.InputEvents
            .Where(ie => ie.PlayerId == playerId && ie.Type == AnalyticsInputType.Block)
            .Count();

        var totalInputs = matchData.InputEvents
            .Where(ie => ie.PlayerId == playerId)
            .Count();

        if (totalInputs > 0 && (double)defensiveInputs / totalInputs > 0.3)
        {
            _logger.LogDebug("Player {PlayerId} demonstrated defensive pattern in match {MatchId}",
                playerId, matchData.MatchId);
        }
    }

    private List<PatternDefinition> InitializePatternDefinitions()
    {
        return new List<PatternDefinition>
        {
            new()
            {
                PatternType = "ComboHeavy",
                Description = "Player frequently executes long combos",
                Threshold = 60.0f,
                AnalysisFunction = AnalyzeComboHeavyPatternAsync
            },
            new()
            {
                PatternType = "SpecialSpammer",
                Description = "Player frequently uses special moves",
                Threshold = 70.0f,
                AnalysisFunction = AnalyzeSpecialSpamPatternAsync
            },
            new()
            {
                PatternType = "DefensivePlayer",
                Description = "Player focuses on defense and blocking",
                Threshold = 50.0f,
                AnalysisFunction = AnalyzeDefensivePatternAsync
            },
            new()
            {
                PatternType = "AggressiveRushdown",
                Description = "Player applies constant offensive pressure",
                Threshold = 65.0f,
                AnalysisFunction = AnalyzeAggressivePatternAsync
            },
            new()
            {
                PatternType = "ComebackPlayer",
                Description = "Player performs well when at a disadvantage",
                Threshold = 55.0f,
                AnalysisFunction = AnalyzeComebackPatternAsync
            }
        };
    }

    private Task<float> AnalyzeComboHeavyPatternAsync(Guid playerId, IReadOnlyList<MatchData> matches, CancellationToken ct)
    {
        float totalCombos = 0;
        float longCombos = 0;

        foreach (var match in matches)
        {
            var combos = match.Rounds
                .SelectMany(r => r.Combos)
                .Where(c => c.PlayerId == playerId);

            foreach (var combo in combos)
            {
                totalCombos++;
                if (combo.Length >= 5)
                {
                    longCombos++;
                }
            }
        }

        float confidence = totalCombos > 0 ? (longCombos / totalCombos) * 100 : 0;
        return Task.FromResult(confidence);
    }

    private Task<float> AnalyzeSpecialSpamPatternAsync(Guid playerId, IReadOnlyList<MatchData> matches, CancellationToken ct)
    {
        float totalMoves = 0;
        float specialMoves = 0;

        foreach (var match in matches)
        {
            specialMoves += match.Rounds
                .SelectMany(r => r.SpecialMoves)
                .Count(sm => sm.PlayerId == playerId);

            totalMoves += match.Rounds
                .SelectMany(r => r.Hits)
                .Count(h => h.AttackerId == playerId);
        }

        float confidence = totalMoves > 0 ? (specialMoves / totalMoves) * 150 : 0; // Scale up since specials are less frequent
        return Task.FromResult(Math.Min(confidence, 100));
    }

    private Task<float> AnalyzeDefensivePatternAsync(Guid playerId, IReadOnlyList<MatchData> matches, CancellationToken ct)
    {
        float totalInputs = 0;
        float defensiveInputs = 0;

        foreach (var match in matches)
        {
            var playerInputs = match.InputEvents.Where(ie => ie.PlayerId == playerId);
            totalInputs += playerInputs.Count();
            defensiveInputs += playerInputs.Count(ie => ie.Type == AnalyticsInputType.Block);
        }

        float confidence = totalInputs > 0 ? (defensiveInputs / totalInputs) * 200 : 0; // Scale up
        return Task.FromResult(Math.Min(confidence, 100));
    }

    private Task<float> AnalyzeAggressivePatternAsync(Guid playerId, IReadOnlyList<MatchData> matches, CancellationToken ct)
    {
        float totalRounds = 0;
        float aggressiveRounds = 0;

        foreach (var match in matches)
        {
            foreach (var round in match.Rounds)
            {
                totalRounds++;
                var damageDealt = round.Hits
                    .Where(h => h.AttackerId == playerId)
                    .Sum(h => h.Damage);

                if (damageDealt > 200)
                {
                    aggressiveRounds++;
                }
            }
        }

        float confidence = totalRounds > 0 ? (aggressiveRounds / totalRounds) * 100 : 0;
        return Task.FromResult(confidence);
    }

    private Task<float> AnalyzeComebackPatternAsync(Guid playerId, IReadOnlyList<MatchData> matches, CancellationToken ct)
    {
        int comebackWins = 0;
        int potentialComebacks = 0;

        foreach (var match in matches)
        {
            // Check if player won the match after losing rounds
            var playerWon = match.Rounds.LastOrDefault()?.WinnerId == playerId;
            var roundsLost = match.Rounds.Count(r => r.WinnerId != playerId && r.WinnerId != Guid.Empty);

            if (roundsLost >= 1)
            {
                potentialComebacks++;
                if (playerWon)
                {
                    comebackWins++;
                }
            }
        }

        float confidence = potentialComebacks > 0 ? ((float)comebackWins / potentialComebacks) * 100 : 0;
        return Task.FromResult(confidence);
    }

    private IReadOnlyList<string> ExtractAssociatedMoves(Guid playerId, IReadOnlyList<MatchData> matches, string patternType)
    {
        var moves = new HashSet<string>();

        foreach (var match in matches)
        {
            switch (patternType)
            {
                case "ComboHeavy":
                    var comboMoves = match.Rounds
                        .SelectMany(r => r.Combos)
                        .Where(c => c.PlayerId == playerId)
                        .SelectMany(c => c.Moves);
                    foreach (var move in comboMoves)
                    {
                        moves.Add(move);
                    }
                    break;

                case "SpecialSpammer":
                    var specialMoves = match.Rounds
                        .SelectMany(r => r.SpecialMoves)
                        .Where(sm => sm.PlayerId == playerId)
                        .Select(sm => sm.MoveName);
                    foreach (var move in specialMoves)
                    {
                        moves.Add(move);
                    }
                    break;

                default:
                    var allMoves = match.Rounds
                        .SelectMany(r => r.Hits)
                        .Where(h => h.AttackerId == playerId)
                        .Select(h => h.MoveName);
                    foreach (var move in allMoves.Take(5))
                    {
                        moves.Add(move);
                    }
                    break;
            }
        }

        return moves.Take(10).ToList();
    }

    private string DeterminePatternImpact(string patternType, float confidence)
    {
        return patternType switch
        {
            "ComboHeavy" => confidence > 80 ? "High damage output potential" : "Moderate combo skill",
            "SpecialSpammer" => confidence > 80 ? "Predictable but dangerous" : "Varied special usage",
            "DefensivePlayer" => confidence > 80 ? "Difficult to open up" : "Balanced defense",
            "AggressiveRushdown" => confidence > 80 ? "Overwhelming pressure" : "Active offense",
            "ComebackPlayer" => confidence > 80 ? "Mental resilience" : "Clutch potential",
            _ => "Variable impact"
        };
    }
}
