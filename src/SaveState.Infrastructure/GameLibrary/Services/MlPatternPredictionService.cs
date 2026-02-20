using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.ML;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// ML-based pattern prediction service that uses machine learning techniques
/// to predict likely memory locations for game patterns based on genre,
/// engine, and learned historical data from previous discoveries.
/// </summary>
public sealed class MlPatternPredictionService : IMlPatternPredictionService
{
    private readonly PatternPredictionModel _predictionModel;
    private readonly GameGenreClassifier _genreClassifier;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<MlPatternPredictionService> _logger;

    /// <summary>
    /// Creates a new instance of the ML pattern prediction service.
    /// </summary>
    public MlPatternPredictionService(
        ITimeProvider timeProvider,
        ILogger<MlPatternPredictionService> logger)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _predictionModel = new PatternPredictionModel(timeProvider);
        _genreClassifier = new GameGenreClassifier();
    }

    /// <inheritdoc />
    public Task<Result<List<PredictedPattern>>> PredictPatternsAsync(
        int processId,
        GameGenre genre,
        GameEngine engine,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Predicting patterns for process {ProcessId} with genre {Genre} and engine {Engine}",
                processId, genre, engine);

            long moduleBaseAddress = 0;
            try
            {
                using var process = Process.GetProcessById(processId);
                if (process.MainModule is not null)
                {
                    moduleBaseAddress = (long)process.MainModule.BaseAddress;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get module base address for process {ProcessId}", processId);
            }

            var predictions = _predictionModel.PredictPatterns(genre, engine, moduleBaseAddress);

            // Filter and rank predictions
            var rankedPredictions = predictions
                .Where(p => p.Confidence >= 0.3) // Minimum confidence threshold
                .OrderByDescending(p => p.Confidence)
                .ThenByDescending(p => p.HistoricalSuccessRate)
                .ThenByDescending(p => p.Priority)
                .Take(20) // Limit to top 20 predictions
                .ToList();

            _logger.LogInformation(
                "Generated {Count} predictions for process {ProcessId}",
                rankedPredictions.Count, processId);

            return Task.FromResult(Result.Success(rankedPredictions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict patterns for process {ProcessId}", processId);
            return Task.FromResult(Result.Failure<List<PredictedPattern>>(
                $"Pattern prediction failed: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> RecordSuccessfulDiscoveryAsync(
        SuccessfulDiscovery discovery,
        CancellationToken ct = default)
    {
        try
        {
            if (discovery is null)
            {
                return Task.FromResult(Result.Failure("Discovery cannot be null"));
            }

            _logger.LogInformation(
                "Recording successful discovery: {PatternType} at 0x{Address:X} for {GameTitle}",
                discovery.PatternType, discovery.Address, discovery.GameTitle);

            var result = _predictionModel.RecordSuccess(discovery, GetProcessIdFromDiscovery(discovery));

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully recorded discovery for {PatternType}",
                    discovery.PatternType);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to record discovery: {Error}",
                    result.Error);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record successful discovery");
            return Task.FromResult(Result.Failure(
                $"Failed to record discovery: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<List<AddressProbability>>> GetLikelyAddressesAsync(
        GameGenre genre,
        string patternType,
        GameEngine? engine = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting likely addresses for {PatternType} in {Genre} games",
                patternType, genre);

            var addresses = _predictionModel.GetLikelyAddresses(genre, patternType, engine);

            _logger.LogInformation(
                "Found {Count} likely address ranges for {PatternType}",
                addresses.Count, patternType);

            return Task.FromResult(Result.Success(addresses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get likely addresses");
            return Task.FromResult(Result.Failure<List<AddressProbability>>(
                $"Failed to get likely addresses: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<ValidationResult>> ValidatePatternAsync(
        long address,
        List<ValueObservation> valueHistory,
        string patternType,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Validating pattern {PatternType} at address 0x{Address:X} with {Count} observations",
                patternType, address, valueHistory?.Count ?? 0);

            var result = _predictionModel.ValidatePattern(address, valueHistory, patternType);

            _logger.LogInformation(
                "Validation result for {PatternType}: IsValid={IsValid}, Confidence={Confidence:F2}",
                patternType, result.IsValid, result.Confidence);

            return Task.FromResult(Result.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate pattern");
            return Task.FromResult(Result.Failure<ValidationResult>(
                $"Pattern validation failed: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<List<EngineMemoryPattern>>> GetEnginePatternsAsync(
        GameEngine engine,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting engine patterns for {Engine}", engine);

            var patterns = _predictionModel.GetEnginePatterns(engine);

            return Task.FromResult(Result.Success(patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine patterns");
            return Task.FromResult(Result.Failure<List<EngineMemoryPattern>>(
                $"Failed to get engine patterns: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<List<string>>> GetRecommendedScanOrderAsync(
        GameGenre genre,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting recommended scan order for {Genre}", genre);

            var scanOrder = _genreClassifier.GetScanPriorityOrder(genre);

            return Task.FromResult(Result.Success(scanOrder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommended scan order");
            return Task.FromResult(Result.Failure<List<string>>(
                $"Failed to get scan order: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<PredictionModelStats>> GetModelStatisticsAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Retrieving prediction model statistics");

            var stats = _predictionModel.GetStatistics();

            return Task.FromResult(Result.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model statistics");
            return Task.FromResult(Result.Failure<PredictionModelStats>(
                $"Failed to get statistics: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Classifies a game's genre from a process.
    /// </summary>
    public GameGenre ClassifyGenre(Process process, string? gameTitle = null)
    {
        return _genreClassifier.ClassifyGame(process, gameTitle);
    }

    /// <summary>
    /// Classifies a game's genre from a classification context.
    /// </summary>
    public GameGenre ClassifyGenre(GenreClassificationContext context)
    {
        return _genreClassifier.ClassifyGame(context);
    }

    /// <summary>
    /// Detects the game engine from a process.
    /// </summary>
    public GameEngine DetectEngine(Process process)
    {
        var database = new EnginePatternDatabase();
        return database.DetectEngine(process);
    }

    /// <summary>
    /// Gets the recommended pattern templates for a genre.
    /// </summary>
    public List<string> GetRecommendedTemplates(GameGenre genre)
    {
        return _genreClassifier.GetRecommendedTemplates(genre);
    }

    /// <summary>
    /// Gets typical value ranges for patterns in a genre.
    /// </summary>
    public (double Min, double Max, string Type)? GetTypicalValueRange(
        GameGenre genre,
        string patternType)
    {
        return _genreClassifier.GetTypicalValueRange(genre, patternType);
    }

    private int GetProcessIdFromDiscovery(SuccessfulDiscovery discovery)
    {
        // Try to find process by name
        try
        {
            var processes = Process.GetProcessesByName(
                discovery.ProcessName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase));

            if (processes.Length > 0)
            {
                return processes[0].Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not find process for discovery");
        }

        return 0;
    }
}
