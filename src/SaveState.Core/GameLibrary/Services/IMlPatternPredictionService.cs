using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Game genre classification for applying appropriate memory pattern heuristics.
/// </summary>
public enum GameGenre
{
    FirstPersonShooter,
    ThirdPersonShooter,
    RolePlayingGame,
    ActionRPG,
    Platformer,
    Metroidvania,
    Fighting,
    Racing,
    Strategy,
    Roguelike,
    Survival,
    Simulation,
    Sports,
    Puzzle,
    VisualNovel,
    Unknown
}

/// <summary>
/// Game engine types with known memory layout patterns.
/// </summary>
public enum GameEngine
{
    Unity,
    Unreal,
    Godot,
    GameMaker,
    CryEngine,
    Source,
    Source2,
    IdTech,
    Frostbite,
    Custom
}

/// <summary>
/// Represents a predicted memory pattern with confidence scoring.
/// </summary>
public sealed class PredictedPattern
{
    /// <summary>
    /// The name of the predicted pattern (e.g., "Health", "Ammo").
    /// </summary>
    public string PatternName { get; set; } = string.Empty;

    /// <summary>
    /// The predicted memory address range (page-aligned).
    /// </summary>
    public long PredictedAddressRange { get; set; }

    /// <summary>
    /// The suggested value type for this pattern (int32, float, etc.).
    /// </summary>
    public string ValueType { get; set; } = "int32";

    /// <summary>
    /// Confidence score (0.0-1.0) for this prediction.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Human-readable explanation of the prediction basis.
    /// </summary>
    public string PredictionBasis { get; set; } = string.Empty;

    /// <summary>
    /// Historical success rate percentage for this pattern type.
    /// </summary>
    public int HistoricalSuccessRate { get; set; }

    /// <summary>
    /// The game genre this prediction is based on.
    /// </summary>
    public GameGenre Genre { get; set; }

    /// <summary>
    /// The game engine this prediction is based on.
    /// </summary>
    public GameEngine Engine { get; set; }

    /// <summary>
    /// Priority rank for scanning (higher = scan first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Suggested scan range offset from module base.
    /// </summary>
    public long? SuggestedModuleOffset { get; set; }

    /// <summary>
    /// Expected value range for validation.
    /// </summary>
    public (double Min, double Max) ExpectedValueRange { get; set; }
}

/// <summary>
/// Represents a successful pattern discovery for learning purposes.
/// </summary>
public sealed class SuccessfulDiscovery
{
    /// <summary>
    /// Unique identifier for this discovery.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The type of pattern discovered (e.g., "Health", "Ammo").
    /// </summary>
    public string PatternType { get; set; } = string.Empty;

    /// <summary>
    /// The memory address where the value was found.
    /// </summary>
    public long Address { get; set; }

    /// <summary>
    /// Address relative to the module base.
    /// </summary>
    public long RelativeAddress { get; set; }

    /// <summary>
    /// The game genre for this discovery.
    /// </summary>
    public GameGenre Genre { get; set; }

    /// <summary>
    /// The game engine for this discovery.
    /// </summary>
    public GameEngine Engine { get; set; }

    /// <summary>
    /// The game title where this discovery was made.
    /// </summary>
    public string GameTitle { get; set; } = string.Empty;

    /// <summary>
    /// The process name (executable) for this discovery.
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// The module name where the address was found.
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// The value type (int32, float, etc.).
    /// </summary>
    public string ValueType { get; set; } = "int32";

    /// <summary>
    /// Timestamp when this discovery was made.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Confidence score at time of discovery.
    /// </summary>
    public double DiscoveryConfidence { get; set; }

    /// <summary>
    /// Whether this discovery has been validated by the user.
    /// </summary>
    public bool IsValidated { get; set; }
}

/// <summary>
/// Represents the probability of finding a pattern at a specific address range.
/// </summary>
public sealed class AddressProbability
{
    /// <summary>
    /// The memory address range (page-aligned).
    /// </summary>
    public long AddressRange { get; set; }

    /// <summary>
    /// Probability of finding the pattern at this range (0.0-1.0).
    /// </summary>
    public double Probability { get; set; }

    /// <summary>
    /// Number of historical discoveries at this range.
    /// </summary>
    public int DiscoveryCount { get; set; }

    /// <summary>
    /// Average confidence of discoveries at this range.
    /// </summary>
    public double AverageConfidence { get; set; }

    /// <summary>
    /// The module name most commonly associated with this range.
    /// </summary>
    public string? CommonModuleName { get; set; }
}

/// <summary>
/// Result of pattern validation using statistical analysis.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Whether the pattern is considered valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Confidence score (0.0-1.0) in the validation result.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Human-readable reasoning for the validation result.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Statistical metrics used for validation.
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();

    /// <summary>
    /// Suggested action based on validation.
    /// </summary>
    public ValidationAction SuggestedAction { get; set; }
}

/// <summary>
/// Actions recommended based on validation results.
/// </summary>
public enum ValidationAction
{
    Accept,
    Reject,
    RequestMoreData,
    ManualReview
}

