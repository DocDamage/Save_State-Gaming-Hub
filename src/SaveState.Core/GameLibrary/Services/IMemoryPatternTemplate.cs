using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Represents a universal memory pattern template for detecting common game values
/// without requiring specific game signatures. Templates define value ranges, change patterns,
/// and detection strategies for common game mechanics like health, currency, ammo, etc.
/// </summary>
public interface IMemoryPatternTemplate
{
    /// <summary>
    /// The unique name of this template (e.g., "Health", "Currency", "Ammo").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The category this template belongs to (e.g., "Combat", "Economy", "Progress").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Human-readable description of what this template detects.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The valid integer value range for this pattern.
    /// </summary>
    ValueRange IntRange { get; }

    /// <summary>
    /// The valid floating-point value range for this pattern.
    /// </summary>
    ValueRange FloatRange { get; }

    /// <summary>
    /// The expected change pattern behavior for values matching this template.
    /// </summary>
    ValueChangePattern ChangePattern { get; }

    /// <summary>
    /// User instruction displayed during detection (e.g., "Take damage to detect health").
    /// </summary>
    string DetectionInstruction { get; }

    /// <summary>
    /// Scans process memory for potential matches to this pattern template.
    /// </summary>
    /// <param name="reader">Memory reader interface for accessing process memory.</param>
    /// <param name="processId">The target process ID to scan.</param>
    /// <param name="context">Scan context containing parameters and state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of potential matches or error.</returns>
    Task<Result<List<PotentialMatch>>> ScanForMatchesAsync(
        IGameMemoryReader reader,
        int processId,
        ScanContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validates whether a detected value change matches this template's expected pattern.
    /// </summary>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The current value.</param>
    /// <param name="valueType">The type of value (int32, float, etc.).</param>
    /// <returns>True if the change pattern matches this template.</returns>
    bool ValidateChangePattern(object oldValue, object newValue, string valueType);

    /// <summary>
    /// Calculates a confidence score (0.0-1.0) for a potential match based on
    /// value ranges, change patterns, and proximity to related values.
    /// </summary>
    /// <param name="match">The potential match to evaluate.</param>
    /// <returns>Confidence score between 0.0 and 1.0.</returns>
    double CalculateConfidence(PotentialMatch match);
}

/// <summary>
/// Defines a numeric value range for pattern matching.
/// </summary>
public sealed class ValueRange
{
    /// <summary>
    /// The minimum value (inclusive).
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// The maximum value (inclusive).
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// Whether this range applies to integer values (vs floating-point).
    /// </summary>
    public bool IsInt { get; set; }

    /// <summary>
    /// Creates a new value range.
    /// </summary>
    public ValueRange(double min, double max, bool isInt = true)
    {
        Min = min;
        Max = max;
        IsInt = isInt;
    }

    /// <summary>
    /// Checks if a value falls within this range.
    /// </summary>
    public bool Contains(double value)
    {
        return value >= Min && value <= Max;
    }

    /// <summary>
    /// Checks if an integer value falls within this range.
    /// </summary>
    public bool Contains(int value)
    {
        return value >= (int)Min && value <= (int)Max;
    }
}

/// <summary>
/// Defines the expected change behavior for a value pattern.
/// </summary>
public enum ValueChangePattern
{
    /// <summary>
    /// Value rarely changes (e.g., max health, inventory capacity).
    /// </summary>
    Static,

    /// <summary>
    /// Value usually decreases (e.g., health when taking damage, ammo when firing).
    /// </summary>
    Decreasing,

    /// <summary>
    /// Value usually increases (e.g., score, experience, currency earned).
    /// </summary>
    Increasing,

    /// <summary>
    /// Value fluctuates up and down (e.g., current mana, stamina, position coordinates).
    /// </summary>
    Fluctuating,

    /// <summary>
    /// Value decreases then jumps up (e.g., ammo that reloads).
    /// </summary>
    DecreasingThenJump,

    /// <summary>
    /// Value decreases steadily over time (e.g., countdown timers).
    /// </summary>
    Countdown
}

/// <summary>
/// Represents a potential memory pattern match found during scanning.
/// </summary>
public sealed class PotentialMatch
{
    /// <summary>
    /// The memory address where the value was found.
    /// </summary>
    public IntPtr Address { get; set; }

    /// <summary>
    /// The current value at this address.
    /// </summary>
    public object Value { get; set; } = null!; // Set during scanning or updated during validation

    /// <summary>
    /// The data type of the value (int32, float, double, etc.).
    /// </summary>
    public string ValueType { get; set; } = "int32";

    /// <summary>
    /// The module name where this address was found (if known).
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// Offset from the module base address (if applicable).
    /// </summary>
    public long ModuleOffset { get; set; }

