namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio feedback combat session data.
/// </summary>
public class BioFeedbackCombatSession
{
    public string SessionId { get; set; } = default!;
    public string ProfileId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public BioDataStream BioDataStream { get; set; } = default!;
    public CombatBioMetrics CombatMetrics { get; set; } = default!;
    public PhysiologicalState PhysiologicalState { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public DateTime? EndedAt { get; set; } = default!;
    public CombatStatus Status { get; set; } = default!;
}
