using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Assistant;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Ai.ML;

/// <summary>
/// Training pipeline for the difficulty analysis ML model.
/// Handles data collection, preprocessing, training, and validation.
/// </summary>
public sealed class DifficultyModelTrainingPipeline
{
    private readonly ILogger _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly DifficultyAnalyzer _analyzer;
    private readonly List<DifficultyTrainingData> _trainingData = new();
    private readonly List<GameplaySessionRecord> _sessionRecords = new();
    private readonly object _dataLock = new();

    public DifficultyModelTrainingPipeline(
        ILogger logger,
        ITimeProvider timeProvider,
        string? modelPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _analyzer = new DifficultyAnalyzer(
            logger,
            timeProvider,
            modelPath);
    }

    /// <summary>
    /// Records a gameplay session for training data collection.
    /// </summary>
    public Result RecordSession(GameplaySessionRecord session)
    {
        if (session == null)
        {
            return Result.Failure("Session cannot be null.", ErrorType.Validation);
        }

        if (session.EndTimeUtc == default)
        {
            return Result.Failure("Session must be completed before recording.", ErrorType.Validation);
        }

        lock (_dataLock)
        {
            _sessionRecords.Add(session);
            
            // Convert session to training data if difficulty adjustment was made
            if (session.ActualDifficultyAdjustment.HasValue)
            {
                var trainingData = ConvertSessionToTrainingData(session);
                _trainingData.Add(trainingData);
            }
        }

        _logger.LogDebug(
            "Recorded session {SessionId}. Total sessions: {TotalSessions}, Training samples: {TrainingSamples}",
            session.SessionId,
            _sessionRecords.Count,
            _trainingData.Count);

        return Result.Success();
    }

    /// <summary>
    /// Records multiple gameplay sessions.
    /// </summary>
    public Result RecordSessions(IEnumerable<GameplaySessionRecord> sessions)
    {
        var count = 0;
        foreach (var session in sessions)
        {
            var result = RecordSession(session);
            if (result.IsSuccess)
            {
                count++;
            }
        }

        _logger.LogInformation("Recorded {Count} sessions", count);
        return Result.Success();
    }

    /// <summary>
    /// Trains the model using collected training data.
    /// </summary>
    public Result TrainModel()
    {
        List<DifficultyTrainingData> data;
        lock (_dataLock)
        {
            data = new List<DifficultyTrainingData>(_trainingData);
        }

        if (data.Count < 50)
        {
            _logger.LogWarning(
                "Insufficient training data. Have {Count} samples, need at least 50.",
                data.Count);
            return Result.Failure(
                "Insufficient training data. At least 50 labeled samples required.",
                ErrorType.Validation);
        }

        _logger.LogInformation("Starting model training with {Count} samples", data.Count);

        var result = _analyzer.TrainModel(data);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Model training completed successfully");
        }
        else
        {
            _logger.LogError("Model training failed: {Error}", result.Error);
        }

