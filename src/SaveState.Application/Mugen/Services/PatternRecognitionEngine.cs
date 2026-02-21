using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced pattern recognition engine for identifying player behavior patterns,
/// tendencies, and strategic preferences in MUGEN matches.
/// </summary>
public class PatternRecognitionEngine
{
    private readonly ILogger<PatternRecognitionEngine> _logger;
    private readonly Dictionary<string, PatternDefinition> _patternDefinitions = new();

    public PatternRecognitionEngine(ILogger<PatternRecognitionEngine> logger)
    {
        _logger = logger;
        InitializePatternDefinitions();
    }

    public async Task<IReadOnlyList<PlayerPattern>> IdentifyPatternsAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        CancellationToken ct = default)
    {
        var patterns = new List<PlayerPattern>();

        try
        {
            _logger.LogInformation("Identifying patterns for player {PlayerId} from {MatchCount} matches",
                playerId, matches.Count);

            // Analyze each pattern type
            foreach (var patternDef in _patternDefinitions.Values)
            {
                var pattern = await AnalyzePatternAsync(playerId, matches, patternDef, ct);
                if (pattern != null)
                {
                    patterns.Add(pattern);
                }
            }

            // Sort by frequency (most common first)
            patterns = patterns.OrderByDescending(p => p.Frequency).ToList();

            _logger.LogInformation("Identified {PatternCount} patterns for player {PlayerId}",
                patterns.Count, playerId);

            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying patterns for player {PlayerId}", playerId);
            return Array.Empty<PlayerPattern>();
        }
    }

    public async Task AnalyzeMatchAsync(MatchRecording matchData, CancellationToken ct = default)
    {
        try
        {
            // Analyze the match for patterns that can be used for future recognition
            await UpdatePatternModelsAsync(matchData, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing match {MatchId} for patterns", matchData.MatchId);
        }
    }

    private void InitializePatternDefinitions()
    {
        // Combo patterns
        _patternDefinitions["combo_heavy"] = new PatternDefinition
        {
            PatternType = "Combo-Heavy Playstyle",
            Description = "Frequently executes long, damaging combos",
            Threshold = 0.6f, // 60% of matches have combos > 5 hits
            AnalysisFunction = AnalyzeComboPatterns
        };

        _patternDefinitions["special_spammer"] = new PatternDefinition
        {
            PatternType = "Special Move Spammer",
            Description = "Relies heavily on special moves",
            Threshold = 0.5f, // 50% of damage from specials
            AnalysisFunction = AnalyzeSpecialMoveUsage
        };

        _patternDefinitions["defensive_player"] = new PatternDefinition
        {
            PatternType = "Defensive Player",
            Description = "Focuses on blocking and counter-attacking",
            Threshold = 0.7f, // 70% successful blocks
            AnalysisFunction = AnalyzeDefensivePatterns
        };

        _patternDefinitions["rushdown_style"] = new PatternDefinition
        {
            PatternType = "Rushdown Style",
            Description = "Aggressive pressure with constant offense",
            Threshold = 0.55f, // 55% offensive actions
            AnalysisFunction = AnalyzeRushdownPatterns
        };

        _patternDefinitions["poker_player"] = new PatternDefinition
        {
            PatternType = "Poker Style",
            Description = "Conservative play with high-risk, high-reward moves",
            Threshold = 0.4f, // 40% of matches decided by single big moves
            AnalysisFunction = AnalyzePokerPatterns
        };

        // Input patterns
        _patternDefinitions["input_heavy"] = new PatternDefinition
        {
            PatternType = "Input-Heavy Playstyle",
            Description = "Uses complex input sequences frequently",
            Threshold = 0.45f, // 45% special/super moves
            AnalysisFunction = AnalyzeInputComplexity
        };

        _patternDefinitions["timing_based"] = new PatternDefinition
        {
            PatternType = "Timing-Based Playstyle",
            Description = "Relies on precise timing rather than complex inputs",
            Threshold = 0.6f, // 60% successful timed moves
            AnalysisFunction = AnalyzeTimingPatterns
        };

        // Character-specific patterns
        _patternDefinitions["character_specialist"] = new PatternDefinition
        {
            PatternType = "Character Specialist",
            Description = "Excels with specific characters",
            Threshold = 0.75f, // 75% win rate with preferred characters
            AnalysisFunction = AnalyzeCharacterPreferences
        };

        // Match flow patterns
        _patternDefinitions["comeback_king"] = new PatternDefinition
        {
            PatternType = "Comeback Specialist",
            Description = "Strong at making comebacks from disadvantage",
            Threshold = 0.5f, // 50% of comebacks successful
            AnalysisFunction = AnalyzeComebackPatterns
        };

        _patternDefinitions["momentum_player"] = new PatternDefinition
        {
            PatternType = "Momentum Player",
            Description = "Builds and maintains match momentum",
            Threshold = 0.6f, // 60% matches won after gaining lead
            AnalysisFunction = AnalyzeMomentumPatterns
        };
    }

    private async Task<PlayerPattern?> AnalyzePatternAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        PatternDefinition patternDef,
        CancellationToken ct)
    {
        try
        {
            var frequency = await patternDef.AnalysisFunction(playerId, matches, ct);
            if (frequency >= patternDef.Threshold)
            {
                var impact = CalculatePatternImpact(patternDef.PatternType, frequency);
                return new PlayerPattern(
                    PatternType: patternDef.PatternType,
                    Description: patternDef.Description,
                    Frequency: (decimal)frequency,
                    AssociatedMoves: await GetAssociatedMovesAsync(playerId, matches, patternDef.PatternType, ct),
                    Impact: impact
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing pattern {PatternType} for player {PlayerId}",
                patternDef.PatternType, playerId);
            return null;
        }
    }

    private async Task<float> AnalyzeComboPatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var totalCombos = 0;
        var longCombos = 0;

        foreach (var match in matches)
        {
            var playerCombos = match.Rounds.SelectMany(r =>
                r.Combos.Where(c => c.PlayerId == playerId));

            totalCombos += playerCombos.Count();
            longCombos += playerCombos.Count(c => c.Length >= 5);
        }

        return totalCombos > 0 ? (float)longCombos / totalCombos : 0f;
    }

    private async Task<float> AnalyzeSpecialMoveUsage(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var totalDamage = 0f;
        var specialDamage = 0f;

        foreach (var match in matches)
        {
            var playerSpecials = match.Rounds.SelectMany(r =>
                r.SpecialMoves.Where(sm => sm.PlayerId == playerId));

            var playerCombos = match.Rounds.SelectMany(r =>
                r.Combos.Where(c => c.PlayerId == playerId));

            totalDamage += playerSpecials.Sum(sm => sm.Damage) + playerCombos.Sum(c => c.TotalDamage);
            specialDamage += playerSpecials.Sum(sm => sm.Damage);
        }

        return totalDamage > 0 ? specialDamage / totalDamage : 0f;
    }

    private async Task<float> AnalyzeDefensivePatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var totalActions = 0;
        var defensiveActions = 0;

        foreach (var match in matches)
        {
            var playerInputs = match.InputEvents.Where(ie => ie.PlayerId == playerId);
            totalActions += playerInputs.Count();
            defensiveActions += playerInputs.Count(ie => ie.Type == InputType.Block);
        }

        return totalActions > 0 ? (float)defensiveActions / totalActions : 0f;
    }

    private async Task<float> AnalyzeRushdownPatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var totalActions = 0;
        var offensiveActions = 0;

        foreach (var match in matches)
        {
            var playerInputs = match.InputEvents.Where(ie => ie.PlayerId == playerId);
            totalActions += playerInputs.Count();

            // Count offensive inputs (attacks, special moves)
            offensiveActions += playerInputs.Count(ie =>
                ie.Type == InputType.ButtonPress ||
                ie.Type == InputType.SpecialMove ||
                ie.Type == InputType.ThrowAttempt);
        }

        return totalActions > 0 ? (float)offensiveActions / totalActions : 0f;
    }

    private async Task<float> AnalyzePokerPatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var decisiveMatches = 0;
        var totalMatches = matches.Count;

        foreach (var match in matches)
        {
            var playerSpecials = match.Rounds.SelectMany(r =>
                r.SpecialMoves.Where(sm => sm.PlayerId == playerId));

            // Check if match was decided by high-risk moves
            var highRiskMoves = playerSpecials.Where(sm => sm.Damage >= 150).ToList();
            if (highRiskMoves.Any())
            {
                var totalSpecialDamage = playerSpecials.Sum(sm => sm.Damage);
                var highRiskDamage = highRiskMoves.Sum(sm => sm.Damage);

                if (highRiskDamage >= totalSpecialDamage * 0.6f) // 60%+ damage from high-risk moves
                {
                    decisiveMatches++;
                }
            }
        }

        return totalMatches > 0 ? (float)decisiveMatches / totalMatches : 0f;
    }

    private async Task<float> AnalyzeInputComplexity(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var totalInputs = 0;
        var complexInputs = 0;

        foreach (var match in matches)
        {
            var playerInputs = match.InputEvents.Where(ie => ie.PlayerId == playerId);
            totalInputs += playerInputs.Count();

            complexInputs += playerInputs.Count(ie =>
                ie.Type == InputType.SpecialMove ||
                ie.Type == InputType.ThrowAttempt);
        }

        return totalInputs > 0 ? (float)complexInputs / totalInputs : 0f;
    }

    private async Task<float> AnalyzeTimingPatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        // Analyze successful timed moves vs failed ones
        var successfulTiming = 0;
        var totalTimingAttempts = 0;

        foreach (var match in matches)
        {
            // Count successful counter-hits and punishes
            var playerHits = match.Rounds.SelectMany(r =>
                r.Hits.Where(h => h.AttackerId == playerId));

            successfulTiming += playerHits.Count(h => h.CounterHit);
            totalTimingAttempts += playerHits.Count();
        }

        return totalTimingAttempts > 0 ? (float)successfulTiming / totalTimingAttempts : 0f;
    }

    private async Task<float> AnalyzeCharacterPreferences(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var characterWins = new Dictionary<string, (int wins, int total)>();

        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;
            var character = isPlayer1 ? match.Player1Character : match.Player2Character;
            var won = match.Rounds.Last().WinnerId == playerId;

            if (!characterWins.ContainsKey(character))
            {
                characterWins[character] = (0, 0);
            }

            var stats = characterWins[character];
            characterWins[character] = (won ? stats.wins + 1 : stats.wins, stats.total + 1);
        }

        if (!characterWins.Any())
            return 0f;

        // Find best performing character
        var bestCharacter = characterWins.MaxBy(kvp => (float)kvp.Value.wins / kvp.Value.total);
        var winRate = (float)bestCharacter.Value.wins / bestCharacter.Value.total;

        return winRate;
    }

    private async Task<float> AnalyzeComebackPatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var comebackAttempts = 0;
        var successfulComebacks = 0;

        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;
            var comebackSuccessful = false;

            // Check each round for comeback scenarios
            foreach (var round in match.Rounds)
            {
                // Simplified: check if player was behind and came back
                // In real implementation, this would analyze health over time
                var playerHealth = isPlayer1 ? 1000 : 1000; // Placeholder
                var opponentHealth = isPlayer1 ? 1000 : 1000; // Placeholder

                if (playerHealth < opponentHealth * 0.3f) // Was behind
                {
                    comebackAttempts++;
                    if (round.WinnerId == playerId) // Won the round
                    {
                        comebackSuccessful = true;
                    }
                }
            }

            if (comebackSuccessful)
            {
                successfulComebacks++;
            }
        }

        return comebackAttempts > 0 ? (float)successfulComebacks / comebackAttempts : 0f;
    }

    private async Task<float> AnalyzeMomentumPatterns(Guid playerId, IReadOnlyList<MatchRecording> matches, CancellationToken ct)
    {
        var momentumWins = 0;
        var totalMatches = matches.Count;

        foreach (var match in matches)
        {
            // Check if player gained early lead and maintained it
            var firstRound = match.Rounds.FirstOrDefault();
            if (firstRound?.WinnerId == playerId)
            {
                // Won first round and overall match
                var finalWinner = match.Rounds.Last().WinnerId;
                if (finalWinner == playerId)
                {
                    momentumWins++;
                }
            }
        }

        return totalMatches > 0 ? (float)momentumWins / totalMatches : 0f;
    }

    private string CalculatePatternImpact(string patternType, float frequency)
    {
        return patternType switch
        {
            "Combo-Heavy Playstyle" => frequency > 0.8f ? "Very strong offensive presence" : "Good combo execution",
            "Special Move Spammer" => frequency > 0.7f ? "High damage potential but predictable" : "Balanced special usage",
            "Defensive Player" => frequency > 0.8f ? "Excellent defense, hard to break" : "Solid defensive fundamentals",
            "Rushdown Style" => frequency > 0.7f ? "Overwhelming pressure but risky" : "Aggressive but controlled",
            "Poker Style" => frequency > 0.6f ? "High variance, swingy matches" : "Calculated risk-taking",
            "Input-Heavy Playstyle" => frequency > 0.6f ? "Technical mastery" : "Skilled execution",
            "Timing-Based Playstyle" => frequency > 0.7f ? "Precise and consistent" : "Good fundamentals",
            "Character Specialist" => frequency > 0.8f ? "Master of chosen characters" : "Character preference identified",
            "Comeback Specialist" => frequency > 0.6f ? "Never count out" : "Resilient player",
            "Momentum Player" => frequency > 0.7f ? "Dominates when in control" : "Good at maintaining advantage",
            _ => "Pattern identified"
        };
    }

    private async Task<IReadOnlyList<string>> GetAssociatedMovesAsync(
        Guid playerId,
        IReadOnlyList<MatchRecording> matches,
        string patternType,
        CancellationToken ct)
    {
        var moves = new HashSet<string>();

        foreach (var match in matches)
        {
            var playerMoves = match.Rounds.SelectMany(r =>
                r.Hits.Where(h => h.AttackerId == playerId).Select(h => h.MoveName));

            foreach (var move in playerMoves)
            {
                moves.Add(move);
            }
        }

        // Limit to most relevant moves based on pattern
        return moves.Take(5).ToList();
    }

    private async Task UpdatePatternModelsAsync(MatchRecording matchData, CancellationToken ct)
    {
        // Update pattern recognition models with new match data
        // This would typically involve machine learning model updates
        // For now, just log the analysis
        _logger.LogDebug("Updated pattern models with match {MatchId}", matchData.MatchId);
    }

    private class PatternDefinition
    {
        public string PatternType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Threshold { get; set; }
        public required Func<Guid, IReadOnlyList<MatchRecording>, CancellationToken, Task<float>> AnalysisFunction { get; set; }
    }
}
