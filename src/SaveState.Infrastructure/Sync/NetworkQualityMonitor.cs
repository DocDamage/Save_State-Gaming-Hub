using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Configuration;
using SaveState.Core.Sync;
using SaveState.Core.Sync.Entities;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Implementation of network quality monitoring with real-time metrics and historical data storage.
/// </summary>
public partial class NetworkQualityMonitor : INetworkQualityMonitor, IDisposable
{
    private readonly ILogger<NetworkQualityMonitor> _logger;
    private readonly HttpClient _httpClient;
    private readonly CloudGamingOptions _options;
    private readonly INetworkQualityHistoryRepository _historyRepository;
    private readonly ITimeProvider _timeProvider;

    private Timer? _monitoringTimer;
    private NetworkQuality _lastQuality = default!;
    private bool _isMonitoring;
    private Guid? _currentSessionId;

    /// <summary>
    /// Event raised when network quality changes significantly.
    /// </summary>
    public event EventHandler<NetworkQualityChangedEventArgs>? NetworkQualityChanged;

    /// <summary>
    /// Gets a value indicating whether network monitoring is currently active.
    /// </summary>
    public bool IsMonitoring => _isMonitoring;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkQualityMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="httpClient">HTTP client for network testing.</param>
    /// <param name="options">Cloud gaming configuration options.</param>
    public NetworkQualityMonitor(
        ILogger<NetworkQualityMonitor> logger,
        HttpClient httpClient,
        IOptions<CloudGamingOptions> options,
        INetworkQualityHistoryRepository historyRepository,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
        _historyRepository = historyRepository;
        _timeProvider = timeProvider;

        // Start background cleanup task for historical data
        if (_options.NetworkMonitoring.StoreHistoricalData)
        {
            _ = Task.Run(HistoricalDataCleanupTask);
        }
    }

    /// <summary>
    /// Performs a comprehensive network quality test including ping, speed, and diagnostics.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing detailed network quality test results.</returns>
    public async Task<Result<NetworkQualityTestResult>> PerformQualityTestAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting comprehensive network quality test");

            var pingTests = await PerformPingTestsAsync(ct).ConfigureAwait(false);
            var speedTests = await PerformSpeedTestsAsync(ct).ConfigureAwait(false);
            var currentQuality = await GetCurrentQualityAsync(ct).ConfigureAwait(false);

            if (!currentQuality.IsSuccess)
            {
                return Result.Failure<NetworkQualityTestResult>(
                    $"Failed to get current quality: {currentQuality.Error}");
            }

            var recommendations = GenerateRecommendations(currentQuality.Value, pingTests, speedTests);

            var result = new NetworkQualityTestResult(
                CurrentQuality: currentQuality.Value,
                PingTests: pingTests,
                SpeedTests: speedTests,
                Recommendations: recommendations,
                TestCompletedAt: _timeProvider.UtcNow);

            _logger.LogInformation("Network quality test completed - Latency: {Latency}ms, Quality: {Quality}",
                currentQuality.Value.LatencyMs, currentQuality.Value.Level);