        return result;
    }

    /// <summary>
    /// Generates synthetic training data for initial model bootstrap.
    /// </summary>
    public Result GenerateSyntheticTrainingData(int sampleCount = 1000)
    {
        if (sampleCount < 100)
        {
            return Result.Failure("At least 100 samples required.", ErrorType.Validation);
        }

        _logger.LogInformation("Generating {Count} synthetic training samples", sampleCount);

        var random = new Random(42); // Fixed seed for reproducibility
        var syntheticData = new List<DifficultyTrainingData>();

        for (int i = 0; i < sampleCount; i++)
        {
            // Generate realistic gameplay scenarios
            var scenario = GenerateScenario(random);
            var trainingData = CreateTrainingDataFromScenario(scenario);
            syntheticData.Add(trainingData);
        }

        lock (_dataLock)
        {
            _trainingData.AddRange(syntheticData);
        }

        _logger.LogInformation("Generated {Count} synthetic training samples", syntheticData.Count);
        return Result.Success();
    }

    /// <summary>
    /// Exports training data to a CSV file.
    /// </summary>
    public Result ExportTrainingData(string filePath)
    {
        List<DifficultyTrainingData> data;
        lock (_dataLock)
        {
            data = new List<DifficultyTrainingData>(_trainingData);
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(filePath);
            
            // Write header
            writer.WriteLine("DeathCount,RetryCount,ActionsPerMinute,InputErrorRate," +
                "TotalSessionDurationMinutes,TimeInCurrentSectionMinutes," +
                "HasRapidInputBursts,HasIdleSpikes,PauseCount,TotalPausedTimeMinutes,Label");

            // Write data
            foreach (var record in data)
            {
                writer.WriteLine($"{record.DeathCount},{record.RetryCount},{record.ActionsPerMinute:F2}," +
                    $"{record.InputErrorRate:F4},{record.TotalSessionDurationMinutes:F2}," +
                    $"{record.TimeInCurrentSectionMinutes:F2},{record.HasRapidInputBursts}," +
                    $"{record.HasIdleSpikes},{record.PauseCount},{record.TotalPausedTimeMinutes:F2}," +
                    $"{record.Label}");
            }

            _logger.LogInformation("Exported {Count} training records to {FilePath}", data.Count, filePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export training data to {FilePath}", filePath);
            return Result.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Imports training data from a CSV file.
    /// </summary>
    public Result ImportTrainingData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result.Failure($"File not found: {filePath}", ErrorType.NotFound);
        }

        try
        {
            var importedData = new List<DifficultyTrainingData>();
            using var reader = new StreamReader(filePath);
            
            // Skip header
            reader.ReadLine();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if (parts.Length >= 11)
                {
                    var data = new DifficultyTrainingData
                    {
                        DeathCount = int.Parse(parts[0]),
                        RetryCount = int.Parse(parts[1]),
                        ActionsPerMinute = float.Parse(parts[2]),
                        InputErrorRate = float.Parse(parts[3]),
                        TotalSessionDurationMinutes = float.Parse(parts[4]),
                        TimeInCurrentSectionMinutes = float.Parse(parts[5]),
                        HasRapidInputBursts = bool.Parse(parts[6]),
                        HasIdleSpikes = bool.Parse(parts[7]),
                        PauseCount = int.Parse(parts[8]),
                        TotalPausedTimeMinutes = float.Parse(parts[9]),
                        Label = parts[10]
                    };
                    importedData.Add(data);
                }
            }

            lock (_dataLock)
            {
                _trainingData.AddRange(importedData);
            }

            _logger.LogInformation("Imported {Count} training records from {FilePath}", importedData.Count, filePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import training data from {FilePath}", filePath);
            return Result.Failure($"Import failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets statistics about collected training data.
    /// </summary>
    public TrainingDataStatistics GetStatistics()
    {
        lock (_dataLock)
        {
            var decreaseCount = _trainingData.Count(d => d.Label == "Decrease");
            var maintainCount = _trainingData.Count(d => d.Label == "Maintain");
            var increaseCount = _trainingData.Count(d => d.Label == "Increase");

            return new TrainingDataStatistics(
                TotalSessions: _sessionRecords.Count,
                LabeledSamples: _trainingData.Count,
                DecreaseSamples: decreaseCount,
                MaintainSamples: maintainCount,
                IncreaseSamples: increaseCount,
                AverageDeaths: _trainingData.Any() ? _trainingData.Average(d => d.DeathCount) : 0,
                AverageRetries: _trainingData.Any() ? _trainingData.Average(d => d.RetryCount) : 0,
                LastUpdatedAtUtc: _timeProvider.UtcNow);
        }
    }

    /// <summary>
    /// Clears all training data.
    /// </summary>
    public void ClearTrainingData()
    {
        lock (_dataLock)
        {
            _trainingData.Clear();
            _sessionRecords.Clear();
        }
        _logger.LogInformation("Training data cleared");
    }

    private static DifficultyTrainingData ConvertSessionToTrainingData(GameplaySessionRecord session)
    {
        var totalDuration = session.EndTimeUtc - session.StartTimeUtc;
        
        return new DifficultyTrainingData
        {
            DeathCount = session.DeathCount,
            RetryCount = session.RetryCount,
            ActionsPerMinute = session.AverageActionsPerMinute,
            InputErrorRate = session.InputErrorRate,
            TotalSessionDurationMinutes = (float)totalDuration.TotalMinutes,
            TimeInCurrentSectionMinutes = (float)session.TimeInCurrentSection.TotalMinutes,
            HasRapidInputBursts = session.HasRapidInputBursts,
            HasIdleSpikes = session.HasIdleSpikes,
            PauseCount = session.PauseCount,
            TotalPausedTimeMinutes = (float)session.TotalPausedTime.TotalMinutes,
            Label = session.ActualDifficultyAdjustment switch
            {
                SuggestedDifficulty.Decrease => "Decrease",
                SuggestedDifficulty.Increase => "Increase",
                _ => "Maintain"
            }
        };
    }

    private static GameplayScenario GenerateScenario(Random random)
    {
        // Generate realistic gameplay scenarios with some randomness
        var difficulty = random.NextDouble();
        
        if (difficulty < 0.33)
        {
            // Frustrated player scenario - should suggest decrease
            return new GameplayScenario
            {
                DeathCount = random.Next(8, 20),
                RetryCount = random.Next(6, 15),
                ActionsPerMinute = (float)(random.NextDouble() * 30 + 20),
                InputErrorRate = (float)(random.NextDouble() * 0.3 + 0.2),
                SessionDurationMinutes = (float)(random.NextDouble() * 40 + 20),
                SectionTimeMinutes = (float)(random.NextDouble() * 20 + 10),
                HasRapidInputBursts = random.NextDouble() > 0.3,
                HasIdleSpikes = random.NextDouble() > 0.4,
                PauseCount = random.Next(2, 8),
                ExpectedDifficulty = "Decrease"
            };
        }
        else if (difficulty < 0.66)
        {
            // Average player scenario - should suggest maintain
            return new GameplayScenario
            {
                DeathCount = random.Next(2, 8),
                RetryCount = random.Next(2, 6),
                ActionsPerMinute = (float)(random.NextDouble() * 40 + 40),
                InputErrorRate = (float)(random.NextDouble() * 0.2 + 0.05),
                SessionDurationMinutes = (float)(random.NextDouble() * 30 + 30),
                SectionTimeMinutes = (float)(random.NextDouble() * 10 + 5),
                HasRapidInputBursts = random.NextDouble() > 0.6,
                HasIdleSpikes = random.NextDouble() > 0.6,
                PauseCount = random.Next(0, 4),
                ExpectedDifficulty = "Maintain"
            };
        }
        else
        {
            // Skilled player scenario - should suggest increase
            return new GameplayScenario
            {
                DeathCount = random.Next(0, 3),
                RetryCount = random.Next(0, 3),
                ActionsPerMinute = (float)(random.NextDouble() * 50 + 60),
                InputErrorRate = (float)(random.NextDouble() * 0.1),
                SessionDurationMinutes = (float)(random.NextDouble() * 20 + 20),
                SectionTimeMinutes = (float)(random.NextDouble() * 8 + 2),
                HasRapidInputBursts = false,
                HasIdleSpikes = false,
                PauseCount = random.Next(0, 2),
                ExpectedDifficulty = "Increase"
            };
        }
    }

    private static DifficultyTrainingData CreateTrainingDataFromScenario(GameplayScenario scenario)
    {
        return new DifficultyTrainingData
        {
            DeathCount = scenario.DeathCount,
            RetryCount = scenario.RetryCount,
            ActionsPerMinute = scenario.ActionsPerMinute,
            InputErrorRate = scenario.InputErrorRate,
            TotalSessionDurationMinutes = scenario.SessionDurationMinutes,
            TimeInCurrentSectionMinutes = scenario.SectionTimeMinutes,
            HasRapidInputBursts = scenario.HasRapidInputBursts,
            HasIdleSpikes = scenario.HasIdleSpikes,
            PauseCount = scenario.PauseCount,
            TotalPausedTimeMinutes = scenario.PauseCount * 2f, // Estimate 2 min per pause
            Label = scenario.ExpectedDifficulty
        };
    }
}

/// <summary>
/// Record of a gameplay session for training.
/// </summary>
public sealed record GameplaySessionRecord
{
    public required Guid SessionId { get; init; }
    public required DateTime StartTimeUtc { get; init; }
    public required DateTime EndTimeUtc { get; init; }
    public required int DeathCount { get; init; }
    public required int RetryCount { get; init; }
    public required TimeSpan TimeInCurrentSection { get; init; }
    public required float AverageActionsPerMinute { get; init; }
    public required float InputErrorRate { get; init; }
    public required bool HasRapidInputBursts { get; init; }
    public required bool HasIdleSpikes { get; init; }
    public required int PauseCount { get; init; }
    public required TimeSpan TotalPausedTime { get; init; }
    public required SuggestedDifficulty? ActualDifficultyAdjustment { get; init; }
}

/// <summary>
/// Internal scenario representation for synthetic data generation.
/// </summary>
internal sealed class GameplayScenario
{
    public int DeathCount { get; set; }
    public int RetryCount { get; set; }
    public float ActionsPerMinute { get; set; }
    public float InputErrorRate { get; set; }
    public float SessionDurationMinutes { get; set; }
    public float SectionTimeMinutes { get; set; }
    public bool HasRapidInputBursts { get; set; }
    public bool HasIdleSpikes { get; set; }
    public int PauseCount { get; set; }
    public string ExpectedDifficulty { get; set; } = "Maintain";
}

/// <summary>
/// Statistics about training data.
/// </summary>
public sealed record TrainingDataStatistics(
    int TotalSessions,
    int LabeledSamples,
    int DecreaseSamples,
    int MaintainSamples,
    int IncreaseSamples,
    double AverageDeaths,
    double AverageRetries,
    DateTime LastUpdatedAtUtc);
