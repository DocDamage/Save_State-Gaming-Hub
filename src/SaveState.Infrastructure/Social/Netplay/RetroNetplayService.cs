using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.Social.Netplay;

namespace SaveState.Infrastructure.Social.Netplay;

/// <summary>
/// Implementation of retro game netplay service with matchmaking and rollback netcode.
/// </summary>
public sealed class RetroNetplayService : IRetroNetplayService
{
    private readonly IMatchmakingEngine _matchmakingEngine;
    private readonly IRollbackNetcodeService _rollbackService;
    private readonly ISpectatorRelayService _spectatorService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RetroNetplayService> _logger;

    private MatchmakingSession? _currentSession;
    private NetplaySession? _currentNetplaySession;
    private NetplayConnection? _currentConnection;

    public event EventHandler<MatchFoundEventArgs>? MatchFound;
    public event EventHandler<QueueStatusChangedEventArgs>? QueueStatusChanged;
    public event EventHandler<ConnectionQualityChangedEventArgs>? ConnectionQualityChanged;

    public RetroNetplayService(
        IMatchmakingEngine matchmakingEngine,
        IRollbackNetcodeService rollbackService,
        ISpectatorRelayService spectatorService,
        ITimeProvider timeProvider,
        ILogger<RetroNetplayService> logger)
    {
        _matchmakingEngine = matchmakingEngine ?? throw new ArgumentNullException(nameof(matchmakingEngine));
        _rollbackService = rollbackService ?? throw new ArgumentNullException(nameof(rollbackService));
        _spectatorService = spectatorService ?? throw new ArgumentNullException(nameof(spectatorService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<MatchmakingSession?>> GetCurrentSessionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<MatchmakingSession?>.Success(_currentSession));
    }

    public async Task<Result<MatchmakingTicket>> JoinQueueAsync(RomFile romFile, MatchmakingPreferences preferences, CancellationToken ct = default)
    {
        if (romFile is null)
        {
            return Result<MatchmakingTicket>.Failure("ROM file is required", ErrorType.Validation);
        }

        if (preferences is null)
        {
            return Result<MatchmakingTicket>.Failure("Matchmaking preferences are required", ErrorType.Validation);
        }

        try
        {
            _logger.LogInformation("Joining matchmaking queue for ROM: {RomHash}, Region: {Region}",
                romFile.Hash, preferences.Region);

            var request = new MatchmakingRequest(
                PlayerId: Guid.NewGuid().ToString(),
                Username: Environment.UserName,
                RomHash: romFile.Hash ?? string.Empty,
                Region: preferences.Region,
                SkillRating: preferences.SkillRating ?? 1500,
                Criteria: new MatchmakingCriteria(
                    MaxSkillDifference: preferences.MaxSkillDifference,
                    MaxWaitTimeSeconds: preferences.MaxWaitTimeSeconds,
                    AllowCrossRegion: false,
                    PreferredRegions: new[] { preferences.Region },
                    AllowSpectators: preferences.AllowSpectators),
                RequestedAt: _timeProvider.UtcNow);

            var result = await _matchmakingEngine.EnqueueAsync(request, ct).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Result<MatchmakingTicket>.Failure(result.Error!, result.ErrorType);
            }

            var ticket = result.Value!;
            _currentSession = new MatchmakingSession(
                TicketId: ticket.Id,
                RomHash: romFile.Hash ?? string.Empty,
                Status: MatchmakingStatus.Queued,
                QueueTime: _timeProvider.UtcNow,
                EstimatedWaitSeconds: ticket.EstimatedWaitSeconds,
                PlayersInQueue: 1);

            QueueStatusChanged?.Invoke(this, new QueueStatusChangedEventArgs(
                MatchmakingStatus.None, MatchmakingStatus.Queued, ticket.EstimatedWaitSeconds, 1));

            _logger.LogInformation("Joined matchmaking queue with ticket: {TicketId}", ticket.Id);
            return Result<MatchmakingTicket>.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join matchmaking queue");
            return Result<MatchmakingTicket>.Failure($"Failed to join queue: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result> LeaveQueueAsync(string ticketId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(ticketId, nameof(ticketId));

            var result = await _matchmakingEngine.DequeueAsync(ticketId, ct).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Result.Failure(result.Error!, result.ErrorType);
            }

            var oldStatus = _currentSession?.Status ?? MatchmakingStatus.None;
            _currentSession = null;

            QueueStatusChanged?.Invoke(this, new QueueStatusChangedEventArgs(
                oldStatus, MatchmakingStatus.Cancelled, null, 0));

            _logger.LogInformation("Left matchmaking queue: {TicketId}", ticketId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave matchmaking queue");
            return Result.Failure($"Failed to leave queue: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<NetplaySession>> AcceptMatchAsync(string matchId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(matchId, nameof(matchId));

            if (_currentSession == null)
            {
                return Result<NetplaySession>.Failure("Not in a matchmaking session", ErrorType.Validation);
            }

            _logger.LogInformation("Accepting match: {MatchId}", matchId);

            var result = await _matchmakingEngine.AcceptMatchAsync(_currentSession.TicketId, matchId, ct).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Result<NetplaySession>.Failure(result.Error!, result.ErrorType);
            }

            var confirmation = result.Value!;

            var localPlayer = new PlayerInfo(
                Id: confirmation.IsHost ? "1" : "2",
                Username: Environment.UserName,
                Region: _currentSession?.RomHash ?? "Unknown",
                SkillRating: 1500);

            var remotePlayer = new PlayerInfo(
                Id: confirmation.IsHost ? "2" : "1",
                Username: "Opponent",
                Region: "Unknown",
                SkillRating: 1500);

            _currentNetplaySession = new NetplaySession(
                Id: confirmation.SessionId,
                RomHash: _currentSession?.RomHash ?? string.Empty,
                HostAddress: confirmation.HostAddress,
                Port: confirmation.Port,
                LocalPlayer: localPlayer,
                RemotePlayer: remotePlayer,
                IsHost: confirmation.IsHost,
                StartedAt: _timeProvider.UtcNow);

            _currentSession = _currentSession with { Status = MatchmakingStatus.Accepted };

            QueueStatusChanged?.Invoke(this, new QueueStatusChangedEventArgs(
                MatchmakingStatus.MatchFound, MatchmakingStatus.Accepted, null, 0));

            _logger.LogInformation("Match accepted, session created: {SessionId}", confirmation.SessionId);
            return Result<NetplaySession>.Success(_currentNetplaySession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept match");
            return Result<NetplaySession>.Failure($"Failed to accept match: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result> DeclineMatchAsync(string matchId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(matchId, nameof(matchId));

            if (_currentSession == null)
            {
                return Result.Failure("Not in a matchmaking session", ErrorType.Validation);
            }

            var result = await _matchmakingEngine.DeclineMatchAsync(_currentSession.TicketId, matchId, ct).ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Result.Failure(result.Error!, result.ErrorType);
            }

            _currentSession = _currentSession with { Status = MatchmakingStatus.Queued };

            QueueStatusChanged?.Invoke(this, new QueueStatusChangedEventArgs(
                MatchmakingStatus.MatchFound, MatchmakingStatus.Queued, null, 1));

            _logger.LogInformation("Match declined: {MatchId}", matchId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decline match");
            return Result.Failure($"Failed to decline match: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<NetplayConnection>> ConnectToSessionAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(sessionId, nameof(sessionId));

            _logger.LogInformation("Connecting to netplay session: {SessionId}", sessionId);

            var config = new RollbackConfiguration(
                MaxRollbackFrames: 8,
                InputDelayFrames: 2,
                LocalInputDelay: 1,
                PredictiveInputs: true,
                FrameRate: 60,
                SimulationDelayMs: 16);

            var rollbackResult = await _rollbackService.InitializeAsync(config, ct).ConfigureAwait(false);

            if (rollbackResult.IsFailure)
            {
                return Result<NetplayConnection>.Failure(rollbackResult.Error!, rollbackResult.ErrorType);
            }

            _currentConnection = new NetplayConnection(
                SessionId: sessionId,
                State: ConnectionState.Connected,
                RollbackConfig: new RollbackConfig(
                    MaxRollbackFrames: config.MaxRollbackFrames,
                    InputDelayFrames: config.InputDelayFrames,
                    PredictiveInputs: config.PredictiveInputs,
                    DesyncDetection: true,
                    ResyncIntervalMs: 1000),
                InputDelay: new InputDelayConfig(
                    LocalDelay: config.LocalInputDelay,
                    RemoteDelay: config.InputDelayFrames,
                    TotalDelay: config.InputDelayFrames + config.LocalInputDelay,
                    AutoAdjust: true),
                ConnectedAt: _timeProvider.UtcNow);

            _logger.LogInformation("Connected to netplay session: {SessionId}", sessionId);
            return Result<NetplayConnection>.Success(_currentConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to session");
            return Result<NetplayConnection>.Failure($"Failed to connect: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result> DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Disconnecting from netplay session");

            _currentConnection = null;
            _currentNetplaySession = null;
            _currentSession = null;

            _logger.LogInformation("Disconnected from netplay session");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
            return Result.Failure($"Error during disconnect: {ex.Message}", ErrorType.External);
        }
    }

    public Task<Result<ConnectionQuality>> GetConnectionQualityAsync(CancellationToken ct = default)
    {
        if (_currentConnection == null)
        {
            return Task.FromResult(Result<ConnectionQuality>.Failure("Not connected", ErrorType.Validation));
        }

        var quality = new ConnectionQuality(
            PingMs: 30,
            JitterMs: 2,
            PacketLossPercent: 0.0,
            RollbackFrames: 0,
            Rating: ConnectionQualityRating.Excellent);

        return Task.FromResult(Result<ConnectionQuality>.Success(quality));
    }
}
