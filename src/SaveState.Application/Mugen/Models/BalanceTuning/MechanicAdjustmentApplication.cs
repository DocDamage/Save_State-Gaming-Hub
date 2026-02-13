namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Mechanic-specific adjustment application result.
/// </summary>
public class MechanicAdjustmentApplication
{
    public bool Success { get; set; } = default!;
    public float PerformanceImpact { get; set; } = default!;
}
