using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Social.Streaming;

namespace SaveState.Infrastructure.Social.Streaming;

/// <summary>
/// Implementation of streaming studio service for multi-platform streaming.
/// </summary>
public sealed class StreamingStudioService : IStreamingStudioService
{
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<StreamingStudioService> _logger;

    private readonly Dictionary<StreamingPlatformType, PlatformAuthResult> _authTokens = new();
    private StreamSession? _currentSession;
    private readonly Dictionary<string, StreamSession> _sessions = new();

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;
    public event EventHandler<StreamingErrorEventArgs>? StreamingError;

    public StreamingStudioService(ITimeProvider timeProvider, ILogger<StreamingStudioService> logger)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<IReadOnlyList<StreamingPlatform>>> GetAvailablePlatformsAsync(CancellationToken ct = default)
    {
        try
        {
            var platforms = new List<StreamingPlatform>
            {
                CreatePlatform(StreamingPlatformType.Twitch),
                CreatePlatform(StreamingPlatformType.YouTube),
                CreatePlatform(StreamingPlatformType.Kick),
                CreatePlatform(StreamingPlatformType.CustomRtmp)
            };

            return Task.FromResult(Result<IReadOnlyList<StreamingPlatform>>.Success(platforms));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available platforms");
            return Task.FromResult(Result<IReadOnlyList<StreamingPlatform>>.Failure($"Failed to get platforms: {ex.Message}", ErrorType.External));
        }
    }

    public async Task<Result<PlatformAuthResult>> AuthenticateAsync(StreamingPlatformType platform, string authCode, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(authCode, nameof(authCode));

            _logger.LogInformation("Authenticating with {Platform}", platform);

            var result = new PlatformAuthResult(
                Platform: platform,
                AccessToken: $"token_{Guid.NewGuid():N}",
                RefreshToken: $"refresh_{Guid.NewGuid():N}",
                ExpiresAt: _timeProvider.UtcNow.AddHours(24),
                Username: $"user_{platform}",
                ChannelId: Guid.NewGuid().ToString());

            _authTokens[platform] = result;

            _logger.LogInformation("Authenticated with {Platform} as {Username}", platform, result.Username);
            return Result<PlatformAuthResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with {Platform}", platform);
            return Result<PlatformAuthResult>.Failure($"Failed to authenticate: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<PlatformAuthResult>> RefreshAuthenticationAsync(StreamingPlatformType platform, string refreshToken, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(refreshToken, nameof(refreshToken));

            _logger.LogInformation("Refreshing authentication for {Platform}", platform);

            var result = new PlatformAuthResult(
                Platform: platform,
                AccessToken: $"token_{Guid.NewGuid():N}",
                RefreshToken: refreshToken,
                ExpiresAt: _timeProvider.UtcNow.AddHours(24),
                Username: $"user_{platform}");

            _authTokens[platform] = result;

            return Result<PlatformAuthResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh authentication for {Platform}", platform);
            return Result<PlatformAuthResult>.Failure($"Failed to refresh authentication: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<StreamSession>> StartStreamAsync(StreamConfiguration config, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.Null(config, nameof(config));

            _logger.LogInformation("Starting stream: {Title} to {PlatformCount} platforms",
                config.Title, config.Platforms.Count);

            var sessionId = Guid.NewGuid().ToString();
            var platformStreams = new List<PlatformStreamInfo>();

            foreach (var platform in config.Platforms)
            {
                if (!_authTokens.TryGetValue(platform, out var auth))
                {
                    StreamingError?.Invoke(this, new StreamingErrorEventArgs(
                        sessionId, platform, $"Not authenticated with {platform}", false));
                    continue;
                }

                platformStreams.Add(new PlatformStreamInfo(
                    Platform: platform,
                    PlatformStreamId: Guid.NewGuid().ToString(),
                    StreamUrl: $"https://{platform.ToString().ToLower()}.com/stream/{sessionId}",
                    ChatUrl: $"https://{platform.ToString().ToLower()}.com/chat/{sessionId}",
                    Status: StreamPlatformStatus.Connecting,
                    ViewerCount: 0,
                    FollowerCount: 0,
                    ThumbnailUrl: null));
            }

            if (platformStreams.Count == 0)
            {
                return Result<StreamSession>.Failure("No platforms available for streaming", ErrorType.Validation);
            }

            var session = new StreamSession(
                Id: sessionId,
                Status: StreamStatus.Starting,
                StartedAt: _timeProvider.UtcNow,
                Configuration: config,
                PlatformStreams: platformStreams);

            _currentSession = session;
            _sessions[sessionId] = session;

            await Task.Delay(1000, ct).ConfigureAwait(false);

            session = session with
            {
                Status = StreamStatus.Live,
                PlatformStreams = platformStreams.Select(p => p with { Status = StreamPlatformStatus.Live }).ToList()
            };

            _currentSession = session;
            _sessions[sessionId] = session;

            StreamStatusChanged?.Invoke(this, new StreamStatusChangedEventArgs(
                sessionId, StreamStatus.Starting, StreamStatus.Live));

            _logger.LogInformation("Stream started: {SessionId}", sessionId);
            return Result<StreamSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start stream");
            return Result<StreamSession>.Failure($"Failed to start stream: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result> StopStreamAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(sessionId, nameof(sessionId));

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure("Session not found", ErrorType.NotFound);
            }

            _logger.LogInformation("Stopping stream: {SessionId}", sessionId);

            var oldStatus = session.Status;
            session = session with
            {
                Status = StreamStatus.Stopping,
                PlatformStreams = session.PlatformStreams.Select(p => p with { Status = StreamPlatformStatus.Disconnected }).ToList()
            };
            _sessions[sessionId] = session;

            await Task.Delay(500, ct).ConfigureAwait(false);

            session = session with
            {
                Status = StreamStatus.Offline,
                EndedAt = _timeProvider.UtcNow
            };
            _sessions[sessionId] = session;

            if (_currentSession?.Id == sessionId)
            {
                _currentSession = null;
            }

            StreamStatusChanged?.Invoke(this, new StreamStatusChangedEventArgs(
                sessionId, oldStatus, StreamStatus.Offline));

            _logger.LogInformation("Stream stopped: {SessionId}", sessionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop stream");
            return Result.Failure($"Failed to stop stream: {ex.Message}", ErrorType.External);
        }
    }

    public Task<Result<StreamSession?>> GetCurrentSessionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<StreamSession?>.Success(_currentSession));
    }