            return Result.Success<NetworkQualityTestResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform network quality test");
            return Result.Failure<NetworkQualityTestResult>(
                $"Network quality test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current network quality metrics including latency, packet loss, and bandwidth.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the current network quality measurements.</returns>
    public async Task<Result<NetworkQuality>> GetCurrentQualityAsync(
        CancellationToken ct = default)
    {
        try
        {
            // Perform basic network measurements
            var latencyTask = MeasureLatencyAsync(ct);
            var packetLossTask = MeasurePacketLossAsync(ct);
            var bandwidthTask = EstimateBandwidthAsync(ct);

            await Task.WhenAll(latencyTask, packetLossTask, bandwidthTask).ConfigureAwait(false);

            var latency = await latencyTask;
            var packetLoss = await packetLossTask;
            var bandwidth = await bandwidthTask;

            // Calculate jitter (simplified)
            var jitter = Math.Min(latency / 10, 50); // Rough estimate

            var qualityLevel = DetermineQualityLevel(latency, packetLoss, bandwidth);

            var quality = new NetworkQuality(
                LatencyMs: latency,
                JitterMs: jitter,
                PacketLossPercent: packetLoss,
                BandwidthMbps: bandwidth,
                Level: qualityLevel,
                MeasuredAt: _timeProvider.UtcNow);

            // Store historical data if enabled
            if (_options.NetworkMonitoring.StoreHistoricalData)
            {
                await StoreHistoricalDataAsync(quality, ct).ConfigureAwait(false);
            }

            // Check for significant quality changes
            if (_lastQuality != null)
            {
                var changeType = DetermineQualityChange(_lastQuality, quality);
                if (changeType != QualityChangeType.Improved) // Only notify on degradation
                {
                    OnNetworkQualityChanged(new NetworkQualityChangedEventArgs
                    {
                        PreviousQuality = _lastQuality,
                        CurrentQuality = quality,
                        ChangeType = changeType
                    });
                }
            }

            _lastQuality = quality;

            return Result.Success<NetworkQuality>(quality);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current network quality");
            return Result.Failure<NetworkQuality>($"Failed to measure network quality: {ex.Message}");
        }
    }

    /// <summary>
    /// Stores network quality data in historical storage.
    /// </summary>
    private async Task StoreHistoricalDataAsync(NetworkQuality quality, CancellationToken ct)
    {
        try
        {
            var historyEntity = NetworkQualityHistory.Create(quality, _currentSessionId);
            var result = await _historyRepository.AddAsync(historyEntity, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to store network quality history: {Error}", result.Error);
            }

            // Log storage periodically
            var totalCount = await _historyRepository.CountAsync(ct).ConfigureAwait(false);
            if (totalCount % 100 == 0) // Log every 100 entries
            {
                _logger.LogInformation("Stored {Count} network quality measurements", totalCount);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store historical network quality data");
        }
    }

    /// <summary>
    /// Background task to clean up old historical data.
    /// </summary>
    private async Task HistoricalDataCleanupTask()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1)); // Run cleanup hourly

                var cutoffDate = _timeProvider.UtcNow.AddDays(-_options.NetworkMonitoring.HistoricalDataRetentionDays);
                var deleteResult = await _historyRepository.DeleteOlderThanAsync(cutoffDate).ConfigureAwait(false);

