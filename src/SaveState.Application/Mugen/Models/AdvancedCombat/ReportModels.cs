namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Advanced combat report.
/// </summary>
public class AdvancedCombatReport
{
    public string SessionId { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public ZAxisUtilization ZAxisUtilization { get; set; } = default!;
    public JuggleMechanics JuggleMechanics { get; set; } = default!;
    public FrameDataInsights FrameDataInsights { get; set; } = default!;
    public InputBufferEfficiency InputBufferEfficiency { get; set; } = default!;
    public float OverallMechanicsScore { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Z-axis utilization data.
/// </summary>
public class ZAxisUtilization
{
    public int TotalMovements { get; set; } = default!;
    public float AverageDistance { get; set; } = default!;
    public float SidestepFrequency { get; set; } = default!;
    public float PositioningEfficiency { get; set; } = default!;
    public float EvasionSuccessRate { get; set; } = default!;
}
