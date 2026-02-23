using SaveState.Core.Common;

namespace SaveState.IntegrationTests;

/// <summary>
/// Fake implementation of INetworkQualityMonitor for integration tests.
/// Matches the interface defined in CloudGamingIntegrationTests.cs.
/// </summary>
public class FakeNetworkQualityMonitor : INetworkQualityMonitor
{
    private NetworkQuality _currentQuality;
    private NetworkStatus _currentStatus;

    public event EventHandler<NetworkQualityChangedEventArgs>? NetworkQualityChanged;

    public FakeNetworkQualityMonitor()
    {
        _currentQuality = new NetworkQuality
        {
            DownloadSpeedKbps = 50000,
            UploadSpeedKbps = 10000,
            LatencyMs = 20,
            PacketLoss = 0.5,
            JitterMs = 2,
            Grade = NetworkGrade.Good
        };

        _currentStatus = new NetworkStatus
        {
            IsConnected = true,
            CurrentNetworkType = "Ethernet",
            CurrentQuality = _currentQuality
        };
    }

    public Task<Result<NetworkQuality>> MeasureNetworkQualityAsync()
    {
        // Simulate slight variations in network quality
        var random = new Random();
        var newQuality = new NetworkQuality
        {
            DownloadSpeedKbps = _currentQuality.DownloadSpeedKbps + random.Next(-1000, 1000),
            UploadSpeedKbps = _currentQuality.UploadSpeedKbps + random.Next(-500, 500),
            LatencyMs = _currentQuality.LatencyMs + random.Next(-2, 3),
            PacketLoss = Math.Max(0, _currentQuality.PacketLoss + random.NextDouble() * 0.1 - 0.05),
            JitterMs = Math.Max(0, _currentQuality.JitterMs + random.NextDouble() * 0.5 - 0.25),
            Grade = _currentQuality.Grade
        };

        // Determine grade based on metrics
        newQuality.Grade = DetermineGrade(newQuality);

        // Raise event if quality changed significantly
        if (Math.Abs(newQuality.LatencyMs - _currentQuality.LatencyMs) > 5)
        {
            NetworkQualityChanged?.Invoke(this, new NetworkQualityChangedEventArgs
            {
                NewQuality = newQuality,
                PreviousQuality = _currentQuality,
                Timestamp = DateTime.UtcNow
            });
        }

        _currentQuality = newQuality;
        _currentStatus.CurrentQuality = newQuality;

        return Task.FromResult(Result.Success(newQuality));
    }

    public Task<Result<NetworkStatus>> GetNetworkStatusAsync()
    {
        return Task.FromResult(Result.Success(_currentStatus));
    }

    public Task<Result<bool>> IsNetworkSuitableForCloudGamingAsync()
    {
        var isSuitable = _currentQuality.Grade is NetworkGrade.Excellent or NetworkGrade.Good or NetworkGrade.Fair;
        return Task.FromResult(Result.Success(isSuitable));
    }

    public Task<Result<List<NetworkRecommendation>>> GetRecommendationsAsync()
    {
        var recommendations = new List<NetworkRecommendation>();

        if (_currentQuality.DownloadSpeedKbps < 15000)
        {
            recommendations.Add(new NetworkRecommendation
            {
                Category = "Bandwidth",
                Message = "Your download speed is below the recommended 15 Mbps for cloud gaming.",
                Priority = RecommendationPriority.Important
            });
        }

        if (_currentQuality.LatencyMs > 40)
        {
            recommendations.Add(new NetworkRecommendation
            {
                Category = "Latency",
                Message = "High latency detected. Try connecting to a closer data center or use a wired connection.",
                Priority = RecommendationPriority.Critical
            });
        }

        if (_currentQuality.PacketLoss > 1)
        {
            recommendations.Add(new NetworkRecommendation
            {
                Category = "Stability",
                Message = "Packet loss detected. Consider using a wired connection or troubleshooting your network.",
                Priority = RecommendationPriority.Critical
            });
        }

        recommendations.Add(new NetworkRecommendation
        {
            Category = "Optimization",
            Message = "Close other bandwidth-intensive applications for best performance.",
            Priority = RecommendationPriority.Suggestion
        });

        recommendations.Add(new NetworkRecommendation
        {
            Category = "Hardware",
            Message = "Use a 5GHz Wi-Fi network or Ethernet cable for better stability.",
            Priority = RecommendationPriority.Info
        });

        return Task.FromResult(Result.Success(recommendations));
    }

    private static NetworkGrade DetermineGrade(NetworkQuality quality)
    {
        if (quality.DownloadSpeedKbps >= 35000 && quality.LatencyMs <= 20 && quality.PacketLoss < 0.5)
            return NetworkGrade.Excellent;

        if (quality.DownloadSpeedKbps >= 15000 && quality.LatencyMs <= 40 && quality.PacketLoss < 1)
            return NetworkGrade.Good;

        if (quality.DownloadSpeedKbps >= 10000 && quality.LatencyMs <= 60 && quality.PacketLoss < 2)
            return NetworkGrade.Fair;

        if (quality.DownloadSpeedKbps >= 5000 && quality.LatencyMs <= 100)
            return NetworkGrade.Poor;

        return NetworkGrade.Unsuitable;
    }
}
