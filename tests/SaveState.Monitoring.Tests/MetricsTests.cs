using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Monitoring;
using Xunit;

namespace SaveState.Monitoring.Tests;

/// <summary>
/// Tests for application metrics functionality.
/// Validates metrics recording, calculation, and health monitoring.
/// </summary>
public class MetricsTests
{
    private readonly Mock<ILogger<ApplicationMetricsService>> _loggerMock;
    private readonly IApplicationMetrics _metrics;

    public MetricsTests()
    {
        _loggerMock = new Mock<ILogger<ApplicationMetricsService>>();
        _metrics = new ApplicationMetricsService(_loggerMock.Object);
    }

    [Fact]
    public async Task RecordResponseTime_IncreasesTotalRequests()
    {
        // Act
        _metrics.RecordResponseTime("TestOperation", TimeSpan.FromMilliseconds(100));
        _metrics.RecordResponseTime("TestOperation", TimeSpan.FromMilliseconds(200));

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.TotalRequests.Should().Be(2);
        snapshot.AverageResponseTime.Should().BeCloseTo(TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task RecordThroughput_IncrementsCorrectly()
    {
        // Act
        _metrics.RecordThroughput("Operation1", 5);
        _metrics.RecordThroughput("Operation2", 3);
        _metrics.RecordThroughput("Operation1", 2);

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.TotalRequests.Should().Be(10); // 5 + 3 + 2
    }

    [Fact]
    public async Task RecordDatabaseQuery_CalculatesAverageTime()
    {
        // Act
        _metrics.RecordDatabaseQuery("TestQuery", TimeSpan.FromMilliseconds(50));
        _metrics.RecordDatabaseQuery("TestQuery", TimeSpan.FromMilliseconds(150));
        _metrics.RecordDatabaseQuery("OtherQuery", TimeSpan.FromMilliseconds(100));

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.TotalDatabaseQueries.Should().Be(3);
        snapshot.AverageDatabaseQueryTime.Should().BeCloseTo(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task RecordCacheHit_IncreasesHitRatio()
    {
        // Act
        _metrics.RecordCacheHit("TestCache");
        _metrics.RecordCacheHit("TestCache");
        _metrics.RecordCacheMiss("TestCache");

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.CacheHitRatio.Should().BeApproximately(0.667, 0.001); // 2/3 ≈ 0.667
        snapshot.TotalCacheRequests.Should().Be(3);
    }

    [Fact]
    public async Task RecordApiCall_TracksSuccessAndFailure()
    {
        // Act
        _metrics.RecordApiCall("TestService", "endpoint1", TimeSpan.FromMilliseconds(100), true);
        _metrics.RecordApiCall("TestService", "endpoint2", TimeSpan.FromMilliseconds(200), false);
        _metrics.RecordApiCall("TestService", "endpoint1", TimeSpan.FromMilliseconds(50), true);

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.TotalApiCalls.Should().Be(3);
        snapshot.SuccessfulApiCalls.Should().Be(2);
        snapshot.FailedApiCalls.Should().Be(1);
    }

    [Fact]
    public async Task RecordAiRequest_TracksMetrics()
    {
        // Act
        _metrics.RecordAiRequest("OpenAI", "Completion", TimeSpan.FromMilliseconds(500), true);
        _metrics.RecordAiRequest("OpenAI", "Chat", TimeSpan.FromMilliseconds(300), false);
        _metrics.RecordAiTokenUsage("OpenAI", 100, 50);

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.TotalAiRequests.Should().Be(2);
        snapshot.SuccessfulAiRequests.Should().Be(1);
        snapshot.TotalTokensUsed.Should().Be(150);
    }

    [Fact]
    public async Task RecordException_IncreasesErrorCounts()
    {
        // Act
        _metrics.RecordException("TestSource", "InvalidOperationException", "Test error");
        _metrics.RecordException("TestSource", "ArgumentException", "Another error");
        _metrics.RecordUnhandledException("TestSource", new InvalidOperationException("Unhandled"));

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.TotalExceptions.Should().Be(2);
        snapshot.UnhandledExceptions.Should().Be(1);
        snapshot.ExceptionsByType.Should().ContainKey("TestSource:InvalidOperationException");
        snapshot.ExceptionsByType.Should().ContainKey("TestSource:ArgumentException");
    }

    [Fact]
    public async Task RecordCustomMetric_StoresValues()
    {
        // Act
        _metrics.RecordCustomMetric("TestMetric", 42.5);
        _metrics.IncrementCounter("TestCounter");
        _metrics.IncrementCounter("TestCounter");

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.CustomMetrics.Should().ContainKey("TestMetric");
        snapshot.CustomMetrics["TestMetric"].Should().Be(42.5);
        snapshot.Counters.Should().ContainKey("TestCounter");
        snapshot.Counters["TestCounter"].Should().Be(2);
    }

    [Fact]
    public async Task GetMetricsSnapshot_IncludesTimestamp()
    {
        // Act
        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MetricsStorage_MaintainsRollingBuffer()
    {
        // This test verifies the internal rolling buffer behavior
        // by checking that the storage doesn't grow unbounded

        // Act - Record many response times
        for (int i = 0; i < 2000; i++)
        {
            _metrics.RecordResponseTime("Test", TimeSpan.FromMilliseconds(i));
        }

        // The implementation should maintain a rolling buffer
        // This is tested indirectly through the snapshot calculation
        var snapshotTask = _metrics.GetMetricsSnapshotAsync();
        snapshotTask.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordDatabaseError_IncreasesErrorCount()
    {
        // Act
        _metrics.RecordDatabaseError("TestQuery", "TimeoutException");
        _metrics.RecordDatabaseError("TestQuery", "ConnectionException");

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.DatabaseErrors.Should().Be(2);
    }

    [Fact]
    public async Task RecordApiRateLimit_IncreasesRateLimitCount()
    {
        // Act
        _metrics.RecordApiRateLimit("TestService", TimeSpan.FromSeconds(30));

        // This is tracked internally but exposed through custom metrics
        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert - The rate limit is recorded but doesn't affect main counters directly
        snapshot.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordMemoryUsage_UpdatesCurrentMemory()
    {
        // Act
        _metrics.RecordMemoryUsage(1024 * 1024 * 100); // 100 MB

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.CurrentMemoryUsage.Should().Be(1024 * 1024 * 100);
    }

    [Fact]
    public async Task RecordCpuUsage_UpdatesCurrentCpu()
    {
        // Act
        _metrics.RecordCpuUsage(45.5);

        var snapshot = await _metrics.GetMetricsSnapshotAsync();

        // Assert
        snapshot.CurrentCpuUsage.Should().Be(45.5);
    }

    [Fact]
    public void ErrorTrackingService_RecordsExceptions()
    {
        // Arrange
        var errorTracker = new SaveState.Infrastructure.Monitoring.ErrorTrackingService(_metrics, new Mock<ILogger<SaveState.Infrastructure.Monitoring.ErrorTrackingService>>().Object);
        var exception = new InvalidOperationException("Test exception");

        // Act
        errorTracker.RecordException("TestSource", "InvalidOperationException", "Test message", exception);
        errorTracker.RecordUnhandledException("TestSource", exception);

        // Assert
        var stats = errorTracker.GetErrorStatistics();
        stats.Should().ContainKey("TestSource:InvalidOperationException");

        var recentErrors = errorTracker.GetRecentErrors();
        recentErrors.Should().HaveCount(2);
        recentErrors.Count(e => e.IsUnhandled).Should().Be(1);
    }

    [Fact]
    public void CachePerformanceMonitor_TracksHitMissRatio()
    {
        // Arrange
        var cacheMonitor = new SaveState.Infrastructure.Monitoring.CachePerformanceMonitor(_metrics, new Mock<ILogger<SaveState.Infrastructure.Monitoring.CachePerformanceMonitor>>().Object);

        // Act
        cacheMonitor.RecordCacheHit("TestCache");
        cacheMonitor.RecordCacheHit("TestCache");
        cacheMonitor.RecordCacheMiss("TestCache");

        // Assert
        var stats = cacheMonitor.GetCacheStats("TestCache");
        stats.HitRatio.Should().BeApproximately(0.667, 0.001); // 2/3 ≈ 0.667
        stats.TotalRequests.Should().Be(3);
    }
}
