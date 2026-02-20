using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.PredictiveAnalytics.Managers;

namespace SaveState.Application.Mugen.Services.PredictiveAnalytics;

/// <summary>
/// Advanced predictive analytics engine using machine learning for match prediction,
/// skill assessment, and performance forecasting in competitive gaming.
/// Acts as a coordinator delegating to specialized managers.
/// </summary>
public class PredictiveAnalyticsEngine : IPredictiveAnalyticsEngine
{
    private readonly ILogger<PredictiveAnalyticsEngine> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;

    // Managers
    private readonly MatchPredictionManager _matchPredictionManager;
    private readonly PlayerSkillManager _playerSkillManager;
    private readonly MachineLearningManager _machineLearningManager;
    private readonly PerformanceForecastingManager _performanceForecastingManager;
    private readonly AnalyticsReportingManager _analyticsReportingManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictiveAnalyticsEngine"/> class.
    /// </summary>
    public PredictiveAnalyticsEngine(
        ILogger<PredictiveAnalyticsEngine> logger,
        ICacheService cache,
        ITimeProvider timeProvider,
        MatchPredictionManager matchPredictionManager,
        PlayerSkillManager playerSkillManager,
        MachineLearningManager machineLearningManager,
        PerformanceForecastingManager performanceForecastingManager,
        AnalyticsReportingManager analyticsReportingManager)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _matchPredictionManager = matchPredictionManager;
        _playerSkillManager = playerSkillManager;
        _machineLearningManager = machineLearningManager;
        _performanceForecastingManager = performanceForecastingManager;
        _analyticsReportingManager = analyticsReportingManager;
    }

    /// <inheritdoc />
    public async Task<Result<MatchPredictionResult>> PredictMatchOutcomeAsync(
        string player1Id,
        string player2Id,
        string character1,
        string character2,
        PredictionContext context,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Predicting match outcome: {P1} ({C1}) vs {P2} ({C2})",
                player1Id, character1, player2Id, character2);

            var player1Skill = await _playerSkillManager.GetPlayerSkillAsync(player1Id, ct);
            var player2Skill = await _playerSkillManager.GetPlayerSkillAsync(player2Id, ct);

            if (!player1Skill.IsSuccess || !player2Skill.IsSuccess)
            {
                return Result<MatchPredictionResult>.Failure("Unable to retrieve player skill data");
            }

            var matchupData = await _matchPredictionManager.GetCharacterMatchupDataAsync(character1, character2, ct);

            var prediction = await _matchPredictionManager.PredictMatchAsync(
                player1Skill.Value,
                player2Skill.Value,
                matchupData,
                context,
                ct);

            if (!prediction.IsSuccess)
            {
                return Result<MatchPredictionResult>.Failure(prediction.Error!);
            }

            var confidenceInterval = _matchPredictionManager.CalculateConfidenceInterval(prediction.Value.Confidence);

            var result = new MatchPredictionResult
            {
                Player1Id = player1Id,
                Player2Id = player2Id,
                Character1 = character1,
                Character2 = character2,
                PredictedWinner = prediction.Value.PredictedWinner,
                WinProbability = prediction.Value.WinProbability,
                Confidence = prediction.Value.Confidence,
                ConfidenceInterval = confidenceInterval,
                KeyFactors = prediction.Value.KeyFactors,
                PredictedMatchLength = prediction.Value.PredictedMatchLength,
                SkillDifference = Math.Abs(player1Skill.Value.Rating - player2Skill.Value.Rating),
                MatchupAdvantage = matchupData.Advantage,
                Context = context,
                GeneratedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Match prediction completed: {Winner} wins with {Prob:P1} probability",
                result.PredictedWinner, result.WinProbability);

            return Result<MatchPredictionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting match outcome");
            return Result<MatchPredictionResult>.Failure($"Prediction failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SkillAssessment>> AssessPlayerSkillAsync(
        string playerId,
        IReadOnlyList<PredictiveMatchResult> recentMatches,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Assessing skill for player {PlayerId} with {Count} recent matches",
                playerId, recentMatches.Count);

            var performanceAnalysis = _playerSkillManager.AnalyzeMatchPerformance(recentMatches, playerId);
            var skillUpdate = await _playerSkillManager.UpdateSkillModelAsync(playerId, performanceAnalysis, ct);

            var assessment = new SkillAssessment
            {
                PlayerId = playerId,
                CurrentRating = skillUpdate.Rating,
                RatingChange = skillUpdate.RatingChange,
                Volatility = skillUpdate.Volatility,
                Confidence = skillUpdate.Confidence,
                SkillTier = _playerSkillManager.DetermineSkillTier(skillUpdate.Rating),
                Strengths = performanceAnalysis.Strengths,
                Weaknesses = performanceAnalysis.Weaknesses,
                Trend = performanceAnalysis.Trend,
                ProjectedRating = await _playerSkillManager.ProjectFutureRatingAsync(playerId, performanceAnalysis, ct),
                AssessmentPeriod = TimeSpan.FromDays(30),
                AssessedAt = _timeProvider.UtcNow
            };

            var cacheKey = $"skill_assessment_{playerId}";
            _cache.Set(cacheKey, assessment, TimeSpan.FromHours(1));

            _logger.LogInformation("Skill assessment completed for {PlayerId}: Rating {Rating:F0}, Tier {Tier}",
                playerId, assessment.CurrentRating, assessment.SkillTier);

            return Result<SkillAssessment>.Success(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing player skill for {PlayerId}", playerId);
            return Result<SkillAssessment>.Failure($"Skill assessment failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<PerformanceForecast>> ForecastPlayerPerformanceAsync(
        string playerId,
        TimeSpan forecastPeriod,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Forecasting performance for player {PlayerId} over {Period}",
                playerId, forecastPeriod);

            var forecast = await _performanceForecastingManager.GenerateForecastAsync(playerId, forecastPeriod, ct);

            var result = new PerformanceForecast
            {
                PlayerId = playerId,
                ForecastPeriod = forecastPeriod,
                PredictedRating = forecast.PredictedRating,
                RatingConfidence = forecast.Confidence,
                ExpectedMatches = forecast.ExpectedMatches,
                WinRateProjection = forecast.WinRateProjection,
                SkillTrend = forecast.SkillTrend,
                KeyInsights = forecast.KeyInsights,
                Recommendations = forecast.Recommendations,
                GeneratedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Performance forecast completed for {PlayerId}: Projected rating {Rating:F0}",
                playerId, result.PredictedRating);

            return Result<PerformanceForecast>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forecasting performance for {PlayerId}", playerId);
            return Result<PerformanceForecast>.Failure($"Forecast failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<TournamentPrediction>> PredictTournamentOutcomeAsync(
        string tournamentId,
        IReadOnlyList<string> participants,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Predicting tournament outcome for {TournamentId} with {Count} participants",
                tournamentId, participants.Count);

            var predictions = new List<MatchPredictionResult>();
            var remainingPlayers = participants.ToList();
            var round = 1;

            while (remainingPlayers.Count > 1)
            {
                var winners = new List<string>();
                for (int i = 0; i < remainingPlayers.Count; i += 2)
                {
                    if (i + 1 < remainingPlayers.Count)
                    {
                        var player1 = remainingPlayers[i];
                        var player2 = remainingPlayers[i + 1];

                        var prediction = await PredictMatchOutcomeAsync(
                            player1, player2, "Default", "Default",
                            new PredictionContext { TournamentId = tournamentId, Round = round }, ct);

                        if (prediction.IsSuccess)
                        {
                            predictions.Add(prediction.Value);
                            winners.Add(prediction.Value.PredictedWinner);
                        }
                    }
                    else
                    {
                        winners.Add(remainingPlayers[i]);
                    }
                }
                remainingPlayers = winners;
                round++;
            }

            var winner = remainingPlayers.FirstOrDefault();
            var winnerProbability = predictions.Any() ? predictions.Last().WinProbability : 0.5;

            var tournamentPrediction = new TournamentPrediction
            {
                TournamentId = tournamentId,
                PredictedWinner = winner,
                WinnerProbability = winnerProbability,
                FinalistPredictions = predictions.Where(p => p.Confidence > 0.7).ToList(),
                RoundPredictions = predictions.GroupBy(p => p.Context?.Round ?? 1)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<MatchPredictionResult>)g.ToList().AsReadOnly()),
                KeyUpsets = _matchPredictionManager.IdentifyPotentialUpsets(predictions),
                Confidence = _matchPredictionManager.CalculateTournamentConfidence(predictions),
                GeneratedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Tournament prediction completed: {Winner} wins tournament with {Prob:P1} probability",
                winner, winnerProbability);

            return Result<TournamentPrediction>.Success(tournamentPrediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting tournament outcome for {TournamentId}", tournamentId);
            return Result<TournamentPrediction>.Failure($"Tournament prediction failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result<AnalyticsReport>> GenerateAnalyticsReportAsync(
        AnalyticsQuery query,
        CancellationToken ct = default)
        => _analyticsReportingManager.GenerateReportAsync(query, ct);

    /// <inheritdoc />
    public async Task<Result<ModelTrainingResult>> TrainPredictionModelAsync(
        IReadOnlyList<TrainingData> trainingData,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Training prediction model with {Count} data points", trainingData.Count);

            var result = await _machineLearningManager.TrainAsync(trainingData, ct);

            if (!result.IsSuccess)
            {
                return Result<ModelTrainingResult>.Failure(result.Error!);
            }

            await _playerSkillManager.UpdateModelsWithTrainingDataAsync(trainingData, ct);

            _logger.LogInformation("Model training completed: Accuracy {Accuracy:P2}, Loss {Loss:F4}",
                result.Value.Accuracy, result.Value.Loss);

            return Result<ModelTrainingResult>.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training prediction model");
            return Result<ModelTrainingResult>.Failure($"Training failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Predictive Analytics Engine interface.
/// </summary>
public interface IPredictiveAnalyticsEngine
{
    Task<Result<MatchPredictionResult>> PredictMatchOutcomeAsync(
        string player1Id, string player2Id, string character1, string character2,
        PredictionContext context, CancellationToken ct = default);

    Task<Result<SkillAssessment>> AssessPlayerSkillAsync(
        string playerId, IReadOnlyList<PredictiveMatchResult> recentMatches, CancellationToken ct = default);

    Task<Result<PerformanceForecast>> ForecastPlayerPerformanceAsync(
        string playerId, TimeSpan forecastPeriod, CancellationToken ct = default);

    Task<Result<TournamentPrediction>> PredictTournamentOutcomeAsync(
        string tournamentId, IReadOnlyList<string> participants, CancellationToken ct = default);

    Task<Result<AnalyticsReport>> GenerateAnalyticsReportAsync(
        AnalyticsQuery query, CancellationToken ct = default);

    Task<Result<ModelTrainingResult>> TrainPredictionModelAsync(
        IReadOnlyList<TrainingData> trainingData, CancellationToken ct = default);
}

/// <summary>
/// Match prediction result.
/// </summary>
public class MatchPredictionResult
{
    public string Player1Id { get; set; } = default!;
    public string Player2Id { get; set; } = default!;
    public string Character1 { get; set; } = default!;
    public string Character2 { get; set; } = default!;
    public string PredictedWinner { get; set; } = default!;
    public double WinProbability { get; set; }
    public double Confidence { get; set; }
    public ConfidenceInterval ConfidenceInterval { get; set; } = default!;
    public IReadOnlyList<string> KeyFactors { get; set; } = default!;
    public TimeSpan PredictedMatchLength { get; set; }
    public double SkillDifference { get; set; }
    public MatchupAdvantage MatchupAdvantage { get; set; }
    public PredictionContext? Context { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Skill assessment data.
/// </summary>
public class SkillAssessment
{
    public string PlayerId { get; set; } = default!;
    public double CurrentRating { get; set; }
    public double RatingChange { get; set; }
    public double Volatility { get; set; }
    public double Confidence { get; set; }
    public SkillTier SkillTier { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = default!;
    public IReadOnlyList<string> Weaknesses { get; set; } = default!;
    public SkillTrend Trend { get; set; }
    public double ProjectedRating { get; set; }
    public TimeSpan AssessmentPeriod { get; set; }
    public DateTime AssessedAt { get; set; }
}

/// <summary>
/// Performance forecast data.
/// </summary>
public class PerformanceForecast
{
    public string PlayerId { get; set; } = default!;
    public TimeSpan ForecastPeriod { get; set; }
    public double PredictedRating { get; set; }
    public double RatingConfidence { get; set; }
    public int ExpectedMatches { get; set; }
    public double WinRateProjection { get; set; }
    public SkillTrend SkillTrend { get; set; }
    public IReadOnlyList<string> KeyInsights { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Tournament prediction data.
/// </summary>
public class TournamentPrediction
{
    public string TournamentId { get; set; } = default!;
    public string? PredictedWinner { get; set; }
    public double WinnerProbability { get; set; }
    public IReadOnlyList<MatchPredictionResult> FinalistPredictions { get; set; } = default!;
    public IReadOnlyDictionary<int, IReadOnlyList<MatchPredictionResult>> RoundPredictions { get; set; } = default!;
    public IReadOnlyList<MatchPredictionResult> KeyUpsets { get; set; } = default!;
    public double Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
}
