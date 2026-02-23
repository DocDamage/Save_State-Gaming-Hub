using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;
using QualityLevel = SaveState.Core.Sync.Services.DTOs.QualityLevel;
using QualityChangeType = SaveState.Core.Sync.Services.DTOs.QualityChangeType;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of INetworkQualityMonitor for integration testing.
/// Provides extended methods that the real implementation doesn't have yet.
/// </summary>
public class FakeNetworkQualityMonitor : INetworkQualityMonitor
{
    private readonly ILogger<FakeNetworkQualityMonitor> _logger;
    private readonly ITimeProvider _timeProvider;

    private bool _isMonitoring;
    private NetworkQuality _lastQuality;
    private readonly List<NetworkQuality> _qualityHistory = new();

    public FakeNetworkQualityMonitor(
        ILogger<FakeNetworkQualityMonitor> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;

        _lastQuality = CreateDefaultQuality();
    }

    private NetworkQuality CreateDefaultQuality()
    {
        return new NetworkQuality(
            LatencyMs: 20,
            JitterMs: 2,
            PacketLossPercent: 0,
            BandwidthMbps: 100,
            Level: QualityLevel.Good,
            MeasuredAt: _timeProvider.UtcNow);
    }

    #region INetworkQualityMonitor Implementation

    public Task<Result<NetworkQualityTestResult>> PerformQualityTestAsync(CancellationToken ct = default)
    {
        var quality = CreateDefaultQuality();
        var result = new NetworkQualityTestResult(
            CurrentQuality: quality,
            PingTests: new List<PingTestResult>(),
            SpeedTests: new List<SpeedTestResult>(),
            Recommendations: new List<string> { "Network quality is good" },
            TestCompletedAt: _timeProvider.UtcNow);

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<NetworkQuality>> GetCurrentQualityAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(_lastQuality));
    }

    public Task<Result> StartMonitoringAsync(TimeSpan interval, CancellationToken ct = default)
    {
        _isMonitoring = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        _isMonitoring = false;
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<NetworkQuality>>> GetQualityHistoryAsync(DateTime startTime, DateTime endTime, CancellationToken ct = default)
    {
        var history = _qualityHistory
            .Where(q => q.MeasuredAt >= startTime && q.MeasuredAt <= endTime)
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<NetworkQuality>>(history));
    }

    public Task<Result<bool>> IsQualitySufficientForCloudGamingAsync(CloudGamingProvider provider, CancellationToken ct = default)
    {
        var isSufficient = _lastQuality.LatencyMs < 50 && _lastQuality.BandwidthMbps >= 25;
        return Task.FromResult(Result.Success(isSufficient));
    }

    public Task<Result<NetworkDiagnostics>> GetNetworkDiagnosticsAsync(CancellationToken ct = default)
    {
        var diagnostics = new NetworkDiagnostics(
            PublicIpAddress: "203.0.113.1",
            LocalIpAddress: "192.168.1.100",
            DnsServers: "8.8.8.8, 8.8.4.4",
            Gateway: "192.168.1.1",
            SubnetMask: "255.255.255.0",
            NetworkAdapter: "Ethernet",
            IsVpnActive: false,
            VpnProvider: null,
            OpenPorts: new List<int> { 80, 443 });

        return Task.FromResult(Result.Success(diagnostics));
    }

    public event EventHandler<NetworkQualityChangedEventArgs>? NetworkQualityChanged;

    public bool IsMonitoring => _isMonitoring;

    #endregion

    #region Extended Methods for Tests

    public Task<Result<NetworkQualityAssessment>> MeasureNetworkQualityAsync(CancellationToken ct = default)
    {
        var assessment = new NetworkQualityAssessment
        {
            Quality = _lastQuality,
            Timestamp = _timeProvider.UtcNow,
            IsSuitableForCloudGaming = true
        };

        // Trigger event
        NetworkQualityChanged?.Invoke(this, new NetworkQualityChangedEventArgs
        {
            PreviousQuality = _lastQuality,
            CurrentQuality = _lastQuality,
            ChangeType = QualityChangeType.None
        });

        return Task.FromResult(Result.Success(assessment));
    }

    public Task<Result<NetworkStatus>> GetNetworkStatusAsync(CancellationToken ct = default)
    {
        var status = new NetworkStatus
        {
            IsConnected = true,
            ConnectionType = "Ethernet",
            Quality = _lastQuality,
            Timestamp = _timeProvider.UtcNow
        };

        return Task.FromResult(Result.Success(status));
    }

    public Task<Result<bool>> IsNetworkSuitableForCloudGamingAsync(CancellationToken ct = default)
    {
        var isSuitable = _lastQuality.LatencyMs < 50 && _lastQuality.BandwidthMbps >= 25;
        return Task.FromResult(Result.Success(isSuitable));
    }

    public Task<Result<IReadOnlyList<string>>> GetRecommendationsAsync(CancellationToken ct = default)
    {
        var recommendations = new List<string>
        {
            "Network quality is suitable for cloud gaming",
            "Use wired connection for best experience"
        };

        return Task.FromResult(Result.Success<IReadOnlyList<string>>(recommendations));
    }

    #endregion

    #region Helper Methods

    public void SimulateQualityChange(NetworkQuality newQuality)
    {
        var oldQuality = _lastQuality;
        _lastQuality = newQuality;
        _qualityHistory.Add(newQuality);

        NetworkQualityChanged?.Invoke(this, new NetworkQualityChangedEventArgs
        {
            PreviousQuality = oldQuality,
            CurrentQuality = newQuality,
            ChangeType = DetermineChangeType(oldQuality, newQuality)
        });
    }

    private static QualityChangeType DetermineChangeType(NetworkQuality old, NetworkQuality current)
    {
        if (current.Level > old.Level)
            return QualityChangeType.Improved;
        if (current.Level < old.Level)
            return QualityChangeType.Degraded;
        return QualityChangeType.None;
    }

    #endregion
}

#region Supporting Types

public class NetworkQualityAssessment
{
    public NetworkQuality Quality { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public bool IsSuitableForCloudGaming { get; set; }
}

public class NetworkStatus
{
    public bool IsConnected { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    public NetworkQuality Quality { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}

public enum QualityChangeType
{
    None,
    Improved,
    Degraded
}

#endregion
