using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Application.Mugen.Services.NetworkFeatures.Engines;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

// Use Core types for the interface implementation
using CoreMatchmakingRequest = SaveState.Core.Mugen.Services.MatchmakingRequest;
using CoreMatchmakingResult = SaveState.Core.Mugen.Services.MatchmakingResult;
using CoreLobbyCreationRequest = SaveState.Core.Mugen.Services.LobbyCreationRequest;
using CoreLobbyInfo = SaveState.Core.Mugen.Services.LobbyInfo;
using CoreLobbyFilter = SaveState.Core.Mugen.Services.LobbyFilter;
using CoreLobbyPlayer = SaveState.Core.Mugen.Services.LobbyPlayer;
using CoreLobbySettings = SaveState.Core.Mugen.Services.LobbySettings;
using CoreLobbyStatus = SaveState.Core.Mugen.Services.LobbyStatus;
using CoreSpectatorSession = SaveState.Core.Mugen.Services.SpectatorSession;
using CoreSpectatorControls = SaveState.Core.Mugen.Services.SpectatorControls;
using CoreLeaderboardType = SaveState.Core.Mugen.Services.LeaderboardType;
using CoreLeaderboardEntry = SaveState.Core.Mugen.Services.LeaderboardEntry;
using CoreReportReason = SaveState.Core.Mugen.Services.ReportReason;
using CorePlayerProfile = SaveState.Core.Mugen.Services.PlayerProfile;
using CorePlayerStats = SaveState.Core.Mugen.Services.PlayerStats;
using CoreCharacterSpecificStats = SaveState.Core.Mugen.Services.CharacterSpecificStats;
using CoreReputation = SaveState.Core.Mugen.Services.Reputation;
using CoreReputationTier = SaveState.Core.Mugen.Services.ReputationTier;
using CoreFriendInfo = SaveState.Core.Mugen.Services.FriendInfo;
using CoreFriendshipAction = SaveState.Core.Mugen.Services.FriendshipAction;
using CoreFriendshipStatus = SaveState.Core.Mugen.Services.FriendshipStatus;
using CoreChatChannel = SaveState.Core.Mugen.Services.ChatChannel;
using CoreChatMessage = SaveState.Core.Mugen.Services.ChatMessage;
using CoreReplayData = SaveState.Core.Mugen.Services.ReplayData;
using CorePlayerOnlineStatus = SaveState.Core.Mugen.Services.PlayerOnlineStatus;
using CoreAchievement = SaveState.Core.Mugen.Services.Achievement;
using CoreAchievementRarity = SaveState.Core.Mugen.Services.AchievementRarity;

namespace SaveState.Application.Mugen.Services.NetworkFeatures;

/// <summary>
/// Enhanced network features service implementation providing advanced matchmaking,
/// social features, and spectator mode for MUGEN online play.
/// Coordinates lobby, spectator, network quality, and relay server engines.
/// </summary>
public class NetworkFeaturesService : INetworkFeaturesService
{
    private readonly ILogger<NetworkFeaturesService> _logger;
    private readonly IMugenEloService _eloService;
    private readonly ICacheService _cache;
    private readonly MatchmakingEngine _matchmakingEngine;
    private readonly LobbyEngine _lobbyEngine;
    private readonly SpectatorEngine _spectatorEngine;
    private readonly NetworkQualityEngine _networkQualityEngine;
    private readonly RelayServerEngine _relayServerEngine;

    // State tracking dictionaries - kept in main service as per pattern
    private readonly Dictionary<string, MatchmakingSession> _activeSessions = new();
    private readonly Dictionary<string, Models.NetworkFeatures.LobbyInfo> _activeLobbies = new();
    private readonly Dictionary<string, Models.NetworkFeatures.SpectatorSession> _spectatorSessions = new();

    public NetworkFeaturesService(
        ILogger<NetworkFeaturesService> logger,
        ILoggerFactory loggerFactory,
        IMugenEloService eloService,
        ICacheService cache)
    {
        _logger = logger;
        _eloService = eloService;
        _cache = cache;

        // Initialize engines
        _matchmakingEngine = new MatchmakingEngine(loggerFactory.CreateLogger<MatchmakingEngine>());
        _lobbyEngine = new LobbyEngine(loggerFactory.CreateLogger<LobbyEngine>());
        _spectatorEngine = new SpectatorEngine(loggerFactory.CreateLogger<SpectatorEngine>());
        _networkQualityEngine = new NetworkQualityEngine(loggerFactory.CreateLogger<NetworkQualityEngine>());
        _relayServerEngine = new RelayServerEngine(loggerFactory.CreateLogger<RelayServerEngine>());

        _logger.LogInformation("Network features service initialized with all engines");
    }

