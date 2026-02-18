namespace SaveState.Application.Mugen.Services.AutomatedBalancing;

/// <summary>
/// Balance analysis result.
/// </summary>
public class BalanceAnalysis
{
    public string GameId { get; set; } = default!;
    public DateTime AnalyzedAt { get; set; }
    public IReadOnlyList<CharacterViabilityMetrics> CharacterMetrics { get; set; } = Array.Empty<CharacterViabilityMetrics>();
    public IReadOnlyList<MatchupPerformanceAnalysis> MatchupAnalyses { get; set; } = Array.Empty<MatchupPerformanceAnalysis>();
    public IReadOnlyList<MovePerformanceAnalysis> MoveAnalyses { get; set; } = Array.Empty<MovePerformanceAnalysis>();
    public MetaAnalysis Meta { get; set; } = default!;
    public BalanceTrends Trends { get; set; } = default!;
}

/// <summary>
/// Balance adjustment recommendation.
/// </summary>
public class BalanceAdjustment
{
    public string AdjustmentId { get; set; } = default!;
    public string TargetElement { get; set; } = default!;
    public AdjustmentType Type { get; set; }
    public double Magnitude { get; set; }
    public string Reason { get; set; } = default!;
    public double Confidence { get; set; }
    public IReadOnlyList<StatAdjustment> StatAdjustments { get; set; } = Array.Empty<StatAdjustment>();
    public IReadOnlyList<MoveAdjustment> MoveAdjustments { get; set; } = Array.Empty<MoveAdjustment>();
}

public enum AdjustmentType
{
    Buff,
    Nerf,
    Rework
}

/// <summary>
/// Balance patch.
/// </summary>
public class BalancePatch
{
    public string PatchId { get; set; } = default!;
    public string Version { get; set; } = default!;
    public DateTime AppliedAt { get; set; }
    public IReadOnlyList<BalanceAdjustment> Adjustments { get; set; } = Array.Empty<BalanceAdjustment>();
    public PatchImpact Impact { get; set; } = default!;
    public PatchTestResult TestResult { get; set; } = default!;
}

/// <summary>
/// Game balance report.
/// </summary>
public class GameBalanceReport
{
    public string GameId { get; set; } = default!;
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<CharacterBalanceOverview> CharacterOverviews { get; set; } = Array.Empty<CharacterBalanceOverview>();
    public IReadOnlyList<BalanceSuggestion> Suggestions { get; set; } = Array.Empty<BalanceSuggestion>();
    public BalancingRiskAssessment RiskAssessment { get; set; } = default!;
}

// Supporting types

public class CharacterViabilityMetrics
{
    public string CharacterId { get; set; } = default!;
    public double WinRate { get; set; }
    public double PickRate { get; set; }
    public double BanRate { get; set; }
    public int Tier { get; set; }
    public double OverallScore { get; set; }
}

public class MatchupPerformanceAnalysis
{
    public string Character1 { get; set; } = default!;
    public string Character2 { get; set; } = default!;
    public double Character1WinRate { get; set; }
    public int SampleSize { get; set; }
    public bool IsBalanced { get; set; }
}

public class MovePerformanceAnalysis
{
    public string CharacterId { get; set; } = default!;
    public string MoveName { get; set; } = default!;
    public double UsageRate { get; set; }
    public double SuccessRate { get; set; }
    public double DamageEfficiency { get; set; }
    public bool IsProblematic { get; set; }
}

public class MetaAnalysis
{
    public IReadOnlyList<string> DominantStrategies { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> UnderusedElements { get; set; } = Array.Empty<string>();
    public double DiversityScore { get; set; }
}

public class BalanceTrends
{
    public IReadOnlyList<ProblematicElement> ProblematicElements { get; set; } = Array.Empty<ProblematicElement>();
    public IReadOnlyList<RecommendedAction> RecommendedActions { get; set; } = Array.Empty<RecommendedAction>();
}

public class StatAdjustment
{
    public string StatName { get; set; } = default!;
    public double CurrentValue { get; set; }
    public double NewValue { get; set; }
    public double PercentageChange { get; set; }
}

public class MoveAdjustment
{
    public string MoveName { get; set; } = default!;
    public string Property { get; set; } = default!;
    public double CurrentValue { get; set; }
    public double NewValue { get; set; }
}

public class PatchImpact
{
    public double WinRateChange { get; set; }
    public double PickRateChange { get; set; }
    public IReadOnlyList<string> AffectedCharacters { get; set; } = Array.Empty<string>();
}

public class PatchTestResult
{
    public bool Passed { get; set; }
    public IReadOnlyList<string> Issues { get; set; } = Array.Empty<string>();
    public double StabilityScore { get; set; }
}

public class CharacterBalanceOverview
{
    public string CharacterId { get; set; } = default!;
    public string CharacterName { get; set; } = default!;
    public double OverallWinRate { get; set; }
    public int TierPlacement { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Weaknesses { get; set; } = Array.Empty<string>();
}

public class BalanceSuggestion
{
    public string TargetCharacter { get; set; } = default!;
    public string Suggestion { get; set; } = default!;
    public double Priority { get; set; }
    public string Rationale { get; set; } = default!;
}

public class BalancingRiskAssessment
{
    public double OverallRisk { get; set; }
    public IReadOnlyList<string> PotentialIssues { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MitigationStrategies { get; set; } = Array.Empty<string>();
}

public class ProblematicElement
{
    public string ElementId { get; set; } = default!;
    public string ElementType { get; set; } = default!;
    public string Issue { get; set; } = default!;
    public double Severity { get; set; }
}

public class RecommendedAction
{
    public string Action { get; set; } = default!;
    public string Target { get; set; } = default!;
    public double Impact { get; set; }
    public double Effort { get; set; }
}