// Type aliases for backward compatibility
// These aliases allow existing code to use the old prefixed type names
// while the new code uses the clean type names

using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

// Models - uses new Models.BalanceTuning namespace
using SaveState.Application.Mugen.Models.BalanceTuning;

// Services - uses Services.BalanceTuning namespace
using SaveState.Application.Mugen.Services.BalanceTuning;

public class BalanceTuningServiceBalanceAnalysis : BalanceAnalysis { }
public class BalanceTuningServiceMechanicUsage : MechanicUsage { }
public class BalanceTuningServiceWinRateData : WinRateData { }
public class BalanceTuningServicePlaytimeDistribution : PlaytimeDistribution { }
public class BalanceTuningServiceSkillGapAnalysis : SkillGapAnalysis { }
public class BalanceTuningServiceBalanceRecommendation : BalanceRecommendation { }
public class BalanceTuningServiceBalanceData : BalanceData { }
public class BalanceTuningServiceBalanceAdjustment : BalanceAdjustment { }
public class BalanceTuningServiceAdjustmentApplication : AdjustmentApplication { }
public class BalanceTuningServiceMechanicAdjustmentApplication : MechanicAdjustmentApplication { }
public class BalanceTuningServiceBalancePatch : BalancePatch { }
public class BalanceTuningServiceTestResults : TestResults { }
public class BalanceTuningServiceBalanceRiskAssessment : BalanceRiskAssessment { }
public class BalanceTuningServiceRollbackPlan : RollbackPlan { }
public class BalanceTuningServiceBalanceMonitoring : BalanceMonitoring { }
public class BalanceTuningServiceBalanceMetrics : BalanceMetrics { }
public class BalanceTuningServiceBalanceTrendAnalysis : BalanceTrendAnalysis { }
public class BalanceTuningServiceBalanceAlert : BalanceAlert { }
public class BalanceTuningServiceCompetitiveRanking : CompetitiveRanking { }
public class BalanceTuningServiceBalancePlayerRanking : PlayerRanking { }
public class BalanceTuningServiceRankingDivision : RankingDivision { }
public class BalanceTuningServiceSeasonStatistics : SeasonStatistics { }
public class BalanceTuningServicePlayerStats : PlayerStats { }
public class BalanceTuningServiceBalanceReport : BalanceReport { }
public class BalanceTuningServiceDateRange : DateRange { }
public class BalanceTuningServiceExecutiveSummary : ExecutiveSummary { }
public class BalanceTuningServiceMechanicBalanceAnalysis : MechanicBalanceAnalysis { }
public class BalanceTuningServiceTrendData : TrendData { }
public class BalanceTuningServicePlayerFeedbackSummary : PlayerFeedbackSummary { }
public class BalanceTuningServiceTournamentResultsAnalysis : TournamentResultsAnalysis { }
public class BalanceTuningServiceReportRecommendation : ReportRecommendation { }
public class BalanceTuningServiceBalanceProfile : BalanceProfile { }
public class BalanceTuningServiceMechanicBalance : MechanicBalance { }
public class BalanceTuningServiceMechanicUsageStats : MechanicUsageStats { }
public class BalanceTuningServiceMatchData : MatchData { }

// Engines
public class BalanceTuningServiceEloCalculator : EloCalculator { }
public class BalanceTuningServiceMatchmakingBalance : MatchmakingBalance { }

// Interface
public interface BalanceTuningServiceIBalanceTuningService : IBalanceTuningService { }

// Enums
public enum BalanceTuningServiceMechanicType
{
    QuantumSuperposition = MechanicType.QuantumSuperposition,
    EmotionalResonance = MechanicType.EmotionalResonance,
    RealityWarping = MechanicType.RealityWarping,
    BioFeedback = MechanicType.BioFeedback,
    ZAxisMovement = MechanicType.ZAxisMovement,
    JuggleGravity = MechanicType.JuggleGravity,
    FrameData = MechanicType.FrameData,
    InputBuffer = MechanicType.InputBuffer,
    AxisAwareHitDetection = MechanicType.AxisAwareHitDetection,
    RealJuggleDecay = MechanicType.RealJuggleDecay,
    CharacterSpecificGravity = MechanicType.CharacterSpecificGravity,
    WallSplatLogic = MechanicType.WallSplatLogic,
    FloorWallBreaks = MechanicType.FloorWallBreaks,
    SymbioticPartner = MechanicType.SymbioticPartner,
    DreamLogicArenas = MechanicType.DreamLogicArenas,
    NarrativeMemoryCrystals = MechanicType.NarrativeMemoryCrystals,
    CrossPhaseSynergy = MechanicType.CrossPhaseSynergy
}

public enum BalanceTuningServiceRecommendationPriority
{
    Low = RecommendationPriority.Low,
    Medium = RecommendationPriority.Medium,
    High = RecommendationPriority.High,
    Critical = RecommendationPriority.Critical
}

public enum BalanceTuningServiceAlertSeverity
{
    Low = AlertSeverity.Low,
    Medium = AlertSeverity.Medium,
    High = AlertSeverity.High,
    Critical = AlertSeverity.Critical
}
