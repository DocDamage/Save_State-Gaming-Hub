using System.Diagnostics;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.ML;

/// <summary>
/// ML-based pattern prediction model that learns from successful discoveries
/// to predict likely memory locations for game patterns.
/// </summary>
public sealed class PatternPredictionModel
{
    private readonly List<SuccessfulDiscovery> _discoveries;
    private readonly Dictionary<GameGenre, GenrePatternProfile> _genreProfiles;
    private readonly Dictionary<GameEngine, EnginePatternProfile> _engineProfiles;
    private readonly EnginePatternDatabase _engineDatabase;
    private readonly StatisticalPatternValidator _validator;
    private readonly ITimeProvider _timeProvider;
    private readonly ReaderWriterLockSlim _lock;

    /// <summary>
    /// Creates a new instance of the pattern prediction model.
    /// </summary>
    public PatternPredictionModel(
        ITimeProvider timeProvider,
        EnginePatternDatabase engineDatabase,
        StatisticalPatternValidator validator)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _engineDatabase = engineDatabase ?? throw new ArgumentNullException(nameof(engineDatabase));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _discoveries = new List<SuccessfulDiscovery>();
        _genreProfiles = new Dictionary<GameGenre, GenrePatternProfile>();
        _engineProfiles = new Dictionary<GameEngine, EnginePatternProfile>();
        _lock = new ReaderWriterLockSlim();

