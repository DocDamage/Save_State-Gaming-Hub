using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.Streaming;

/// <summary>
/// Service for managing multi-platform streaming integration.
/// </summary>
public interface IStreamingStudioService
{
    /// <summary>
    /// Gets available streaming platforms.
    /// </summary>
    Task<Result<IReadOnlyList<StreamingPlatform>>> GetAvailablePlatformsAsync(CancellationToken ct = default);

    /// <summary>
    /// Authenticates with a streaming platform.
    /// </summary>
    Task<Result<PlatformAuthResult>> AuthenticateAsync(StreamingPlatformType platform, string authCode, CancellationToken ct = default);

    /// <summary>
    /// Refreshes authentication for a platform.
    /// </summary>
    Task<Result<PlatformAuthResult>> RefreshAuthenticationAsync(StreamingPlatformType platform, string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Starts streaming to one or more platforms.
    /// </summary>
    Task<Result<StreamSession>> StartStreamAsync(StreamConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Stops the current stream.
    /// </summary>
    Task<Result> StopStreamAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current streaming session.
    /// </summary>
    Task<Result<StreamSession?>> GetCurrentSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates stream metadata (title, category, etc.).
    /// </summary>
    Task<Result> UpdateStreamMetadataAsync(string sessionId, StreamMetadata metadata, CancellationToken ct = default);

    /// <summary>
    /// Gets stream health and performance metrics.
    /// </summary>
    Task<Result<StreamHealth>> GetStreamHealthAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when stream status changes.
    /// </summary>
    event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    /// <summary>
    /// Event raised when streaming errors occur.
    /// </summary>
    event EventHandler<StreamingErrorEventArgs>? StreamingError;
}

/// <summary>
/// Streaming platform information.
/// </summary>
public sealed record StreamingPlatform(
    StreamingPlatformType Type,
    string Name,
    string IconUrl,
    bool IsAuthenticated,
    bool SupportsChat,
    bool SupportsOverlays,
    IReadOnlyList<StreamQuality> SupportedQualities);

/// <summary>
/// Stream configuration for starting a stream.
/// </summary>
public sealed record StreamConfiguration(
    string Title,
    string GameName,
    IReadOnlyList<StreamingPlatformType> Platforms,
    StreamQuality Quality,
    bool EnableOverlays,
    bool EnableUnifiedChat,
    bool RecordLocally,
    string? RecordingPath = null);

/// <summary>
/// Stream metadata.
/// </summary>
public sealed record StreamMetadata(
    string? Title,
    string? GameName,
    string? Category,
    string[]? Tags,
    string? Language,
    bool? IsMature);

/// <summary>
/// Streaming platform authentication result.
/// </summary>
public sealed record PlatformAuthResult(
    StreamingPlatformType Platform,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Username,
    string? ChannelId = null);

/// <summary>
/// Active stream session.
/// </summary>
public sealed record StreamSession(
    string Id,
    StreamStatus Status,
    DateTime StartedAt,
    StreamConfiguration Configuration,
    IReadOnlyList<PlatformStreamInfo> PlatformStreams,
    DateTime? EndedAt = null);

/// <summary>
/// Platform-specific stream information.
/// </summary>
public sealed record PlatformStreamInfo(
    StreamingPlatformType Platform,
    string PlatformStreamId,
    string StreamUrl,
    string ChatUrl,
    StreamPlatformStatus Status,
    long ViewerCount,
    long FollowerCount,
    string? ThumbnailUrl = null);

/// <summary>
/// Stream quality settings.
/// </summary>
public sealed record StreamQuality(
    string Name,
    int Width,
    int Height,
    int Fps,
    int VideoBitrateKbps,
    int AudioBitrateKbps,
    string Codec = "h264");

/// <summary>
/// Stream health metrics.
/// </summary>
public sealed record StreamHealth(
    string SessionId,
    double Fps,
    double DroppedFramesPercent,
    double NetworkBitrateKbps,
    int CpuUsagePercent,
    int MemoryUsageMb,
    double RenderTimeMs,
    IReadOnlyList<PlatformHealth> PlatformHealths);

/// <summary>
/// Per-platform stream health.
/// </summary>
public sealed record PlatformHealth(
    StreamingPlatformType Platform,
    bool IsConnected,
    double UploadBitrateKbps,
    int BufferUsagePercent,
    int ReconnectCount,
    string? ErrorMessage = null);

/// <summary>
/// Streaming platform types.
/// </summary>
public enum StreamingPlatformType
{
    Twitch,
    YouTube,
    Kick,
    Facebook,
    CustomRtmp
}

/// <summary>
/// Stream status states.
/// </summary>
public enum StreamStatus
{
    Starting,
    Live,
    Reconnecting,
    Paused,
    Stopping,
    Offline,
    Error
}

/// <summary>
/// Platform-specific stream status.
/// </summary>
public enum StreamPlatformStatus
{
    Connecting,
    Live,
    Disconnected,
    Error
}

/// <summary>
/// Event args for stream status changed events.
/// </summary>
public sealed class StreamStatusChangedEventArgs : EventArgs
{
    public string SessionId { get; }
    public StreamStatus OldStatus { get; }
    public StreamStatus NewStatus { get; }
    public DateTime ChangedAt { get; }

    public StreamStatusChangedEventArgs(string sessionId, StreamStatus oldStatus, StreamStatus newStatus, ITimeProvider? timeProvider = null)
    {
        SessionId = sessionId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for streaming error events.
/// </summary>
public sealed class StreamingErrorEventArgs : EventArgs
{
    public string SessionId { get; }
    public StreamingPlatformType? Platform { get; }
    public string ErrorMessage { get; }
    public bool IsFatal { get; }
    public DateTime OccurredAt { get; }

    public StreamingErrorEventArgs(string sessionId, StreamingPlatformType? platform, string errorMessage, bool isFatal, ITimeProvider? timeProvider = null)
    {
        SessionId = sessionId;
        Platform = platform;
        ErrorMessage = errorMessage;
        IsFatal = isFatal;
        OccurredAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}
