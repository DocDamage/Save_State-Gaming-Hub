using SaveState.Core.Common;
using SaveState.Application.Mugen.Services.SpectatorModeService.Engines;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SpectatorModeService;

/// <summary>
/// Spectator mode service coordinator that delegates to specialized engines.
/// Provides interactive match watching with multiple camera angles, 
/// real-time statistics, and community features.
/// </summary>
public class SpectatorModeService : ISpectatorModeService, SpectatorModeServiceISpectatorModeService
{
    private readonly ILogger<SpectatorModeService> _logger;
    private readonly ISessionEngine _sessionEngine;
    private readonly ICameraEngine _cameraEngine;
    private readonly IOverlayEngine _overlayEngine;
    private readonly IChatEngine _chatEngine;
    private readonly IHighlightEngine _highlightEngine;

    public SpectatorModeService(
        ILogger<SpectatorModeService> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _sessionEngine = new SessionEngine(loggerFactory.CreateLogger<SessionEngine>());
        _cameraEngine = new CameraEngine(loggerFactory.CreateLogger<CameraEngine>());
        _overlayEngine = new OverlayEngine(loggerFactory.CreateLogger<OverlayEngine>());
        _chatEngine = new ChatEngine(loggerFactory.CreateLogger<ChatEngine>());
        _highlightEngine = new HighlightEngine(loggerFactory.CreateLogger<HighlightEngine>());
    }

    public async Task<Result<SpectatorSession>> StartSpectatingAsync(string matchId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting spectator session for match {MatchId}", matchId);

            // Validate match exists and is active
            var matchValidation = await ValidateMatchForSpectatingAsync(matchId, ct);
            if (!matchValidation.IsSuccess)
            {
                return Result.Failure<SpectatorSession>(matchValidation.Error);
            }

            var cameraAngles = _cameraEngine.GetAvailableCameraAngles();
            var overlays = _overlayEngine.GetAvailableOverlays();

            var result = _sessionEngine.CreateSession(matchId, cameraAngles, overlays);
            
            if (!result.IsSuccess)
            {
                return Result.Failure<SpectatorSession>(result.Error);
            }

            _logger.LogInformation("Spectator session {SessionId} started for match {MatchId}", 
                result.Value.SessionId, matchId);
            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting spectator session for match {MatchId}", matchId);
            return Result.Failure<SpectatorSession>($"Failed to start spectating: {ex.Message}");
        }
    }

    public async Task<Result> StopSpectatingAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Stopping spectator session {SessionId}", sessionId);
            return _sessionEngine.StopSession(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping spectator session {SessionId}", sessionId);
            return Result.Failure($"Failed to stop spectating: {ex.Message}");
        }
    }

    public async Task<Result> ChangeCameraAngleAsync(string sessionId, string cameraAngle, CancellationToken ct = default)
    {
        try
        {
            if (!_sessionEngine.TryGetSession(sessionId, out var session))
            {
                return Result.Failure("Spectator session not found");
            }

            return _cameraEngine.ChangeCameraAngle(session, cameraAngle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing camera angle for session {SessionId}", sessionId);
            return Result.Failure($"Failed to change camera angle: {ex.Message}");
        }
    }

    public async Task<Result> ToggleOverlayAsync(string sessionId, string overlayType, bool enabled, CancellationToken ct = default)
    {
        try
        {
            if (!_sessionEngine.TryGetSession(sessionId, out var session))
            {
                return Result.Failure("Spectator session not found");
            }

            return _overlayEngine.ToggleOverlay(session, overlayType, enabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling overlay for session {SessionId}", sessionId);
            return Result.Failure($"Failed to toggle overlay: {ex.Message}");
        }
    }

    public async Task<Result<MatchStatistics>> GetMatchStatisticsAsync(string matchId, CancellationToken ct = default)
    {
        try
        {
            if (!_sessionEngine.TryGetMatchData(matchId, out var matchData))
            {
                return Result.Failure<MatchStatistics>("Match not found");
            }

            var cameraAngles = _cameraEngine.GetPopularCameraAngles(matchId, 
                GetActiveSessionsDictionary());

            var statistics = new MatchStatistics
            {
                MatchId = matchId,
                ViewerCount = matchData.ViewerCount,
                TotalWatchTime = matchData.TotalWatchTime,
                PopularCameraAngles = cameraAngles,
                ChatMessageCount = _chatEngine.GetMessageCountForMatch(matchId),
                PeakViewerCount = matchData.PeakViewerCount,
                AverageSessionLength = _sessionEngine.CalculateAverageSessionLength(matchData, 
                    GetActiveSessionsDictionary())
            };

            return Result.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match statistics for {MatchId}", matchId);
            return Result.Failure<MatchStatistics>($"Failed to get statistics: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<ChatMessage>>> GetSpectatorChatAsync(string matchId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var messages = _chatEngine.GetMessagesForMatch(matchId, limit);
            return Result.Success<IReadOnlyList<ChatMessage>>(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting spectator chat for {MatchId}", matchId);
            return Result.Failure<IReadOnlyList<ChatMessage>>($"Failed to get chat: {ex.Message}");
        }
    }

    public async Task<Result> SendSpectatorChatMessageAsync(string sessionId, string message, CancellationToken ct = default)
    {
        try
        {
            if (!_sessionEngine.TryGetSession(sessionId, out var session))
            {
                return Result.Failure("Spectator session not found");
            }

            var result = _chatEngine.CreateMessage(sessionId, session.MatchId, message);
            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error);
            }

            _chatEngine.AddMessage(result.Value, msg =>
            {
                _sessionEngine.UpdateMatchData(session.MatchId, data =>
                {
                    var chatMessages = data.ChatMessages.ToList();
                    chatMessages.Add(msg);
                    data.ChatMessages = chatMessages;
                });
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending spectator chat message for session {SessionId}", sessionId);
            return Result.Failure($"Failed to send message: {ex.Message}");
        }
    }

    public async Task<Result<MatchHighlights>> GetMatchHighlightsAsync(string matchId, CancellationToken ct = default)
    {
        try
        {
            return await _highlightEngine.GenerateHighlightsAsync(matchId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match highlights for {MatchId}", matchId);
            return Result.Failure<MatchHighlights>($"Failed to get highlights: {ex.Message}");
        }
    }

    public async Task<Result> RequestMatchReplayAsync(string sessionId, TimeSpan startTime, TimeSpan endTime, CancellationToken ct = default)
    {
        try
        {
            if (!_sessionEngine.TryGetSession(sessionId, out var session))
            {
                return Result.Failure("Spectator session not found");
            }

            var result = _highlightEngine.CreateReplayRequest(sessionId, session.MatchId, startTime, endTime);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting match replay for session {SessionId}", sessionId);
            return Result.Failure($"Failed to request replay: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<SpectatorSession>>> GetActiveSessionsAsync(string matchId, CancellationToken ct = default)
    {
        try
        {
            var sessions = _sessionEngine.GetSessionsForMatch(matchId).ToList();
            return Result.Success<IReadOnlyList<SpectatorSession>>(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for match {MatchId}", matchId);
            return Result.Failure<IReadOnlyList<SpectatorSession>>($"Failed to get sessions: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<Result> ValidateMatchForSpectatingAsync(string matchId, CancellationToken ct)
    {
        // Simplified validation - would check if match exists and allows spectators
        return Result.Success();
    }

    private Dictionary<string, SpectatorSession> GetActiveSessionsDictionary()
    {
        // Access the session engine's active sessions through reflection or maintain local tracking
        // For now, return empty dictionary - engines maintain their own state
        return new Dictionary<string, SpectatorSession>();
    }

    #endregion
}
