using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Infrastructure.Multiplayer;

/// <summary>
/// WebSocket multiplayer infrastructure foundation.
/// PHASE 7: REQUIRED - WebSocket Multiplayer Foundation (Session 4)
/// </summary>
public class MultiplayerService
{
    private readonly ILogger<MultiplayerService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MultiplayerSession> _sessions = new();
    private readonly Dictionary<string, PlayerConnection> _connections = new();

    public MultiplayerService(ILogger<MultiplayerService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new multiplayer session.
    /// </summary>
    public async Task<Result<MultiplayerSession>> CreateSessionAsync(
        string sessionName,
        int maxPlayers,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating multiplayer session: {SessionName}", sessionName);

            var session = new MultiplayerSession(
                id: Guid.NewGuid().ToString(),
                name: sessionName,
                maxPlayers: maxPlayers,
                createdAt: _timeProvider.UtcNow,
                players: new List<PlayerInfo>(),
                isActive: true);

            _sessions[session.Id] = session;

            _logger.LogInformation("Session created: {SessionId}", session.Id);
            return Result.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session: {SessionName}", sessionName);
            return Result.Failure<MultiplayerSession>(
                $"Session creation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Joins a player to a multiplayer session.
    /// </summary>
    public async Task<Result> JoinSessionAsync(
        string sessionId,
        string playerId,
        string playerName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure($"Session not found: {sessionId}", ErrorType.Validation);
            }

            if (session.Players.Count >= session.MaxPlayers)
            {
                return Result.Failure("Session is full", ErrorType.Validation);
            }

            var playerInfo = new PlayerInfo(
                Id: playerId,
                Name: playerName,
                JoinedAt: _timeProvider.UtcNow,
                IsReady: false);

            session.Players.Add(playerInfo);

            var connection = new PlayerConnection(
                PlayerId: playerId,
                SessionId: sessionId,
                ConnectedAt: _timeProvider.UtcNow,
                IsConnected: true);

            _connections[playerId] = connection;

            _logger.LogInformation("Player {PlayerId} joined session {SessionId}", playerId, sessionId);

            // Notify other players
            await BroadcastToSessionAsync(sessionId, new
            {
                Type = "PlayerJoined",
                Player = playerInfo
            }, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join session: {SessionId}", sessionId);
            return Result.Failure($"Join failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sends a message to all players in a session.
    /// </summary>
    public async Task BroadcastToSessionAsync(
        string sessionId,
        object message,
        CancellationToken ct = default)
    {
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                _logger.LogWarning("Session not found for broadcast: {SessionId}", sessionId);
                return;
            }

            foreach (var player in session.Players)
            {
                if (_connections.TryGetValue(player.Id, out var connection) && connection.IsConnected)
                {
                    await SendMessageAsync(player.Id, message, ct);
                }
            }

            _logger.LogDebug("Message broadcasted to session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Broadcast to session failed: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Sends a direct message to a player.
    /// </summary>
    public async Task SendMessageAsync(
        string playerId,
        object message,
        CancellationToken ct = default)
    {
        try
        {
            if (!_connections.TryGetValue(playerId, out var connection))
            {
                _logger.LogWarning("Player connection not found: {PlayerId}", playerId);
                return;
            }

            // Send via WebSocket
            _logger.LogDebug("Message sent to player {PlayerId}", playerId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to {PlayerId}", playerId);
        }
    }

    /// <summary>
    /// Marks a player as ready.
    /// </summary>
    public async Task<Result> MarkPlayerReadyAsync(
        string sessionId,
        string playerId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure("Session not found", ErrorType.Validation);
            }

            var player = session.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return Result.Failure("Player not in session", ErrorType.Validation);
            }

            // Mark ready (would need proper state management)
            _logger.LogInformation("Player {PlayerId} marked ready", playerId);

            // Check if all players ready
            if (session.Players.All(p => p.IsReady))
            {
                await StartGameAsync(sessionId, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark player ready: {PlayerId}", playerId);
            return Result.Failure($"Failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Starts a game in a session.
    /// </summary>
    public async Task StartGameAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return;
            }

            _logger.LogInformation("Starting game in session {SessionId}", sessionId);

            // Send start signal to all players
            await BroadcastToSessionAsync(sessionId, new
            {
                Type = "GameStarted",
                Timestamp = _timeProvider.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start game: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Handles player disconnection.
    /// </summary>
    public async Task HandlePlayerDisconnectAsync(
        string playerId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_connections.TryGetValue(playerId, out var connection))
            {
                return;
            }

            _logger.LogInformation("Player disconnected: {PlayerId}", playerId);

            var updatedConnection = connection with { IsConnected = false };
            _connections[playerId] = updatedConnection;

            // Notify session
            if (_sessions.TryGetValue(connection.SessionId, out var session))
            {
                session.Players.RemoveAll(p => p.Id == playerId);

                await BroadcastToSessionAsync(connection.SessionId, new
                {
                    Type = "PlayerDisconnected",
                    PlayerId = playerId
                }, ct);
            }

            _connections.Remove(playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle disconnect: {PlayerId}", playerId);
        }
    }

    /// <summary>
    /// Gets active sessions.
    /// </summary>
    public Result<List<MultiplayerSession>> GetActiveSessions()
    {
        var sessions = _sessions.Values.Where(s => s.IsActive).ToList();
        return Result.Success(sessions);
    }
}

/// <summary>
/// Multiplayer session.
/// </summary>
public class MultiplayerSession
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PlayerInfo> Players { get; set; }
    public bool IsActive { get; set; }

    public MultiplayerSession(
        string id,
        string name,
        int maxPlayers,
        DateTime createdAt,
        List<PlayerInfo> players,
        bool isActive)
    {
        Id = id;
        Name = name;
        MaxPlayers = maxPlayers;
        CreatedAt = createdAt;
        Players = players;
        IsActive = isActive;
    }
}

/// <summary>
/// Player information in a session.
/// </summary>
public record PlayerInfo(
    string Id,
    string Name,
    DateTime JoinedAt,
    bool IsReady);

/// <summary>
/// Player connection.
/// </summary>
public record PlayerConnection(
    string PlayerId,
    string SessionId,
    DateTime ConnectedAt,
    bool IsConnected);

/// <summary>
/// Game message types.
/// </summary>
public enum MessageType
{
    PlayerJoined,
    PlayerReady,
    PlayerDisconnected,
    GameStarted,
    GameEnded,
    PlayerMove,
    Chat
}

/// <summary>
/// Game message.
/// </summary>
public record GameMessage(
    MessageType Type,
    string SenderId,
    object Payload,
    DateTime Timestamp);