/// <summary>
/// Engine-specific memory pattern definition.
/// </summary>
public sealed class EngineMemoryPattern
{
    /// <summary>
    /// The name of this engine pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The byte pattern to search for (Cheat Engine style with ?? wildcards).
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Offset from pattern match to the actual value.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Expected value type.
    /// </summary>
    public string ValueType { get; set; } = "float";

    /// <summary>
    /// The game engine this pattern applies to.
    /// </summary>
    public GameEngine Engine { get; set; }

    /// <summary>
    /// Pattern category (e.g., "Health", "PlayerState").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Historical success rate (0-100).
    /// </summary>
    public int SuccessRate { get; set; }
}

/// <summary>
/// Context for genre classification.
/// </summary>
public sealed class GenreClassificationContext
{
    /// <summary>
    /// The process name (executable).
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// The window title.
    /// </summary>
    public string? WindowTitle { get; set; }

    /// <summary>
    /// Detected game engine.
    /// </summary>
    public GameEngine Engine { get; set; }

    /// <summary>
    /// File paths of loaded modules.
    /// </summary>
    public List<string> LoadedModules { get; set; } = new();

    /// <summary>
    /// User-provided game title.
    /// </summary>
    public string? GameTitle { get; set; }

    /// <summary>
    /// Steam app ID if available.
    /// </summary>
    public string? SteamAppId { get; set; }
}

/// <summary>
/// Service interface for ML-based memory pattern prediction.
/// Uses machine learning techniques to predict likely memory locations
/// based on game genre, engine, and historical successful discoveries.
/// </summary>
public interface IMlPatternPredictionService
{
    /// <summary>
    /// Predicts likely memory patterns for a given process based on
    /// genre classification and historical learning data.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="genre">The classified game genre.</param>
    /// <param name="engine">The detected game engine.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing ranked list of predicted patterns.</returns>
    Task<Result<List<PredictedPattern>>> PredictPatternsAsync(
        int processId,
        GameGenre genre,
        GameEngine engine,
        CancellationToken ct = default);

    /// <summary>
    /// Records a successful pattern discovery for future learning.
    /// </summary>
    /// <param name="discovery">The successful discovery details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RecordSuccessfulDiscoveryAsync(
        SuccessfulDiscovery discovery,
        CancellationToken ct = default);

    /// <summary>
    /// Gets likely address ranges for a pattern type based on historical data.
    /// </summary>
    /// <param name="genre">The game genre.</param>
    /// <param name="patternType">The pattern type (e.g., "Health").</param>
    /// <param name="engine">Optional engine filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing ranked address probabilities.</returns>
    Task<Result<List<AddressProbability>>> GetLikelyAddressesAsync(
        GameGenre genre,
        string patternType,
        GameEngine? engine = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a discovered pattern using statistical analysis.
    /// </summary>
    /// <param name="address">The memory address.</param>
    /// <param name="valueHistory">Historical value observations.</param>
    /// <param name="patternType">The expected pattern type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing validation result.</returns>
    Task<Result<ValidationResult>> ValidatePatternAsync(
        long address,
        List<ValueObservation> valueHistory,
        string patternType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets engine-specific memory patterns for a given engine.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing known patterns for the engine.</returns>
    Task<Result<List<EngineMemoryPattern>>> GetEnginePatternsAsync(
        GameEngine engine,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the most effective scan priority order for a given genre.
    /// </summary>
    /// <param name="genre">The game genre.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing ordered list of pattern types to scan.</returns>
    Task<Result<List<string>>> GetRecommendedScanOrderAsync(
        GameGenre genre,
        CancellationToken ct = default);

    /// <summary>
    /// Gets learning statistics for the prediction model.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing model statistics.</returns>
    Task<Result<PredictionModelStats>> GetModelStatisticsAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Statistics for the prediction model.
/// </summary>
public sealed class PredictionModelStats
{
    /// <summary>
    /// Total number of recorded discoveries.
    /// </summary>
    public int TotalDiscoveries { get; set; }

    /// <summary>
    /// Number of validated discoveries.
    /// </summary>
    public int ValidatedDiscoveries { get; set; }

    /// <summary>
    /// Overall accuracy rate (0-100).
    /// </summary>
    public double OverallAccuracy { get; set; }

    /// <summary>
    /// Accuracy by genre.
    /// </summary>
    public Dictionary<GameGenre, double> AccuracyByGenre { get; set; } = new();

    /// <summary>
    /// Accuracy by pattern type.
    /// </summary>
    public Dictionary<string, double> AccuracyByPatternType { get; set; } = new();

    /// <summary>
    /// Number of discoveries per genre.
    /// </summary>
    public Dictionary<GameGenre, int> DiscoveriesByGenre { get; set; } = new();

    /// <summary>
    /// Last training timestamp.
    /// </summary>
    public DateTime? LastTrainingTimestamp { get; set; }

    /// <summary>
    /// Model version identifier.
    /// </summary>
    public string ModelVersion { get; set; } = "1.0";
}
