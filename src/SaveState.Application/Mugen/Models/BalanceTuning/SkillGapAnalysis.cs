namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Analysis of skill gaps between player groups.
/// </summary>
public class SkillGapAnalysis
{
    public float AverageRatingGap { get; set; }
    public float UpsetRate { get; set; }
    public float CloseMatchRate { get; set; }
    public IReadOnlyDictionary<string, int> SkillGroups { get; set; } = default!;
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> MechanicUsageBySkill { get; set; } = default!;
    public IReadOnlyDictionary<string, float> WinRatesBySkill { get; set; } = default!;
}
