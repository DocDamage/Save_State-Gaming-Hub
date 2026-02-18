using SaveState.Core.Common;

namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Builds and analyzes move sequences from replay events.
/// </summary>
public interface ISequenceAnalysisEngine
{
    /// <summary>
    /// Builds move sequences from replay events.
    /// </summary>
    IReadOnlyList<MoveSequenceSummary> BuildSequences(IReadOnlyList<ReplayEvent> events);

    /// <summary>
    /// Finds the most common transition between moves.
    /// </summary>
    Result<MoveSequenceSummary> FindMostCommonTransition(IReadOnlyList<ReplayEvent> events, int playerIndex);
}
