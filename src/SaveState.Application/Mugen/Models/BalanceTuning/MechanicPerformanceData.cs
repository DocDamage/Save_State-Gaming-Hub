namespace SaveState.Application.Mugen.Models.BalanceTuning;

public class MechanicPerformanceData
{
    public string MechanicName { get; set; } = default!;
    public float WinRate { get; set; }
    public int UsageCount { get; set; }
}
