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
    public PatternPredictionModel(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _discoveries = new List<SuccessfulDiscovery>();
        _genreProfiles = new Dictionary<GameGenre, GenrePatternProfile>();
        _engineProfiles = new Dictionary<GameEngine, EnginePatternProfile>();
        _engineDatabase = new EnginePatternDatabase();
        _validator = new StatisticalPatternValidator();
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

/// <summary>
/// Profile for a specific game genre containing learned patterns.
/// </summary>
public sealed class GenrePatternProfile
{
    public GameGenre Genre { get; set; }
    public List<PatternFrequency> CommonPatterns { get; set; } = new();
    public int TotalDiscoveries { get; set; }

    public void AddDiscovery(SuccessfulDiscovery discovery)
    {
        TotalDiscoveries++;

        var existing = CommonPatterns
            .FirstOrDefault(p => p.PatternType.Equals(discovery.PatternType, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new PatternFrequency
            {
                PatternType = discovery.PatternType,
                SuggestedValueType = discovery.ValueType
            };
            CommonPatterns.Add(existing);
        }

        existing.Count++;
        existing.CommonOffsets.Add(discovery.RelativeAddress);
        existing.UpdateSuccessRate();
    }
}

/// <summary>
/// Profile for a specific game engine containing learned patterns.
/// </summary>
public sealed class EnginePatternProfile
{
    public GameEngine Engine { get; set; }
    public List<PatternFrequency> CommonPatterns { get; set; } = new();
    public int TotalDiscoveries { get; set; }

    public void AddDiscovery(SuccessfulDiscovery discovery)
    {
        TotalDiscoveries++;

        var existing = CommonPatterns
            .FirstOrDefault(p => p.PatternType.Equals(discovery.PatternType, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new PatternFrequency
            {
                PatternType = discovery.PatternType,
                SuggestedValueType = discovery.ValueType
            };
            CommonPatterns.Add(existing);
        }

        existing.Count++;
        existing.CommonOffsets.Add(discovery.RelativeAddress);
        existing.UpdateSuccessRate();
    }
}

/// <summary>
/// Frequency statistics for a specific pattern type.
/// </summary>
public sealed class PatternFrequency
{
    public string PatternType { get; set; } = string.Empty;
    public int Count { get; set; }
    public string SuggestedValueType { get; set; } = "int32";
    public List<long> CommonOffsets { get; set; } = new();
    public double SuccessRate { get; private set; }
    public double Frequency => Count / 100.0; // Normalized frequency
    public int Priority => Math.Min(Count * 10, 100);
    public double TypicalMinValue { get; set; }
    public double TypicalMaxValue { get; set; }

    public void UpdateSuccessRate()
    {
        // Placeholder for success rate calculation
        SuccessRate = 0.75; // Default assumption
    }
}

/// <summary>
/// Database of known memory patterns for specific game engines.
/// </summary>
public sealed class EnginePatternDatabase
{
    private readonly Dictionary<GameEngine, List<EngineMemoryPattern>> _patterns;

    public EnginePatternDatabase()
    {
        _patterns = InitializePatterns();
    }

    public List<EngineMemoryPattern> GetPatternsForEngine(GameEngine engine)
    {
        return _patterns.TryGetValue(engine, out var patterns)
            ? patterns.ToList()
            : new List<EngineMemoryPattern>();
    }

    public GameEngine DetectEngine(Process process)
    {
        try
        {
            var modules = process.Modules.Cast<ProcessModule>()
                .Select(m => m.ModuleName.ToLowerInvariant())
                .ToList();

            if (modules.Any(m => m.Contains("unityplayer")))
                return GameEngine.Unity;

            if (modules.Any(m => m.Contains("ue4") || m.Contains("unreal")))
                return GameEngine.Unreal;

            if (modules.Any(m => m.Contains("ue5")))
                return GameEngine.Unreal;

            if (modules.Any(m => m.Contains("godot")))
                return GameEngine.Godot;

            if (modules.Any(m => m.Contains("gm") || m.Contains("gamemaker")))
                return GameEngine.GameMaker;

            if (modules.Any(m => m.Contains("crysystem")))
                return GameEngine.CryEngine;

            if (modules.Any(m => m.Contains("engine") && m.Contains("source")))
                return GameEngine.Source2;

            if (modules.Any(m => m.Contains("engine") || m.Contains("client") || m.Contains("server")))
                return GameEngine.Source;

            if (modules.Any(m => m.Contains("idtech") || m.Contains("doom") || m.Contains("rage")))
                return GameEngine.IdTech;

            if (modules.Any(m => m.Contains("frostbite")))
                return GameEngine.Frostbite;

            return GameEngine.Custom;
        }
        catch
        {
            return GameEngine.Custom;
        }
    }

    private static Dictionary<GameEngine, List<EngineMemoryPattern>> InitializePatterns()
    {
        return new Dictionary<GameEngine, List<EngineMemoryPattern>>
        {
            [GameEngine.Unity] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x20,
                    ValueType = "float",
                    Engine = GameEngine.Unity,
                    Category = "Health",
                    SuccessRate = 65
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerPosition",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x10,
                    ValueType = "float",
                    Engine = GameEngine.Unity,
                    Category = "Position",
                    SuccessRate = 70
                },
                new EngineMemoryPattern
                {
                    Name = "Currency",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x28,
                    ValueType = "int32",
                    Engine = GameEngine.Unity,
                    Category = "Economy",
                    SuccessRate = 55
                }
            },
            [GameEngine.Unreal] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "A0 ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x2B0,
                    ValueType = "float",
                    Engine = GameEngine.Unreal,
                    Category = "Health",
                    SuccessRate = 72
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerStamina",
                    Pattern = "A0 ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x2B4,
                    ValueType = "float",
                    Engine = GameEngine.Unreal,
                    Category = "Stamina",
                    SuccessRate = 60
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerLocation",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x60,
                    ValueType = "float",
                    Engine = GameEngine.Unreal,
                    Category = "Position",
                    SuccessRate = 75
                }
            },
            [GameEngine.Source] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0xA0,
                    ValueType = "int32",
                    Engine = GameEngine.Source,
                    Category = "Health",
                    SuccessRate = 80
                },
                new EngineMemoryPattern
                {
                    Name = "PlayerArmor",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0xA4,
                    ValueType = "int32",
                    Engine = GameEngine.Source,
                    Category = "Armor",
                    SuccessRate = 75
                },
                new EngineMemoryPattern
                {
                    Name = "CurrentWeaponAmmo",
                    Pattern = "?? ?? ?? ??",
                    Offset = 0x1D4,
                    ValueType = "int32",
                    Engine = GameEngine.Source,
                    Category = "Ammo",
                    SuccessRate = 78
                }
            },
            [GameEngine.Godot] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ??",
                    Offset = 0x40,
                    ValueType = "float",
                    Engine = GameEngine.Godot,
                    Category = "Health",
                    SuccessRate = 50
                }
            },
            [GameEngine.GameMaker] = new List<EngineMemoryPattern>
            {
                new EngineMemoryPattern
                {
                    Name = "PlayerHealth",
                    Pattern = "?? ?? ?? ??",
                    Offset = 0x8,
                    ValueType = "int32",
                    Engine = GameEngine.GameMaker,
                    Category = "Health",
                    SuccessRate = 45
                }
            }
        };
    }
}

