using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Interface for mobile companion service providing remote control, real-time statistics,
/// and mobile-first features for MUGEN players on the go.
/// </summary>
public interface IMobileCompanionService
{
    /// <summary>
    /// Initializes a new mobile session.
    /// </summary>
    Task<Result<MobileCompanionServiceMobileSession>> InitializeSessionAsync(
        MobileCompanionServiceDeviceRegistrationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a remote command to the MUGEN application.
    /// </summary>
    Task<Result> SendRemoteCommandAsync(
        string sessionId,
        MobileCompanionServiceRemoteCommand command,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the mobile dashboard data.
    /// </summary>
    Task<Result<MobileCompanionServiceMobileDashboard>> GetDashboardAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets live match data.
    /// </summary>
    Task<Result<MobileCompanionServiceLiveMatchData>> GetLiveMatchDataAsync(
        string sessionId,
        string matchId,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a push notification to a device.
    /// </summary>
    Task<Result> SendPushNotificationAsync(
        string deviceId,
        MobileCompanionServicePushNotification notification,
        CancellationToken ct = default);

    /// <summary>
    /// Synchronizes device data.
    /// </summary>
    Task<Result<MobileCompanionServiceDeviceSyncData>> SynchronizeDeviceAsync(
        string sessionId,
        MobileCompanionServiceDeviceSyncRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets mobile analytics for a user.
    /// </summary>
    Task<Result<MobileCompanionServiceMobileAnalytics>> GetMobileAnalyticsAsync(
        string sessionId,
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Ends a mobile session.
    /// </summary>
    Task<Result> EndSessionAsync(
        string sessionId,
        CancellationToken ct = default);
}
