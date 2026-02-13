namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// Types of training modes available.
/// </summary>
public enum TrainingType
{
    Reflex,
    PatternRecognition,
    ComboLab
}

/// <summary>
/// Session status states.
/// </summary>
public enum SessionStatus
{
    Active,
    Paused,
    Completed,
    Failed
}

/// <summary>
/// Reflex training modes.
/// </summary>
public enum ReflexTrainingMode
{
    VisualStimuli,
    AudioStimuli,
    MixedStimuli,
    Predictive
}

/// <summary>
/// Pattern types for pattern recognition training.
/// </summary>
public enum PatternType
{
    InputSequence,
    MoveSequence,
    TimingSequence,
    Mixed
}

/// <summary>
/// Combo lab exercise types.
/// </summary>
public enum ComboLabType
{
    BasicCombos,
    AdvancedCombos,
    CustomCombos,
    ChallengeCombos
}

/// <summary>
/// Types of training input.
/// </summary>
public enum InputType
{
    ButtonPress,
    Timing,
    Sequence,
    Direction
}

/// <summary>
/// Priority levels for training recommendations.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Challenge difficulty levels.
/// </summary>
public enum ChallengeDifficulty
{
    Beginner,
    Easy,
    Normal,
    Hard,
    Expert,
    Master
}

/// <summary>
/// AI dummy behavior modes.
/// </summary>
public enum DummyBehavior
{
    Stand,
    Crouch,
    Jump,
    Walk,
    Attack,
    Block,
    BlockAll,
    BlockRandom,
    Counter,
    Reversal,
    GuardCancel,
    Recovery
}

/// <summary>
/// Recording playback modes.
/// </summary>
public enum PlaybackMode
{
    Once,
    Loop,
    Random,
    PingPong
}
