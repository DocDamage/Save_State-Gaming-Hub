using SaveState.Application.Mugen.Services.MobileCompanion.Engines;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Mobile companion service providing remote control, real-time statistics,
/// and mobile-first features for MUGEN players on the go.
/// </summary>
public class MobileCompanionService : IMobileCompanionService
{
    private readonly ILogger<MobileCompanionService> _logger;
    private readonly ICacheService _cache;
    private readonly Dictionary<string, MobileCompanionServiceMobileSession> _activeSessions = new();
    private readonly Dictionary<string, MobileCompanionServiceCompanionDevice> _registeredDevices = new();

    // Extracted engines
    private readonly RemoteControlEngine _remoteControlEngine;
    private readonly DataStreamingEngine _dataStreamingEngine;
    private readonly NotificationEngine _notificationEngine;
    private readonly DeviceSyncEngine _deviceSyncEngine;
    private readonly CompanionUiEngine _companionUiEngine;
    private readonly AnalyticsEngine _analyticsEngine;

    public MobileCompanionService(
        ILogger<MobileCompanionService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
        _remoteControlEngine = new RemoteControlEngine(loggerFactory.CreateLogger<RemoteControlEngine>());
        _dataStreamingEngine = new DataStreamingEngine(loggerFactory.CreateLogger<DataStreamingEngine>());
        _notificationEngine = new NotificationEngine(loggerFactory.CreateLogger<NotificationEngine>());
        _deviceSyncEngine = new DeviceSyncEngine(loggerFactory.CreateLogger<DeviceSyncEngine>());
        _companionUiEngine = new CompanionUiEngine(loggerFactory.CreateLogger<CompanionUiEngine>());
        _analyticsEngine = new AnalyticsEngine(loggerFactory.CreateLogger<AnalyticsEngine>());
    }

