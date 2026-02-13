namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Analysis of skill gaps between player groups.
/// </summary>
public class SkillGapAnalysis
{
    public IReadOnlyDictionary<string, int> SkillGroups { get; set; } = default!;
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> MechanicUsageBySkill { get; set; } = default!;
    public IReadOnlyDictionary<string, float> WinRatesBySkill { get; set; } = default!;
}
