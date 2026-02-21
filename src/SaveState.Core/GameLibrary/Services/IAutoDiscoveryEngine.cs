using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Interface for the AI-powered memory pattern auto-discovery engine.
/// Automatically discovers game values without prior knowledge or signatures.
/// </summary>
public interface IAutoDiscoveryEngine
{
    /// <summary>
    /// Starts a new discovery session for the specified process.
    /// </summary>
    /// <param name="processId">The process ID to analyze.</param>
    /// <param name="options">Discovery options and configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the discovery session, or failure if the session could not be started.</returns>
    Task<Result<DiscoverySession>> StartDiscoverySessionAsync(int processId, DiscoveryOptions options, CancellationToken ct = default);

    /// <summary>
    /// Analyzes a player action to filter and refine discovered values.
    /// </summary>
    /// <param name="session">The active discovery session.</param>
    /// <param name="action">The player action that occurred.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the discovery result with updated candidates.</returns>
    Task<Result<DiscoveryResult>> AnalyzeChangeAsync(DiscoverySession session, PlayerAction action, CancellationToken ct = default);

    /// <summary>
    /// Gets the ranked list of discovered values ordered by confidence score.
    /// </summary>
    /// <param name="session">The active discovery session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of discovered values.</returns>
    Task<Result<List<DiscoveredValue>>> GetRankedResultsAsync(DiscoverySession session, CancellationToken ct = default);

    /// <summary>
    /// Stops the discovery session and releases resources.
    /// </summary>
    /// <param name="session">The session to stop.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> StopDiscoverySessionAsync(DiscoverySession session, CancellationToken ct = default);

    /// <summary>
    /// Submits user feedback for a discovered value to improve future detection.
    /// </summary>
    /// <param name="feedback">The feedback to submit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SubmitFeedbackAsync(DiscoveryFeedback feedback, CancellationToken ct = default);
}

