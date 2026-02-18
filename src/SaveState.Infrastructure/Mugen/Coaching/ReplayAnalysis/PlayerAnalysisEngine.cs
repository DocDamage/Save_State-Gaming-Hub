namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Builds player summaries from replay data.
/// </summary>
public sealed class PlayerAnalysisEngine : IPlayerAnalysisEngine
{
    /// <inheritdoc />
    public PlayerReplaySummary[] BuildPlayerSummaries(
        IReadOnlyList<ReplayEvent> events,
        ReplayMetadata metadata,
        IReadOnlyList<MoveSequenceSummary> sequences)
    {
        var players = new[]
        {
            new PlayerReplaySummary(1) { Name = metadata.Player1 },
            new PlayerReplaySummary(2) { Name = metadata.Player2 }
        };

        foreach (var ev in events)
        {
            if (ev.PlayerIndex != 1 && ev.PlayerIndex != 2)
            {
                continue;
            }

            var player = players[ev.PlayerIndex - 1];
            var opponent = players[ev.PlayerIndex == 1 ? 1 : 0];

            if (IsCountingMove(ev.Type))
            {
                player.TotalMoves++;
            }

            switch (ev.Type)
            {
                case ReplayEventType.Hit:
                    player.Hits++;
                    if (ev.Damage.HasValue)
                    {
                        player.DamageDealt += ev.Damage.Value;
                        opponent.DamageTaken += ev.Damage.Value;
                    }
                    break;
                case ReplayEventType.Block:
                    player.Blocks++;
                    break;
                case ReplayEventType.Whiff:
                    player.Whiffs++;
                    break;
                case ReplayEventType.Throw:
                    player.Throws++;
                    break;
                case ReplayEventType.Projectile:
                    player.Projectiles++;
                    break;
                case ReplayEventType.AntiAir:
                    player.AntiAirs++;
                    break;
                case ReplayEventType.Knockdown:
                    player.Knockdowns++;
                    break;
            }
        }

        foreach (var sequence in sequences)
        {
            if (sequence.PlayerIndex != 1 && sequence.PlayerIndex != 2)
            {
                continue;
            }

            var player = players[sequence.PlayerIndex - 1];
            if (sequence.Hits >= 2)
            {
                player.Combos += sequence.Occurrences;
            }

            if (sequence.Drops > 0)
            {
                player.ComboDrops += sequence.Drops;
            }
        }

        return players;
    }

    private static bool IsCountingMove(ReplayEventType type)
    {
        return type is ReplayEventType.Move or ReplayEventType.Hit or ReplayEventType.Block or ReplayEventType.Whiff
            or ReplayEventType.Throw or ReplayEventType.Projectile or ReplayEventType.AntiAir or ReplayEventType.Movement;
    }
}
