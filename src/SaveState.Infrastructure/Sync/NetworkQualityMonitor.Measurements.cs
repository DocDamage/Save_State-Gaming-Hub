using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync;
using SaveState.Core.Sync.Entities;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Infrastructure.Sync;

public partial class NetworkQualityMonitor
{
    private async Task<int> MeasureLatencyAsync(CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();

            // Test against multiple servers for better accuracy
            var servers = new[] { "8.8.8.8", "1.1.1.1", "208.67.222.222" };
            var latencies = new List<long>();

            foreach (var server in servers)
            {
                try
                {
                    var reply = await ping.SendPingAsync(server, 1000).ConfigureAwait(false);
                    if (reply.Status == IPStatus.Success)
                    {
                        latencies.Add(reply.RoundtripTime);
                    }
                }
                catch
                {
                    // Ignore ping failures to individual servers
                }
            }

            if (latencies.Any())
            {
                return (int)latencies.Average();
            }

            return 999; // High latency if all pings fail
        }
        catch
        {
            return 999;
        }
    }

    private async Task<int> MeasurePacketLossAsync(CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();

            // Send 10 pings and calculate packet loss
            const int pingCount = 10;
            var successfulPings = 0;

            for (int i = 0; i < pingCount; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync("8.8.8.8", 1000).ConfigureAwait(false);
                    if (reply.Status == IPStatus.Success)
                    {
                        successfulPings++;
                    }
                }
                catch
                {
                    // Continue with next ping
                }
            }

            var packetLossPercent = ((pingCount - successfulPings) * 100) / pingCount;
            return packetLossPercent;
        }
        catch
        {
            return 0; // Assume no packet loss if measurement fails
        }
    }

    private async Task<int> EstimateBandwidthAsync(CancellationToken ct)
    {
        try
        {
            // Simple bandwidth estimation using download speed test
            var stopwatch = Stopwatch.StartNew();

            // Download a small test file to estimate bandwidth
            var testUrl = "https://www.google.com/favicon.ico"; // Small file for quick test
            var response = await _httpClient.GetAsync(testUrl, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                var bytesDownloaded = content.Length;
                var seconds = stopwatch.Elapsed.TotalSeconds;

                // Estimate bandwidth in Mbps (rough calculation)
                var bitsPerSecond = (bytesDownloaded * 8) / seconds;
                var mbps = (int)(bitsPerSecond / 1_000_000);

                // Clamp to reasonable range
                return Math.Clamp(mbps, 1, 1000);
            }

            return 25; // Default assumption
        }
        catch
        {
            return 25; // Default assumption if test fails
        }
    }

    private async Task<IReadOnlyList<PingTestResult>> PerformPingTestsAsync(CancellationToken ct)
    {
        var results = new List<PingTestResult>();
        var testServers = new[]
        {
            ("Google DNS", "8.8.8.8"),
            ("Cloudflare DNS", "1.1.1.1"),
            ("OpenDNS", "208.67.222.222")
        };

        using var ping = new Ping();

        foreach (var (name, ip) in testServers)
        {
            try
            {
                var reply = await ping.SendPingAsync(ip, 2000).ConfigureAwait(false);
                var result = new PingTestResult(
                    Endpoint: name,
                    LatencyMs: reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : 999,
                    PacketLossPercent: reply.Status == IPStatus.Success ? 0 : 100,
                    Success: reply.Status == IPStatus.Success);

                results.Add(result);
            }
            catch
            {
                results.Add(new PingTestResult(
                    Endpoint: name,
                    LatencyMs: 999,
                    PacketLossPercent: 100,
                    Success: false));
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<SpeedTestResult>> PerformSpeedTestsAsync(CancellationToken ct)
    {
        var results = new List<SpeedTestResult>();
        var testServers = new[]
        {
            "https://www.google.com/favicon.ico",
            "https://www.cloudflare.com/favicon.ico"
        };

        foreach (var url in testServers)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    var bytesDownloaded = content.Length;
                    var seconds = stopwatch.Elapsed.TotalSeconds;

                    // Rough speed estimation
                    var bitsPerSecond = (bytesDownloaded * 8) / seconds;
                    var downloadMbps = (int)(bitsPerSecond / 1_000_000);

                    results.Add(new SpeedTestResult(
                        Server: url,
                        DownloadSpeedMbps: downloadMbps,
                        UploadSpeedMbps: downloadMbps / 2, // Rough estimate
                        Success: true));
                }
                else
                {
                    results.Add(new SpeedTestResult(
                        Server: url,
                        DownloadSpeedMbps: 0,
                        UploadSpeedMbps: 0,
                        Success: false));
                }
            }
            catch
            {
                results.Add(new SpeedTestResult(
                    Server: url,
                    DownloadSpeedMbps: 0,
                    UploadSpeedMbps: 0,
                    Success: false));
            }
        }

        return results;
    }

    private static QualityLevel DetermineQualityLevel(int latency, int packetLoss, int bandwidth)
    {
        // Cloud gaming quality requirements
        if (latency <= 20 && packetLoss <= 1 && bandwidth >= 50)
            return QualityLevel.Excellent;
        if (latency <= 40 && packetLoss <= 2 && bandwidth >= 25)
            return QualityLevel.Good;
        if (latency <= 80 && packetLoss <= 5 && bandwidth >= 10)
            return QualityLevel.Fair;

        return QualityLevel.Poor;
    }

    private static QualityChangeType DetermineQualityChange(NetworkQuality previous, NetworkQuality current)
    {
        var latencyDiff = current.LatencyMs - previous.LatencyMs;
        var packetLossDiff = current.PacketLossPercent - previous.PacketLossPercent;

        if (current.Level < previous.Level)
        {
            return latencyDiff > 20 || packetLossDiff > 2
                ? QualityChangeType.SignificantDrop
                : QualityChangeType.Degraded;
        }

        if (current.Level > previous.Level)
        {
            return QualityChangeType.Recovered;
        }

        return QualityChangeType.Improved;
    }

    private static IReadOnlyList<string> GenerateRecommendations(
        NetworkQuality quality,
        IReadOnlyList<PingTestResult> pingTests,
        IReadOnlyList<SpeedTestResult> speedTests)
    {
        var recommendations = new List<string>();

        if (quality.LatencyMs > 50)
        {
            recommendations.Add("Use a wired Ethernet connection instead of Wi-Fi for lower latency");
            recommendations.Add("Connect to a server closer to your location");
        }

        if (quality.PacketLossPercent > 2)
        {
            recommendations.Add("Check for network congestion or interference");
            recommendations.Add("Consider upgrading your internet plan for more stable connection");
        }

        if (quality.BandwidthMbps < 25)
        {
            recommendations.Add("Upgrade to a higher-speed internet plan (25+ Mbps recommended for 1080p cloud gaming)");
        }

        if (quality.JitterMs > 30)
        {
            recommendations.Add("Reduce network congestion by pausing downloads/uploads during gaming");
        }

        var failedPings = pingTests.Count(t => !t.Success);
        if (failedPings > 0)
        {
            recommendations.Add($"Network connectivity issues detected ({failedPings}/{pingTests.Count} ping tests failed)");
        }

        var failedSpeeds = speedTests.Count(t => !t.Success);
        if (failedSpeeds > 0)
        {
            recommendations.Add($"Speed test issues detected ({failedSpeeds}/{speedTests.Count} speed tests failed)");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Your network quality is good for cloud gaming!");
        }

        return recommendations;
    }

    private static bool IsQualitySufficient(NetworkQuality quality, CloudGamingProvider provider)
    {
        return provider switch
        {
            CloudGamingProvider.GeForceNow => quality.LatencyMs <= 40 && quality.BandwidthMbps >= 25,
            CloudGamingProvider.XboxCloud => quality.LatencyMs <= 100 && quality.BandwidthMbps >= 10,
            CloudGamingProvider.AmazonLuna => quality.LatencyMs <= 80 && quality.BandwidthMbps >= 10,
            CloudGamingProvider.PlayStationNow => quality.LatencyMs <= 80 && quality.BandwidthMbps >= 15,
            CloudGamingProvider.Boosteroid => quality.LatencyMs <= 60 && quality.BandwidthMbps >= 15,
            _ => quality.Level >= QualityLevel.Fair
        };
    }

    private async Task<Result<string>> GetPublicIpAddressAsync(CancellationToken ct)
    {
        try
        {
            // Use a public IP service
            var response = await _httpClient.GetStringAsync("https://api.ipify.org", ct).ConfigureAwait(false);
            var ip = response.Trim();
            return !string.IsNullOrEmpty(ip)
                ? Result.Success<string>(ip)
                : Result.Failure<string>("Empty response from IP service", ErrorType.ExternalService);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to resolve public IP: {ex.Message}", ErrorType.ExternalService);
        }
    }

    private static Result<string> GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return Result.Success<string>(ip.ToString());
                }
            }

            return Result.Failure<string>("No IPv4 address found", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to get local IP: {ex.Message}", ErrorType.Internal);
        }
    }

    private static string[] GetDnsServers()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .SelectMany(ni => ni.GetIPProperties().DnsAddresses)
                .Select(ip => ip.ToString())
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static Result<string> GetDefaultGateway()
    {
        try
        {
            var gateway = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .SelectMany(ni => ni.GetIPProperties().GatewayAddresses)
                .Select(gw => gw.Address.ToString())
                .FirstOrDefault();

            return !string.IsNullOrEmpty(gateway)
                ? Result.Success<string>(gateway)
                : Result.Failure<string>("No default gateway found", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to get default gateway: {ex.Message}", ErrorType.Internal);
        }
    }

    private static Result<string> GetActiveNetworkAdapter()
    {
        try
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                      ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return adapter != null
                ? Result.Success<string>(adapter.Name)
                : Result.Failure<string>("No active network adapter found", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to get active adapter: {ex.Message}", ErrorType.Internal);
        }
    }

    private static bool IsVpnActive()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(ni => ni.OperationalStatus == OperationalStatus.Up &&
                          (ni.Name.Contains("VPN", StringComparison.OrdinalIgnoreCase) ||
                           ni.Name.Contains("TAP", StringComparison.OrdinalIgnoreCase) ||
                           ni.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return false;
        }
    }

    private static Result<string> DetectVpnProvider()
    {
        try
        {
            var vpnAdapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                      (ni.Name.Contains("VPN", StringComparison.OrdinalIgnoreCase) ||
                                       ni.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase)));

            if (vpnAdapter != null)
            {
                // Try to identify provider from description
                var description = vpnAdapter.Description.ToLowerInvariant();
                if (description.Contains("openvpn")) return Result.Success<string>("OpenVPN");
                if (description.Contains("nord")) return Result.Success<string>("NordVPN");
                if (description.Contains("express")) return Result.Success<string>("ExpressVPN");
                if (description.Contains("proton")) return Result.Success<string>("ProtonVPN");
                if (description.Contains("mullvad")) return Result.Success<string>("Mullvad VPN");

                return Result.Success<string>("Unknown VPN");
            }

            return Result.Failure<string>("VPN adapter found but could not identify provider", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to detect VPN provider: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<IReadOnlyList<OpenPort>> CheckCommonPortsAsync(CancellationToken ct)
    {
        var openPorts = new List<OpenPort>();
        var commonPorts = new[]
        {
            (80, "HTTP"),
            (443, "HTTPS"),
            (22, "SSH"),
            (3389, "RDP"),
            (25565, "Minecraft"),
            (7777, "Game Port")
        };

        foreach (var (port, service) in commonPorts)
        {
            try
            {
                // Simple port check (this is a basic implementation)
                using var tcpClient = new System.Net.Sockets.TcpClient();
                var connectTask = tcpClient.ConnectAsync("127.0.0.1", port);
                var timeoutTask = Task.Delay(1000, ct);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (completedTask == connectTask && !ct.IsCancellationRequested)
                {
                    openPorts.Add(new OpenPort(port, "tcp", service));
                }
            }
            catch
            {
                // Port is closed or unreachable
            }
        }

        return openPorts;
    }

    private void OnNetworkQualityChanged(NetworkQualityChangedEventArgs e)
    {
        try
        {
            NetworkQualityChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in network quality changed event handler");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _httpClient?.Dispose();
    }
}