    public async Task<Result> UpdateStreamMetadataAsync(string sessionId, StreamMetadata metadata, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.NullOrEmpty(sessionId, nameof(sessionId));
            Guard.Against.Null(metadata, nameof(metadata));

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure("Session not found", ErrorType.NotFound);
            }

            _logger.LogDebug("Updating stream metadata for {SessionId}", sessionId);

            var newConfig = session.Configuration with { };
            if (!string.IsNullOrEmpty(metadata.Title))
                newConfig = newConfig with { Title = metadata.Title };
            if (!string.IsNullOrEmpty(metadata.GameName))
                newConfig = newConfig with { GameName = metadata.GameName };

            session = session with { Configuration = newConfig };
            _sessions[sessionId] = session;

            if (_currentSession?.Id == sessionId)
            {
                _currentSession = session;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update stream metadata");
            return Result.Failure($"Failed to update metadata: {ex.Message}", ErrorType.External);
        }
    }

    public Task<Result<StreamHealth>> GetStreamHealthAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(Result<StreamHealth>.Failure("Session not found", ErrorType.NotFound));
            }

            var platformHealths = session.PlatformStreams.Select(p => new PlatformHealth(
                Platform: p.Platform,
                IsConnected: p.Status == StreamPlatformStatus.Live,
                UploadBitrateKbps: 6000,
                BufferUsagePercent: 25,
                ReconnectCount: 0,
                ErrorMessage: null)).ToList();

            var health = new StreamHealth(
                SessionId: sessionId,
                Fps: 60.0,
                DroppedFramesPercent: 0.1,
                NetworkBitrateKbps: 6000,
                CpuUsagePercent: 35,
                MemoryUsageMb: 512,
                RenderTimeMs: 8.0,
                PlatformHealths: platformHealths);

            return Task.FromResult(Result<StreamHealth>.Success(health));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stream health");
            return Task.FromResult(Result<StreamHealth>.Failure($"Failed to get health: {ex.Message}", ErrorType.External));
        }
    }

    private StreamingPlatform CreatePlatform(StreamingPlatformType type)
    {
        var qualities = new List<StreamQuality>
        {
            new("1080p60", 1920, 1080, 60, 6000, 160),
            new("720p60", 1280, 720, 60, 4500, 160),
            new("720p30", 1280, 720, 30, 3000, 128),
            new("480p30", 854, 480, 30, 1500, 128)
        };

        return new StreamingPlatform(
            Type: type,
            Name: type.ToString(),
            IconUrl: $"/icons/{type.ToString().ToLower()}.png",
            IsAuthenticated: _authTokens.ContainsKey(type),
            SupportsChat: type != StreamingPlatformType.CustomRtmp,
            SupportsOverlays: true,
            SupportedQualities: qualities);
    }
}
