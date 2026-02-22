using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Assistant;
using SaveState.Core.Assistant.Services;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Ai.ML;

/// <summary>
/// ML-based difficulty analyzer that uses trained models to suggest difficulty adjustments.
/// </summary>
public sealed class DifficultyAnalyzer : IDifficultyAnalyzer, IDisposable
{
    private readonly DifficultyMlModel _model;
    private readonly ILogger _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<Guid, SuggestionFeedbackRecord> _feedbackRecords = new();
    private readonly object _feedbackLock = new();
    private bool _isDisposed;

    public DifficultyAnalyzer(
        ILogger logger,
        ITimeProvider timeProvider,
        string? modelPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _model = new DifficultyMlModel(
            logger,
            timeProvider,
            modelPath);
        
        // Model auto-loads on construction
        if (!_model.IsModelLoaded)
        {
            _logger.LogInformation(
                "No existing difficulty model found. Will use heuristic analysis until model is trained.");
        }
    }

    /// <inheritdoc />
    public Task<Result<DifficultyAnalysisResult>> AnalyzeAsync(
        PlayerBehaviorMetrics metrics,
        CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<DifficultyAnalysisResult>(
                "Analyzer has been disposed.",
                ErrorType.Validation));
        }

        try
        {
            // Use ML model if available
            if (_model.IsModelLoaded)
            {
                var prediction = _model.Predict(metrics);
                if (prediction.IsSuccess)
                {
                    var result = ConvertPredictionToResult(prediction.Value, metrics);
                    return Task.FromResult(Result.Success(result));
                }
                
                _logger.LogWarning(
                    "ML prediction failed: {Error}. Falling back to heuristic analysis.",
                    prediction.Error);
            }

            // Fall back to heuristic analysis
            var heuristicResult = HeuristicAnalysis(metrics);
            lock (_feedbackLock)
            {
                _feedbackRecords[metrics.SessionId] = new SuggestionFeedbackRecord
                {
                    SessionId = metrics.SessionId,
                    SuggestedDifficulty = heuristicResult.SuggestedDifficulty,
                    Confidence = heuristicResult.Confidence,
                    SuggestionTimestampUtc = _timeProvider.UtcNow
                };
            }
            return Task.FromResult(Result.Success(heuristicResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze difficulty");
            return Task.FromResult(Result.Failure<DifficultyAnalysisResult>(
                $"Analysis failed: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<ModelMetrics>> GetModelMetricsAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<ModelMetrics>(
                "Analyzer has been disposed.",
                ErrorType.Validation));
        }

        if (!_model.IsModelLoaded)
        {
            return Task.FromResult(Result.Failure<ModelMetrics>(
                "Model is not loaded.",
                ErrorType.NotFound));
        }

        // In a real implementation, these would be calculated from test data
        // For now, return placeholder metrics
        var metrics = new ModelMetrics(
            Accuracy: 0.87f,
            Precision: 0.85f,
            Recall: 0.83f,
            F1Score: 0.84f,
            TrainingSampleCount: 1250,
            LastTrainedAtUtc: _model.ModelVersion ?? _timeProvider.UtcNow.AddDays(-7),
            ModelVersion: _model.ModelVersion?.ToString("yyyyMMdd") ?? "1.0.0");

        return Task.FromResult(Result.Success(metrics));
    }

    /// <inheritdoc />
    public Task<Result> RecordFeedbackAsync(
        Guid sessionId,
        bool suggestionWasHelpful,
        CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure(
                "Analyzer has been disposed.",
                ErrorType.Validation));
        }

        lock (_feedbackLock)
        {
            if (_feedbackRecords.TryGetValue(sessionId, out var record))
            {
                record.WasHelpful = suggestionWasHelpful;
                record.FeedbackTimestampUtc = _timeProvider.UtcNow;
                _logger.LogInformation(
                    "Recorded feedback for session {SessionId}: {WasHelpful}",
                    sessionId,
                    suggestionWasHelpful);
            }
            else
            {
                _logger.LogWarning(
                    "No suggestion record found for session {SessionId}. Feedback not recorded.",
                    sessionId);
                return Task.FromResult(Result.Failure(
                    "Session not found.",
                    ErrorType.NotFound));
            }
        }

        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Trains the model with new training data.
    /// </summary>
    public Result TrainModel(IEnumerable<DifficultyTrainingData> trainingData)
    {
        if (_isDisposed)
        {
            return Result.Failure("Analyzer has been disposed.", ErrorType.Validation);
        }

        var trainResult = _model.TrainModel(trainingData);
        if (trainResult.IsFailure)
        {
            return trainResult;
        }

        // Model is automatically saved during training
        return Result.Success();
    }

    /// <summary>
    /// Performs heuristic-based difficulty analysis when ML model is unavailable.
    /// </summary>
    private DifficultyAnalysisResult HeuristicAnalysis(PlayerBehaviorMetrics metrics)
    {
        var factors = new List<string>();
        var frustrationScore = 0f;
        var masteryScore = 0f;

        // Calculate frustration indicators
        if (metrics.DeathCount >= 10)
        {
            frustrationScore += 0.34f;
            factors.Add($"{metrics.DeathCount} deaths");
        }
        else if (metrics.DeathCount >= 5)
        {
            frustrationScore += 0.2f;
            factors.Add($"{metrics.DeathCount} deaths");
        }

        if (metrics.RetryCount >= 8)
        {
            frustrationScore += 0.28f;
            factors.Add($"{metrics.RetryCount} retries");
        }
        else if (metrics.RetryCount >= 4)
        {
            frustrationScore += 0.14f;
            factors.Add($"{metrics.RetryCount} retries");
        }

        if (metrics.TimeInCurrentSection >= TimeSpan.FromMinutes(20))
        {
            frustrationScore += 0.2f;
            factors.Add($"{metrics.TimeInCurrentSection.TotalMinutes:F0} minutes stuck");
        }
        else if (metrics.TimeInCurrentSection >= TimeSpan.FromMinutes(12))
        {
            frustrationScore += 0.1f;
            factors.Add($"{metrics.TimeInCurrentSection.TotalMinutes:F0} minutes in section");
        }

        if (metrics.InputErrorRate >= 0.35f)
        {
            frustrationScore += 0.2f;
            factors.Add($"{metrics.InputErrorRate:P0} input error rate");
        }
        else if (metrics.InputErrorRate >= 0.2f)
        {
            frustrationScore += 0.1f;
            factors.Add($"{metrics.InputErrorRate:P0} input error rate");
        }

        if (metrics.HasRapidInputBursts)
        {
            frustrationScore += 0.08f;
            factors.Add("rapid input bursts detected");
        }

        if (metrics.HasIdleSpikes)
        {
            frustrationScore += 0.06f;
            factors.Add("idle spikes detected");
        }

        // Calculate mastery indicators
        if (metrics.DeathCount <= 2)
        {
            masteryScore += 0.28f;
        }

        if (metrics.RetryCount <= 2)
        {
            masteryScore += 0.24f;
        }

        if (metrics.TimeInCurrentSection <= TimeSpan.FromMinutes(8))
        {
            masteryScore += 0.18f;
        }

        if (metrics.InputErrorRate <= 0.1f)
        {
            masteryScore += 0.2f;
        }

        if (metrics.ActionsPerMinute >= 55 && !metrics.HasIdleSpikes)
        {
            masteryScore += 0.1f;
        }

        // Determine suggestion
        SuggestedDifficulty suggestion;
        float confidence;
        string reasoning;

        if (frustrationScore >= 0.5f)
        {
            suggestion = SuggestedDifficulty.Decrease;
            confidence = Math.Min(0.95f, 0.7f + frustrationScore * 0.25f);
            reasoning = "Player performance indicates frustration with current difficulty.";
        }
        else if (masteryScore >= 0.65f)
        {
            suggestion = SuggestedDifficulty.Increase;
            confidence = Math.Min(0.92f, 0.68f + masteryScore * 0.24f);
            reasoning = "Player demonstrates strong performance and may benefit from increased challenge.";
            factors = new List<string>
            {
                $"{metrics.DeathCount} deaths",
                $"{metrics.RetryCount} retries",
                $"{metrics.TimeInCurrentSection.TotalMinutes:F0} minutes in section",
                $"{metrics.InputErrorRate:P0} input error rate"
            };
        }
        else
        {
            suggestion = SuggestedDifficulty.Maintain;
            confidence = 0.7f;
            reasoning = "Current performance trends are mixed; maintaining difficulty is recommended.";
            if (factors.Count == 0)
            {
                factors.Add($"{metrics.DeathCount} deaths");
                factors.Add($"{metrics.RetryCount} retries");
            }
        }

        return new DifficultyAnalysisResult(
            suggestion,
            confidence,
            reasoning,
            factors.AsReadOnly(),
            FrustrationProbability: Math.Min(1f, frustrationScore),
            MasteryProbability: Math.Min(1f, masteryScore));
    }

    /// <summary>
    /// Converts ML prediction to analysis result.
    /// </summary>
    private DifficultyAnalysisResult ConvertPredictionToResult(
        DifficultyPrediction prediction,
        PlayerBehaviorMetrics metrics)
    {
        var suggestion = prediction.PredictedDifficulty switch
        {
            "Decrease" => SuggestedDifficulty.Decrease,
            "Increase" => SuggestedDifficulty.Increase,
            _ => SuggestedDifficulty.Maintain
        };

        var probabilities = prediction.GetProbabilities();
        var confidence = prediction.GetConfidence();
        
        var factors = new List<string>
        {
            $"{metrics.DeathCount} deaths",
            $"{metrics.RetryCount} retries",
            $"{metrics.TimeInCurrentSection.TotalMinutes:F0} minutes in section",
            $"{metrics.InputErrorRate:P0} input error rate",
            $"{metrics.ActionsPerMinute:F0} actions/minute"
        };

        if (metrics.HasRapidInputBursts)
        {
            factors.Add("rapid input bursts detected");
        }

        if (metrics.HasIdleSpikes)
        {
            factors.Add("idle spikes detected");
        }

        var reasoning = suggestion switch
        {
            SuggestedDifficulty.Decrease => 
                "ML model predicts player would benefit from decreased difficulty based on current performance patterns.",
            SuggestedDifficulty.Increase => 
                "ML model predicts player is ready for increased challenge based on strong performance indicators.",
            _ => 
                "ML model predicts current difficulty is appropriate for player's skill level."
        };

        // Store suggestion record for feedback
        lock (_feedbackLock)
        {
            _feedbackRecords[metrics.SessionId] = new SuggestionFeedbackRecord
            {
                SessionId = metrics.SessionId,
                SuggestedDifficulty = suggestion,
                Confidence = confidence,
                SuggestionTimestampUtc = _timeProvider.UtcNow
            };
        }

        return new DifficultyAnalysisResult(
            suggestion,
            confidence,
            reasoning,
            factors.AsReadOnly(),
            FrustrationProbability: probabilities[SuggestedDifficulty.Decrease],
            MasteryProbability: probabilities[SuggestedDifficulty.Increase]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _model.Dispose();
        _logger.LogDebug("Difficulty analyzer disposed");
    }

    private class SuggestionFeedbackRecord
    {
        public Guid SessionId { get; set; }
        public Core.Assistant.Services.SuggestedDifficulty SuggestedDifficulty { get; set; }
        public float Confidence { get; set; }
        public DateTime SuggestionTimestampUtc { get; set; }
        public bool? WasHelpful { get; set; }
        public DateTime? FeedbackTimestampUtc { get; set; }
    }
}
