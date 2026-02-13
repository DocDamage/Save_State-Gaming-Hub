namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance profile for a session.
/// </summary>
public class BalanceProfile
{
    public string SessionId { get; set; } = default!;
    public MechanicBalance CurrentBalance { get; set; } = default!;
    public IReadOnlyList<BalanceAdjustment> AdjustmentHistory { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}
