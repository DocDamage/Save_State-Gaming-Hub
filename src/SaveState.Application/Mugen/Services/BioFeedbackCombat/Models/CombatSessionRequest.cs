namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Request to start a combat session.
/// </summary>
public class CombatSessionRequest
{
    public string OpponentId { get; set; } = default!;
    public string MatchType { get; set; } = default!;
    public TimeSpan ExpectedDuration { get; set; } = default!;
}