    /// <inheritdoc />
    public async Task<Result<MobileCompanionServiceMobileSession>> InitializeSessionAsync(
        MobileCompanionServiceDeviceRegistrationRequest request,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Initializing mobile session for device {DeviceId}", request.DeviceId);

            if (!await ValidateDeviceAsync(request, ct))
                return Result.Failure<MobileCompanionServiceMobileSession>("Device validation failed");

            var session = CreateSession(request);
            _activeSessions[session.SessionId] = session;
            RegisterDeviceIfNew(request);

            _logger.LogInformation("Mobile session initialized: {SessionId}", session.SessionId);
            return Result.Success(session);
        }, request.DeviceId, "Session initialization failed");
    }

    /// <inheritdoc />
    public async Task<Result> SendRemoteCommandAsync(
        string sessionId,
        MobileCompanionServiceRemoteCommand command,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
                return Result.Failure("Session not found");

            if (!session.Features.RemoteControl)
                return Result.Failure("Remote control not enabled for this session");

            _logger.LogInformation("Processing remote command {CommandType} for session {SessionId}",
                command.MobileCompanionServiceCommandType, sessionId);

            var result = await _remoteControlEngine.ExecuteCommandAsync(session, command, ct);
            if (!result.IsSuccess)
                return Result.Failure(result.Error);

            session.LastActivity = DateTime.UtcNow;
            _logger.LogInformation("Remote command executed successfully");
            return Result.Success();
        }, sessionId, "Remote command failed");
    }

    /// <inheritdoc />
    public async Task<Result<MobileCompanionServiceMobileDashboard>> GetDashboardAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
                return Result.Failure<MobileCompanionServiceMobileDashboard>("Session not found");

            _logger.LogInformation("Generating mobile dashboard for session {SessionId}", sessionId);

            var dashboard = new MobileCompanionServiceMobileDashboard
            {
                SessionId = sessionId,
                UserId = session.UserId,
                GeneratedAt = DateTime.UtcNow,
                QuickActions = await _companionUiEngine.GetQuickActionsAsync(session, ct),
                LiveStats = session.Features.RealTimeStats
                    ? await _dataStreamingEngine.GetLiveStatsAsync(session.UserId, ct) : null,
                RecentActivity = await _companionUiEngine.GetRecentActivityAsync(session.UserId, ct),
                Notifications = session.Features.Notifications
                    ? await _notificationEngine.GetPendingNotificationsAsync(session.UserId, ct)
                    : new List<MobileCompanionServiceMobileNotification>(),
                SocialFeed = session.Features.SocialFeatures
                    ? await _companionUiEngine.GetSocialFeedAsync(session.UserId, ct)
                    : new List<MobileCompanionServiceSocialActivity>(),
                ContentQueue = session.Features.ContentManagement
                    ? await _companionUiEngine.GetContentQueueAsync(session.UserId, ct)
                    : new List<MobileCompanionServiceContentItem>()
            };

            _logger.LogInformation("Mobile dashboard generated successfully");
            return Result.Success(dashboard);
        }, sessionId, "Dashboard generation failed");
    }

    /// <inheritdoc />
    public async Task<Result<MobileCompanionServiceLiveMatchData>> GetLiveMatchDataAsync(
        string sessionId,
        string matchId,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
                return Result.Failure<MobileCompanionServiceLiveMatchData>("Session not found");

            if (!session.Features.RealTimeStats)
                return Result.Failure<MobileCompanionServiceLiveMatchData>("Real-time stats not enabled for this session");

            _logger.LogInformation("Retrieving live match data for match {MatchId}", matchId);
            var liveData = await _dataStreamingEngine.GetLiveMatchDataAsync(matchId, ct);

            _logger.LogInformation("Live match data retrieved successfully");
            return Result.Success(liveData);
        }, sessionId, "Live data retrieval failed");
    }

    /// <inheritdoc />
    public async Task<Result> SendPushNotificationAsync(
        string deviceId,
        MobileCompanionServicePushNotification notification,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_registeredDevices.TryGetValue(deviceId, out var device))
                return Result.Failure("Device not found");

            _logger.LogInformation("Sending push notification to device {DeviceId}: {Title}", deviceId, notification.Title);

            await _notificationEngine.SendPlatformNotificationAsync(device, notification, ct);
            await _notificationEngine.LogNotificationAsync(device.UserId, notification, ct);

            _logger.LogInformation("Push notification sent successfully");
            return Result.Success();
        }, deviceId, "Push notification failed");
    }

    /// <inheritdoc />
    public async Task<Result<MobileCompanionServiceDeviceSyncData>> SynchronizeDeviceAsync(
        string sessionId,
        MobileCompanionServiceDeviceSyncRequest request,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
                return Result.Failure<MobileCompanionServiceDeviceSyncData>("Session not found");

            _logger.LogInformation("Synchronizing device for session {SessionId}", sessionId);

            var syncData = new MobileCompanionServiceDeviceSyncData
            {
                SessionId = sessionId,
                UserId = session.UserId,
                LastSyncTimestamp = DateTime.UtcNow,
                SettingsSync = await _deviceSyncEngine.SynchronizeSettingsAsync(session.UserId, request.Settings, ct),
                ProgressSync = await _deviceSyncEngine.SynchronizeProgressAsync(session.UserId, request.ProgressData, ct),
                AchievementsSync = await _deviceSyncEngine.SynchronizeAchievementsAsync(session.UserId, request.Achievements, ct),
                FriendsSync = session.Features.SocialFeatures
                    ? await _deviceSyncEngine.SynchronizeFriendsAsync(session.UserId, request.FriendsData, ct) : null,
                ContentSync = session.Features.ContentManagement
                    ? await _deviceSyncEngine.SynchronizeContentAsync(session.UserId, request.ContentData, ct) : null
            };

            UpdateDeviceLastSeen(session.DeviceId);
            _logger.LogInformation("Device synchronization completed");
            return Result.Success(syncData);
        }, sessionId, "Synchronization failed");
    }

    /// <inheritdoc />
    public async Task<Result<MobileCompanionServiceMobileAnalytics>> GetMobileAnalyticsAsync(
        string sessionId,
        TimeSpan period,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
                return Result.Failure<MobileCompanionServiceMobileAnalytics>("Session not found");

            _logger.LogInformation("Generating mobile analytics for session {SessionId}", sessionId);
            var analytics = await _analyticsEngine.GenerateMobileAnalyticsAsync(session.UserId, period, ct);

            _logger.LogInformation("Mobile analytics generated successfully");
            return Result.Success(analytics);
        }, sessionId, "Analytics generation failed");
    }

    /// <inheritdoc />
    public async Task<Result> EndSessionAsync(string sessionId, CancellationToken ct = default)
    {
        return await ExecuteAsync(async () =>
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
                return Result.Failure("Session not found");

            _logger.LogInformation("Ending mobile session {SessionId}", sessionId);
            await CleanupSessionAsync(session, ct);
            _activeSessions.Remove(sessionId);

            _logger.LogInformation("Mobile session ended successfully");
            return Result.Success();
        }, sessionId, "Session cleanup failed");
    }

    #region Private Methods

    private async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> action, string context, string errorPrefix)
    {
        try { return await action(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorPrefix} for {Context}", errorPrefix, context);
            return Result.Failure<T>($"{errorPrefix}: {ex.Message}");
        }
    }

    private async Task<Result> ExecuteAsync(Func<Task<Result>> action, string context, string errorPrefix)
    {
        try { return await action(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorPrefix} for {Context}", errorPrefix, context);
            return Result.Failure($"{errorPrefix}: {ex.Message}");
        }
    }

    private static Task<bool> ValidateDeviceAsync(MobileCompanionServiceDeviceRegistrationRequest request, CancellationToken ct)
    {
        return Task.FromResult(
            !string.IsNullOrEmpty(request.DeviceId) &&
            !string.IsNullOrEmpty(request.UserId) &&
            Enum.IsDefined(typeof(MobileCompanionServiceMobilePlatform), request.Platform));
    }

    private static MobileCompanionServiceMobileSession CreateSession(MobileCompanionServiceDeviceRegistrationRequest request)
    {
        return new MobileCompanionServiceMobileSession
        {
            SessionId = Guid.NewGuid().ToString(),
            DeviceId = request.DeviceId,
            UserId = request.UserId,
            Platform = request.Platform,
            AppVersion = request.AppVersion,
            Permissions = request.RequestedPermissions,
            Status = MobileCompanionServiceSessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            Features = new MobileCompanionServiceSessionFeatures
            {
                RemoteControl = request.RequestedPermissions.Contains(MobileCompanionServicePermission.RemoteControl),
                RealTimeStats = request.RequestedPermissions.Contains(MobileCompanionServicePermission.RealTimeStats),
                Notifications = request.RequestedPermissions.Contains(MobileCompanionServicePermission.Notifications),
                SocialFeatures = request.RequestedPermissions.Contains(MobileCompanionServicePermission.SocialFeatures),
                ContentManagement = request.RequestedPermissions.Contains(MobileCompanionServicePermission.ContentManagement)
            }
        };
    }

    private void RegisterDeviceIfNew(MobileCompanionServiceDeviceRegistrationRequest request)
    {
        if (_registeredDevices.ContainsKey(request.DeviceId)) return;
        _registeredDevices[request.DeviceId] = new MobileCompanionServiceCompanionDevice
        {
            DeviceId = request.DeviceId,
            UserId = request.UserId,
            Platform = request.Platform,
            DeviceName = request.DeviceName,
            PushToken = request.PushToken,
            RegisteredAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };
    }

    private void UpdateDeviceLastSeen(string deviceId)
    {
        if (_registeredDevices.TryGetValue(deviceId, out var device))
            device.LastSeen = DateTime.UtcNow;
    }

    private static async Task CleanupSessionAsync(MobileCompanionServiceMobileSession session, CancellationToken ct)
    {
        await Task.Delay(50, ct);
    }

    #endregion
}
