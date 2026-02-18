namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Builds player summaries from replay data.
/// </summary>
public interface IPlayerAnalysisEngine
{
    /// <summary>
    /// Builds player summaries from replay events.
    /// </summary>
    PlayerReplaySummary[] BuildPlayerSummaries(
        IReadOnlyList<ReplayEvent> events,
        ReplayMetadata metadata,
        IReadOnlyList<MoveSequenceSummary> sequences);
}
