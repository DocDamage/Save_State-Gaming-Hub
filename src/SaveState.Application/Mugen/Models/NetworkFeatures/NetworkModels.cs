namespace SaveState.Application.Mugen.Models.NetworkFeatures;

/// <summary>
/// Request for matchmaking.
/// </summary>
public record MatchmakingRequest(
    string PlayerId,
    string CharacterName,
    MatchmakingMode Mode,
    MatchmakingPreferences Preferences,
    TimeSpan Timeout);

/// <summary>
/// Player matchmaking preferences.
/// </summary>
public record MatchmakingPreferences(
    int? MinRating,
    int? MaxRating,
    IReadOnlyList<string> PreferredCharacters,
    IReadOnlyList<string> AvoidedCharacters,
    bool AllowCrossplay,
    string Region);

/// <summary>
/// Result of matchmaking.
/// </summary>
public record MatchmakingResult(
    bool MatchFound,
    string? MatchId,
    string? OpponentId,
    string? OpponentName,
    TimeSpan? WaitTime,
    string? ErrorMessage);

/// <summary>
/// Request to create a lobby.
/// </summary>
public record LobbyCreationRequest(
    string HostId,
    string LobbyName,
    LobbySettings Settings,
    string? Password);

/// <summary>
/// Lobby settings.
/// </summary>
public record LobbySettings(
    int MaxPlayers,
    bool IsPrivate,
    string GameMode,
    string Rules,
    bool AllowSpectators,
    int TimeLimitMinutes);

/// <summary>
/// Information about a lobby.
/// </summary>
public record LobbyInfo(
    string LobbyId,
    string LobbyCode,
    string HostName,
    string LobbyName,
    LobbySettings Settings,
    IReadOnlyList<LobbyPlayer> Players,
    LobbyStatus Status);

/// <summary>
/// Represents a game lobby.
/// </summary>
public class Lobby
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HostPlayerId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public string? PasswordHash { get; set; }
    public bool AllowSpectators { get; set; }
    public LobbyStatus Status { get; set; }
    public List<LobbyPlayer> Players { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> CustomSettings { get; set; } = new();

    public int CurrentPlayerCount => Players.Count;
    public bool IsFull => Players.Count >= MaxPlayers;
    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);
}

/// <summary>
/// Filter for lobby search.
/// </summary>
public record LobbyFilter(
    string? GameMode,
    bool? PrivateOnly,
    int? MinPlayers,
    int? MaxPlayers,
    string? Region,
    bool? HideFull = null,
    bool? HidePasswordProtected = null);

/// <summary>
/// Configuration for creating a lobby.
/// </summary>
public record LobbyConfiguration(
    string Name,
    int MaxPlayers,
    string GameMode,
    string Region,
    bool IsPrivate,
    string? Password,
    bool AllowSpectators,
    Dictionary<string, string>? CustomSettings = null);

/// <summary>
/// Network session information.
/// </summary>
public record NetworkSession(
    string SessionId,
    string PlayerId,
    string? OpponentId,
    MatchmakingMode Mode,
    NetworkSessionState State,
    DateTime CreatedAt,
    DateTime? ConnectedAt,
    NetworkQuality Quality);

/// <summary>
/// Player report data.
/// </summary>
public class PlayerReport
{
    public string ReportId { get; set; } = default!;
    public string ReporterId { get; set; } = default!;
    public string ReportedPlayerId { get; set; } = default!;
    public ReportReason Reason { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime SubmittedAt { get; set; } = default!;
    public ReportStatus Status { get; set; } = default!;
}

/// <summary>
/// Chat message.
/// </summary>
public record ChatMessage(
    string MessageId,
    string SenderId,
    string SenderName,
    string Message,
    ChatChannel Channel,
    DateTime Timestamp,
    string? TargetId);

/// <summary>
/// Replay data.
/// </summary>
public record ReplayData(
    string ReplayId,
    string MatchId,
    string Player1Name,
    string Player2Name,
    string Player1Character,
    string Player2Character,
    byte[] Data,
    DateTime RecordedAt,
    TimeSpan Duration);

/// <summary>
/// Network quality metrics.
/// </summary>
public record NetworkQualityMetrics(
    int PingMs,
    float PacketLossPercent,
    float JitterMs,
    float UploadSpeedMbps,
    float DownloadSpeedMbps,
    NetworkQuality OverallQuality);

/// <summary>
/// Relay server information.
/// </summary>
public record RelayServerInfo(
    string ServerId,
    string Hostname,
    int Port,
    RelayRegion Region,
    int CurrentLoad,
    int MaxCapacity,
    bool IsAvailable);
