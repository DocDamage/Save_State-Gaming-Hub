namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Combo move data.
/// </summary>
public class ComboMove
{
    public string MoveId { get; set; } = default!;
    public string MoveName { get; set; } = default!;
    public int Damage { get; set; } = default!;
    public int FrameAdvantage { get; set; } = default!;
    public int StartupFrames { get; set; } = default!;
    public float ScalingFactor { get; set; } = default!;
    public AttackProperty Property { get; set; } = default!;
    public bool IsJuggleStarter { get; set; } = default!;
    public bool IsLauncher { get; set; } = default!;
    public DateTime ExecutedAt { get; set; } = default!;
}

/// <summary>
/// Combo validation result.
/// </summary>
public class ComboValidation
{
    public bool IsValid { get; set; } = default!;
    public string ComboId { get; set; } = default!;
    public int TotalDamage { get; set; } = default!;
    public int MoveCount { get; set; } = default!;
    public ComboType Type { get; set; } = default!;
    public IReadOnlyList<string> Errors { get; set; } = default!;
    public float ScalingApplied { get; set; } = default!;
}

/// <summary>
/// Combo sequence data.
/// </summary>
public class ComboSequence
{
    public string ComboId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public IReadOnlyList<ComboMove> Moves { get; set; } = default!;
    public int TotalDamage { get; set; } = default!;
    public int TotalHits { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public DateTime? EndedAt { get; set; } = default!;
    public bool IsComplete { get; set; } = default!;
}

/// <summary>
/// Combo input buffer request.
/// </summary>
public class ComboInputRequest
{
    public string SessionId { get; set; } = default!;
    public string Input { get; set; } = default!;
    public int CurrentFrame { get; set; } = default!;
    public ComboType PreferredType { get; set; } = default!;
}

/// <summary>
/// Input buffer result.
/// </summary>
public class InputBufferResult
{
    public string ProcessedInput { get; set; } = default!;
    public string ExpectedInput { get; set; } = default!;
    public bool Success { get; set; } = default!;
    public int BufferedFrames { get; set; } = default!;
    public int TimingOffset { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
}

/// <summary>
/// Input buffer request.
/// </summary>
public class InputBufferRequest
{
    public string Input { get; set; } = default!;
    public string ExpectedInput { get; set; } = default!;
    public int BufferSize { get; set; } = default!;
}

/// <summary>
/// Input buffer data.
/// </summary>
public class InputBuffer
{
    public string BufferId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public string Input { get; set; } = default!;
    public string Expected { get; set; } = default!;
    public int BufferSize { get; set; } = default!;
    public bool Success { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Input buffer stats.
/// </summary>
public class InputBufferStats
{
    public int TotalInputs { get; set; } = default!;
    public int SuccessfulBuffers { get; set; } = default!;
    public float AverageBufferSize { get; set; } = default!;
    public float ForgivenessRate { get; set; } = default!;
    public string[] CommonMistakes { get; set; } = default!;
    public DateTime MeasuredAt { get; set; } = default!;
}

/// <summary>
/// Input buffer efficiency.
/// </summary>
public class InputBufferEfficiency
{
    public int BufferSizeUsed { get; set; } = default!;
    public int InputsBuffered { get; set; } = default!;
    public int SuccessfulBuffers { get; set; } = default!;
    public float ForgivenessRate { get; set; } = default!;
    public float InputAccuracy { get; set; } = default!;
}
