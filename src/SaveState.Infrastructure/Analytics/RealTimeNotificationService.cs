using System;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Analytics.Services;

namespace SaveState.Infrastructure.Analytics;

public class RealTimeNotificationService : IRealTimeNotificationService
{
    public event EventHandler<AnalyticsUpdatedEventArgs>? OnAnalyticsUpdated;

    public Task NotifyAnalyticsUpdatedAsync(string source, CancellationToken ct = default)
    {
        OnAnalyticsUpdated?.Invoke(this, new AnalyticsUpdatedEventArgs { Source = source });
        return Task.CompletedTask;
    }
}
