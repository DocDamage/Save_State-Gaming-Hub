using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Interface for generating match highlights and managing replays.
/// </summary>
public interface IHighlightEngine
{
    /// <summary>
    /// Generates highlights for a match.
    /// </summary>
    Task<Result<MatchHighlights>> GenerateHighlightsAsync(string matchId, CancellationToken ct = default);

    /// <summary>
    /// Creates a replay request for a specific time range.
    /// </summary>
    Result<ReplayRequest> CreateReplayRequest(string sessionId, string matchId, TimeSpan startTime, TimeSpan endTime);
}