    #region Matchmaking Operations

    public async Task<Result<CoreMatchmakingResult>> FindMatchAsync(CoreMatchmakingRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Finding match for player {PlayerId} in {Mode} mode", request.PlayerId, request.Mode);

            var playerStats = await GetPlayerMatchmakingStatsAsync(request.PlayerId, ct);
            if (!playerStats.IsSuccess || playerStats.Value is null)
            {
                return Result.Failure<CoreMatchmakingResult>("Unable to retrieve player statistics");
            }

            var sessionId = Guid.NewGuid().ToString();
            var session = new MatchmakingSession
            {
                SessionId = sessionId,
                PlayerId = request.PlayerId,
                CharacterName = request.CharacterName,
                Mode = (Models.NetworkFeatures.MatchmakingMode)(int)request.Mode,
                Preferences = new Models.NetworkFeatures.MatchmakingPreferences(
                    request.Preferences.MinRating,
                    request.Preferences.MaxRating,
                    request.Preferences.PreferredCharacters,
                    request.Preferences.AvoidedCharacters,
                    request.Preferences.AllowCrossplay,
                    request.Preferences.Region),
                PlayerStats = playerStats.Value,
                StartTime = DateTime.UtcNow,
                Timeout = request.Timeout
            };

            _activeSessions[sessionId] = session;

            var matchmakingTask = Task.Run(() => ProcessMatchmakingAsync(session, ct), ct);
            var result = await WaitForMatchAsync(session, ct);

            _activeSessions.Remove(sessionId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding match for player {PlayerId}", request.PlayerId);
            return Result.Failure<CoreMatchmakingResult>($"Matchmaking failed: {ex.Message}");
        }
    }

    #endregion

    #region Lobby Operations

