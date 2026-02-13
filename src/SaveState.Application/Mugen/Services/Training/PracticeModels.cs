using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// Training session data.
/// </summary>
public class TrainingSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public TrainingType TrainingType { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public TimeSpan? Duration { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public TrainingTypes.TrainingProgressData Progress { get; set; } = default!;
    public ReflexTrainingMode? TrainingMode { get; set; }
    public ReflexTrainingData? ReflexData { get; set; }
    public PatternRecognitionData? PatternData { get; set; }
    public ComboLabData? ComboData { get; set; }
}

/// <summary>
/// Reflex training request.
/// </summary>
public class ReflexTrainingRequest
{
    public ReflexTrainingMode TrainingMode { get; set; } = ReflexTrainingMode.VisualStimuli;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Pattern recognition request.
/// </summary>
public class PatternRecognitionRequest
{
    public int SequenceLength { get; set; } = 4;
    public PatternType SequenceType { get; set; } = PatternType.InputSequence;
    public int SequenceCount { get; set; } = 10;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Combo lab request.
/// </summary>
public class ComboLabRequest
{
    public ComboLabType LabType { get; set; } = ComboLabType.BasicCombos;
    public IReadOnlyList<string> TargetCombo { get; set; } = Array.Empty<string>();
    public int MaxAttempts { get; set; } = 10;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Training input data.
/// </summary>
public class TrainingInput
{
    public string InputId { get; set; } = Guid.NewGuid().ToString();
    public InputType InputType { get; set; } = InputType.ButtonPress;
    public object InputData { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Reflex training data.
/// </summary>
public class ReflexTrainingData
{
    public int StimuliPresented { get; set; }
    public double ResponseAccuracy { get; set; }
    public TimeSpan AverageReactionTime { get; set; }
    public TimeSpan FastestReaction { get; set; } = TimeSpan.MaxValue;
    public TimeSpan SlowestReaction { get; set; }
    public ReflexTrainingMode TrainingMode { get; set; }
}

/// <summary>
/// Pattern recognition data.
/// </summary>
public class PatternRecognitionData
{
    public int SequenceLength { get; set; }
    public PatternType SequenceType { get; set; }
    public IReadOnlyList<string> CurrentSequence { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PlayerSequence { get; set; } = Array.Empty<string>();
    public int SequencesCompleted { get; set; }
    public double AccuracyRate { get; set; }
}

/// <summary>
/// Combo lab data.
/// </summary>
public class ComboLabData
{
    public ComboLabType LabType { get; set; }
    public IReadOnlyList<string> TargetCombo { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> CurrentInput { get; set; } = Array.Empty<string>();
    public int ComboProgress { get; set; }
    public int Mistakes { get; set; }
    public TimeSpan TimeToComplete { get; set; }
    public TimeSpan BestTime { get; set; } = TimeSpan.MaxValue;
    public int Attempts { get; set; }
}

/// <summary>
/// Practice session configuration.
/// </summary>
public class PracticeSettings
{
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public TimeSpan? Duration { get; set; }
    public bool AutoReset { get; set; } = true;
    public bool ShowInputDisplay { get; set; } = true;
    public bool ShowFrameData { get; set; } = false;
    public bool InfiniteMeter { get; set; } = false;
    public bool InfiniteHealth { get; set; } = false;
}

/// <summary>
/// Practice session state.
/// </summary>
public class PracticeSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public PracticeSettings Settings { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public int ComboCount { get; set; }
    public int MaxComboHits { get; set; }
    public double DamageDealt { get; set; }
}