    /// <summary>
    /// Confidence score (0.0-1.0) for this match.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Addresses of nearby related values (e.g., max health near current health).
    /// </summary>
    public List<IntPtr> NearbyAddresses { get; set; } = new();

    /// <summary>
    /// Historical values observed at this address during scanning.
    /// </summary>
    public List<object> ValueHistory { get; set; } = new();

    /// <summary>
    /// Timestamp when this match was first detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Additional metadata about the match.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the value as an integer (if applicable).
    /// </summary>
    public int? AsInt() => Value switch
    {
        int i => i,
        long l => (int)l,
        float f => (int)f,
        double d => (int)d,
        _ => null
    };

    /// <summary>
    /// Gets the value as a float (if applicable).
    /// </summary>
    public float? AsFloat() => Value switch
    {
        float f => f,
        double d => (float)d,
        int i => i,
        long l => l,
        _ => null
    };

    /// <summary>
    /// Gets the value as a double (if applicable).
    /// </summary>
    public double? AsDouble() => Value switch
    {
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        _ => null
    };
}

/// <summary>
/// Context for memory scanning operations.
/// </summary>
public sealed class ScanContext
{
    /// <summary>
    /// The memory region start address (null = scan all accessible memory).
    /// </summary>
    public IntPtr? StartAddress { get; set; }

    /// <summary>
    /// The memory region end address (null = scan to end).
    /// </summary>
    public IntPtr? EndAddress { get; set; }

    /// <summary>
    /// Specific module name to scan within (null = scan all modules).
    /// </summary>
    public string? TargetModule { get; set; }

    /// <summary>
    /// Maximum number of results to return (0 = unlimited).
    /// </summary>
    public int MaxResults { get; set; }

    /// <summary>
    /// Minimum confidence threshold for matches (0.0-1.0).
    /// </summary>
    public double MinConfidence { get; set; } = 0.5;

    /// <summary>
    /// Whether to scan for aligned values only (4-byte alignment for 32-bit values).
    /// </summary>
    public bool AlignedOnly { get; set; } = true;

    /// <summary>
    /// Value types to scan for (int32, float, double, etc.).
    /// </summary>
    public List<string> ValueTypes { get; set; } = new() { "int32", "float" };

    /// <summary>
    /// Previous scan results to compare against (for multi-pass scanning).
    /// </summary>
    public List<PotentialMatch>? PreviousResults { get; set; }

    /// <summary>
    /// Time to wait between scan passes (for detecting value changes).
    /// </summary>
    public TimeSpan DelayBetweenPasses { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Number of scan passes to perform.
    /// </summary>
    public int PassCount { get; set; } = 2;

    /// <summary>
    /// Whether to look for related values nearby (e.g., max health near current health).
    /// </summary>
    public bool FindRelatedValues { get; set; } = true;

    /// <summary>
    /// Maximum distance in bytes to search for related values.
    /// </summary>
    public int RelatedValueSearchDistance { get; set; } = 64;
}

/// <summary>
/// Result of a pattern detection operation.
/// </summary>
public sealed class DetectedPattern
{
    /// <summary>
    /// The template that produced this detection.
    /// </summary>
    public required IMemoryPatternTemplate Template { get; init; }

    /// <summary>
    /// The detected memory matches.
    /// </summary>
    public List<PotentialMatch> Matches { get; set; } = new();

    /// <summary>
    /// The highest confidence match (if any).
    /// </summary>
    public PotentialMatch? BestMatch => Matches.Count > 0 ? Matches[0] : null;

    /// <summary>
    /// Overall confidence score for this detection.
    /// </summary>
    public double OverallConfidence { get; set; }

    /// <summary>
    /// Detection strategy used.
    /// </summary>
    public DetectionStrategy Strategy { get; set; }

    /// <summary>
    /// Time taken for detection.
    /// </summary>
    public TimeSpan DetectionTime { get; set; }

    /// <summary>
    /// Timestamp when detection completed.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Whether the detection was successful (has at least one match above threshold).
    /// </summary>
    public bool IsSuccessful => Matches.Count > 0 && Matches.Any(m => m.Confidence >= 0.7);
}

/// <summary>
/// Detection strategies for pattern scanning.
/// </summary>
public enum DetectionStrategy
{
    /// <summary>
    /// Single-pass scan for values in range.
    /// </summary>
    SinglePass,

    /// <summary>
    /// Multi-pass scan filtering by value changes.
    /// </summary>
    MultiPass,

    /// <summary>
    /// Detect based on user-triggered value changes.
    /// </summary>
    UserTriggered,

    /// <summary>
    /// Detect by comparing memory snapshots.
    /// </summary>
    SnapshotComparison,

    /// <summary>
    /// Use statistical analysis of value behavior.
    /// </summary>
    StatisticalAnalysis,

    /// <summary>
    /// Combine multiple strategies for maximum accuracy.
    /// </summary>
    Hybrid
}