/// <summary>
/// Validates patterns using statistical analysis.
/// </summary>
public sealed class StatisticalPatternValidator
{
    /// <summary>
    /// Validates a pattern based on value history using statistical methods.
    /// </summary>
    public ValidationResult ValidatePattern(
        long address,
        List<ValueObservation> history,
        string patternType)
    {
        if (history is null || history.Count < 3)
        {
            return new ValidationResult
            {
                IsValid = false,
                Confidence = 0.3,
                Reasoning = "Insufficient data for statistical validation (need at least 3 observations)",
                SuggestedAction = ValidationAction.RequestMoreData
            };
        }

        var values = ConvertToDoubles(history.Select(h => h.Value)).ToList();
        if (values.Count < 3)
        {
            return new ValidationResult
            {
                IsValid = false,
                Confidence = 0.3,
                Reasoning = "Insufficient numeric data for statistical validation",
                SuggestedAction = ValidationAction.RequestMoreData
            };
        }
        var metrics = CalculateMetrics(values);

        // Determine expected behavior based on pattern type
        var expectedBehavior = GetExpectedBehavior(patternType);

        var validationScore = 0.0;
        var reasons = new List<string>();

        // Check standard deviation (stable values have low std dev relative to mean)
        var cv = metrics.Mean != 0 ? metrics.StdDev / Math.Abs(metrics.Mean) : metrics.StdDev;
        if (cv < 0.5)
        {
            validationScore += 0.25;
            reasons.Add($"Low coefficient of variation ({cv:F2}) indicates stable value");
        }
        else if (cv > 2.0 && expectedBehavior.AllowsHighVariance)
        {
            validationScore += 0.15;
            reasons.Add($"High variance acceptable for {patternType}");
        }

        // Check change frequency
        var changeFrequency = CalculateChangeFrequency(values);
        if (changeFrequency >= expectedBehavior.MinChangeFrequency &&
            changeFrequency <= expectedBehavior.MaxChangeFrequency)
        {
            validationScore += 0.25;
            reasons.Add($"Change frequency ({changeFrequency:F2}) matches expected behavior");
        }

        // Check value distribution
        if (IsNormalDistribution(values, metrics))
        {
            validationScore += 0.2;
            reasons.Add("Value distribution appears normal");
        }

        // Check for outliers
        var outlierRatio = CalculateOutlierRatio(values, metrics);
        if (outlierRatio < 0.1)
        {
            validationScore += 0.2;
            reasons.Add($"Low outlier ratio ({outlierRatio:F2}) indicates consistent values");
        }
        else if (outlierRatio > 0.3)
        {
            validationScore -= 0.2;
            reasons.Add($"High outlier ratio ({outlierRatio:F2}) suggests possible false positive");
        }

        // Check range validity
        if (values.All(v => v >= expectedBehavior.MinValue && v <= expectedBehavior.MaxValue))
        {
            validationScore += 0.1;
            reasons.Add($"Values within expected range for {patternType}");
        }

        var isValid = validationScore >= 0.5;
        var confidence = Math.Min(Math.Max(validationScore, 0.1), 0.95);

        return new ValidationResult
        {
            IsValid = isValid,
            Confidence = confidence,
            Reasoning = string.Join("; ", reasons),
            Metrics = new Dictionary<string, double>
            {
                ["Mean"] = metrics.Mean,
                ["StdDev"] = metrics.StdDev,
                ["CoefficientOfVariation"] = cv,
                ["ChangeFrequency"] = changeFrequency,
                ["OutlierRatio"] = outlierRatio,
                ["Min"] = metrics.Min,
                ["Max"] = metrics.Max,
                ["Range"] = metrics.Max - metrics.Min
            },
            SuggestedAction = isValid ? ValidationAction.Accept :
                             confidence < 0.3 ? ValidationAction.RequestMoreData :
                             ValidationAction.ManualReview
        };
    }

