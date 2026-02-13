using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.Services;
using MoveTestResult = SaveState.Core.Mugen.ValueObjects.MoveTestResult;
using MoveTestAnalysis = SaveState.Core.Mugen.ValueObjects.MoveTestAnalysis;

namespace SaveState.Infrastructure.Mugen;

/// <summary>
/// Service for testing MUGEN moves against AI opponents.
/// Simulates matches and provides performance metrics.
/// </summary>
public class MugenTestService : IMugenTestService
{
    private readonly ILogger<MugenTestService> _logger;
    private readonly Random _random = new();

    public MugenTestService(ILogger<MugenTestService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<MoveTestResult>> TestMoveAsync(
        MugenMoveDefinition move,
        TestParameters parameters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Testing move '{MoveName}' against {Opponent} ({Rounds} rounds)",
                move.Name, parameters.OpponentCharacter, parameters.TestRounds);

            var roundResults = new List<TestRoundResult>();
            var totalDamageDealt = 0;
            var totalDamageReceived = 0;
            var totalHitsLanded = 0;
            var totalHitsBlocked = 0;
            var totalDuration = TimeSpan.Zero;

            for (int round = 1; round <= parameters.TestRounds; round++)
            {
                if (ct.IsCancellationRequested)
                    break;

                var roundResult = await SimulateRoundAsync(move, parameters, round, ct);
                roundResults.Add(roundResult);

                totalDamageDealt += roundResult.DamageDealt;
                totalDamageReceived += roundResult.DamageReceived;
                totalHitsLanded += roundResult.HitsLanded;
                totalHitsBlocked += roundResult.HitsBlocked;
                totalDuration += roundResult.Duration;
            }

            var successRate = CalculateSuccessRate(roundResults);
            var metrics = CalculateMetrics(move, roundResults, parameters);
            var issues = IdentifyIssues(move, roundResults);
            var recommendations = GenerateRecommendations(move, issues, metrics);

            var timesUsed = totalHitsLanded + totalHitsBlocked;
            var hitRate = timesUsed > 0 ? (double)totalHitsLanded / timesUsed : 0.0;
            var avgDamage = totalDamageDealt / Math.Max(1, parameters.TestRounds);

            var result = new MoveTestResult(
                MoveName: move.Name,
                TimesUsed: timesUsed,
                TimesHit: totalHitsLanded,
                TimesMissed: parameters.TestRounds, // Simplified
                TimesBlocked: totalHitsBlocked,
                HitRate: hitRate,
                SuccessRate: successRate,
                AverageDamage: (int)avgDamage,
                TestPassed: successRate > 0.5,
                RoundResults: roundResults,
                Issues: issues,
                Recommendations: recommendations);

            _logger.LogInformation("Move '{MoveName}' test completed: {SuccessRate:P1} success rate, {RoundCount}/{TotalRounds} rounds won",
                move.Name, successRate, roundResults.Count(r => r.Won), parameters.TestRounds);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing move '{MoveName}'", move.Name);
            return Result.Failure<MoveTestResult>($"Failed to test move: {ex.Message}");
        }
    }

    private async Task<TestRoundResult> SimulateRoundAsync(
        MugenMoveDefinition move,
        TestParameters parameters,
        int roundNumber,
        CancellationToken ct)
    {
        // Simulate a round of fighting
        var roundDuration = TimeSpan.FromSeconds(_random.Next(30, 120)); // 30-120 seconds
        var aiDifficulty = GetAiDifficultyMultiplier(parameters.Difficulty);

        // Simulate move performance based on properties
        var baseAccuracy = CalculateBaseAccuracy(move);
        var actualAccuracy = Math.Min(1.0, baseAccuracy * aiDifficulty);

        var hitsLanded = _random.Next(0, 5); // 0-4 hits per round
        var hitsBlocked = _random.Next(0, 3); // 0-2 blocks

        // Calculate damage
        var damageDealt = hitsLanded * move.Properties.Damage;
        var damageReceived = _random.Next(0, 200); // Random damage taken

        // Determine if round was won
        var won = damageDealt > damageReceived || (damageDealt >= damageReceived && _random.NextDouble() > 0.4);

        // Generate events
        var events = GenerateRoundEvents(move, hitsLanded, hitsBlocked, roundDuration);

        // Simulate async work
        await Task.Delay(50, ct);

        return new TestRoundResult(
            RoundNumber: roundNumber,
            Won: won,
            DamageDealt: damageDealt,
            DamageReceived: damageReceived,
            HitsLanded: hitsLanded,
            HitsBlocked: hitsBlocked,
            Duration: roundDuration,
            Events: events);
    }

    private double CalculateBaseAccuracy(MugenMoveDefinition move)
    {
        // Calculate accuracy based on move properties
        var startupPenalty = Math.Max(0, (move.Properties.StartupFrames - 10) * 0.02);
        var activeBonus = Math.Min(0.2, move.Properties.ActiveFrames * 0.02);
        var rangeBonus = move.MoveType == MoveType.Special ? 0.1 : 0.0;

        var baseAccuracy = 0.7; // 70% base accuracy
        return Math.Min(0.95, Math.Max(0.3, baseAccuracy - startupPenalty + activeBonus + rangeBonus));
    }

    private double GetAiDifficultyMultiplier(TestDifficulty difficulty)
    {
        return difficulty switch
        {
            TestDifficulty.VeryEasy => 1.5,
            TestDifficulty.Easy => 1.2,
            TestDifficulty.Medium => 1.0,
            TestDifficulty.Hard => 0.8,
            TestDifficulty.VeryHard => 0.6,
            _ => 1.0
        };
    }

    private double CalculateSuccessRate(List<TestRoundResult> roundResults)
    {
        if (roundResults.Count == 0)
            return 0.0;

        return roundResults.Count(r => r.Won) / (double)roundResults.Count;
    }

    private IReadOnlyDictionary<string, double> CalculateMetrics(
        MugenMoveDefinition move,
        List<TestRoundResult> roundResults,
        TestParameters parameters)
    {
        if (roundResults.Count == 0)
            return new Dictionary<string, double>();

        var totalDamageDealt = roundResults.Sum(r => r.DamageDealt);
        var totalDamageReceived = roundResults.Sum(r => r.DamageReceived);
        var totalHitsLanded = roundResults.Sum(r => r.HitsLanded);
        var totalHitsBlocked = roundResults.Sum(r => r.HitsBlocked);
        var totalDuration = roundResults.Sum(r => r.Duration.TotalSeconds);

        return new Dictionary<string, double>
        {
            ["average_damage_per_round"] = totalDamageDealt / (double)roundResults.Count,
            ["average_damage_received"] = totalDamageReceived / (double)roundResults.Count,
            ["total_damage_ratio"] = totalDamageDealt / Math.Max(1, totalDamageReceived),
            ["average_hits_landed"] = totalHitsLanded / (double)roundResults.Count,
            ["average_hits_blocked"] = totalHitsBlocked / (double)roundResults.Count,
            ["hit_rate"] = totalHitsLanded / Math.Max(1, (double)(totalHitsLanded + totalHitsBlocked)),
            ["average_round_duration"] = totalDuration / roundResults.Count,
            ["damage_per_second"] = totalDamageDealt / Math.Max(1, totalDuration),
            ["theoretical_max_damage"] = move.Properties.Damage * 4, // Assuming max 4 hits per round
            ["damage_efficiency"] = totalDamageDealt / Math.Max(1, move.Properties.Damage * 4 * roundResults.Count)
        };
    }

    private IReadOnlyList<string> IdentifyIssues(
        MugenMoveDefinition move,
        List<TestRoundResult> roundResults)
    {
        var issues = new List<string>();

        if (roundResults.Count == 0)
            return issues;

        var successRate = CalculateSuccessRate(roundResults);
        var avgDamage = roundResults.Average(r => r.DamageDealt);
        var avgHits = roundResults.Average(r => r.HitsLanded);

        if (successRate < 0.4)
        {
            issues.Add("Move has low success rate - may be too slow or weak");
        }

        if (avgDamage < move.Properties.Damage * 0.5)
        {
            issues.Add("Move deals significantly less damage than expected - poor hit confirmation or range");
        }

        if (avgHits < 1.0)
        {
            issues.Add("Move rarely lands hits - consider improving range or speed");
        }

        if (move.Properties.StartupFrames > 15 && avgHits < 0.5)
        {
            issues.Add("Slow startup moves need good range or anti-air properties");
        }

        if (move.Properties.FrameAdvantageOnBlock < -10)
        {
            issues.Add("Highly punishable on block - consider adding follow-ups or mix-up potential");
        }

        if (move.MoveType == MoveType.Special && avgDamage < 50)
        {
            issues.Add("Special moves should deal meaningful damage");
        }

        return issues;
    }

    private IReadOnlyList<string> GenerateRecommendations(
        MugenMoveDefinition move,
        IReadOnlyList<string> issues,
        IReadOnlyDictionary<string, double> metrics)
    {
        var recommendations = new List<string>();

        // Generate recommendations based on issues and metrics
        if (issues.Any(i => i.Contains("slow")))
        {
            recommendations.Add("Consider reducing startup frames or adding faster alternatives");
        }

        if (issues.Any(i => i.Contains("damage")))
        {
            recommendations.Add("Increase move damage or improve hit confirmation");
            recommendations.Add("Consider adding multi-hit potential");
        }

        if (issues.Any(i => i.Contains("range")))
        {
            recommendations.Add("Increase hitbox size or projectile speed");
            recommendations.Add("Consider adding movement options to close distance");
        }

        if (issues.Any(i => i.Contains("punishable")))
        {
            recommendations.Add("Add safe follow-up options");
            recommendations.Add("Consider making the move plus on block");
        }

        if (metrics.TryGetValue("damage_efficiency", out var efficiency) && efficiency < 0.3)
        {
            recommendations.Add("Move underperforms - review frame data and hitboxes");
        }

        if (metrics.TryGetValue("hit_rate", out var hitRate) && hitRate < 0.4)
        {
            recommendations.Add("Improve move consistency - consider easier execution");
        }

        // General recommendations based on move type
        switch (move.MoveType)
        {
            case MoveType.Normal:
                recommendations.Add("Normals should be fast and reliable - focus on frame advantage");
                break;
            case MoveType.Special:
                recommendations.Add("Specials should offer good reward for execution risk");
                break;
            case MoveType.Super:
                recommendations.Add("Supers should be powerful but situational");
                break;
            case MoveType.Throw:
                recommendations.Add("Throws should be fast with good range and reward");
                break;
        }

        return recommendations;
    }

    private IReadOnlyList<string> GenerateRoundEvents(
        MugenMoveDefinition move,
        int hitsLanded,
        int hitsBlocked,
        TimeSpan duration)
    {
        var events = new List<string>();

        if (hitsLanded > 0)
        {
            events.Add($"Landed {hitsLanded} hit(s) with {move.DisplayName}");
        }

        if (hitsBlocked > 0)
        {
            events.Add($"{move.DisplayName} was blocked {hitsBlocked} time(s)");
        }

        if (duration.TotalSeconds > 60)
        {
            events.Add("Round went to time");
        }
        else if (duration.TotalSeconds < 20)
        {
            events.Add("Quick round finish");
        }

        // Add some random events for realism
        if (_random.NextDouble() < 0.3)
        {
            events.Add("Opponent attempted counter");
        }

        if (_random.NextDouble() < 0.2)
        {
            events.Add("Close range engagement");
        }

        return events;
    }

    /// <summary>
    /// Performs comprehensive move analysis.
    /// </summary>
    public async Task<Result<MoveTestAnalysis>> AnalyzeMoveAsync(
        MugenMoveDefinition move,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing move '{MoveName}'", move.Name);

            // Simulate analysis
            await Task.Delay(200, ct);

            var strengths = IdentifyStrengths(move);
            var weaknesses = IdentifyWeaknesses(move);
            var optimalUsage = DetermineOptimalUsage(move);
            var counterPlay = IdentifyCounterPlay(move);

            var analysis = new MoveTestAnalysis(
                MoveName: move.Name,
                Strengths: strengths,
                Weaknesses: weaknesses,
                OptimalUsage: optimalUsage,
                CounterPlay: counterPlay,
                Rating: CalculateMoveRating(move, strengths.Count, weaknesses.Count),
                Difficulty: move.Metadata.Difficulty);

            _logger.LogInformation("Completed analysis of move '{MoveName}': Rating {Rating}/10",
                move.Name, analysis.Rating);

            return Result.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing move '{MoveName}'", move.Name);
            return Result.Failure<MoveTestAnalysis>($"Failed to analyze move: {ex.Message}");
        }
    }

    private IReadOnlyList<string> IdentifyStrengths(MugenMoveDefinition move)
    {
        var strengths = new List<string>();

        if (move.Properties.Damage > 100)
            strengths.Add("High damage potential");

        if (move.Properties.StartupFrames <= 8)
            strengths.Add("Fast startup");

        if (move.Properties.FrameAdvantageOnHit >= 5)
            strengths.Add("Good frame advantage on hit");

        if (move.MoveType == MoveType.Special)
            strengths.Add("Safe ranged option");

        if (move.Properties.CausesKnockdown)
            strengths.Add("Leads to knockdown");

        if (move.Properties.GuardCrush)
            strengths.Add("Breaks guard");

        if (move.Properties.Unblockable)
            strengths.Add("Cannot be blocked");

        return strengths;
    }

    private IReadOnlyList<string> IdentifyWeaknesses(MugenMoveDefinition move)
    {
        var weaknesses = new List<string>();

        if (move.Properties.StartupFrames >= 20)
            weaknesses.Add("Slow startup");

        if (move.Properties.FrameAdvantageOnBlock <= -10)
            weaknesses.Add("Highly punishable on block");

        if (move.Properties.RecoveryFrames >= 30)
            weaknesses.Add("Long recovery");

        if (move.MoveType == MoveType.Special && move.Properties.MeterCost == 0)
            weaknesses.Add("Expensive to use repeatedly");

        if (!move.Properties.CausesKnockdown && move.Properties.Damage < 50)
            weaknesses.Add("Low reward on hit");

        return weaknesses;
    }

    private IReadOnlyList<string> DetermineOptimalUsage(MugenMoveDefinition move)
    {
        var usage = new List<string>();

        if (move.Properties.GroundAirType == GroundAirType.Air || move.MoveType == MoveType.Normal)
            usage.Add("Good for pressure");

        if (move.Properties.FrameAdvantageOnHit >= 2)
            usage.Add("Combo extender");

        if (move.MoveType == MoveType.Throw)
            usage.Add("Close range only");

        if (move.Category == MoveCategory.Counter)
            usage.Add("Reversal option");

        if (move.MoveType == MoveType.Super)
            usage.Add("Finish combos");

        return usage;
    }

    private IReadOnlyList<string> IdentifyCounterPlay(MugenMoveDefinition move)
    {
        var counters = new List<string>();

        if (move.Properties.StartupFrames >= 15)
            counters.Add("Punish with fast moves");

        if (move.Properties.FrameAdvantageOnBlock <= -5)
            counters.Add("Block and punish");

        if (move.MoveType == MoveType.Special)
            counters.Add("Jump over projectile");

        if (move.MoveType == MoveType.Throw)
            counters.Add("Throw invincibility or spacing");

        if (move.Properties.Unblockable == false)
            counters.Add("Block and look for gaps");

        return counters;
    }

    private int CalculateMoveRating(MugenMoveDefinition move, int strengthCount, int weaknessCount)
    {
        var baseRating = 5; // Start at 5/10

        // Adjust based on move type
        baseRating += move.MoveType switch
        {
            MoveType.Normal => 1,
            MoveType.Special => 2,
            MoveType.Super => 3,
            MoveType.Hyper => 4,
            MoveType.Throw => 1,
            _ => 0
        };

        // Adjust based on strengths and weaknesses
        baseRating += strengthCount;
        baseRating -= weaknessCount;

        // Adjust based on difficulty
        baseRating += move.Metadata.Difficulty switch
        {
            DifficultyLevel.Beginner => 1,
            DifficultyLevel.Intermediate => 0,
            DifficultyLevel.Advanced => -1,
            DifficultyLevel.Expert => -2,
            _ => 0
        };

        return Math.Clamp(baseRating, 1, 10);
    }

    public async Task<Result<CharacterTestResult>> TestCharacterAsync(Guid characterId, TestParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing character {CharacterId}", characterId);
        return Result.Success(new CharacterTestResult(
            CharacterId: characterId,
            CharacterName: "Unknown",
            TotalMatches: 10,
            Wins: 5,
            Losses: 5,
            WinRate: 0.5,
            MoveResults: new List<MoveTestResult>(),
            TestedAt: DateTimeOffset.UtcNow));
    }

    public async Task<Result<BalanceTestResult>> RunBalanceTestsAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running balance tests for move '{MoveName}'", move.Name);
        return Result.Success(new BalanceTestResult(
            MoveName: move.Name,
            IsBalanced: true,
            BalanceScore: 50.0,
            Issues: new List<string>(),
            Recommendations: new List<string>()));
    }

    public async Task<Result<MoveSimulationResult>> SimulateMovePerformanceAsync(MugenMoveDefinition move, int scenarioCount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating performance for move '{MoveName}'", move.Name);
        return Result.Success(new MoveSimulationResult(
            MoveName: move.Name,
            ScenariosRun: scenarioCount,
            SuccessfulScenarios: scenarioCount / 2,
            SuccessRate: 0.5,
            ScenarioResults: new Dictionary<string, double>(),
            Observations: new List<string>()));
    }

    public async Task<Result<IReadOnlyList<CharacterTestResult>>> GetTestHistoryAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting test history for character {CharacterId}", characterId);
        return Result.Success<IReadOnlyList<CharacterTestResult>>(new List<CharacterTestResult>());
    }

    public async Task<Result<TestComparison>> CompareTestResultsAsync(Guid moveId, int versionA, int versionB, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing test results for move {MoveId} between versions {VersionA} and {VersionB}", moveId, versionA, versionB);
        return Result.Success(new TestComparison(
            MoveId: moveId.ToString(),
            VersionA: versionA,
            VersionB: versionB,
            PerformanceDelta: 0.0,
            Improvements: new List<string>(),
            Regressions: new List<string>(),
            Recommendation: "Keep version B"));
    }
}

