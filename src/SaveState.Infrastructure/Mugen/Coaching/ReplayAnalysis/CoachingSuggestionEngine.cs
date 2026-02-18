using System.Text;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Generates coaching suggestions from replay analysis.
/// </summary>
public sealed class CoachingSuggestionEngine : ICoachingSuggestionEngine
{
    /// <inheritdoc />
    public List<string> BuildCoachingSuggestions(ReplayAnalysisResult analysis)
    {
        var suggestions = new List<string>();
        var player = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 1);
        var opponent = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 2);

        if (player is null)
        {
            return suggestions;
        }

        if (player.TotalMoves > 0)
        {
            if (player.HitRate < 0.25f)
            {
                suggestions.Add("Your move success rate is low; tighten spacing and confirm before committing.");
            }

            if (player.WhiffRate > 0.35f)
            {
                suggestions.Add("High whiff rate detected; use faster pokes and check ranges before big swings.");
            }

            if (player.DamageTaken > player.DamageDealt * 1.2f && player.Blocks < Math.Max(3, player.Hits / 2))
            {
                suggestions.Add("You are taking more damage than you deal; slow the pace, block more, and punish unsafe strings.");
            }

            if (player.Throws == 0 && player.Blocks >= 6)
            {
                suggestions.Add("Opponent is blocking often; add throws to open them up.");
            }

            if (player.Projectiles >= 4 && player.HitRate < 0.2f)
            {
                suggestions.Add("Projectiles are getting avoided; vary timing and advance behind them.");
            }
        }

        if (opponent is not null)
        {
            var opponentAirHits = analysis.Events.Count(ev =>
                ev.PlayerIndex == opponent.PlayerIndex &&
                (ev.Type == ReplayEventType.Movement || ev.Type == ReplayEventType.AntiAir || IsAirMove(ev.Move)));

            if (opponentAirHits >= 3 && player.AntiAirs < 2)
            {
                suggestions.Add("Opponent landed several jump-ins; prioritize anti-airs and pre-emptive spacing.");
            }
        }

        var bestSequence = analysis.Sequences
            .Where(sequence => sequence.PlayerIndex == 1 && sequence.Moves.Count >= 2)
            .OrderByDescending(sequence => sequence.Damage)
            .ThenByDescending(sequence => sequence.Hits)
            .FirstOrDefault();

        bestSequence ??= FindMostCommonTransition(analysis.Events, 1).Value;

        if (bestSequence is not null && bestSequence.Moves.Count >= 2)
        {
            suggestions.Add(FormatSequenceSuggestion("Better sequence", bestSequence));
        }

        var dropSequence = analysis.Sequences
            .Where(sequence => sequence.PlayerIndex == 1 && sequence.Drops > 0 && sequence.Moves.Count >= 2)
            .OrderByDescending(sequence => sequence.Drops)
            .FirstOrDefault();

        if (dropSequence is not null)
        {
            suggestions.Add($"Sequence cleanup: {string.Join(" -> ", dropSequence.Moves)} - tighten timing to avoid drops.");
        }

        if (!suggestions.Any())
        {
            suggestions.Add("Look for safer confirms into short combos, then reset to neutral.");
        }

        return suggestions;
    }

    /// <inheritdoc />
    public string BuildCoachPrompt(ReplayAnalysisResult analysis)
    {
        var player = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 1);
        var opponent = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 2);

        if (player is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("You are a fighting game coach. Provide 4-6 short bullet tips.");
        sb.AppendLine("Focus on improving move sequences, hit confirms, spacing, and defense.");
        sb.AppendLine();
        sb.AppendLine($"Player1: {player.Name ?? "Player1"}");
        sb.AppendLine($"Damage dealt: {player.DamageDealt}, damage taken: {player.DamageTaken}");
        sb.AppendLine($"Hits: {player.Hits}, blocks: {player.Blocks}, whiffs: {player.Whiffs}");
        sb.AppendLine($"Throws: {player.Throws}, projectiles: {player.Projectiles}, anti-airs: {player.AntiAirs}");

        if (opponent is not null)
        {
            sb.AppendLine($"Opponent: {opponent.Name ?? "Player2"}");
            sb.AppendLine($"Opponent damage dealt: {opponent.DamageDealt}, damage taken: {opponent.DamageTaken}");
        }

        if (!string.IsNullOrWhiteSpace(analysis.Metadata.Winner))
        {
            sb.AppendLine($"Winner: {analysis.Metadata.Winner}");
        }

        var topSequences = analysis.Sequences
            .Where(sequence => sequence.PlayerIndex == 1 && sequence.Moves.Count >= 2)
            .OrderByDescending(sequence => sequence.Damage)
            .ThenByDescending(sequence => sequence.Hits)
            .Take(3)
            .ToList();

        if (topSequences.Count > 0)
        {
            sb.AppendLine("Top sequences used by Player1:");
            foreach (var sequence in topSequences)
            {
                sb.AppendLine($"- {string.Join(" -> ", sequence.Moves)} (hits {sequence.Hits}, dmg {sequence.Damage}, uses {sequence.Occurrences})");
            }
        }

        sb.AppendLine("Return only bullet points.");
        return sb.ToString();
    }

    private static Result<MoveSequenceSummary> FindMostCommonTransition(IReadOnlyList<ReplayEvent> events, int playerIndex)
    {
        var moves = events
            .Where(ev => ev.PlayerIndex == playerIndex)
            .Select(ev => CleanMoveName(ev.Move) ?? CleanMoveName(ev.Command))
            .Where(move => !string.IsNullOrWhiteSpace(move))
            .ToList();

        if (moves.Count < 2)
        {
            return Result.Failure<MoveSequenceSummary>("Not enough moves to find transitions", ErrorType.Validation);
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < moves.Count - 1; i++)
        {
            var current = moves[i]!;
            var next = moves[i + 1]!;
            if (string.Equals(current, next, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var key = $"{current} -> {next}";
            counts[key] = counts.TryGetValue(key, out var value) ? value + 1 : 1;
        }

        var best = counts.OrderByDescending(pair => pair.Value).FirstOrDefault();
        if (best.Value < 2)
        {
            return Result.Failure<MoveSequenceSummary>("No common transition found (minimum 2 occurrences required)", ErrorType.NotFound);
        }

        var parts = best.Key.Split(" -> ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return Result.Failure<MoveSequenceSummary>("Invalid transition format", ErrorType.Validation);
        }

        return Result.Success(new MoveSequenceSummary(playerIndex, parts.ToList(), 0, 0, best.Value, 0));
    }

    private static string? CleanMoveName(string? move)
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            return null;
        }

        var trimmed = move.Trim();
        if (trimmed.Length > 64)
        {
            trimmed = trimmed.Substring(0, 64);
        }

        return trimmed;
    }

    private static bool IsAirMove(string? move)
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            return false;
        }

        var lower = move.ToLowerInvariant();
        return lower.Contains("j.") || lower.Contains("jump") || lower.Contains("air");
    }

    private static string FormatSequenceSuggestion(string label, MoveSequenceSummary sequence)
    {
        var moveText = string.Join(" -> ", sequence.Moves);
        var detail = sequence.Damage > 0 ? $" (approx {sequence.Damage} dmg)" : string.Empty;
        return $"{label}: {moveText}{detail}";
    }
}