/// <summary>
/// Represents a discovery session with its state and context.
/// </summary>
public sealed class DiscoverySession
{
    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    public Guid SessionId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the process ID being analyzed.
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Gets the discovery options for this session.
    /// </summary>
    public DiscoveryOptions Options { get; init; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the current discovery pass number.
    /// </summary>
    public int CurrentPass { get; set; }

    /// <summary>
    /// Gets or sets whether the session is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets the list of candidate values being evaluated.
    /// </summary>
    public List<DiscoveredValue> Candidates { get; } = new();

    /// <summary>
    /// Gets the history of player actions and observations.
    /// </summary>
    public List<PlayerActionRecord> ActionHistory { get; } = new();
}

/// <summary>
/// Configuration options for memory pattern discovery.
/// </summary>
public sealed class DiscoveryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of candidates to track.
    /// </summary>
    public int MaxCandidates { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the minimum confidence score threshold (0.0-1.0).
    /// </summary>
    public double MinConfidenceThreshold { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets whether to scan for integer values.
    /// </summary>
    public bool ScanIntegers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to scan for float values.
    /// </summary>
    public bool ScanFloats { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to scan for double values.
    /// </summary>
    public bool ScanDoubles { get; set; } = false;

    /// <summary>
    /// Gets or sets the memory scan start address.
    /// </summary>
    public nuint ScanStartAddress { get; set; } = 0x00400000;

    /// <summary>
    /// Gets or sets the memory scan size in bytes.
    /// </summary>
    public nuint ScanSize { get; set; } = 0x10000000; // 256MB default

    /// <summary>
    /// Gets or sets the scan interval in milliseconds between passes.
    /// </summary>
    public int ScanIntervalMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; } = 20;
}

/// <summary>
/// Represents a player action for pattern analysis.
/// </summary>
public enum PlayerAction
{
    /// <summary>Unknown or unspecified action.</summary>
    Unknown,

    /// <summary>Player took damage (health decreased).</summary>
    TookDamage,

    /// <summary>Player healed (health increased).</summary>
    Healed,

    /// <summary>Player spent money/currency (value decreased).</summary>
    SpentMoney,

    /// <summary>Player earned money/currency (value increased).</summary>
    EarnedMoney,

    /// <summary>Player used ammo (ammo count decreased).</summary>
    UsedAmmo,

    /// <summary>Player reloaded (ammo count increased).</summary>
    Reloaded,

    /// <summary>Player gained experience points.</summary>
    GainedXp,

    /// <summary>Player leveled up.</summary>
    LeveledUp,

    /// <summary>Player position changed.</summary>
    PositionChanged,

    /// <summary>Player score increased.</summary>
    ScoreIncreased,

    /// <summary>Player performed an attack (for combo detection).</summary>
    Attacked,

    /// <summary>Player blocked incoming damage (for shield detection).</summary>
    BlockedDamage,

    /// <summary>Player sprinted or ran (for stamina detection).</summary>
    Sprinted,

    /// <summary>Player used a special ability (for energy detection).</summary>
    UsedAbility,

    /// <summary>Player used an item (for hunger/thirst detection).</summary>
    UsedItem,

    /// <summary>Player died or was defeated (for lives detection).</summary>
    Died,

    /// <summary>Player is idle/resting (for regeneration detection).</summary>
    Idle,

    /// <summary>Player moved (for vehicle fuel detection).</summary>
    Moved,

    /// <summary>Player jumped (for jump detection).</summary>
    Jumped,

    /// <summary>Player rotated/turned (for rotation detection).</summary>
    Rotated,

    /// <summary>Player aimed/looked (for aim detection).</summary>
    AimChanged,

    /// <summary>Player dodged (for dodge detection).</summary>
    Dodged,

    /// <summary>Custom action with metadata.</summary>
    Custom
}

/// <summary>
/// Represents a discovered memory value with confidence scoring.
/// </summary>
public sealed class DiscoveredValue
{
    /// <summary>
    /// Gets or sets the memory address of the value.
    /// </summary>
    public IntPtr Address { get; set; }

    /// <summary>
    /// Gets or sets the data type of the value (e.g., "Int32", "Float").
    /// </summary>
    public string ValueType { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the current value at this address.
    /// </summary>
    public object? CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the previous value at this address.
    /// </summary>
    public object? PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0-1.0) for this discovery.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the suggested name for this value based on heuristics.
    /// </summary>
    public string SuggestedName { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the category of this value (e.g., "Health", "Currency").
    /// </summary>
    public string Category { get; set; } = "Unknown";

    /// <summary>
    /// Gets the observation history for this value.
    /// </summary>
    public List<ValueObservation> ObservationHistory { get; } = new();

    /// <summary>
    /// Gets or sets when this value was first discovered.
    /// </summary>
    public DateTime FirstObserved { get; set; }

    /// <summary>
    /// Gets or sets when this value was last observed.
    /// </summary>
    public DateTime LastObserved { get; set; }

    /// <summary>
    /// Gets or sets the number of times this value has been observed.
    /// </summary>
    public int ObservationCount { get; set; }

    /// <summary>
    /// Gets or sets whether this value has been confirmed by the user.
    /// </summary>
    public bool IsConfirmed { get; set; }
}

/// <summary>
/// Represents a single observation of a value.
/// </summary>
public sealed class ValueObservation
{
    /// <summary>
    /// Gets or sets the timestamp of the observation.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the value at the time of observation.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the player action that occurred at this observation, if any.
    /// </summary>
    public PlayerAction? RelatedAction { get; set; }

    /// <summary>
    /// Gets or sets the change delta from the previous value.
    /// </summary>
    public double? Delta { get; set; }
}

/// <summary>
/// Represents a record of a player action during discovery.
/// </summary>
public sealed class PlayerActionRecord
{
    /// <summary>
    /// Gets or sets the timestamp of the action.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the action type.
    /// </summary>
    public PlayerAction Action { get; set; }

    /// <summary>
    /// Gets or sets custom metadata for the action.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of a discovery analysis.
/// </summary>
public sealed class DiscoveryResult
{
    /// <summary>
    /// Gets or sets the session ID this result belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the action that was analyzed.
    /// </summary>
    public PlayerAction AnalyzedAction { get; set; }

    /// <summary>
    /// Gets or sets the number of candidates remaining after filtering.
    /// </summary>
    public int RemainingCandidates { get; set; }

    /// <summary>
    /// Gets or sets the number of candidates eliminated.
    /// </summary>
    public int EliminatedCandidates { get; set; }

    /// <summary>
    /// Gets or sets the top discovered values after this analysis.
    /// </summary>
    public List<DiscoveredValue> TopValues { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the analysis improved confidence scores.
    /// </summary>
    public bool ConfidenceImproved { get; set; }
}

/// <summary>
/// Represents user feedback for a discovered value.
/// </summary>
public sealed class DiscoveryFeedback
{
    /// <summary>
    /// Gets or sets the session ID this feedback is for.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the memory address of the value.
    /// </summary>
    public IntPtr Address { get; set; }

    /// <summary>
    /// Gets or sets whether the discovery was correct.
    /// </summary>
    public bool WasCorrect { get; set; }

    /// <summary>
    /// Gets or sets the correct name provided by the user, if renamed.
    /// </summary>
    public string? CorrectName { get; set; }

    /// <summary>
    /// Gets or sets the correct category provided by the user.
    /// </summary>
    public string? CorrectCategory { get; set; }

    /// <summary>
    /// Gets or sets when the feedback was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets optional feedback notes.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Interface for value heuristics that score potential memory values.
/// </summary>
public interface IValueHeuristic
{
    /// <summary>
    /// Gets the name of this heuristic.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the category this heuristic detects (e.g., "Health", "Currency").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Calculates a confidence score for the given value based on its observation history.
    /// </summary>
    /// <param name="value">The discovered value to evaluate.</param>
    /// <param name="history">The observation history for this value.</param>
    /// <returns>A confidence score between 0.0 and 1.0.</returns>
    double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history);

    /// <summary>
    /// Determines if this heuristic supports the given value type.
    /// </summary>
    /// <param name="valueType">The value type (e.g., "Int32", "Float").</param>
    /// <returns>True if this heuristic can evaluate the value type.</returns>
    bool SupportsValueType(string valueType);
}
