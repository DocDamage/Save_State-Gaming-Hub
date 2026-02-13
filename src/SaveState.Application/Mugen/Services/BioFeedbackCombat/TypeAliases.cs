// Type aliases for backward compatibility
// These aliases allow existing code to use the old prefixed type names
// while the new code uses the clean type names

using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

// Models
using SaveState.Application.Mugen.Services.BioFeedbackCombat;

public class BioFeedbackCombatServiceBioProfile : BioProfile { }
public class BioFeedbackCombatServiceBaselineMetrics : BaselineMetrics { }
public class BioFeedbackCombatServiceBioCalibration : BioCalibration { }
public class BioFeedbackCombatServiceBioSettings : BioSettings { }
public class BioFeedbackCombatServiceBioCombatModifiers : BioCombatModifiers { }
public class BioFeedbackCombatServiceBioFeedbackCombatSession : BioFeedbackCombatSession { }
public class BioFeedbackCombatServiceBioDataStream : BioDataStream { }
public class BioFeedbackCombatServiceBioDataPoint : BioDataPoint { }
public class BioFeedbackCombatServiceCombatBioMetrics : CombatBioMetrics { }
public class BioFeedbackCombatServicePhysiologicalState : PhysiologicalState { }
public class BioFeedbackCombatServiceBioProfileRequest : BioProfileRequest { }
public class BioFeedbackCombatServiceCombatSessionRequest : CombatSessionRequest { }
public class BioFeedbackCombatServiceBioDataInput : BioDataInput { }
public class BioFeedbackCombatServiceBioFeedback : BioFeedback { }
public class BioFeedbackCombatServiceHeartRateFeedback : HeartRateFeedback { }
public class BioFeedbackCombatServiceBreathingFeedback : BreathingFeedback { }
public class BioFeedbackCombatServiceMuscleFeedback : MuscleFeedback { }
public class BioFeedbackCombatServiceWeaponChargeRequest : WeaponChargeRequest { }
public class BioFeedbackCombatServiceHeartRateWeapon : HeartRateWeapon { }
public class BioFeedbackCombatServiceComboEnhancementRequest : ComboEnhancementRequest { }
public class BioFeedbackCombatServiceBreathingCombo : BreathingCombo { }
public class BioFeedbackCombatServiceDefenseRequest : DefenseRequest { }
public class BioFeedbackCombatServiceMusclePoweredDefense : MusclePoweredDefense { }
public class BioFeedbackCombatServiceBurstTrigger : BurstTrigger { }
public class BioFeedbackCombatServiceAdrenalineBurst : AdrenalineBurst { }
public class BioFeedbackCombatServiceMeditationRequest : MeditationRequest { }
public class BioFeedbackCombatServiceMeditationMode : MeditationMode { }
public class BioFeedbackCombatServiceBioCombatReport : BioCombatReport { }
public class BioFeedbackCombatServicePhysiologicalTrends : PhysiologicalTrends { }
public class BioFeedbackCombatServiceBioEffectiveness : BioEffectiveness { }
public class BioFeedbackCombatServicePeakMoment : PeakMoment { }
public class BioFeedbackCombatServiceFatigueAnalysis : FatigueAnalysis { }
public class BioFeedbackCombatServiceStressAnalysis : StressAnalysis { }

// Engines
public class BioFeedbackCombatServiceHeartRateEngine : HeartRateEngine
{
    public BioFeedbackCombatServiceHeartRateEngine(ILogger<HeartRateEngine> logger) : base(logger) { }
}

public class BioFeedbackCombatServiceBreathingEngine : BreathingEngine
{
    public BioFeedbackCombatServiceBreathingEngine(ILogger<BreathingEngine> logger) : base(logger) { }
}

public class BioFeedbackCombatServiceMuscleTensionEngine : MuscleTensionEngine
{
    public BioFeedbackCombatServiceMuscleTensionEngine(ILogger<MuscleTensionEngine> logger) : base(logger) { }
}

public class BioFeedbackCombatServiceAdrenalineEngine : AdrenalineEngine
{
    public BioFeedbackCombatServiceAdrenalineEngine(ILogger<AdrenalineEngine> logger) : base(logger) { }
}

public class BioFeedbackCombatServiceMeditationEngine : MeditationEngine
{
    public BioFeedbackCombatServiceMeditationEngine(ILogger<MeditationEngine> logger) : base(logger) { }
}

// Interface
public interface BioFeedbackCombatServiceIBioFeedbackCombatService : IBioFeedbackCombatService { }

// Enums
public enum BioFeedbackCombatServiceBioProfileStatus
{
    Active = BioProfileStatus.Active,
    Calibrating = BioProfileStatus.Calibrating,
    Inactive = BioProfileStatus.Inactive,
    Error = BioProfileStatus.Error
}

public enum BioFeedbackCombatServiceCombatStatus
{
    Preparing = CombatStatus.Preparing,
    Active = CombatStatus.Active,
    Paused = CombatStatus.Paused,
    Completed = CombatStatus.Completed,
    Failed = CombatStatus.Failed
}

public enum BioFeedbackCombatServiceBurstTriggerType
{
    Physiological = BurstTriggerType.Physiological,
    Combat = BurstTriggerType.Combat,
    Manual = BurstTriggerType.Manual,
    Emergency = BurstTriggerType.Emergency
}

public enum BioFeedbackCombatServiceMeditationTechnique
{
    BreathingFocus = MeditationTechnique.BreathingFocus,
    Mindfulness = MeditationTechnique.Mindfulness,
    Visualization = MeditationTechnique.Visualization,
    Zen = MeditationTechnique.Zen
}

public enum BioFeedbackCombatServicePeakType
{
    AdrenalineBurst = PeakType.AdrenalineBurst,
    PerfectCombo = PeakType.PerfectCombo,
    MeditationPeak = PeakType.MeditationPeak,
    HeartRateSpike = PeakType.HeartRateSpike
}
