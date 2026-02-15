namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

// Legacy enum aliases for backward compatibility
public enum BioFeedbackCombatServiceBioProfileStatus
{
    Active = 0,
    Calibrating = 1,
    Inactive = 2,
    Error = 3
}

public enum BioFeedbackCombatServiceCombatStatus
{
    Preparing = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Failed = 4
}

public enum BioFeedbackCombatServiceMeditationTechnique
{
    BreathingFocus = 0,
    BodyScan = 1,
    Visualization = 2,
    Mantra = 3,
    Mindfulness = 4,
    Zen = 5
}

public enum BioFeedbackCombatServiceBurstTriggerType
{
    Physiological = 0,
    Combat = 1,
    Manual = 2,
    Emergency = 3
}

public enum BioFeedbackCombatServicePeakType
{
    AdrenalineBurst = 0,
    PerfectCombo = 1,
    MeditationPeak = 2,
    HeartRateSpike = 3
}

public enum BioFeedbackCombatServiceTrendDirection
{
    Increasing = 0,
    Decreasing = 1,
    Stable = 2
}
