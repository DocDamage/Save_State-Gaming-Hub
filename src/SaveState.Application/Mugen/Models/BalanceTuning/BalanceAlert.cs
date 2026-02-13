namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance alert data.
/// </summary>
public class BalanceAlert
{
    public string AlertType { get; set; } = default!;
    public AlertSeverity Severity { get; set; } = default!;
    public string Message { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}
