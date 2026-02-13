namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance adjustment data.
/// </summary>
public class BalanceAdjustment
{
    public MechanicType Mechanic { get; set; } = default!;
    public IReadOnlyDictionary<string, object> CurrentParameters { get; set; } = default!;
    public IReadOnlyDictionary<string, object> TargetParameters { get; set; } = default!;
    public string AdjustmentType { get; set; } = default!;
    public float Magnitude { get; set; } = default!;
    public float Confidence { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
}