        InitializeProfiles();
    }

    /// <summary>
    /// Gets all recorded discoveries.
    /// </summary>
    public IReadOnlyList<SuccessfulDiscovery> Discoveries
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _discoveries.ToList().AsReadOnly();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Records a successful pattern discovery for learning.
    /// </summary>
    public Result RecordSuccess(SuccessfulDiscovery discovery, int processId)
    {
        if (discovery is null)
            return Result.Failure("Discovery cannot be null");

        try
        {
            _lock.EnterWriteLock();
            try
            {
                // Calculate relative address if module name is provided
                if (!string.IsNullOrEmpty(discovery.ModuleName))
                {
                    discovery.RelativeAddress = CalculateRelativeAddress(
                        discovery.Address, processId, discovery.ModuleName);
                }
                else
                {
                    discovery.RelativeAddress = discovery.Address;
                }

                discovery.Timestamp = _timeProvider.UtcNow;
                _discoveries.Add(discovery);

                // Update genre profile
                UpdateGenreProfile(discovery);

                // Update engine profile
                UpdateEngineProfile(discovery);

                return Result.Success();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to record discovery: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Predicts likely patterns for a given genre and engine combination.
    /// </summary>
    public List<PredictedPattern> PredictPatterns(
        GameGenre genre,
        GameEngine engine,
        long moduleBaseAddress = 0)
    {
        _lock.EnterReadLock();
        try
        {
            var predictions = new List<PredictedPattern>();
            var patternScores = new Dictionary<string, double>();

            // Get genre-based predictions
            var genrePredictions = GetGenreBasedPredictions(genre, moduleBaseAddress);
            predictions.AddRange(genrePredictions);

            // Get engine-based predictions
            var enginePredictions = GetEngineBasedPredictions(engine, moduleBaseAddress);
            predictions.AddRange(enginePredictions);

            // Get historical predictions
            var historicalPredictions = GetHistoricalPredictions(genre, engine, moduleBaseAddress);
            predictions.AddRange(historicalPredictions);

            // Merge and rank predictions
            var mergedPredictions = MergePredictions(predictions);

            // Sort by confidence (descending)
            return mergedPredictions
                .OrderByDescending(p => p.Confidence)
                .ThenByDescending(p => p.HistoricalSuccessRate)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets likely address ranges based on historical data.
    /// </summary>
    public List<AddressProbability> GetLikelyAddresses(
        GameGenre genre,
        string patternType,
        GameEngine? engine = null)
    {
        _lock.EnterReadLock();
        try
        {
            var query = _discoveries
                .Where(d => d.Genre == genre &&
                           d.PatternType.Equals(patternType, StringComparison.OrdinalIgnoreCase));

            if (engine.HasValue)
            {
                query = query.Where(d => d.Engine == engine.Value);
            }

            var relevantDiscoveries = query.ToList();

            if (relevantDiscoveries.Count == 0)
                return new List<AddressProbability>();

            // Group by 4KB pages
            const long PageSize = 0x1000;
            var pageGroups = relevantDiscoveries
                .GroupBy(d => d.RelativeAddress / PageSize)
                .Select(g => new
                {
                    PageStart = g.Key * PageSize,
                    Count = g.Count(),
                    AvgConfidence = g.Average(d => d.DiscoveryConfidence),
                    CommonModule = g.GroupBy(d => d.ModuleName)
                                   .OrderByDescending(grp => grp.Count())
                                   .FirstOrDefault()?.Key
                })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToList();

            var totalCount = relevantDiscoveries.Count;

            return pageGroups.Select(g => new AddressProbability
            {
                AddressRange = g.PageStart,
                Probability = (double)g.Count / totalCount,
                DiscoveryCount = g.Count,
                AverageConfidence = g.AvgConfidence,
                CommonModuleName = g.CommonModule
            }).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Validates a potential pattern using statistical analysis.
    /// </summary>
    public ValidationResult ValidatePattern(
        long address,
        List<ValueObservation> history,
        string patternType)
    {
        return _validator.ValidatePattern(address, history, patternType);
    }

    /// <summary>
    /// Gets model statistics for reporting.
    /// </summary>
    public PredictionModelStats GetStatistics()
    {
        _lock.EnterReadLock();
        try
        {
            var stats = new PredictionModelStats
            {
                TotalDiscoveries = _discoveries.Count,
                ValidatedDiscoveries = _discoveries.Count(d => d.IsValidated),
                LastTrainingTimestamp = _discoveries.Count > 0
                    ? _discoveries.Max(d => d.Timestamp)
                    : null,
                ModelVersion = "1.0.0"
            };

            // Calculate overall accuracy (validated / total)
            stats.OverallAccuracy = stats.TotalDiscoveries > 0
                ? (double)stats.ValidatedDiscoveries / stats.TotalDiscoveries * 100
                : 0;

            // Accuracy by genre
            stats.AccuracyByGenre = _discoveries
                .GroupBy(d => d.Genre)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0
                        ? (double)g.Count(d => d.IsValidated) / g.Count() * 100
                        : 0);

            // Accuracy by pattern type
            stats.AccuracyByPatternType = _discoveries
                .GroupBy(d => d.PatternType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0
                        ? (double)g.Count(d => d.IsValidated) / g.Count() * 100
                        : 0);

            // Discoveries by genre
            stats.DiscoveriesByGenre = _discoveries
                .GroupBy(d => d.Genre)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all recorded discoveries (useful for testing).
    /// </summary>
    public void ClearDiscoveries()
    {
        _lock.EnterWriteLock();
        try
        {
            _discoveries.Clear();
            InitializeProfiles();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets engine-specific memory patterns.
    /// </summary>
    public List<EngineMemoryPattern> GetEnginePatterns(GameEngine engine)
    {
        return _engineDatabase.GetPatternsForEngine(engine);
    }

    private List<PredictedPattern> GetGenreBasedPredictions(GameGenre genre, long moduleBaseAddress)
    {
        var predictions = new List<PredictedPattern>();

        if (!_genreProfiles.TryGetValue(genre, out var profile))
            return predictions;

        foreach (var pattern in profile.CommonPatterns)
        {
            var prediction = new PredictedPattern
            {
                PatternName = pattern.PatternType,
                Genre = genre,
                Engine = GameEngine.Custom,
                ValueType = pattern.SuggestedValueType,
                Confidence = pattern.Frequency * 0.7, // Genre confidence capped at 0.7
                PredictionBasis = $"Based on {pattern.Count} similar {genre} games",
                HistoricalSuccessRate = (int)(pattern.SuccessRate * 100),
                Priority = pattern.Priority,
                ExpectedValueRange = (pattern.TypicalMinValue, pattern.TypicalMaxValue)
            };

            // Predict address based on genre patterns
            if (pattern.CommonOffsets.Count > 0)
            {
                var avgOffset = (long)pattern.CommonOffsets.Average();
                prediction.PredictedAddressRange = moduleBaseAddress + avgOffset;
                prediction.SuggestedModuleOffset = avgOffset;
            }

            predictions.Add(prediction);
        }

        return predictions;
    }

    private List<PredictedPattern> GetEngineBasedPredictions(GameEngine engine, long moduleBaseAddress)
    {
        var predictions = new List<PredictedPattern>();
        var enginePatterns = _engineDatabase.GetPatternsForEngine(engine);

        foreach (var pattern in enginePatterns)
        {
            predictions.Add(new PredictedPattern
            {
                PatternName = pattern.Name,
                Genre = GameGenre.Unknown,
                Engine = engine,
                ValueType = pattern.ValueType,
                Confidence = 0.6 + (pattern.SuccessRate / 100.0 * 0.3), // 0.6-0.9 based on success rate
                PredictionBasis = $"Known {engine} engine pattern with {pattern.SuccessRate}% success rate",
                HistoricalSuccessRate = pattern.SuccessRate,
                Priority = pattern.SuccessRate / 10, // Higher success = higher priority
                SuggestedModuleOffset = pattern.Offset,
                ExpectedValueRange = (0, 999999) // Wide range for engine patterns
            });
        }

        return predictions;
    }

    private List<PredictedPattern> GetHistoricalPredictions(
        GameGenre genre,
        GameEngine engine,
        long moduleBaseAddress)
    {
        var predictions = new List<PredictedPattern>();

        // Get recent discoveries for this genre/engine combination
        var recentDiscoveries = _discoveries
            .Where(d => d.Genre == genre && d.Engine == engine)
            .GroupBy(d => d.PatternType)
            .Select(g => new
            {
                PatternType = g.Key,
                Count = g.Count(),
                AvgConfidence = g.Average(d => d.DiscoveryConfidence),
                Latest = g.OrderByDescending(d => d.Timestamp).First(),
                SuccessRate = (double)g.Count(d => d.IsValidated) / g.Count()
            })
            .Where(x => x.Count >= 2) // Need at least 2 samples
            .ToList();

        foreach (var discovery in recentDiscoveries)
        {
            predictions.Add(new PredictedPattern
            {
                PatternName = discovery.PatternType,
                Genre = genre,
                Engine = engine,
                ValueType = discovery.Latest.ValueType,
                Confidence = Math.Min(discovery.AvgConfidence * discovery.SuccessRate, 0.95),
                PredictionBasis = $"Based on {discovery.Count} previous discoveries for {genre} {engine} games",
                HistoricalSuccessRate = (int)(discovery.SuccessRate * 100),
                Priority = discovery.Count * 10,
                PredictedAddressRange = moduleBaseAddress + discovery.Latest.RelativeAddress,
                SuggestedModuleOffset = discovery.Latest.RelativeAddress,
                ExpectedValueRange = GetExpectedRange(discovery.PatternType)
            });
        }

        return predictions;
    }

    private List<PredictedPattern> MergePredictions(List<PredictedPattern> predictions)
    {
        // Group by pattern name and merge
        return predictions
            .GroupBy(p => p.PatternName)
            .Select(g =>
            {
                var best = g.OrderByDescending(p => p.Confidence).First();
                var merged = new PredictedPattern
                {
                    PatternName = best.PatternName,
                    Genre = best.Genre,
                    Engine = best.Engine,
                    ValueType = best.ValueType,
                    Confidence = Math.Min(g.Average(p => p.Confidence) + 0.1, 0.95),
                    PredictionBasis = string.Join("; ", g.Select(p => p.PredictionBasis).Distinct()),
                    HistoricalSuccessRate = (int)g.Average(p => p.HistoricalSuccessRate),
                    Priority = g.Max(p => p.Priority),
                    PredictedAddressRange = g.FirstOrDefault(p => p.PredictedAddressRange != 0)?.PredictedAddressRange ?? 0,
                    SuggestedModuleOffset = g.FirstOrDefault(p => p.SuggestedModuleOffset.HasValue)?.SuggestedModuleOffset,
                    ExpectedValueRange = best.ExpectedValueRange
                };
                return merged;
            })
            .ToList();
    }

    private void UpdateGenreProfile(SuccessfulDiscovery discovery)
    {
        if (!_genreProfiles.TryGetValue(discovery.Genre, out var profile))
        {
            profile = new GenrePatternProfile { Genre = discovery.Genre };
            _genreProfiles[discovery.Genre] = profile;
        }

        profile.AddDiscovery(discovery);
    }

    private void UpdateEngineProfile(SuccessfulDiscovery discovery)
    {
        if (!_engineProfiles.TryGetValue(discovery.Engine, out var profile))
        {
            profile = new EnginePatternProfile { Engine = discovery.Engine };
            _engineProfiles[discovery.Engine] = profile;
        }

        profile.AddDiscovery(discovery);
    }

    private long CalculateRelativeAddress(long absoluteAddress, int processId, string moduleName)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return absoluteAddress - (long)module.BaseAddress;
                }
            }
        }
        catch
        {
            // Fall back to absolute address
        }

        return absoluteAddress;
    }

    private (double Min, double Max) GetExpectedRange(string patternType)
    {
        return patternType.ToLowerInvariant() switch
        {
            "health" => (1, 10000),
            "ammo" => (0, 999),
            "currency" => (0, 999999999),
            "experience" => (0, 999999999),
            "level" => (1, 999),
            "score" => (0, 9999999999),
            "timer" => (0, 3600),
            "lives" => (0, 99),
            "mana" => (0, 9999),
            "stamina" => (0, 1000),
            _ => (0, 999999)
        };
    }

    private void InitializeProfiles()
    {
        // Initialize default profiles for common genres
        foreach (GameGenre genre in Enum.GetValues<GameGenre>())
        {
            _genreProfiles[genre] = new GenrePatternProfile { Genre = genre };
        }

        foreach (GameEngine engine in Enum.GetValues<GameEngine>())
        {
            _engineProfiles[engine] = new EnginePatternProfile { Engine = engine };
        }
    }
}
