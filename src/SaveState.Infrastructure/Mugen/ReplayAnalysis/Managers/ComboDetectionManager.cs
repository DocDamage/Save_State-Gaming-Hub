using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;

/// <summary>
/// Manager responsible for detecting and analyzing combos from replay events.
/// </summary>
public static class ComboDetectionManager
{
    private const int ComboGapThreshold = 45; // frames (approx 0.75s at 60fps)

    /// <summary>
    /// Detects combos from a list of replay events based on minimum hit and damage thresholds.
    /// Uses a 45-frame gap threshold (0.75s at 60fps) to detect combo sequences.
    /// </summary>
    /// <param name="events">The replay events to analyze.</param>
    /// <param name="minHits">Minimum number of hits required for a combo.</param>
    /// <param name="minDamage">Minimum damage required for a combo.</param>
    /// <returns>A list of detected combos.</returns>
    public static List<DetectedCombo> DetectCombos(List<ReplayEvent> events, int minHits, int minDamage)
    {
        var combos = new List<DetectedCombo>();
        var currentCombo = new List<ReplayEvent>();
        var lastFrame = 0;

        foreach (var evt in events.Where(e => e.Type == ReplayEventType.Hit || e.Type == ReplayEventType.Move))
        {
            if (currentCombo.Count == 0)
            {
                currentCombo.Add(evt);
                lastFrame = evt.Frame ?? 0;
                continue;
            }

            var frame = evt.Frame ?? lastFrame;
            if (frame - lastFrame <= ComboGapThreshold && evt.PlayerIndex == currentCombo[0].PlayerIndex)
            {
                currentCombo.Add(evt);
                lastFrame = frame;
            }
            else
            {
                // End of combo
                if (currentCombo.Count >= minHits)
                {
                    var combo = CreateComboFromEvents(currentCombo);
                    if (combo.TotalDamage >= minDamage)
                    {
                        combos.Add(combo);
                    }
                }
                currentCombo = new List<ReplayEvent> { evt };
                lastFrame = frame;
            }
        }

        // Handle final combo
        if (currentCombo.Count >= minHits)
        {
            var combo = CreateComboFromEvents(currentCombo);
            if (combo.TotalDamage >= minDamage)
            {
                combos.Add(combo);
            }
        }

        return combos;
    }

    /// <summary>
    /// Creates a DetectedCombo from a list of replay events.
    /// </summary>
    /// <param name="events">The events that make up the combo.</param>
    /// <returns>A DetectedCombo with calculated properties.</returns>
    public static DetectedCombo CreateComboFromEvents(List<ReplayEvent> events)
    {
        var firstFrame = events.Min(e => e.Frame ?? 0);
        var lastFrame = events.Max(e => e.Frame ?? 0);
        var totalDamage = events.Sum(e => e.Damage ?? 0);

        var moves = events.Select(e => new ComboMove
        {
            MoveName = e.Move ?? "Unknown",
            Input = e.Command ?? "",
            Frame = e.Frame ?? 0,
            Damage = e.Damage ?? 0,
            IsCounterHit = e.Type == ReplayEventType.Hit
        }).ToList();

        return new DetectedCombo
        {
            Player = events.First().PlayerIndex,
            Character = $"Player{events.First().PlayerIndex}",
            StartFrame = firstFrame,
            EndFrame = lastFrame,
            HitCount = events.Count,
            TotalDamage = totalDamage,
            Moves = moves,
            QualityScore = CalculateComboQuality(events.Count, totalDamage),
            Difficulty = DetermineComboDifficulty(events.Count, moves.Count)
        };
    }

    /// <summary>
    /// Calculates a quality score for a combo based on hit count and damage.
    /// </summary>
    /// <param name="hitCount">Number of hits in the combo.</param>
    /// <param name="damage">Total damage dealt.</param>
    /// <returns>A quality score from 0-100.</returns>
    public static int CalculateComboQuality(int hitCount, int damage)
    {
        var hitScore = Math.Min(hitCount * 5, 40);
        var damageScore = Math.Min(damage / 50, 40);
        var lengthBonus = hitCount >= 10 ? 20 : hitCount >= 5 ? 10 : 0;
        return Math.Min(hitScore + damageScore + lengthBonus, 100);
    }

    /// <summary>
    /// Determines the difficulty of a combo based on hit count and unique moves.
    /// </summary>
    /// <param name="hitCount">Number of hits in the combo.</param>
    /// <param name="uniqueMoves">Number of unique moves used.</param>
    /// <returns>The combo difficulty level.</returns>
    public static ComboDifficulty DetermineComboDifficulty(int hitCount, int uniqueMoves)
    {
        if (hitCount >= 20) return ComboDifficulty.TOD;
        if (hitCount >= 15 || uniqueMoves >= 8) return ComboDifficulty.VeryHard;
        if (hitCount >= 10 || uniqueMoves >= 5) return ComboDifficulty.Hard;
        if (hitCount >= 5) return ComboDifficulty.Medium;
        return ComboDifficulty.Easy;
    }
}
