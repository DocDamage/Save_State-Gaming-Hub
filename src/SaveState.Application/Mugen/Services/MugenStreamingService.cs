using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional live streaming service for tournament broadcasting.
/// Provides multi-platform streaming, interactive overlays, and esports production.
/// </summary>
public class MugenStreamingService : MugenStreamingServiceIMugenStreamingService
{
    private readonly ILogger<MugenStreamingService> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MugenStreamingServiceStreamSession> _activeStreams = new();
    private readonly Dictionary<string, MugenStreamingServiceTournamentBroadcast> _tournamentBroadcasts = new();
    private readonly MugenStreamingServiceStreamAnalyticsEngine _analyticsEngine;
    private readonly MugenStreamingServiceOverlayManager _overlayManager;

    public MugenStreamingService(
        ILogger<MugenStreamingService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _analyticsEngine = new MugenStreamingServiceStreamAnalyticsEngine(loggerFactory.CreateLogger<MugenStreamingServiceStreamAnalyticsEngine>(), timeProvider);
        _overlayManager = new MugenStreamingServiceOverlayManager(loggerFactory.CreateLogger<MugenStreamingServiceOverlayManager>());
    }

    public async Task<Result<MugenStreamingServiceStreamSession>> StartTournamentStreamAsync(MugenStreamingServiceStreamRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting tournament stream for {TournamentId}", request.TournamentId);

            // Validate tournament exists and is active
            var tournament = await ValidateTournamentAsync(request.TournamentId, ct);
            if (!tournament.IsSuccess || tournament.Value is null)
            {
                return Result.Failure<MugenStreamingServiceStreamSession>(tournament.Error ?? "Tournament validation failed");
            }

            // Create stream session
            var streamId = Guid.NewGuid().ToString();
            var stream = new MugenStreamingServiceStreamSession
            {
                StreamId = streamId,
                TournamentId = request.TournamentId,
                Title = request.Title ?? $"{tournament.Value.Name} - Live Tournament",
                Description = request.Description ?? $"Watch the {tournament.Value.Name} tournament live!",
                StreamerId = request.StreamerId,
                Platform = request.Platform,
                StreamUrl = GenerateStreamUrl(streamId, request.Platform),
                Status = MugenStreamingServiceStreamStatus.Starting,
                StartTime = _timeProvider.UtcNow,
                Viewers = 0,
                Quality = request.Quality,
                IsInteractive = request.EnableInteractiveFeatures
            };

            _activeStreams[streamId] = stream;

            // Initialize overlays and analytics
            await _overlayManager.InitializeTournamentOverlaysAsync(streamId, request.TournamentId, ct);
            await _analyticsEngine.StartTrackingAsync(streamId, ct);

            // Start stream on platform
            var platformResult = await StartPlatformStreamAsync(stream, ct);
            if (!platformResult.IsSuccess)
            {
                _activeStreams.Remove(streamId);
                return Result.Failure<MugenStreamingServiceStreamSession>($"Failed to start stream on {request.Platform}: {platformResult.Error}");
            }

            stream.Status = MugenStreamingServiceStreamStatus.Live;
            _logger.LogInformation("Tournament stream started: {StreamId}", streamId);

            return Result.Success<MugenStreamingServiceStreamSession>(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting tournament stream for {TournamentId}", request.TournamentId);
            return Result.Failure<MugenStreamingServiceStreamSession>($"Failed to start stream: {ex.Message}");
        }
    }