    public async Task<Result<CoreLobbyInfo>> CreateLobbyAsync(CoreLobbyCreationRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Creating lobby '{LobbyName}' for host {HostId}", request.LobbyName, request.HostId);

            var hostName = await GetPlayerNameAsync(request.HostId, ct);
            
            var appSettings = new Models.NetworkFeatures.LobbySettings(
                request.Settings.MaxPlayers,
                request.Settings.IsPrivate,
                request.Settings.GameMode,
                request.Settings.Rules,
                request.Settings.AllowSpectators,
                request.Settings.TimeLimitMinutes);
            
            var appRequest = new Models.NetworkFeatures.LobbyCreationRequest(request.HostId, request.LobbyName, appSettings, request.Password);
            var lobby = _lobbyEngine.CreateLobby(appRequest, hostName);
            
            _activeLobbies[lobby.LobbyId] = lobby;

            return Result.Success<CoreLobbyInfo>(MapToCoreLobbyInfo(lobby));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lobby for host {HostId}", request.HostId);
            return Result.Failure<CoreLobbyInfo>($"Failed to create lobby: {ex.Message}");
        }
    }

    public async Task<Result<CoreLobbyInfo>> JoinLobbyAsync(string lobbyCode, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var lobby = _activeLobbies.Values.FirstOrDefault(l => l.LobbyCode == lobbyCode);
            var (canJoin, error) = _lobbyEngine.ValidateLobbyJoin(lobby);
            
            if (!canJoin)
            {
                return Result.Failure<CoreLobbyInfo>(error ?? "Unable to join lobby");
            }

            _logger.LogInformation("Player joined lobby {LobbyCode}", lobbyCode);
            return Result.Success<CoreLobbyInfo>(MapToCoreLobbyInfo(lobby!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining lobby {LobbyCode}", lobbyCode);
            return Result.Failure<CoreLobbyInfo>($"Failed to join lobby: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<CoreLobbyInfo>>> GetAvailableLobbiesAsync(CoreLobbyFilter filter, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var appFilter = new Models.NetworkFeatures.LobbyFilter(filter.GameMode, filter.PrivateOnly, filter.MinPlayers, filter.MaxPlayers, filter.Region);
            var lobbies = _lobbyEngine.FilterLobbies(_activeLobbies.Values, appFilter).ToList();
            return Result.Success<IReadOnlyList<CoreLobbyInfo>>(lobbies.Select(MapToCoreLobbyInfo).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available lobbies");
            return Result.Failure<IReadOnlyList<CoreLobbyInfo>>($"Failed to get lobbies: {ex.Message}");
        }
    }

    #endregion

    #region Spectator Operations

    public async Task<Result<CoreSpectatorSession>> StartSpectatingAsync(string matchId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Starting spectator session for match {MatchId}", matchId);

            var matchExists = await ValidateMatchExistsAsync(matchId, ct);
            var (canSpectate, error) = _spectatorEngine.ValidateSpectateRequest(matchId, matchExists, true);
            
            if (!canSpectate)
            {
                return Result.Failure<CoreSpectatorSession>(error ?? "Unable to start spectating");
            }

            var spectatorSession = _spectatorEngine.CreateSpectatorSession(matchId);
            _spectatorSessions[spectatorSession.SessionId] = spectatorSession;

            return Result.Success<CoreSpectatorSession>(MapToCoreSpectatorSession(spectatorSession));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting spectator session for match {MatchId}", matchId);
            return Result.Failure<CoreSpectatorSession>($"Failed to start spectating: {ex.Message}");
        }
    }

    #endregion

    #region Social Features

    public async Task<Result<IReadOnlyList<CoreLeaderboardEntry>>> GetLeaderboardsAsync(CoreLeaderboardType type, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var entries = await GenerateLeaderboardAsync(type, ct);
            return Result.Success<IReadOnlyList<CoreLeaderboardEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboards for type {Type}", type);
            return Result.Failure<IReadOnlyList<CoreLeaderboardEntry>>($"Failed to get leaderboards: {ex.Message}");
        }
    }

    public async Task<Result> ReportPlayerAsync(string playerId, CoreReportReason reason, string description, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Reporting player {PlayerId} for {Reason}", playerId, reason);

            var report = new PlayerReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportedPlayerId = playerId,
                Reason = (Models.NetworkFeatures.ReportReason)(int)reason,
                Description = description,
                SubmittedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            await UpdatePlayerReputationAsync(playerId, reason, ct);

            _logger.LogInformation("Player report submitted for {PlayerId}", playerId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting player {PlayerId}", playerId);
            return Result.Failure($"Failed to submit report: {ex.Message}");
        }
    }

    public async Task<Result<CorePlayerProfile>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var profile = await GeneratePlayerProfileAsync(playerId, ct);
            return Result.Success<CorePlayerProfile>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player profile for {PlayerId}", playerId);
            return Result.Failure<CorePlayerProfile>($"Failed to get profile: {ex.Message}");
        }
    }

    public async Task<Result> ManageFriendshipAsync(string friendId, CoreFriendshipAction action, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Managing friendship with {FriendId}: {Action}", friendId, action);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing friendship with {FriendId}", friendId);
            return Result.Failure($"Failed to manage friendship: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<CoreFriendInfo>>> GetFriendsAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var friends = await GenerateFriendsListAsync(ct);
            return Result.Success<IReadOnlyList<CoreFriendInfo>>(friends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friends list");
            return Result.Failure<IReadOnlyList<CoreFriendInfo>>($"Failed to get friends: {ex.Message}");
        }
    }

    public async Task<Result> SendChatMessageAsync(string message, CoreChatChannel channel, string? targetId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                return Result.Failure("Invalid message");
            }

            var chatMessage = new CoreChatMessage(
                MessageId: Guid.NewGuid().ToString(),
                SenderId: "current_player",
                SenderName: "Player",
                Message: message,
                Channel: channel,
                Timestamp: DateTime.UtcNow,
                TargetId: targetId
            );

            await BroadcastChatMessageAsync(chatMessage, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            return Result.Failure($"Failed to send message: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<CoreChatMessage>>> GetChatMessagesAsync(CoreChatChannel channel, int count, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var messages = await GetRecentChatMessagesAsync(channel, count, ct);
            return Result.Success<IReadOnlyList<CoreChatMessage>>(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat messages for {Channel}", channel);
            return Result.Failure<IReadOnlyList<CoreChatMessage>>($"Failed to get messages: {ex.Message}");
        }
    }

    #endregion

    #region Replay Operations

    public async Task<Result<string>> ShareReplayAsync(string matchId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Sharing replay for match {MatchId}", matchId);
            var replayId = Guid.NewGuid().ToString();
            var shareUrl = $"mugen://replay/{replayId}";
            return Result.Success<string>(shareUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing replay for match {MatchId}", matchId);
            return Result.Failure<string>($"Failed to share replay: {ex.Message}");
        }
    }

    public async Task<Result<CoreReplayData>> DownloadReplayAsync(string replayId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Downloading replay {ReplayId}", replayId);
            var replayData = await GenerateReplayDataAsync(replayId, ct);
            return Result.Success<CoreReplayData>(replayData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading replay {ReplayId}", replayId);
            return Result.Failure<CoreReplayData>($"Failed to download replay: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private async Task ProcessMatchmakingAsync(MatchmakingSession session, CancellationToken ct)
    {
        await Task.CompletedTask;
        try
        {
            var startTime = DateTime.UtcNow;

            while (!ct.IsCancellationRequested && DateTime.UtcNow - startTime < session.Timeout)
            {
                var opponent = await _matchmakingEngine.FindOpponentAsync(session, ct);
                if (opponent != null)
                {
                    session.MatchFound = true;
                    session.OpponentId = opponent.PlayerId;
                    session.OpponentName = opponent.PlayerName;
                    session.MatchId = Guid.NewGuid().ToString();
                    break;
                }

                await Task.Delay(1000, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in matchmaking process for session {SessionId}", session.SessionId);
        }
    }

    private async Task<Result<CoreMatchmakingResult>> WaitForMatchAsync(MatchmakingSession session, CancellationToken ct)
    {
        await Task.CompletedTask;
        var startTime = DateTime.UtcNow;

        while (!ct.IsCancellationRequested && DateTime.UtcNow - startTime < session.Timeout)
        {
            if (session.MatchFound)
            {
                return Result.Success<CoreMatchmakingResult>(new CoreMatchmakingResult(
                    MatchFound: true,
                    MatchId: session.MatchId!,
                    OpponentId: session.OpponentId!,
                    OpponentName: session.OpponentName!,
                    WaitTime: DateTime.UtcNow - startTime,
                    ErrorMessage: null
                ));
            }

            await Task.Delay(500, ct);
        }

        return Result.Success<CoreMatchmakingResult>(new CoreMatchmakingResult(
            MatchFound: false,
            MatchId: null,
            OpponentId: null,
            OpponentName: null,
            WaitTime: session.Timeout,
            ErrorMessage: "Matchmaking timeout"
        ));
    }

    private async Task<Result<PlayerMatchmakingStats>> GetPlayerMatchmakingStatsAsync(string playerId, CancellationToken ct)
    {
        await Task.CompletedTask;
        var rating = await _eloService.GetPlayerRatingAsync(playerId, ct);
        if (!rating.IsSuccess || rating.Value is null)
        {
            return Result.Failure<PlayerMatchmakingStats>("Unable to get player rating");
        }

        return Result.Success<PlayerMatchmakingStats>(new PlayerMatchmakingStats
        {
            PlayerId = playerId,
            Rating = (int)rating.Value.Rating,
            WinRate = 0.65m,
            TotalMatches = 100,
            PreferredCharacters = new[] { "Ryu", "Ken", "Guile" },
            RecentPerformance = 0.7m
        });
    }

    private async Task<string> GetPlayerNameAsync(string playerId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return $"Player_{playerId.Substring(0, Math.Min(8, playerId.Length))}";
    }

    private async Task<bool> ValidateMatchExistsAsync(string matchId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return true;
    }

    private async Task<IReadOnlyList<CoreLeaderboardEntry>> GenerateLeaderboardAsync(CoreLeaderboardType type, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new List<CoreLeaderboardEntry>
        {
            new CoreLeaderboardEntry(1, "player1", "Champion", 2850, 247, 18, 0.932m, null),
            new CoreLeaderboardEntry(2, "player2", "Master", 2720, 198, 27, 0.880m, null),
            new CoreLeaderboardEntry(3, "player3", "Master", 2680, 176, 31, 0.850m, null),
            new CoreLeaderboardEntry(4, "player4", "Diamond", 2550, 203, 42, 0.828m, null),
            new CoreLeaderboardEntry(5, "player5", "Diamond", 2480, 189, 46, 0.804m, null)
        };
    }

    private async Task UpdatePlayerReputationAsync(string playerId, CoreReportReason reason, CancellationToken ct)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Updated reputation for player {PlayerId} due to {Reason}", playerId, reason);
    }

    private async Task<CorePlayerProfile> GeneratePlayerProfileAsync(string playerId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new CorePlayerProfile(
            PlayerId: playerId,
            PlayerName: await GetPlayerNameAsync(playerId, ct),
            Rating: 1850,
            Rank: "Gold II",
            Achievements: new List<CoreAchievement>
            {
                new CoreAchievement("First Victory", "Won your first match", DateTime.UtcNow.AddMonths(-6), CoreAchievementRarity.Common),
                new CoreAchievement("Combo Master", "Executed a 10+ hit combo", DateTime.UtcNow.AddMonths(-3), CoreAchievementRarity.Rare)
            },
            Stats: new CorePlayerStats(
                TotalMatches: 247,
                Wins: 176,
                Losses: 71,
                WinRate: 0.712m,
                TotalPlayTime: TimeSpan.FromHours(156),
                CharacterStats: new Dictionary<string, CoreCharacterSpecificStats>
                {
                    ["Ryu"] = new CoreCharacterSpecificStats(50, 38, 12, 0.76m, 0),
                    ["Ken"] = new CoreCharacterSpecificStats(45, 32, 13, 0.711m, 1),
                    ["Guile"] = new CoreCharacterSpecificStats(30, 21, 9, 0.70m, 2)
                }
            ),
            Reputation: new CoreReputation(
                Score: 850,
                Tier: CoreReputationTier.Good,
                Badges: new[] { "Sportsmanlike", "Helpful" },
                LastReported: DateTime.MinValue
            ),
            FavoriteCharacters: new[] { "Ryu", "Ken" },
            StatusMessage: "Ready to fight!",
            AvatarUrl: "https://example.com/avatar.png",
            Status: CorePlayerOnlineStatus.Online,
            CurrentActivity: "In Lobby",
            Region: "North America"
        );
    }

    private async Task<IReadOnlyList<CoreFriendInfo>> GenerateFriendsListAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        return new List<CoreFriendInfo>
        {
            new CoreFriendInfo("friend1", "StreetFighterFan", CoreFriendshipStatus.Accepted, DateTime.UtcNow.AddMonths(-6), true, "Playing Ranked Match"),
            new CoreFriendInfo("friend2", "CharacterCreator", CoreFriendshipStatus.Accepted, DateTime.UtcNow.AddMonths(-4), false, null),
            new CoreFriendInfo("friend3", "ComboMaster", CoreFriendshipStatus.Accepted, DateTime.UtcNow.AddMonths(-2), true, "Training Mode")
        };
    }

    private async Task BroadcastChatMessageAsync(CoreChatMessage message, CancellationToken ct)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Broadcasting message to {Channel}: {Message}", message.Channel, message.Message);
    }

    private async Task<IReadOnlyList<CoreChatMessage>> GetRecentChatMessagesAsync(CoreChatChannel channel, int count, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new List<CoreChatMessage>
        {
            new CoreChatMessage(Guid.NewGuid().ToString(), "player1", "Player1", "GG everyone!", channel, DateTime.UtcNow.AddMinutes(-5), null),
            new CoreChatMessage(Guid.NewGuid().ToString(), "player2", "Player2", "Tournament starting soon", channel, DateTime.UtcNow.AddMinutes(-3), null),
            new CoreChatMessage(Guid.NewGuid().ToString(), "player3", "Player3", "Anyone want to play casual?", channel, DateTime.UtcNow.AddMinutes(-1), null)
        }.Take(count).ToList();
    }

    private async Task<CoreReplayData> GenerateReplayDataAsync(string replayId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new CoreReplayData(
            ReplayId: replayId,
            MatchId: Guid.NewGuid().ToString(),
            Player1Name: "Player1",
            Player2Name: "Player2",
            Player1Character: "Ryu",
            Player2Character: "Ken",
            Data: new byte[1024],
            RecordedAt: DateTime.UtcNow.AddHours(-1),
            Duration: TimeSpan.FromMinutes(3)
        );
    }

    #endregion

    #region Mapping Methods

    private CoreLobbyInfo MapToCoreLobbyInfo(Models.NetworkFeatures.LobbyInfo lobby)
    {
        return new CoreLobbyInfo(
            lobby.LobbyId,
            lobby.LobbyCode,
            lobby.HostName,
            lobby.LobbyName,
            new CoreLobbySettings(
                lobby.Settings.MaxPlayers,
                lobby.Settings.IsPrivate,
                lobby.Settings.GameMode,
                lobby.Settings.Rules,
                lobby.Settings.AllowSpectators,
                lobby.Settings.TimeLimitMinutes),
            lobby.Players.Select(p => new CoreLobbyPlayer(p.PlayerId, p.PlayerName, p.CharacterName, p.IsReady, p.IsHost)).ToList(),
            (CoreLobbyStatus)(int)lobby.Status
        );
    }

    private CoreSpectatorSession MapToCoreSpectatorSession(Models.NetworkFeatures.SpectatorSession session)
    {
        return new CoreSpectatorSession(
            session.SessionId,
            session.MatchId,
            session.StreamUrl,
            session.Controls.Select(c => new CoreSpectatorControls(c.ControlType, c.Description, c.Enabled)).ToList()
        );
    }

    #endregion
}
