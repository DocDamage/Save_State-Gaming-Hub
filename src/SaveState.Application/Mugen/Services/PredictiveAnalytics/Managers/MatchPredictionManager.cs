using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.PredictiveAnalytics.Managers;

/// <summary>
/// Manages match prediction operations using skill ratings and matchup data.
/// </summary>
public sealed class MatchPredictionManager
{
    private readonly ILogger<MatchPredictionManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchPredictionManager"/> class.
    /// </summary>
    public MatchPredictionManager(ILogger<MatchPredictionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Predicts the outcome of a match between two players.
    /// </summary>
    public Task<Result<MatchPrediction>> PredictMatchAsync(
        PlayerSkill player1,
        PlayerSkill player2,
        CharacterMatchupData matchup,
        PredictionContext context,
        CancellationToken ct)
    {
        try
        {
            var skillDifference = player1.Rating - player2.Rating;
            var baseProbability = 1.0 / (1.0 + Math.Pow(10, -skillDifference / 400.0));

            var matchupAdjustment = matchup.Advantage switch
            {
                MatchupAdvantage.StronglyFavored => 0.15,
                MatchupAdvantage.SlightlyFavored => 0.07,
                MatchupAdvantage.Even => 0.0,
                MatchupAdvantage.SlightlyUnfavored => -0.07,
                MatchupAdvantage.StronglyUnfavored => -0.15,
                _ => 0.0
            };

            var finalProbability = Math.Clamp(baseProbability + matchupAdjustment, 0.05, 0.95);
            var predictedWinner = finalProbability > 0.5 ? "Player1" : "Player2";

            var prediction = new MatchPrediction
            {
                PredictedWinner = predictedWinner,
                WinProbability = finalProbability,
                Confidence = Math.Min(0.9, 0.5 + Math.Abs(finalProbability - 0.5)),
                KeyFactors = new[] { $"Skill difference: {skillDifference:F0}", $"Matchup: {matchup.Advantage}" },
                PredictedMatchLength = TimeSpan.FromMinutes(3.5)
            };

            return Task.FromResult(Result<MatchPrediction>.Success(prediction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting match");
            return Task.FromResult(Result<MatchPrediction>.Failure($"Prediction failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets character matchup data for prediction analysis.
    /// </summary>
    public Task<CharacterMatchupData> GetCharacterMatchupDataAsync(
        string char1,
        string char2,
        CancellationToken ct)
    {
        return Task.FromResult(new CharacterMatchupData
        {
            Character1 = char1,
            Character2 = char2,
            WinRate = 0.52,
            Advantage = MatchupAdvantage.SlightlyFavored,
            Strengths = new[] { "Good fundamentals", "Strong normals" },
            Weaknesses = new[] { "Susceptible to rushdown", "Limited projectile options" }
        });
    }

    /// <summary>
    /// Calculates confidence interval for predictions.
    /// </summary>
    public ConfidenceInterval CalculateConfidenceInterval(double confidence)
    {
        var margin = (1.0 - confidence) * 0.5;
        return new ConfidenceInterval
        {
            LowerBound = Math.Max(0, confidence - margin),
            UpperBound = Math.Min(1, confidence + margin),
            MarginOfError = margin
        };
    }

    /// <summary>
    /// Identifies potential upsets based on predictions.
    /// </summary>
    public IReadOnlyList<MatchPredictionResult> IdentifyPotentialUpsets(IReadOnlyList<MatchPredictionResult> predictions)
    {
        return predictions.Where(p =>
            p.SkillDifference > 300 &&
            p.PredictedWinner != p.Player1Id &&
            p.Confidence > 0.6)
            .ToList();
    }

    /// <summary>
    /// Calculates overall tournament confidence.
    /// </summary>
    public double CalculateTournamentConfidence(IReadOnlyList<MatchPredictionResult> predictions)
    {
        if (!predictions.Any()) return 0.5;
        return predictions.Average(p => p.Confidence);
    }
}

/// <summary>
/// Match prediction data.
/// </summary>
public class MatchPrediction
{
    public string PredictedWinner { get; set; } = default!;
    public double WinProbability { get; set; }
    public double Confidence { get; set; }
    public IReadOnlyList<string> KeyFactors { get; set; } = default!;
    public TimeSpan PredictedMatchLength { get; set; }
}

/// <summary>
/// Character matchup data.
/// </summary>
public class CharacterMatchupData
{
    public string Character1 { get; set; } = default!;
    public string Character2 { get; set; } = default!;
    public double WinRate { get; set; }
    public MatchupAdvantage Advantage { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = default!;
    public IReadOnlyList<string> Weaknesses { get; set; } = default!;
}

/// <summary>
/// Matchup advantage enumeration.
/// </summary>
public enum MatchupAdvantage
{
    StronglyFavored,
    SlightlyFavored,
    Even,
    SlightlyUnfavored,
    StronglyUnfavored
}

/// <summary>
/// Confidence interval for predictions.
/// </summary>
public class ConfidenceInterval
{
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double MarginOfError { get; set; }
}

/// <summary>
/// Prediction context.
/// </summary>
public class PredictionContext
{
    public string? TournamentId { get; set; }
    public int? Round { get; set; }
}
