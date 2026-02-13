using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Spectator mode service interface for interactive match watching
/// with multiple camera angles, real-time statistics, and community features.
/// </summary>
public interface ISpectatorModeService
{
    Task<Result<SpectatorSession>> StartSpectatingAsync(string matchId, CancellationToken ct = default);
    Task<Result> StopSpectatingAsync(string sessionId, CancellationToken ct = default);
    Task<Result> ChangeCameraAngleAsync(string sessionId, string cameraAngle, CancellationToken ct = default);
    Task<Result> ToggleOverlayAsync(string sessionId, string overlayType, bool enabled, CancellationToken ct = default);
    Task<Result<MatchStatistics>> GetMatchStatisticsAsync(string matchId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ChatMessage>>> GetSpectatorChatAsync(string matchId, int limit = 50, CancellationToken ct = default);
    Task<Result> SendSpectatorChatMessageAsync(string sessionId, string message, CancellationToken ct = default);
    Task<Result<MatchHighlights>> GetMatchHighlightsAsync(string matchId, CancellationToken ct = default);
    Task<Result> RequestMatchReplayAsync(string sessionId, TimeSpan startTime, TimeSpan endTime, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SpectatorSession>>> GetActiveSessionsAsync(string matchId, CancellationToken ct = default);
}
