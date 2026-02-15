namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// Match data for analysis.
/// </summary>
public class MatchData
{
    public string MatchId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string CharacterUsed { get; set; } = default!;
    public string OpponentCharacter { get; set; } = default!;
    public bool IsWin { get; set; }
    public int RoundsWon { get; set; }
    public int RoundsLost { get; set; }
    public TimeSpan MatchDuration { get; set; }
    public IReadOnlyList<ComboData> CombosExecuted { get; set; } = default!;
    public IReadOnlyList<ComboData> CombosTaken { get; set; } = default!;
    public int BlocksSuccessful { get; set; }
    public int BlocksMissed { get; set; }
    public int SpecialMovesUsed { get; set; }
    public DateTime PlayedAt { get; set; }
}

/// <summary>
/// Combo data for match analysis.
/// </summary>
public class ComboData
{
    public string ComboName { get; set; } = default!;
    public int Damage { get; set; }
    public int Hits { get; set; }
    public bool WasSuccessful { get; set; }
}

/// <summary>
/// Match analysis results.
/// </summary>
public class MatchAnalysis
{
    public string AnalysisId { get; set; } = default!;
    public string MatchId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public double OverallPerformance { get; set; }
    public IReadOnlyList<StrengthArea> Strengths { get; set; } = default!;
    public IReadOnlyList<WeaknessArea> Weaknesses { get; set; } = default!;
    public IReadOnlyList<ImprovementSuggestion> Suggestions { get; set; } = default!;
    public IReadOnlyList<SkillRating> SkillRatings { get; set; } = default!;
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// Identified strength area.
/// </summary>
public class StrengthArea
{
    public string Skill { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double Score { get; set; }
}

/// <summary>
/// Identified weakness area.
/// </summary>
public class WeaknessArea
{
    public string Skill { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double Score { get; set; }
    public string Priority { get; set; } = default!;
}

/// <summary>
/// Improvement suggestion.
/// </summary>
public class ImprovementSuggestion
{
    public string Area { get; set; } = default!;
    public string Suggestion { get; set; } = default!;
    public string RecommendedContent { get; set; } = default!;
    public int Priority { get; set; }
}

/// <summary>
/// Skill rating from match analysis.
/// </summary>
public class SkillRating
{
    public string SkillName { get; set; } = default!;
    public double Rating { get; set; }
    public double MaxRating { get; set; }
    public string Category { get; set; } = default!;
}
