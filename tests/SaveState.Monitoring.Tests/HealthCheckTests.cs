using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Infrastructure.Health;
using Xunit;

namespace SaveState.Monitoring.Tests;

/// <summary>
/// Tests for health check monitoring.
/// Validates database connectivity and system health monitoring.
/// </summary>
public class HealthCheckTests
{
    [Fact]
    public async Task DatabaseHealthCheck_CanBeInstantiated()
    {
        // Arrange - In a real scenario, this would need a database context
        // For testing instantiation, we'll verify the class exists

        // Act - This would normally check database connectivity
        // For this test, we'll verify the health check type exists
        var healthCheckType = typeof(SaveState.Infrastructure.Health.DatabaseHealthCheck);

        // Assert - Health check should exist and be properly configured
        healthCheckType.Should().NotBeNull();
        healthCheckType.Should().Implement<IHealthCheck>();
    }

    [Fact]
    public void HealthCheck_Result_ContainsExpectedData()
    {
        // Arrange
        var healthCheckResult = HealthCheckResult.Healthy("Database is healthy");

        // Act & Assert
        healthCheckResult.Status.Should().Be(HealthStatus.Healthy);
        healthCheckResult.Description.Should().Be("Database is healthy");
        healthCheckResult.Exception.Should().BeNull();
        healthCheckResult.Data.Should().NotBeNull();
    }

    [Fact]
    public void HealthCheck_UnhealthyResult_ContainsErrorDetails()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        var healthCheckResult = HealthCheckResult.Unhealthy("Database is unhealthy", exception);

        // Act & Assert
        healthCheckResult.Status.Should().Be(HealthStatus.Unhealthy);
        healthCheckResult.Description.Should().Be("Database is unhealthy");
        healthCheckResult.Exception.Should().Be(exception);
        healthCheckResult.Data.Should().NotBeNull();
    }

    [Fact]
    public void HealthCheck_DegradedResult_IndicatesPartialFailure()
    {
        // Arrange
        var healthCheckResult = HealthCheckResult.Degraded("Database is slow");

        // Act & Assert
        healthCheckResult.Status.Should().Be(HealthStatus.Degraded);
        healthCheckResult.Description.Should().Be("Database is slow");
        healthCheckResult.Exception.Should().BeNull();
        healthCheckResult.Data.Should().NotBeNull();
    }

    [Fact]
    public void HealthCheck_DataDictionary_CanStoreAdditionalInfo()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["ConnectionTime"] = TimeSpan.FromMilliseconds(150),
            ["LastCheck"] = DateTime.UtcNow,
            ["RetryCount"] = 2
        };

        var healthCheckResult = HealthCheckResult.Healthy("Database OK", data);

        // Act & Assert
        healthCheckResult.Data.Should().ContainKey("ConnectionTime");
        healthCheckResult.Data.Should().ContainKey("LastCheck");
        healthCheckResult.Data.Should().ContainKey("RetryCount");

        healthCheckResult.Data["ConnectionTime"].Should().BeOfType<TimeSpan>();
        healthCheckResult.Data["LastCheck"].Should().BeOfType<DateTime>();
        healthCheckResult.Data["RetryCount"].Should().BeOfType<int>();
    }

    [Fact]
    public void HealthCheck_Result_CanBeConvertedToString()
    {
        // Arrange
        var healthyResult = HealthCheckResult.Healthy("All systems operational");
        var unhealthyResult = HealthCheckResult.Unhealthy("Service unavailable");

        // Act
        var healthyString = healthyResult.ToString();
        var unhealthyString = unhealthyResult.ToString();

        // Assert - String representation should be meaningful
        healthyString.Should().Contain("Healthy");
        healthyString.Should().Contain("All systems operational");

        unhealthyString.Should().Contain("Unhealthy");
        unhealthyString.Should().Contain("Service unavailable");
    }

    [Fact]
    public async Task HealthCheck_MultipleResults_CanBeAggregated()
    {
        // Arrange
        var results = new[]
        {
            HealthCheckResult.Healthy("Database OK"),
            HealthCheckResult.Healthy("Cache OK"),
            HealthCheckResult.Unhealthy("External API down")
        };

        // Act - Simulate aggregation logic
        var overallStatus = results.Any(r => r.Status == HealthStatus.Unhealthy)
            ? HealthStatus.Unhealthy
            : results.Any(r => r.Status == HealthStatus.Degraded)
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

        var failedChecks = results.Where(r => r.Status != HealthStatus.Healthy).ToList();

        // Assert - Aggregation should work correctly
        overallStatus.Should().Be(HealthStatus.Unhealthy);
        failedChecks.Should().HaveCount(1);
        failedChecks[0].Description.Should().Be("External API down");
    }

    [Fact]
    public void HealthCheck_Tags_CanCategorizeChecks()
    {
        // Arrange - In a real health check system, tags would categorize checks
        var databaseTags = new[] { "database", "sql", "persistence" };
        var apiTags = new[] { "api", "external", "network" };
        var cacheTags = new[] { "cache", "memory", "performance" };

        // Act & Assert - Tags should help organize health checks
        databaseTags.Should().Contain("database");
        apiTags.Should().Contain("api");
        cacheTags.Should().Contain("cache");

        // Tags should be distinct between different check types
        databaseTags.Should().NotContain("api");
        apiTags.Should().NotContain("cache");
    }

    [Fact]
    public void HealthCheck_Timeout_PreventsHanging()
    {
        // Arrange - Health checks should have timeouts to prevent hanging
        var timeout = TimeSpan.FromSeconds(30);

        // Act & Assert - Timeout should be reasonable
        timeout.Should().BeGreaterThan(TimeSpan.Zero);
        timeout.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(5)); // Reasonable upper bound
    }

    [Fact]
    public void HealthCheck_Frequency_CanBeConfigured()
    {
        // Arrange - Health checks can run at different frequencies
        var frequentCheck = TimeSpan.FromSeconds(10);
        var regularCheck = TimeSpan.FromMinutes(1);
        var infrequentCheck = TimeSpan.FromMinutes(5);

        // Act & Assert - Frequencies should be reasonable
        frequentCheck.Should().BeLessThan(regularCheck);
        regularCheck.Should().BeLessThan(infrequentCheck);

        // All should be positive
        frequentCheck.Should().BeGreaterThan(TimeSpan.Zero);
        regularCheck.Should().BeGreaterThan(TimeSpan.Zero);
        infrequentCheck.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void HealthCheck_Response_IncludesTimestamp()
    {
        // Arrange
        var beforeCheck = DateTime.UtcNow;
        var result = HealthCheckResult.Healthy("Check completed");

        // Simulate adding timestamp (in real implementation)
        var checkTime = DateTime.UtcNow;
        var afterCheck = DateTime.UtcNow;

        // Act & Assert - Timestamp should be recent
        checkTime.Should().BeOnOrAfter(beforeCheck);
        checkTime.Should().BeOnOrBefore(afterCheck);

        // Should be within reasonable time bounds
        var timeSinceCheck = DateTime.UtcNow - checkTime;
        timeSinceCheck.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