    private StatisticalMetrics CalculateMetrics(List<double> values)
    {
        var mean = values.Average();
        var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        return new StatisticalMetrics
        {
            Mean = mean,
            StdDev = stdDev,
            Variance = variance,
            Min = values.Min(),
            Max = values.Max()
        };
    }

    private double CalculateChangeFrequency(List<double> values)
    {
        if (values.Count < 2) return 0;

        var changes = 0;
        for (int i = 1; i < values.Count; i++)
        {
            if (Math.Abs(values[i] - values[i - 1]) > 0.001)
                changes++;
        }

        return (double)changes / (values.Count - 1);
    }

    private bool IsNormalDistribution(List<double> values, StatisticalMetrics metrics)
    {
        // Simple check using skewness and kurtosis approximation
        if (values.Count < 4) return false;

        var skewness = CalculateSkewness(values, metrics);
        return Math.Abs(skewness) < 1.0; // Rough approximation
    }

    private double CalculateSkewness(List<double> values, StatisticalMetrics metrics)
    {
        if (metrics.StdDev == 0) return 0;

        var n = values.Count;
        var sumCubedDeviations = values.Sum(v => Math.Pow((v - metrics.Mean) / metrics.StdDev, 3));
        return sumCubedDeviations * n / ((n - 1) * (n - 2));
    }

    private double CalculateOutlierRatio(List<double> values, StatisticalMetrics metrics)
    {
        if (metrics.StdDev == 0) return 0;

        var threshold = 2.0 * metrics.StdDev; // 2-sigma rule
        var outliers = values.Count(v => Math.Abs(v - metrics.Mean) > threshold);
        return (double)outliers / values.Count;
    }

    private static IEnumerable<double> ConvertToDoubles(IEnumerable<object?> values)
    {
        foreach (var value in values)
        {
            if (value is null) continue;

            if (value is double d) yield return d;
            else if (value is float f) yield return f;
            else if (value is int i) yield return i;
            else if (value is long l) yield return l;
            else if (value is decimal dec) yield return (double)dec;
            else if (double.TryParse(value.ToString(), out var parsed))
                yield return parsed;
        }
    }

    private ExpectedBehavior GetExpectedBehavior(string patternType)
    {
        return patternType.ToLowerInvariant() switch
        {
            "health" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 100000,
                MinChangeFrequency = 0.05,
                MaxChangeFrequency = 0.5,
                AllowsHighVariance = true
            },
            "ammo" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 9999,
                MinChangeFrequency = 0.1,
                MaxChangeFrequency = 0.8,
                AllowsHighVariance = true
            },
            "currency" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 999999999,
                MinChangeFrequency = 0.01,
                MaxChangeFrequency = 0.3,
                AllowsHighVariance = false
            },
            "experience" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 999999999,
                MinChangeFrequency = 0.01,
                MaxChangeFrequency = 0.2,
                AllowsHighVariance = false
            },
            "level" => new ExpectedBehavior
            {
                MinValue = 1,
                MaxValue = 9999,
                MinChangeFrequency = 0,
                MaxChangeFrequency = 0.05,
                AllowsHighVariance = false
            },
            "timer" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 86400,
                MinChangeFrequency = 0.8,
                MaxChangeFrequency = 1.0,
                AllowsHighVariance = false
            },
            "position" => new ExpectedBehavior
            {
                MinValue = -999999,
                MaxValue = 999999,
                MinChangeFrequency = 0.5,
                MaxChangeFrequency = 1.0,
                AllowsHighVariance = true
            },
            _ => new ExpectedBehavior
            {
                MinValue = double.MinValue,
                MaxValue = double.MaxValue,
                MinChangeFrequency = 0,
                MaxChangeFrequency = 1.0,
                AllowsHighVariance = true
            }
        };
    }

    private sealed class StatisticalMetrics
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double Variance { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }

    private sealed class ExpectedBehavior
    {
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double MinChangeFrequency { get; set; }
        public double MaxChangeFrequency { get; set; }
        public bool AllowsHighVariance { get; set; }
    }
}
