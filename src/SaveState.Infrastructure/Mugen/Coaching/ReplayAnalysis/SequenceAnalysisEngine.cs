using SaveState.Core.Common;

namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Builds and analyzes move sequences from replay events.
/// </summary>
public sealed class SequenceAnalysisEngine : ISequenceAnalysisEngine
{
    /// <inheritdoc />
    public IReadOnlyList<MoveSequenceSummary> BuildSequences(IReadOnlyList<ReplayEvent> events)
    {
        var ordered = events
            .Select((ev, index) => new { Event = ev, Index = index })
            .OrderBy(item => item.Event.TimeSeconds ?? (item.Event.Frame.HasValue ? item.Event.Frame.Value / 60d : item.Index))
            .ThenBy(item => item.Index)
            .Select(item => item.Event)
            .ToList();

        var rawSequences = new List<MoveSequenceSummary>();

        foreach (var group in ordered.GroupBy(ev => ev.PlayerIndex).Where(group => group.Key > 0))
        {
            SequenceBuilder? builder = null;
            ReplayEvent? last = null;

            foreach (var ev in group)
            {
                if (builder is null)
                {
                    builder = new SequenceBuilder(ev.PlayerIndex);
                }

                if (last is not null && IsSequenceBreak(last, ev))
                {
                    if (builder.HasContent)
                    {
                        rawSequences.Add(builder.Build());
                    }

                    builder = new SequenceBuilder(ev.PlayerIndex);
                }

                builder.Add(ev);
                last = ev;
            }

            if (builder?.HasContent == true)
            {
                rawSequences.Add(builder.Build());
            }
        }

        var grouped = rawSequences
            .Where(sequence => sequence.Moves.Count > 0)
            .GroupBy(sequence => (sequence.PlayerIndex, Key: string.Join(" -> ", sequence.Moves)))
            .Select(group => new MoveSequenceSummary(
                group.Key.PlayerIndex,
                group.First().Moves,
                group.Sum(item => item.Hits),
                group.Sum(item => item.Damage),
                group.Count(),
                group.Sum(item => item.Drops)))
            .ToList();

        return grouped;
    }

    /// <inheritdoc />
    public Result<MoveSequenceSummary> FindMostCommonTransition(IReadOnlyList<ReplayEvent> events, int playerIndex)
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

    private static bool IsSequenceBreak(ReplayEvent last, ReplayEvent current)
    {
        if (last.TimeSeconds.HasValue && current.TimeSeconds.HasValue)
        {
            if (current.TimeSeconds.Value - last.TimeSeconds.Value > 2.5d)
            {
                return true;
            }
        }

        if (last.Frame.HasValue && current.Frame.HasValue)
        {
            if (current.Frame.Value - last.Frame.Value > 120)
            {
                return true;
            }
        }

        return false;
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

    private sealed class SequenceBuilder
    {
        private readonly List<string> _moves = new();
        private int _hits;
        private int _damage;
        private int _drops;

        public int PlayerIndex { get; }
        public bool HasContent => _moves.Count > 0;

        public SequenceBuilder(int playerIndex)
        {
            PlayerIndex = playerIndex;
        }

        public void Add(ReplayEvent ev)
        {
            var moveName = CleanMoveName(ev.Move) ?? CleanMoveName(ev.Command);

            if (!string.IsNullOrWhiteSpace(moveName) &&
                (_moves.Count == 0 || !string.Equals(_moves.Last(), moveName, StringComparison.OrdinalIgnoreCase)))
            {
                _moves.Add(moveName);
            }

            switch (ev.Type)
            {
                case ReplayEventType.Hit:
                    _hits++;
                    if (ev.Damage.HasValue)
                    {
                        _damage += ev.Damage.Value;
                    }
                    break;

                case ReplayEventType.Whiff when _moves.Count > 1:
                    _drops++;
                    break;
            }
        }

        public MoveSequenceSummary Build()
        {
            return new MoveSequenceSummary(PlayerIndex, _moves.ToList(), _hits, _damage, 1, _drops);
        }
    }
}
