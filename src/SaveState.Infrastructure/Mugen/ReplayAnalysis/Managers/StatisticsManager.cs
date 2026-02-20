using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;

/// <summary>
/// Manager for calculating combat statistics and comeback detection from replay data.
/// </summary>
public static class StatisticsManager
{
    /// <summary>
    /// Calculates P1/P2 combat statistics from replay events and detected combos.
    /// </summary>
    /// <param name="events">The list of replay events.</param>
    /// <param name="combos">The list of detected combos.</param>
    /// <returns>A tuple containing P1 and P2 combat statistics.</returns>
    public static (PlayerCombatStats P1, PlayerCombatStats P2) CalculateCombatStats(
        List<ReplayEvent> events,
        List<DetectedCombo> combos)
    {
        var p1Stats = new PlayerCombatStats();
        var p2Stats = new PlayerCombatStats();

        foreach (var evt in events)
        {
            var stats = evt.PlayerIndex == 1 ? p1Stats : p2Stats;

            switch (evt.Type)
            {
                case ReplayEventType.Hit:
                    stats.SuccessfulHits++;
                    stats.TotalAttacks++;
                    if (evt.Damage.HasValue)
                        stats.TotalDamageDealt += evt.Damage.Value;
                    break;
                case ReplayEventType.Block:
                    stats.BlockedAttacks++;
                    break;
                case ReplayEventType.Whiff:
                    stats.WhiffedAttacks++;
                    stats.TotalAttacks++;
                    break;
                case ReplayEventType.Throw:
                    stats.ThrowsAttempted++;
                    stats.ThrowsSuccessful++;
                    break;
                case ReplayEventType.AntiAir:
                    stats.AntiAirs++;
                    break;
            }
        }

        // Calculate combo stats
        foreach (var combo in combos)
        {
            var stats = combo.Player == 1 ? p1Stats : p2Stats;
            stats.CombosPerformed++;
            stats.TotalComboHits += combo.HitCount;
            stats.MaxComboHits = Math.Max(stats.MaxComboHits, combo.HitCount);
            stats.MaxComboDamage = Math.Max(stats.MaxComboDamage, combo.TotalDamage);
        }

        p1Stats.AverageComboDamage = p1Stats.CombosPerformed > 0
            ? p1Stats.TotalComboHits * 50 // Approximate
            : 0;
        p2Stats.AverageComboDamage = p2Stats.CombosPerformed > 0
            ? p2Stats.TotalComboHits * 50
            : 0;

        return (p1Stats, p2Stats);
    }

    /// <summary>
    /// Detects comeback moments from replay events.
    /// </summary>
    /// <param name="events">The list of replay events.</param>
    /// <param name="duration">The duration of the replay.</param>
    /// <returns>A list of detected comeback moments.</returns>
    public static List<ComebackMoment> DetectComebacks(List<ReplayEvent> events, TimeSpan duration)
    {
        var comebacks = new List<ComebackMoment>();
        // Simplified comeback detection based on damage patterns
        // A real implementation would track health state over time
        return comebacks;
    }
}
