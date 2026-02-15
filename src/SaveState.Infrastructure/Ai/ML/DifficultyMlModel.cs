using Microsoft.Extensions.Logging;
using SaveState.Core.AI.Assistant;
using SaveState.Core.Assistant.Services;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Text.Json;

namespace SaveState.Infrastructure.AI.ML;

/// <summary>
/// ML-based difficulty analysis model using a weighted scoring approach.
/// This implementation uses a deterministic algorithm that can be trained with data
/// without requiring external ML libraries.
/// </summary>
public sealed class DifficultyMlModel : IDisposable
{
    private readonly ILogger _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _modelPath;
    private ModelWeights _weights;
    private bool _hasPersistedModel;
    private bool _isDisposed;

    public DifficultyMlModel(
        ILogger logger,
        ITimeProvider timeProvider,
        string? modelPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _modelPath = modelPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "MLModels",
            "difficulty_model.json");
        var loadedWeights = LoadWeights();
        _weights = loadedWeights ?? CreateDefaultWeights();
        _hasPersistedModel = loadedWeights is not null;
    }

    /// <summary>
    /// Gets whether a trained model is loaded.
    /// </summary>
    public bool IsModelLoaded => _hasPersistedModel && !_isDisposed;

    /// <summary>
    /// Gets the model version/timestamp.
    /// </summary>
    public DateTime? ModelVersion => _weights?.LastTrainedAtUtc;

    /// <summary>
    /// Predicts difficulty adjustment for given gameplay metrics.
    /// </summary>
    public Result<DifficultyPrediction> Predict(PlayerBehaviorMetrics metrics)
    {
        if (_isDisposed)
        {
            return Result.Failure<DifficultyPrediction>(
                "Model has been disposed.",
                ErrorType.Validation);
        }

        try
        {
            // Calculate feature scores
            var frustrationScore = CalculateFrustrationScore(metrics);
            var masteryScore = CalculateMasteryScore(metrics);

            // Apply weights
            var decreaseScore = _weights.FrustrationWeight * frustrationScore;
            var increaseScore = _weights.MasteryWeight * masteryScore;
            var maintainScore = _weights.BaselineWeight * (1 - frustrationScore) * (1 - masteryScore);

            // Normalize scores to probabilities
            var total = decreaseScore + maintainScore + increaseScore;
            if (total < 0.001f) total = 1f;

            var probs = new float[3]
            {
                decreaseScore / total,
                maintainScore / total,
                increaseScore / total
            };

            // Determine prediction
            var maxIndex = Array.IndexOf(probs, probs.Max());
            var prediction = new DifficultyPrediction
            {
                PredictedDifficulty = maxIndex switch
                {
                    0 => "Decrease",
                    2 => "Increase",
                    _ => "Maintain"
                },
                Scores = new[] { decreaseScore, maintainScore, increaseScore },
                Probabilities = probs
            };

            return Result.Success(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make difficulty prediction");
            return Result.Failure<DifficultyPrediction>(
                $"Prediction failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Trains the model using provided training data.
    /// </summary>
    public Result TrainModel(IEnumerable<DifficultyTrainingData> trainingData)
    {
        if (_isDisposed)
        {
            return Result.Failure("Model has been disposed.", ErrorType.Validation);
        }

        try
        {
            var data = trainingData.ToList();
            if (data.Count < 10)
            {
                return Result.Failure("At least 10 training samples required.", ErrorType.Validation);
            }

            _logger.LogInformation("Starting model training with {Count} samples", data.Count);

            // Calculate optimal weights using simple gradient descent
            var newWeights = OptimizeWeights(data);
            _weights = newWeights;
            _weights.LastTrainedAtUtc = _timeProvider.UtcNow;
            _hasPersistedModel = true;

            // Persist weights
            SaveWeights();

            _logger.LogInformation("Model training completed");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train difficulty model");
            return Result.Failure($"Training failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Calculates accuracy metrics on test data.
    /// </summary>
    public ModelMetrics CalculateMetrics(IEnumerable<DifficultyTrainingData> testData)
    {
        var data = testData.ToList();
        if (data.Count == 0)
        {
            return new ModelMetrics(0, 0, 0, 0, 0, DateTime.UtcNow, "0.0.0");
        }

        var correct = 0;
        var truePositives = new Dictionary<string, int> { ["Decrease"] = 0, ["Maintain"] = 0, ["Increase"] = 0 };
        var falsePositives = new Dictionary<string, int> { ["Decrease"] = 0, ["Maintain"] = 0, ["Increase"] = 0 };
        var falseNegatives = new Dictionary<string, int> { ["Decrease"] = 0, ["Maintain"] = 0, ["Increase"] = 0 };

        foreach (var item in data)
        {
            var metrics = ConvertToPlayerMetrics(item);
            var prediction = Predict(metrics);
            
            if (prediction.IsSuccess)
            {
                var predicted = prediction.Value.PredictedDifficulty;
                var actual = item.Label;

                if (predicted == actual)
                {
                    correct++;
                    truePositives[actual]++;
                }
                else
                {
                    falsePositives[predicted]++;
                    falseNegatives[actual]++;
                }
            }
        }

        var accuracy = (float)correct / data.Count;
        
        // Calculate precision and recall as averages across classes
        var precision = CalculateAverageMetric(truePositives, falsePositives, (tp, fp) => tp + fp > 0 ? (float)tp / (tp + fp) : 0);
        var recall = CalculateAverageMetric(truePositives, falseNegatives, (tp, fn) => tp + fn > 0 ? (float)tp / (tp + fn) : 0);
        var f1Score = precision + recall > 0 ? 2 * (precision * recall) / (precision + recall) : 0;

        return new ModelMetrics(
            accuracy,
            precision,
            recall,
            f1Score,
            data.Count,
            _weights.LastTrainedAtUtc,
            _weights.Version);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _logger.LogDebug("Difficulty ML model disposed");
    }

    private static float CalculateFrustrationScore(PlayerBehaviorMetrics metrics)
    {
        var score = 0f;

        // Deaths contribution
        if (metrics.DeathCount >= 10) score += 0.35f;
        else if (metrics.DeathCount >= 5) score += 0.2f;
        else if (metrics.DeathCount >= 3) score += 0.1f;

        // Retries contribution
        if (metrics.RetryCount >= 8) score += 0.25f;
        else if (metrics.RetryCount >= 4) score += 0.15f;

        // Time stuck contribution
        if (metrics.TimeInCurrentSection >= TimeSpan.FromMinutes(20)) score += 0.2f;
        else if (metrics.TimeInCurrentSection >= TimeSpan.FromMinutes(12)) score += 0.1f;

        // Error rate contribution
        if (metrics.InputErrorRate >= 0.35f) score += 0.15f;
        else if (metrics.InputErrorRate >= 0.2f) score += 0.08f;

        // Input pattern contribution
        if (metrics.HasRapidInputBursts) score += 0.05f;

        return Math.Min(1f, score);
    }

    private static float CalculateMasteryScore(PlayerBehaviorMetrics metrics)
    {
        var score = 0f;

        // Low deaths
        if (metrics.DeathCount <= 1) score += 0.3f;
        else if (metrics.DeathCount <= 3) score += 0.2f;

        // Low retries
        if (metrics.RetryCount <= 1) score += 0.25f;
        else if (metrics.RetryCount <= 3) score += 0.15f;

        // Fast section completion
        if (metrics.TimeInCurrentSection <= TimeSpan.FromMinutes(5)) score += 0.2f;
        else if (metrics.TimeInCurrentSection <= TimeSpan.FromMinutes(10)) score += 0.1f;

        // Low error rate
        if (metrics.InputErrorRate <= 0.05f) score += 0.15f;
        else if (metrics.InputErrorRate <= 0.1f) score += 0.08f;

        // Good APM without idle spikes
        if (metrics.ActionsPerMinute >= 50 && !metrics.HasIdleSpikes) score += 0.1f;

        return Math.Min(1f, score);
    }

    private ModelWeights OptimizeWeights(List<DifficultyTrainingData> trainingData)
    {
        var weights = CreateDefaultWeights();
        const int iterations = 100;
        const float learningRate = 0.01f;

        for (int iter = 0; iter < iterations; iter++)
        {
            var gradientF = 0f;
            var gradientM = 0f;
            var errors = 0;

            foreach (var item in trainingData)
            {
                var metrics = ConvertToPlayerMetrics(item);
                var frustration = CalculateFrustrationScore(metrics);
                var mastery = CalculateMasteryScore(metrics);

                var decreaseScore = weights.FrustrationWeight * frustration;
                var increaseScore = weights.MasteryWeight * mastery;
                var maintainScore = weights.BaselineWeight * (1 - frustration) * (1 - mastery);

                var predicted = decreaseScore > maintainScore && decreaseScore > increaseScore ? "Decrease" :
                               increaseScore > maintainScore ? "Increase" : "Maintain";

                if (predicted != item.Label)
                {
                    errors++;
                    // Simple gradient update
                    if (item.Label == "Decrease")
                        gradientF += frustration;
                    else if (item.Label == "Increase")
                        gradientM += mastery;
                }
            }

            // Update weights
            weights.FrustrationWeight += learningRate * gradientF / Math.Max(1, trainingData.Count);
            weights.MasteryWeight += learningRate * gradientM / Math.Max(1, trainingData.Count);
            weights.BaselineWeight = Math.Max(0.1f, 1 - weights.FrustrationWeight - weights.MasteryWeight);

            // Normalize
            var total = weights.FrustrationWeight + weights.MasteryWeight + weights.BaselineWeight;
            weights.FrustrationWeight /= total;
            weights.MasteryWeight /= total;
            weights.BaselineWeight /= total;
        }

        weights.Version = $"1.0.{trainingData.Count}";
        return weights;
    }

    private static PlayerBehaviorMetrics ConvertToPlayerMetrics(DifficultyTrainingData data)
    {
        return new PlayerBehaviorMetrics
        {
            SessionId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SessionStartTimeUtc = DateTime.UtcNow.AddHours(-1),
            TimestampUtc = DateTime.UtcNow,
            DeathCount = data.DeathCount,
            RetryCount = data.RetryCount,
            TimeInCurrentSection = TimeSpan.FromMinutes(data.TimeInCurrentSectionMinutes),
            TotalSessionDuration = TimeSpan.FromMinutes(data.TotalSessionDurationMinutes),
            ActionsPerMinute = data.ActionsPerMinute,
            InputErrorRate = data.InputErrorRate,
            HasRapidInputBursts = data.HasRapidInputBursts,
            HasIdleSpikes = data.HasIdleSpikes,
            PauseCount = data.PauseCount,
            TotalPausedTime = TimeSpan.FromMinutes(data.TotalPausedTimeMinutes),
            CurrentDifficultyLevel = null
        };
    }

    private static float CalculateAverageMetric(
        Dictionary<string, int> truePositives,
        Dictionary<string, int> falseMetrics,
        Func<int, int, float> calculator)
    {
        var scores = new List<float>();
        foreach (var label in truePositives.Keys)
        {
            scores.Add(calculator(truePositives[label], falseMetrics[label]));
        }
        return scores.Count > 0 ? scores.Average() : 0;
    }

    private void SaveWeights()
    {
        try
        {
            var directory = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_weights, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_modelPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save model weights");
        }
    }

    private ModelWeights? LoadWeights()
    {
        try
        {
            if (!File.Exists(_modelPath))
            {
                return null;
            }

            var json = File.ReadAllText(_modelPath);
            return JsonSerializer.Deserialize<ModelWeights>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model weights");
            return null;
        }
    }

    private static ModelWeights CreateDefaultWeights()
    {
        return new ModelWeights
        {
            FrustrationWeight = 0.5f,
            MasteryWeight = 0.3f,
            BaselineWeight = 0.2f,
            Version = "1.0.0",
            LastTrainedAtUtc = DateTime.UtcNow
        };
    }

    private class ModelWeights
    {
        public float FrustrationWeight { get; set; }
        public float MasteryWeight { get; set; }
        public float BaselineWeight { get; set; }
        public string Version { get; set; } = "1.0.0";
        public DateTime LastTrainedAtUtc { get; set; }
    }
}

/// <summary>
/// Training data structure for difficulty classification.
/// </summary>
public sealed class DifficultyTrainingData
{
    public int DeathCount { get; set; }
    public int RetryCount { get; set; }
    public float ActionsPerMinute { get; set; }
    public float InputErrorRate { get; set; }
    public float TotalSessionDurationMinutes { get; set; }
    public float TimeInCurrentSectionMinutes { get; set; }
    public bool HasRapidInputBursts { get; set; }
    public bool HasIdleSpikes { get; set; }
    public int PauseCount { get; set; }
    public float TotalPausedTimeMinutes { get; set; }
    public string Label { get; set; } = "Maintain"; // "Decrease", "Maintain", "Increase"
}

/// <summary>
/// Prediction output from the difficulty model.
/// </summary>
public sealed class DifficultyPrediction
{
    public string PredictedDifficulty { get; set; } = "Maintain";
    public float[] Scores { get; set; } = Array.Empty<float>();
    public float[] Probabilities { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets the confidence score for the predicted difficulty.
    /// </summary>
    public float GetConfidence()
    {
        if (Probabilities != null && Probabilities.Length >= 3)
        {
            return Probabilities.Max();
        }

        if (Scores != null && Scores.Length >= 3)
        {
            return CalculateSoftmaxProbabilities(Scores).Max();
        }

        return 0.5f;
    }

    /// <summary>
    /// Gets all difficulty probabilities.
    /// </summary>
    public IReadOnlyDictionary<SuggestedDifficulty, float> GetProbabilities()
    {
        if (Probabilities == null || Probabilities.Length < 3)
        {
            if (Scores != null && Scores.Length >= 3)
            {
                var derived = CalculateSoftmaxProbabilities(Scores);
                return new Dictionary<SuggestedDifficulty, float>
                {
                    [SuggestedDifficulty.Decrease] = derived[0],
                    [SuggestedDifficulty.Maintain] = derived[1],
                    [SuggestedDifficulty.Increase] = derived[2]
                };
            }

            return new Dictionary<SuggestedDifficulty, float>
            {
                [SuggestedDifficulty.Decrease] = 0.33f,
                [SuggestedDifficulty.Maintain] = 0.34f,
                [SuggestedDifficulty.Increase] = 0.33f
            };
        }

        return new Dictionary<SuggestedDifficulty, float>
        {
            [SuggestedDifficulty.Decrease] = Probabilities[0],
            [SuggestedDifficulty.Maintain] = Probabilities[1],
            [SuggestedDifficulty.Increase] = Probabilities[2]
        };
    }

    private static float[] CalculateSoftmaxProbabilities(float[] scores)
    {
        var maxScore = scores.Max();
        var exp = scores.Select(s => MathF.Exp(s - maxScore)).ToArray();
        var sum = exp.Sum();
        if (sum <= 0)
        {
            return [0.33f, 0.34f, 0.33f];
        }

        return
        [
            exp[0] / sum,
            exp[1] / sum,
            exp[2] / sum
        ];
    }
}