                if (deleteResult.IsSuccess && deleteResult.Value > 0)
                {
                    _logger.LogInformation("Cleaned up {Removed} old network quality records",
                        deleteResult.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical data cleanup");
            }
        }
    }

    /// <summary>
    /// Starts continuous network quality monitoring at the specified interval.
    /// </summary>
    /// <param name="interval">The time interval between quality checks.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> StartMonitoringAsync(
        TimeSpan interval,
        CancellationToken ct = default)
    {
        try
        {
            if (_isMonitoring)
            {
                return Task.FromResult(Result.Success()); // Already monitoring
            }

            // Start a new monitoring session
            _currentSessionId = Guid.NewGuid();

            _monitoringTimer = new Timer(
                async _ =>
                {
                    try
                    {
                        var qualityResult = await GetCurrentQualityAsync(ct).ConfigureAwait(false);
                        if (qualityResult.IsSuccess)
                        {
                            _logger.LogDebug("Network monitoring - Latency: {Latency}ms, Quality: {Quality}",
                                qualityResult.Value.LatencyMs, qualityResult.Value.Level);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during network monitoring");
                    }
                },
                null,
                TimeSpan.Zero,
                interval);

            _isMonitoring = true;
            _logger.LogInformation("Started network quality monitoring with {Interval} interval", interval);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start network monitoring");
            return Task.FromResult(Result.Failure($"Failed to start monitoring: {ex.Message}"));
        }
    }

    /// <summary>
    /// Stops continuous network quality monitoring.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> StopMonitoringAsync(
        CancellationToken ct = default)
    {
        try
        {
            if (!_isMonitoring)
            {
                return Task.FromResult(Result.Success()); // Not monitoring
            }

            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
            _isMonitoring = false;
            _currentSessionId = null;

            _logger.LogInformation("Stopped network quality monitoring");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop network monitoring");
            return Task.FromResult(Result.Failure($"Failed to stop monitoring: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets historical network quality data for the specified time range.
    /// </summary>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the historical network quality data.</returns>
    public async Task<Result<IReadOnlyList<NetworkQuality>>> GetQualityHistoryAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct = default)
    {
        try
        {
            if (!_options.NetworkMonitoring.StoreHistoricalData)
            {
                _logger.LogWarning("Historical data storage is disabled");
                var currentQuality = await GetCurrentQualityAsync(ct).ConfigureAwait(false);

                if (!currentQuality.IsSuccess || currentQuality.Value is null)
                {
                    return Result.Failure<IReadOnlyList<NetworkQuality>>(currentQuality.Error ?? "Failed to get current quality");
                }

                return Result.Success<IReadOnlyList<NetworkQuality>>(new[] { currentQuality.Value });
            }

            var historyResult = await _historyRepository.GetByTimeRangeAsync(startTime, endTime, ct).ConfigureAwait(false);

            if (!historyResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<NetworkQuality>>(historyResult.Error);
            }

            var history = historyResult.Value.Select(h => h.ToDto()).ToList();

            _logger.LogInformation("Retrieved {Count} historical records from {Start} to {End}",
                history.Count, startTime, endTime);

            return Result.Success<IReadOnlyList<NetworkQuality>>(history.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network quality history");
            return Result.Failure<IReadOnlyList<NetworkQuality>>(
                $"Failed to get quality history: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if the current network quality is sufficient for cloud gaming on the specified provider.
    /// </summary>
    /// <param name="provider">The cloud gaming provider to check compatibility for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing true if the network quality is sufficient, false otherwise.</returns>
    public async Task<Result<bool>> IsQualitySufficientForCloudGamingAsync(
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        try
        {
            var qualityResult = await GetCurrentQualityAsync(ct).ConfigureAwait(false);
            if (!qualityResult.IsSuccess)
            {
                return Result.Failure<bool>(qualityResult.Error);
            }

            var quality = qualityResult.Value;
            var isSufficient = IsQualitySufficient(quality, provider);

            _logger.LogDebug("Network quality sufficiency for {Provider}: {Sufficient} (Latency: {Latency}ms)",
                provider, isSufficient, quality.LatencyMs);

            return Result.Success<bool>(isSufficient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check network quality sufficiency for {Provider}", provider);
            return Result.Failure<bool>($"Failed to check quality sufficiency: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets detailed network diagnostics information including adapter details and routing information.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing comprehensive network diagnostics.</returns>
    public async Task<Result<NetworkDiagnostics>> GetNetworkDiagnosticsAsync(
        CancellationToken ct = default)
    {
        try
        {
            // Gather basic network information
            var publicIpResult = await GetPublicIpAddressAsync(ct).ConfigureAwait(false);
            var localIpResult = GetLocalIpAddress();
            var dnsServers = string.Join(", ", GetDnsServers());
            var gatewayResult = GetDefaultGateway();
            var networkAdapterResult = GetActiveNetworkAdapter();

            // Check for VPN (simplified)
            var isVpnActive = IsVpnActive();
            var vpnProviderResult = isVpnActive ? DetectVpnProvider() : Result.Failure<string>("VPN not active", ErrorType.None);

            // Basic port check (simplified)
            var openPorts = await CheckCommonPortsAsync(ct).ConfigureAwait(false);

            var diagnostics = new NetworkDiagnostics(
                PublicIpAddress: publicIpResult.IsSuccess && publicIpResult.Value is not null ? publicIpResult.Value : "Unknown",
                LocalIpAddress: localIpResult.IsSuccess && localIpResult.Value is not null ? localIpResult.Value : "Unknown",
                DnsServers: dnsServers,
                Gateway: gatewayResult.IsSuccess && gatewayResult.Value is not null ? gatewayResult.Value : "Unknown",
                SubnetMask: "255.255.255.0", // Placeholder
                NetworkAdapter: networkAdapterResult.IsSuccess && networkAdapterResult.Value is not null ? networkAdapterResult.Value : "Unknown",
                IsVpnActive: isVpnActive,
                VpnProvider: vpnProviderResult.IsSuccess ? vpnProviderResult.Value : null,
                OpenPorts: openPorts);

            return Result.Success<NetworkDiagnostics>(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network diagnostics");
            return Result.Failure<NetworkDiagnostics>($"Failed to get diagnostics: {ex.Message}");
        }
    }

}
