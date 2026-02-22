namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

/// <summary>
/// Represents a recording of dummy behavior during a MUGEN training session.
/// Captures the dummy's actions and responses for analysis and replay.
/// </summary>
public class MugenDummyRecording : EntityBase
{
    /// <summary>
    /// The ID of the training session this recording belongs to.
    /// </summary>
    public Guid TrainingSessionId { get; private set; }

    /// <summary>
    /// The training session this recording belongs to.
    /// </summary>
    public MugenTrainingSession TrainingSession { get; private set; } = null;

    /// <summary>
    /// The type of dummy behavior recorded.
    /// </summary>
    public DummyBehaviorType BehaviorType { get; private set; }

    /// <summary>
    /// The sequence of actions performed by the dummy.
    /// Stored as JSON string containing the action sequence.
    /// </summary>
    public string ActionSequence { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of what was being practiced.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// When this recording was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The duration of this recording segment.
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// Whether this recording was marked as successful/favorable.
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// Optional path to a video replay file.
    /// </summary>
    public string? ReplayPath { get; private set; }

    /// <summary>
    /// The number of hits achieved in the combo during this recording.
    /// </summary>
    public int ComboHits { get; private set; }

    /// <summary>
    /// The total damage dealt during the combo in this recording.
    /// </summary>
    public int ComboDamage { get; private set; }

    /// <summary>
    /// Creates a new dummy recording.
    /// </summary>
    /// <param name="trainingSessionId">The training session ID.</param>
    /// <param name="behaviorType">The type of dummy behavior.</param>
    /// <param name="actionSequence">The sequence of actions (JSON).</param>
    /// <param name="duration">The duration of the recording.</param>
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="isSuccessful">Whether this was a successful recording.</param>
    /// <returns>A new MugenDummyRecording instance.</returns>
    public static MugenDummyRecording Create(
        Guid trainingSessionId,
        DummyBehaviorType behaviorType,
        string actionSequence,
        TimeSpan duration,
        ITimeProvider timeProvider,
        string? description = null,
        bool isSuccessful = false)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new MugenDummyRecording
        {
            Id = Guid.NewGuid(),
            TrainingSessionId = trainingSessionId,
            BehaviorType = behaviorType,
            ActionSequence = Guard.Against.NullOrWhiteSpace(actionSequence, nameof(actionSequence)),
            Duration = duration,
            Description = description,
            IsSuccessful = isSuccessful,
            CreatedAt = timeProvider.UtcNow,
            ComboHits = 0,
            ComboDamage = 0
        };
    }

    /// <summary>
    /// Creates a new dummy recording with explicit timestamp.
    /// </summary>
    /// <param name="trainingSessionId">The training session ID.</param>
    /// <param name="behaviorType">The type of dummy behavior.</param>
    /// <param name="actionSequence">The sequence of actions (JSON).</param>
    /// <param name="duration">The duration of the recording.</param>
    /// <param name="createdAt">Creation timestamp.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="isSuccessful">Whether this was a successful recording.</param>
    /// <returns>A new MugenDummyRecording instance.</returns>
    public static MugenDummyRecording Create(
        Guid trainingSessionId,
        DummyBehaviorType behaviorType,
        string actionSequence,
        TimeSpan duration,
        DateTime createdAt,
        string? description = null,
        bool isSuccessful = false)
    {
        return new MugenDummyRecording
        {
            Id = Guid.NewGuid(),
            TrainingSessionId = trainingSessionId,
            BehaviorType = behaviorType,
            ActionSequence = Guard.Against.NullOrWhiteSpace(actionSequence, nameof(actionSequence)),
            Duration = duration,
            Description = description,
            IsSuccessful = isSuccessful,
            CreatedAt = createdAt,
            ComboHits = 0,
            ComboDamage = 0
        };
    }

    [Obsolete("Use Create(Guid, DummyBehaviorType, string, TimeSpan, ITimeProvider, string?, bool) or Create(Guid, DummyBehaviorType, string, TimeSpan, DateTime, string?, bool) instead")]
    public static MugenDummyRecording Create(
        Guid trainingSessionId,
        DummyBehaviorType behaviorType,
        string actionSequence,
        TimeSpan duration,
        string? description = null,
        bool isSuccessful = false)
    {
        return new MugenDummyRecording
        {
            Id = Guid.NewGuid(),
            TrainingSessionId = trainingSessionId,
            BehaviorType = behaviorType,
            ActionSequence = Guard.Against.NullOrWhiteSpace(actionSequence, nameof(actionSequence)),
            Duration = duration,
            Description = description,
            IsSuccessful = isSuccessful,
            CreatedAt = SystemTimeProvider.Instance.UtcNow,
            ComboHits = 0,
            ComboDamage = 0
        };
    }

    /// <summary>
    /// Updates the recording metadata.
    /// </summary>
    /// <param name="description">New description.</param>
    /// <param name="isSuccessful">New success status.</param>
    public void Update(string? description, bool isSuccessful)
    {
        Description = description;
        IsSuccessful = isSuccessful;
    }

    /// <summary>
    /// Sets the replay path for this recording.
    /// </summary>
    /// <param name="path">Path to the replay file.</param>
    public void SetReplayPath(string? path)
    {
        ReplayPath = path;
    }

    /// <summary>
    /// Sets the combo statistics for this recording.
    /// </summary>
    /// <param name="comboHits">Number of hits in the combo.</param>
    /// <param name="comboDamage">Total damage dealt by the combo.</param>
    public void SetComboStats(int comboHits, int comboDamage)
    {
        ComboHits = Guard.Against.Negative(comboHits, nameof(comboHits));
        ComboDamage = Guard.Against.Negative(comboDamage, nameof(comboDamage));
    }

    // EF Core constructor
    private MugenDummyRecording() { }
}

/// <summary>
/// Represents the type of dummy behavior recorded.
/// </summary>
public enum DummyBehaviorType
{
    Standing,
    Crouching,
    Jumping,
    Walking,
    Blocking,
    ComboString,
    MixupPattern,
    RecoveryPractice,
    Custom
}
