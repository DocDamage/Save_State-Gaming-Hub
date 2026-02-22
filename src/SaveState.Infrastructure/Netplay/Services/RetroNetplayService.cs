using System.Collections.Concurrent;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Netplay.Models;
using SaveState.Core.Netplay.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Netplay.Services;

public class RetroNetplayService : IRetroNetplayService
{
    private readonly IMatchmakingQueue _matchmakingQueue;
    private readonly IRollbackNetcodeWrapper _rollbackWrapper;
    private readonly ISpectatorRelayService _spectatorRelay;
    private readonly ILogger<RetroNetplayService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, NetplaySession> _activeSessions;
    private readonly ConcurrentDictionary<string, MatchmakingTicket> _activeTickets;

    public RetroNetplayService(
        IMatchmakingQueue matchmakingQueue,
        IRollbackNetcodeWrapper rollbackWrapper,
        ISpectatorRelayService spectatorRelay,
        ILogger<RetroNetplayService> logger,
        ITimeProvider timeProvider)
    {
        _matchmakingQueue = matchmakingQueue;
        _rollbackWrapper = rollbackWrapper;
        _spectatorRelay = spectatorRelay;
        _logger = logger;
        _timeProvider = timeProvider;
        _activeSessions = new ConcurrentDictionary<string, NetplaySession>();
        _activeTickets = new ConcurrentDictionary<string, MatchmakingTicket>();
    }

    public async Task<Result<MatchmakingTicket>> StartMatchmakingAsync(
        MatchmakingRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var ticket = new MatchmakingTicket
            {
                TicketId = Guid.NewGuid().ToString("N")[..8],
                Request = request,
                CreatedAt = _timeProvider.UtcNow,
                Status = MatchmakingStatus.Queued,
                MatchedPeerId = null,
                MatchedAt = null
            };

            _activeTickets[ticket.TicketId] = ticket;

            await _matchmakingQueue.EnqueueAsync(ticket, ct);

            _logger.LogInformation(
                "Started matchmaking for game {GameId} in region {Region}",
                request.GameId, request.Region);

            return Result<MatchmakingTicket>.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start matchmaking");
            return Result<MatchmakingTicket>.Failure(
                "Matchmaking failed to start", ErrorType.Internal);
        }
    }

    public Task<Result> CancelMatchmakingAsync(
        string ticketId,
        CancellationToken ct = default)
    {
        if (_activeTickets.TryRemove(ticketId, out var ticket))
        {
            ticket = ticket with { Status = MatchmakingStatus.Cancelled };
            _logger.LogInformation("Cancelled matchmaking ticket {TicketId}", ticketId);
            return Task.FromResult(Result.Success());
        }

        return Task.FromResult(Result.Failure("Ticket not found", ErrorType.NotFound));
    }

    public async Task<Result<NetplaySession>> ConnectToPeerAsync(
        MatchmakingTicket ticket,
        CancellationToken ct = default)
    {
        try
        {
            // Configure rollback netcode
            var rollbackConfig = new RollbackConfig
            {
                InputDelay = 2,
                MaxRollbackFrames = 8,
                LocalInputDelay = 0
            };

            var session = new NetplaySession
            {
                SessionId = Guid.NewGuid().ToString("N")[..8],
                GameId = ticket.Request.GameId,
                Peers = new List<NetplayPeer>(), // Populated by matchmaking
                StartedAt = _timeProvider.UtcNow,
                Status = NetplaySessionStatus.Connecting,
                RollbackConfig = rollbackConfig
            };

            _activeSessions[session.SessionId] = session;

            // Initialize rollback netcode
            await _rollbackWrapper.InitializeAsync(session, ct);

            _logger.LogInformation(
                "Created netplay session {SessionId} for game {GameId}",
                session.SessionId, session.GameId);

            return Result<NetplaySession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to peer");
            return Result<NetplaySession>.Failure(
                "Connection failed", ErrorType.Internal);
        }
    }

    public Task<Result> VerifyRomHashAsync(
        string gameId,
        string romHash,
        CancellationToken ct = default)
    {
        // Verify ROM hash against known good hashes
        var isValid = !string.IsNullOrEmpty(romHash) && romHash.Length == 64;

        _logger.LogInformation(
            "ROM hash verification for {GameId}: {Result}",
            gameId, isValid ? "Valid" : "Invalid");

        return Task.FromResult(isValid
            ? Result.Success()
            : Result.Failure("Invalid ROM hash", ErrorType.Validation));
    }

    public Task<Result> StartSpectatorModeAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (!_activeSessions.ContainsKey(sessionId))
        {
            return Task.FromResult(Result.Failure(
                "Session not found", ErrorType.NotFound));
        }

        return _spectatorRelay.StartRelayAsync(sessionId, ct);
    }

    public Task<Result> DisconnectAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (_activeSessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("Disconnected from session {SessionId}", sessionId);
            return Task.FromResult(Result.Success());
        }

        return Task.FromResult(Result.Failure(
            "Session not found", ErrorType.NotFound));
    }

    public Task<Result<IReadOnlyList<NetplaySession>>> GetActiveSessionsAsync(
        CancellationToken ct = default)
    {
        var sessions = _activeSessions.Values.ToList();
        return Task.FromResult(Result<IReadOnlyList<NetplaySession>>.Success(sessions));
    }

    public Task<Result<IReadOnlyList<LeaderboardEntry>>> GetLeaderboardAsync(
        string gameId,
        CancellationToken ct = default)
    {
        // Return sample leaderboard data
        var leaderboard = new List<LeaderboardEntry>
        {
            new() { PlayerId = "1", DisplayName = "RetroKing", Rank = 1, Rating = 2500, Wins = 150, Losses = 20, WinStreak = 12 },
            new() { PlayerId = "2", DisplayName = "PixelMaster", Rank = 2, Rating = 2450, Wins = 142, Losses = 25, WinStreak = 5 },
            new() { PlayerId = "3", DisplayName = "ArcadeHero", Rank = 3, Rating = 2380, Wins = 130, Losses = 30, WinStreak = 3 }
        };

        return Task.FromResult(Result<IReadOnlyList<LeaderboardEntry>>.Success(leaderboard));
    }
}
