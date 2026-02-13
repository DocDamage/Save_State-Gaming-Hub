namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Status of a bio feedback combat session.
/// </summary>
public enum CombatStatus
{
    Preparing,
    Active,
    Paused,
    Completed,
    Failed
}
