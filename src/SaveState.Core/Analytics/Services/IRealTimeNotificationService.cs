using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Analytics.Services;

/// <summary>
/// Service for handling real-time notifications for analytics updates.
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Event triggered when analytics data changes.
    /// </summary>
    event EventHandler<AnalyticsUpdatedEventArgs> OnAnalyticsUpdated;

    /// <summary>
    /// Notifies subscribers that analytics data has been updated.
    /// </summary>
    /// <param name="source">The source of the update (e.g., "Session", "Backlog").</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyAnalyticsUpdatedAsync(string source, CancellationToken ct = default);
}

/// <summary>
/// Event arguments for analytics updates.
/// </summary>
public class AnalyticsUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// The source of the update.
    /// </summary>
    public required string Source { get; init; }
}
