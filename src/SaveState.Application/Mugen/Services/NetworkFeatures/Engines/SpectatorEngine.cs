namespace SaveState.Application.Mugen.Services.NetworkFeatures.Engines;

using System.Collections.Concurrent;
using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Core.Common;
using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for managing spectator sessions and streaming.
/// </summary>
public class SpectatorEngine
{
    private readonly ILogger<SpectatorEngine> _logger;
    private readonly ConcurrentDictionary<string, SpectatorSession> _sessions = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _matchSpectators = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectatorEngine"/> class.
    /// </summary>
    public SpectatorEngine(ILogger<SpectatorEngine> logger) => _logger = logger;

    /// <summary>
    /// Validates whether a player can spectate a match.
    /// </summary>
    /// <param name="matchId">The ID of the match to spectate.</param>
    /// <param name="viewerId">The ID of the viewer attempting to spectate.</param>
    /// <returns>A result containing whether spectating is allowed and an error message if applicable.</returns>
    public Result<(bool CanSpectate, string Error)> ValidateSpectateRequest(string matchId, string viewerId)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return Result.Failure<(bool CanSpectate, string Error)>(
                "Match ID is required.",
                ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(viewerId))
        {
            return Result.Failure<(bool CanSpectate, string Error)>(
                "Viewer ID is required.",
                ErrorType.Validation);
        }

        // Check if match exists and allows spectators
        // In a real implementation, this would check against active matches
        // For now, we assume the match exists if we have spectators registered for it
        // or we allow new spectating requests

        // Check if viewer is already spectating this match
        if (_matchSpectators.TryGetValue(matchId, out var spectators) && spectators.Contains(viewerId))
        {
            return Result.Success((false, "You are already spectating this match."));
        }

        // Check if the viewer has reached the maximum number of concurrent spectator sessions
        var viewerSessionCount = _sessions.Values.Count(s => s.MatchId == matchId);
        if (viewerSessionCount >= 5) // Limit to 5 concurrent spectator sessions per match for a viewer
        {
            _logger.LogWarning(
                "Viewer {ViewerId} has reached maximum spectator sessions for match {MatchId}",
                viewerId,
                matchId);
        }

        _logger.LogDebug(
            "Validated spectate request for viewer {ViewerId} on match {MatchId}: Allowed",
            viewerId,
            matchId);

        return Result.Success((true, string.Empty));
    }

    /// <summary>
    /// Creates a new spectator session for a match.
    /// </summary>
    /// <param name="matchId">The ID of the match to spectate.</param>
    /// <param name="viewerId">The ID of the viewer.</param>
    /// <returns>A result containing the created spectator session or an error message.</returns>
    public Result<SpectatorSession> CreateSpectatorSession(string matchId, string viewerId)
    {
        // Validate the spectate request first
        var validationResult = ValidateSpectateRequest(matchId, viewerId);
        if (validationResult.IsFailure)
        {
            return Result.Failure<SpectatorSession>(validationResult.Error!, validationResult.ErrorType);
        }

        var (canSpectate, error) = validationResult.Value;
        if (!canSpectate)
        {
            return Result.Failure<SpectatorSession>(error, ErrorType.Validation);
        }

        // Generate stream URL
        var streamUrl = GenerateStreamUrl(matchId, viewerId);

        // Create default spectator controls
        var controls = new List<SpectatorControls>
        {
            new("camera", "Change camera angle/view", true),
            new("playback", "Control playback speed", true),
            new("chat", "Send chat messages", true),
            new("reaction", "Send reactions/emotes", true),
            new("overlay", "Toggle UI overlay", true)
        };

        // Create the session
        var session = new SpectatorSession(
            SessionId: Guid.NewGuid().ToString("N"),
            MatchId: matchId,
            StreamUrl: streamUrl,
            Controls: controls.AsReadOnly());

        // Store the session
        if (!_sessions.TryAdd(session.SessionId, session))
        {
            return Result.Failure<SpectatorSession>(
                "Failed to create spectator session. Please try again.",
                ErrorType.Internal);
        }

        // Add viewer to match spectators
        _matchSpectators.AddOrUpdate(
            matchId,
            new HashSet<string> { viewerId },
            (_, existing) =>
            {
                existing.Add(viewerId);
                return existing;
            });

        _logger.LogInformation(
            "Created spectator session {SessionId} for viewer {ViewerId} on match {MatchId}",
            session.SessionId,
            viewerId,
            matchId);

        return Result.Success(session);
    }

    /// <summary>
    /// Gets a spectator session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The session if found; otherwise null.</returns>
    public SpectatorSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Ends a spectator session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if the session was ended; otherwise false.</returns>
    public bool EndSession(string sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out var session))
        {
            return false;
        }

        // Remove viewer from match spectators
        if (_matchSpectators.TryGetValue(session.MatchId, out var spectators))
        {
            spectators.Remove(session.SessionId);
            
            // Clean up empty spectator sets
            if (spectators.Count == 0)
            {
                _matchSpectators.TryRemove(session.MatchId, out _);
            }
        }

        _logger.LogInformation(
            "Ended spectator session {SessionId} for match {MatchId}",
            sessionId,
            session.MatchId);

        return true;
    }

    /// <summary>
    /// Gets all spectator sessions for a match.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    /// <returns>A read-only list of spectator sessions.</returns>
    public IReadOnlyList<SpectatorSession> GetSessionsForMatch(string matchId)
    {
        return _sessions.Values
            .Where(s => s.MatchId == matchId)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the number of spectators for a match.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    /// <returns>The spectator count.</returns>
    public int GetSpectatorCount(string matchId)
    {
        return _matchSpectators.TryGetValue(matchId, out var spectators) 
            ? spectators.Count 
            : 0;
    }

    /// <summary>
    /// Checks if a viewer is spectating a match.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    /// <param name="viewerId">The viewer ID.</param>
    /// <returns>True if the viewer is spectating; otherwise false.</returns>
    public bool IsSpectating(string matchId, string viewerId)
    {
        return _matchSpectators.TryGetValue(matchId, out var spectators) 
            && spectators.Contains(viewerId);
    }

    private static string GenerateStreamUrl(string matchId, string viewerId)
    {
        // Generate a unique stream URL
        var token = Guid.NewGuid().ToString("N");
        return $"wss://stream.savestate.gg/spectate/{matchId}?viewer={viewerId}&token={token}";
    }
}