    public async Task<Result> SendStreamNotificationAsync(string streamId, string message, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Sending notification for stream {StreamId}", streamId);

            if (!_activeStreams.TryGetValue(streamId, out var stream))
            {
                return Result.Failure("Stream not found");
            }

            // Simulate sending a notification to viewers
            _logger.LogInformation("Notification sent to stream {StreamId}: {Message}", streamId, message);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stream notification for {StreamId}", streamId);
            return Result.Failure($"Failed to send notification: {ex.Message}");
        }
    }

    public async Task<Result> EndTournamentStreamAsync(string streamId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Ending tournament stream {StreamId}", streamId);

            if (!_activeStreams.TryGetValue(streamId, out var stream))
            {
                return Result.Failure("Stream not found");
            }

            // End platform stream
            await EndPlatformStreamAsync(stream, ct);

            // Finalize analytics and overlays
            await _analyticsEngine.FinalizeTrackingAsync(streamId, ct);
            await _overlayManager.CleanupOverlaysAsync(streamId, ct);

            // Archive stream data
            stream.Status = MugenStreamingServiceStreamStatus.Ended;
            stream.EndTime = _timeProvider.UtcNow;
            await ArchiveStreamDataAsync(stream, ct);

            _activeStreams.Remove(streamId);
            _logger.LogInformation("Tournament stream ended: {StreamId}", streamId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending tournament stream {StreamId}", streamId);
            return Result.Failure($"Failed to end stream: {ex.Message}");
        }
    }

    public async Task<Result<MugenStreamingServiceStreamAnalytics>> GetStreamAnalyticsAsync(string streamId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var analytics = await _analyticsEngine.GetAnalyticsAsync(streamId, ct);
            return Result.Success<MugenStreamingServiceStreamAnalytics>(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stream analytics for {StreamId}", streamId);
            return Result.Failure<MugenStreamingServiceStreamAnalytics>($"Failed to get analytics: {ex.Message}");
        }
    }

    public async Task<Result> UpdateStreamOverlaysAsync(string streamId, MugenStreamingServiceOverlayUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating overlays for stream {StreamId}", streamId);

            if (!_activeStreams.TryGetValue(streamId, out var stream))
            {
                return Result.Failure("Stream not found");
            }

            await _overlayManager.UpdateOverlaysAsync(streamId, request, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stream overlays for {StreamId}", streamId);
            return Result.Failure($"Failed to update overlays: {ex.Message}");
        }
    }

    public async Task<Result<MugenStreamingServiceTournamentBroadcast>> CreateTournamentBroadcastAsync(MugenStreamingServiceBroadcastRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating tournament broadcast for {TournamentId}", request.TournamentId);

            var broadcast = new MugenStreamingServiceTournamentBroadcast
            {
                BroadcastId = Guid.NewGuid().ToString(),
                TournamentId = request.TournamentId,
                Title = request.Title,
                Description = request.Description,
                ScheduledStart = request.ScheduledStart,
                EstimatedDuration = request.EstimatedDuration,
                Platforms = request.Platforms,
                ProductionTeam = request.ProductionTeam,
                Commentators = request.Commentators,
                Status = MugenStreamingServiceBroadcastStatus.Scheduled
            };

            _tournamentBroadcasts[broadcast.BroadcastId] = broadcast;

            // Schedule pre-production setup
            await ScheduleBroadcastPreparationAsync(broadcast, ct);

            _logger.LogInformation("Tournament broadcast created: {BroadcastId}", broadcast.BroadcastId);
            return Result.Success<MugenStreamingServiceTournamentBroadcast>(broadcast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tournament broadcast for {TournamentId}", request.TournamentId);
            return Result.Failure<MugenStreamingServiceTournamentBroadcast>($"Failed to create broadcast: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MugenStreamingServiceStreamSession>>> GetActiveStreamsAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var activeStreams = _activeStreams.Values
                .Where(s => s.Status == MugenStreamingServiceStreamStatus.Live)
                .ToList();

            return Result.Success<IReadOnlyList<MugenStreamingServiceStreamSession>>(activeStreams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active streams");
            return Result.Failure<IReadOnlyList<MugenStreamingServiceStreamSession>>($"Failed to get active streams: {ex.Message}");
        }
    }

    public async Task<Result> SendStreamInteractionAsync(string streamId, MugenStreamingServiceStreamInteraction interaction, CancellationToken ct = default)
    {
        try
        {
            if (!_activeStreams.TryGetValue(streamId, out var stream))
            {
                return Result.Failure("Stream not found");
            }

            if (!stream.IsInteractive)
            {
                return Result.Failure("Stream does not support interactive features");
            }

            // Process interaction (voting, predictions, etc.)
            await ProcessStreamInteractionAsync(streamId, interaction, ct);

            // Update overlays if needed
            if (interaction.Type == MugenStreamingServiceStreamingInteractionType.Prediction || interaction.Type == MugenStreamingServiceStreamingInteractionType.Vote)
            {
                await _overlayManager.UpdateInteractiveOverlayAsync(streamId, interaction, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stream interaction for {StreamId}", streamId);
            return Result.Failure($"Failed to process interaction: {ex.Message}");
        }
    }

    public async Task<Result<MugenStreamingServiceStreamSession>> GetStreamSessionAsync(string streamId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Retrieving stream session for {StreamId}", streamId);

            if (_activeStreams.TryGetValue(streamId, out var stream))
            {
                return Result.Success(stream);
            }
            else
            {
                // In a real scenario, you might also check an archive or database for past streams
                return Result.Failure<MugenStreamingServiceStreamSession>("Stream session not found or no longer active.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stream session for {StreamId}", streamId);
            return Result.Failure<MugenStreamingServiceStreamSession>($"Failed to retrieve stream session: {ex.Message}");
        }
    }

    public async Task<Result<MugenStreamingServiceStreamHighlights>> GenerateStreamHighlightsAsync(string streamId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating highlights for stream {StreamId}", streamId);

            var highlights = await _analyticsEngine.GenerateHighlightsAsync(streamId, ct);
            return Result.Success<MugenStreamingServiceStreamHighlights>(highlights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating highlights for {StreamId}", streamId);
            return Result.Failure<MugenStreamingServiceStreamHighlights>($"Failed to generate highlights: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<Result<MugenTournament>> ValidateTournamentAsync(string tournamentId, CancellationToken ct)
    {
        // Simplified - would check tournament repository
        return Result.Success<MugenTournament>(new MugenTournament(Guid.Parse(tournamentId), "Sample Tournament", _timeProvider));
    }

    private string GenerateStreamUrl(string streamId, MugenStreamingServiceStreamingPlatform platform)
    {
        return platform switch
        {
            MugenStreamingServiceStreamingPlatform.Twitch => $"https://twitch.tv/savestate_{streamId}",
            MugenStreamingServiceStreamingPlatform.YouTube => $"https://youtube.com/watch?v={streamId}",
            MugenStreamingServiceStreamingPlatform.Discord => $"https://discord.gg/stream/{streamId}",
            _ => $"https://savestate.gg/stream/{streamId}"
        };
    }

    private async Task<Result> StartPlatformStreamAsync(MugenStreamingServiceStreamSession stream, CancellationToken ct)
    {
        // Platform-specific stream initialization
        await Task.Delay(1000, ct); // Simulate platform API call
        return Result.Success();
    }

    private async Task<Result> EndPlatformStreamAsync(MugenStreamingServiceStreamSession stream, CancellationToken ct)
    {
        // Platform-specific stream termination
        await Task.Delay(500, ct); // Simulate platform API call
        return Result.Success();
    }

    private async Task ArchiveStreamDataAsync(MugenStreamingServiceStreamSession stream, CancellationToken ct)
    {
        // Archive stream metadata and analytics
        await Task.Delay(200, ct);
    }

    private async Task ScheduleBroadcastPreparationAsync(MugenStreamingServiceTournamentBroadcast broadcast, CancellationToken ct)
    {
        // Schedule pre-production tasks
        await Task.Delay(100, ct);
    }

    private async Task ProcessStreamInteractionAsync(string streamId, MugenStreamingServiceStreamInteraction interaction, CancellationToken ct)
    {
        // Process viewer interactions
        await _analyticsEngine.RecordInteractionAsync(streamId, interaction, ct);
    }

    #endregion
}

/// <summary>
/// Stream analytics engine for tracking viewer engagement and performance.
/// </summary>
public class MugenStreamingServiceStreamAnalyticsEngine
{
    private readonly ILogger<MugenStreamingServiceStreamAnalyticsEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MugenStreamingServiceStreamMetrics> _streamMetrics = new();

    public MugenStreamingServiceStreamAnalyticsEngine(ILogger<MugenStreamingServiceStreamAnalyticsEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task StartTrackingAsync(string streamId, CancellationToken ct = default)
    {
        _streamMetrics[streamId] = new MugenStreamingServiceStreamMetrics
        {
            StreamId = streamId,
            StartTime = _timeProvider.UtcNow,
            ViewerCount = 0,
            PeakViewers = 0,
            TotalInteractions = 0,
            EngagementRate = 0.0
        };
    }

    public async Task<MugenStreamingServiceStreamAnalytics> GetAnalyticsAsync(string streamId, CancellationToken ct = default)
    {
        if (!_streamMetrics.TryGetValue(streamId, out var metrics))
        {
            throw new InvalidOperationException("Stream metrics not found");
        }

        return new MugenStreamingServiceStreamAnalytics
        {
            StreamId = streamId,
            CurrentViewers = metrics.ViewerCount,
            PeakViewers = metrics.PeakViewers,
            AverageViewers = metrics.ViewerCount, // Simplified
            TotalInteractions = metrics.TotalInteractions,
            EngagementRate = metrics.EngagementRate,
            TopMoments = await GetTopMomentsAsync(streamId, ct),
            ViewerDemographics = GetViewerDemographics(),
            MugenStreamingServiceStreamHealth = GetStreamHealth(metrics)
        };
    }

    public async Task RecordInteractionAsync(string streamId, MugenStreamingServiceStreamInteraction interaction, CancellationToken ct = default)
    {
        if (_streamMetrics.TryGetValue(streamId, out var metrics))
        {
            metrics.TotalInteractions++;
            metrics.EngagementRate = Math.Min(1.0, metrics.EngagementRate + 0.01); // Simplified
        }
    }

    public async Task FinalizeTrackingAsync(string streamId, CancellationToken ct = default)
    {
        // Finalize and archive metrics
        if (_streamMetrics.TryGetValue(streamId, out var metrics))
        {
            metrics.EndTime = _timeProvider.UtcNow;
        }
    }

    public async Task<MugenStreamingServiceStreamHighlights> GenerateHighlightsAsync(string streamId, CancellationToken ct = default)
    {
        return new MugenStreamingServiceStreamHighlights
        {
            StreamId = streamId,
            Highlights = new List<MugenStreamingServiceHighlightClip>
            {
                new MugenStreamingServiceHighlightClip { Title = "Tournament Opening", Timestamp = TimeSpan.FromMinutes(0), Views = 150 },
                new MugenStreamingServiceHighlightClip { Title = "Epic Comeback", Timestamp = TimeSpan.FromMinutes(25), Views = 300 },
                new MugenStreamingServiceHighlightClip { Title = "Final Match", Timestamp = TimeSpan.FromMinutes(45), Views = 500 }
            },
            TotalHighlights = 3,
            GeneratedAt = _timeProvider.UtcNow
        };
    }

    private async Task<IReadOnlyList<MugenStreamingServiceHighlightMoment>> GetTopMomentsAsync(string streamId, CancellationToken ct)
    {
        return new List<MugenStreamingServiceHighlightMoment>
        {
            new MugenStreamingServiceHighlightMoment { Description = "High damage combo", Timestamp = TimeSpan.FromMinutes(12), Engagement = 85 },
            new MugenStreamingServiceHighlightMoment { Description = "Perfect victory", Timestamp = TimeSpan.FromMinutes(28), Engagement = 92 },
            new MugenStreamingServiceHighlightMoment { Description = "Crowd favorite upset", Timestamp = TimeSpan.FromMinutes(41), Engagement = 78 }
        };
    }

    private IReadOnlyDictionary<string, int> GetViewerDemographics()
    {
        return new Dictionary<string, int>
        {
            ["18-24"] = 35,
            ["25-34"] = 45,
            ["35-44"] = 15,
            ["45+"] = 5
        };
    }

    private MugenStreamingServiceStreamHealth GetStreamHealth(MugenStreamingServiceStreamMetrics metrics)
    {
        var health = MugenStreamingServiceStreamHealth.Excellent;

        if (metrics.ViewerCount < 10) health = MugenStreamingServiceStreamHealth.Poor;
        else if (metrics.ViewerCount < 50) health = MugenStreamingServiceStreamHealth.Fair;
        else if (metrics.ViewerCount < 100) health = MugenStreamingServiceStreamHealth.Good;

        return health;
    }
}

/// <summary>
/// Overlay manager for stream graphics and interactive elements.
/// </summary>
public class MugenStreamingServiceOverlayManager
{
    private readonly ILogger<MugenStreamingServiceOverlayManager> _logger;
    private readonly Dictionary<string, MugenStreamingServiceStreamOverlay> _activeOverlays = new();

    public MugenStreamingServiceOverlayManager(ILogger<MugenStreamingServiceOverlayManager> logger)
    {
        _logger = logger;
    }

    public async Task InitializeTournamentOverlaysAsync(string streamId, string tournamentId, CancellationToken ct)
    {
        var overlay = new MugenStreamingServiceStreamOverlay
        {
            StreamId = streamId,
            TournamentId = tournamentId,
            Elements = new List<MugenStreamingServiceOverlayElement>
            {
                new MugenStreamingServiceOverlayElement { Type = MugenStreamingServiceOverlayType.Scoreboard, MugenStreamingServicePosition = new MugenStreamingServicePosition(10, 10), Visible = true },
                new MugenStreamingServiceOverlayElement { Type = MugenStreamingServiceOverlayType.Timer, MugenStreamingServicePosition = new MugenStreamingServicePosition(400, 10), Visible = true },
                new MugenStreamingServiceOverlayElement { Type = MugenStreamingServiceOverlayType.HealthBars, MugenStreamingServicePosition = new MugenStreamingServicePosition(10, 50), Visible = true },
                new MugenStreamingServiceOverlayElement { Type = MugenStreamingServiceOverlayType.Chat, MugenStreamingServicePosition = new MugenStreamingServicePosition(600, 300), Visible = true }
            }
        };

        _activeOverlays[streamId] = overlay;
    }

    public async Task UpdateOverlaysAsync(string streamId, MugenStreamingServiceOverlayUpdateRequest request, CancellationToken ct)
    {
        if (_activeOverlays.TryGetValue(streamId, out var overlay))
        {
            foreach (var update in request.Updates)
            {
                var element = overlay.Elements.FirstOrDefault(e => e.Type == update.ElementType);
                if (element != null)
                {
                    element.Visible = update.Visible;
                    element.MugenStreamingServicePosition = update.MugenStreamingServicePosition ?? element.MugenStreamingServicePosition;
                    element.Content = update.Content ?? element.Content;
                }
            }
        }
    }

    public async Task UpdateInteractiveOverlayAsync(string streamId, MugenStreamingServiceStreamInteraction interaction, CancellationToken ct)
    {
        // Update interactive elements based on viewer interactions
    }

    public async Task CleanupOverlaysAsync(string streamId, CancellationToken ct)
    {
        _activeOverlays.Remove(streamId);
    }
}

/// <summary>
/// MUGEN Streaming service interface.
/// </summary>
public interface MugenStreamingServiceIMugenStreamingService
{
    Task<Result<MugenStreamingServiceStreamSession>> StartTournamentStreamAsync(MugenStreamingServiceStreamRequest request, CancellationToken ct = default);
    Task<Result> EndTournamentStreamAsync(string streamId, CancellationToken ct = default);
    Task<Result<MugenStreamingServiceStreamAnalytics>> GetStreamAnalyticsAsync(string streamId, CancellationToken ct = default);
    Task<Result> UpdateStreamOverlaysAsync(string streamId, MugenStreamingServiceOverlayUpdateRequest request, CancellationToken ct = default);
    Task<Result<MugenStreamingServiceTournamentBroadcast>> CreateTournamentBroadcastAsync(MugenStreamingServiceBroadcastRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MugenStreamingServiceStreamSession>>> GetActiveStreamsAsync(CancellationToken ct = default);
    Task<Result> SendStreamInteractionAsync(string streamId, MugenStreamingServiceStreamInteraction interaction, CancellationToken ct = default);
    Task<Result<MugenStreamingServiceStreamHighlights>> GenerateStreamHighlightsAsync(string streamId, CancellationToken ct = default);
}

/// <summary>
/// Stream session data.
/// </summary>
public class MugenStreamingServiceStreamSession
{
    public string StreamId { get; set; } = default!;
    public string TournamentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string StreamerId { get; set; } = default!;
    public MugenStreamingServiceStreamingPlatform Platform { get; set; } = default!;
    public string StreamUrl { get; set; } = default!;
    public MugenStreamingServiceStreamStatus Status { get; set; } = default!;
    public DateTime StartTime { get; set; } = default!;
    public int Viewers { get; set; } = default!;
    public MugenStreamingServiceStreamQuality Quality { get; set; } = default!;
    public bool IsInteractive { get; set; } = default!;
    public DateTime? EndTime { get; set; } = default!;
}

/// <summary>
/// Stream request.
/// </summary>
public class MugenStreamingServiceStreamRequest
{
    public string TournamentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string StreamerId { get; set; } = default!;
    public MugenStreamingServiceStreamingPlatform Platform { get; set; } = default!;
    public MugenStreamingServiceStreamQuality Quality { get; set; } = default!;
    public bool EnableInteractiveFeatures { get; set; } = default!;
}

/// <summary>
/// Streaming platform enumeration.
/// </summary>
public enum MugenStreamingServiceStreamingPlatform
{
    Twitch,
    YouTube,
    Discord,
    Custom
}

/// <summary>
/// Stream status enumeration.
/// </summary>
public enum MugenStreamingServiceStreamStatus
{
    Starting,
    Live,
    Paused,
    Ended
}

/// <summary>
/// Stream quality enumeration.
/// </summary>
public enum MugenStreamingServiceStreamQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Stream analytics data.
/// </summary>
public class MugenStreamingServiceStreamAnalytics
{
    public string StreamId { get; set; } = default!;
    public int CurrentViewers { get; set; } = default!;
    public int PeakViewers { get; set; } = default!;
    public double AverageViewers { get; set; } = default!;
    public int TotalInteractions { get; set; } = default!;
    public double EngagementRate { get; set; } = default!;
    public IReadOnlyList<MugenStreamingServiceHighlightMoment> TopMoments { get; set; } = default!;
    public IReadOnlyDictionary<string , int> ViewerDemographics { get; set; } = default!;
    public MugenStreamingServiceStreamHealth MugenStreamingServiceStreamHealth { get; set; } = default!;
}

/// <summary>
/// Stream health enumeration.
/// </summary>
public enum MugenStreamingServiceStreamHealth
{
    Poor,
    Fair,
    Good,
    Excellent
}

/// <summary>
/// Highlight moment data.
/// </summary>
public class MugenStreamingServiceHighlightMoment
{
    public string Description { get; set; } = default!;
    public TimeSpan Timestamp { get; set; } = default!;
    public double Engagement { get; set; } = default!;
}

/// <summary>
/// Stream highlights data.
/// </summary>
public class MugenStreamingServiceStreamHighlights
{
    public string StreamId { get; set; } = default!;
    public IReadOnlyList<MugenStreamingServiceHighlightClip> Highlights { get; set; } = default!;
    public int TotalHighlights { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Highlight clip data.
/// </summary>
public class MugenStreamingServiceHighlightClip
{
    public string Title { get; set; } = default!;
    public TimeSpan Timestamp { get; set; } = default!;
    public int Views { get; set; } = default!;
}

/// <summary>
/// Overlay update request.
/// </summary>
public class MugenStreamingServiceOverlayUpdateRequest
{
    public IReadOnlyList<MugenStreamingServiceOverlayUpdate> Updates { get; set; } = default!;
}

/// <summary>
/// Overlay update data.
/// </summary>
public class MugenStreamingServiceOverlayUpdate
{
    public MugenStreamingServiceOverlayType ElementType { get; set; } = default!;
    public bool Visible { get; set; } = default!;
    public MugenStreamingServicePosition? MugenStreamingServicePosition { get; set; } = default!;
    public string? Content { get; set; } = default!;
}

/// <summary>
/// Overlay type enumeration.
/// </summary>
public enum MugenStreamingServiceOverlayType
{
    Scoreboard,
    Timer,
    HealthBars,
    Chat,
    Predictions,
    Polls,
    SponsorBanner
}

/// <summary>
/// Tournament broadcast data.
/// </summary>
public class MugenStreamingServiceTournamentBroadcast
{
    public string BroadcastId { get; set; } = default!;
    public string TournamentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime ScheduledStart { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<MugenStreamingServiceStreamingPlatform> Platforms { get; set; } = default!;
    public IReadOnlyList<string> ProductionTeam { get; set; } = default!;
    public IReadOnlyList<string> Commentators { get; set; } = default!;
    public MugenStreamingServiceBroadcastStatus Status { get; set; } = default!;
}

/// <summary>
/// Broadcast status enumeration.
/// </summary>
public enum MugenStreamingServiceBroadcastStatus
{
    Scheduled,
    Preparing,
    Live,
    Completed,
    Cancelled
}

/// <summary>
/// Broadcast request.
/// </summary>
public class MugenStreamingServiceBroadcastRequest
{
    public string TournamentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime ScheduledStart { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<MugenStreamingServiceStreamingPlatform> Platforms { get; set; } = default!;
    public IReadOnlyList<string> ProductionTeam { get; set; } = default!;
    public IReadOnlyList<string> Commentators { get; set; } = default!;
}

/// <summary>
/// Stream interaction data.
/// </summary>
public class MugenStreamingServiceStreamInteraction
{
    public string UserId { get; set; } = default!;
    public MugenStreamingServiceStreamingInteractionType Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Interaction type enumeration.
/// </summary>
public enum MugenStreamingServiceStreamingInteractionType
{
    Chat,
    Prediction,
    Vote,
    Donation,
    Subscription
}

/// <summary>
/// Stream metrics data.
/// </summary>
public class MugenStreamingServiceStreamMetrics
{
    public string StreamId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ViewerCount { get; set; }
    public int PeakViewers { get; set; }
    public int TotalInteractions { get; set; }
    public double EngagementRate { get; set; }
}

/// <summary>
/// Stream overlay data.
/// </summary>
public class MugenStreamingServiceStreamOverlay
{
    public string StreamId { get; set; } = string.Empty;
    public string TournamentId { get; set; } = string.Empty;
    public IReadOnlyList<MugenStreamingServiceOverlayElement> Elements { get; set; } = new List<MugenStreamingServiceOverlayElement>();
}

/// <summary>
/// Overlay element data.
/// </summary>
public class MugenStreamingServiceOverlayElement
{
    public MugenStreamingServiceOverlayType Type { get; set; }
    public MugenStreamingServicePosition MugenStreamingServicePosition { get; set; } = new MugenStreamingServicePosition(0, 0);
    public bool Visible { get; set; }
    public string? Content { get; set; }
}

/// <summary>
/// MugenStreamingServicePosition data.
/// </summary>
public class MugenStreamingServicePosition
{
    public int X { get; set; } = default!;
    public int Y { get; set; } = default!;

    public MugenStreamingServicePosition(int x, int y)
    {
        X = x;
        Y = y;
    }
}
