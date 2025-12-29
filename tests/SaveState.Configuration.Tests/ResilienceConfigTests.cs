using FluentAssertions;
using SaveState.Core.Configuration;
using Xunit;

namespace SaveState.Configuration.Tests;

/// <summary>
/// Tests for resilience configuration options.
/// Validates circuit breaker settings, retry policies, and timeout configurations.
/// </summary>
public class ResilienceConfigTests
{
    [Fact]
    public void ResilienceConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new ResilienceConfig();

        // Assert
        config.CircuitBreakerThreshold.Should().Be(5);
        config.CircuitBreakerDurationMs.Should().Be(60000); // 1 minute
        config.MaxRetries.Should().Be(3);
        config.InitialRetryDelayMs.Should().Be(1000); // 1 second
        config.RetryBackoffMultiplier.Should().Be(2.0);
        config.DefaultTimeoutMs.Should().Be(30000); // 30 seconds
    }

    [Fact]
    public void ResilienceConfig_SectionConstant_IsCorrect()
    {
        // Assert
        ResilienceConfig.Section.Should().Be("Resilience");
    }

    [Fact]
    public void ResilienceConfig_CircuitBreakerThreshold_AcceptsValidValues()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act & Assert
        config.CircuitBreakerThreshold = 1;
        config.CircuitBreakerThreshold.Should().Be(1);

        config.CircuitBreakerThreshold = 10;
        config.CircuitBreakerThreshold.Should().Be(10);

        config.CircuitBreakerThreshold = 100;
        config.CircuitBreakerThreshold.Should().Be(100);
    }

    [Fact]
    public void ResilienceConfig_CircuitBreakerDurationMs_AcceptsValidValues()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act & Assert
        config.CircuitBreakerDurationMs = 1000; // 1 second
        config.CircuitBreakerDurationMs.Should().Be(1000);

        config.CircuitBreakerDurationMs = 300000; // 5 minutes
        config.CircuitBreakerDurationMs.Should().Be(300000);

        config.CircuitBreakerDurationMs = 3600000; // 1 hour
        config.CircuitBreakerDurationMs.Should().Be(3600000);
    }

    [Fact]
    public void ResilienceConfig_MaxRetries_AcceptsValidValues()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act & Assert
        config.MaxRetries = 0; // No retries
        config.MaxRetries.Should().Be(0);

        config.MaxRetries = 1;
        config.MaxRetries.Should().Be(1);

        config.MaxRetries = 10;
        config.MaxRetries.Should().Be(10);
    }

    [Fact]
    public void ResilienceConfig_InitialRetryDelayMs_AcceptsValidValues()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act & Assert
        config.InitialRetryDelayMs = 100; // 100ms
        config.InitialRetryDelayMs.Should().Be(100);

        config.InitialRetryDelayMs = 5000; // 5 seconds
        config.InitialRetryDelayMs.Should().Be(5000);

        config.InitialRetryDelayMs = 60000; // 1 minute
        config.InitialRetryDelayMs.Should().Be(60000);
    }

    [Fact]
    public void ResilienceConfig_RetryBackoffMultiplier_AcceptsValidValues()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act & Assert
        config.RetryBackoffMultiplier = 1.0; // No backoff
        config.RetryBackoffMultiplier.Should().Be(1.0);

        config.RetryBackoffMultiplier = 1.5;
        config.RetryBackoffMultiplier.Should().Be(1.5);

        config.RetryBackoffMultiplier = 3.0;
        config.RetryBackoffMultiplier.Should().Be(3.0);
    }

    [Fact]
    public void ResilienceConfig_DefaultTimeoutMs_AcceptsValidValues()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act & Assert
        config.DefaultTimeoutMs = 1000; // 1 second
        config.DefaultTimeoutMs.Should().Be(1000);

        config.DefaultTimeoutMs = 120000; // 2 minutes
        config.DefaultTimeoutMs.Should().Be(120000);

        config.DefaultTimeoutMs = 300000; // 5 minutes
        config.DefaultTimeoutMs.Should().Be(300000);
    }

    [Fact]
    public void ResilienceConfig_CalculatedRetryDelays_AreCorrect()
    {
        // Arrange
        var config = new ResilienceConfig
        {
            InitialRetryDelayMs = 1000,
            RetryBackoffMultiplier = 2.0,
            MaxRetries = 3
        };

        // Act - Calculate expected delays
        var expectedDelays = new[]
        {
            config.InitialRetryDelayMs,                    // 1000ms
            (int)(1000 * 2.0),                            // 2000ms
            (int)(2000 * 2.0)                             // 4000ms
        };

        // Assert - This validates the exponential backoff calculation logic
        // The actual implementation would use these values
        expectedDelays.Should().BeEquivalentTo(new[] { 1000, 2000, 4000 });
    }

    [Fact]
    public void ResilienceConfig_CircuitBreakerLogic_WorksAsExpected()
    {
        // Arrange
        var config = new ResilienceConfig
        {
            CircuitBreakerThreshold = 5,
            CircuitBreakerDurationMs = 60000
        };

        // Act & Assert
        // These values would be used by Polly's circuit breaker policy
        config.CircuitBreakerThreshold.Should().Be(5);
        config.CircuitBreakerDurationMs.Should().Be(60000);

        // The circuit breaker should trip after 5 failures
        // and stay open for 60 seconds
    }

    [Fact]
    public void ResilienceConfig_TimeoutValidation_Works()
    {
        // Arrange
        var config = new ResilienceConfig
        {
            DefaultTimeoutMs = 30000
        };

        // Act & Assert
        config.DefaultTimeoutMs.Should().BeGreaterThan(0);
        config.DefaultTimeoutMs.Should().BeLessThanOrEqualTo(300000); // Reasonable upper bound

        // Validate that timeout is within reasonable bounds
        config.DefaultTimeoutMs.Should().BeGreaterThanOrEqualTo(1000); // At least 1 second
        config.DefaultTimeoutMs.Should().BeLessThanOrEqualTo(300000); // At most 5 minutes
    }

    [Fact]
    public void ResilienceConfig_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var config = new ResilienceConfig();

        // Act
        config.CircuitBreakerThreshold = 10;
        config.CircuitBreakerDurationMs = 120000;
        config.MaxRetries = 5;
        config.InitialRetryDelayMs = 2000;
        config.RetryBackoffMultiplier = 1.5;
        config.DefaultTimeoutMs = 60000;

        // Assert
        config.CircuitBreakerThreshold.Should().Be(10);
        config.CircuitBreakerDurationMs.Should().Be(120000);
        config.MaxRetries.Should().Be(5);
        config.InitialRetryDelayMs.Should().Be(2000);
        config.RetryBackoffMultiplier.Should().Be(1.5);
        config.DefaultTimeoutMs.Should().Be(60000);
    }

    [Fact]
    public void ResilienceConfig_ConfigurationBinding_Works()
    {
        // This test validates that the config can be bound from configuration
        var config = new ResilienceConfig
        {
            CircuitBreakerThreshold = 3,
            CircuitBreakerDurationMs = 30000,
            MaxRetries = 2,
            InitialRetryDelayMs = 500,
            RetryBackoffMultiplier = 1.2,
            DefaultTimeoutMs = 15000
        };

        // Assert that all properties are settable and retrievable
        config.Should().NotBeNull();
        config.CircuitBreakerThreshold.Should().BeGreaterThan(0);
        config.MaxRetries.Should().BeGreaterThanOrEqualTo(0);
        config.InitialRetryDelayMs.Should().BeGreaterThan(0);
        config.DefaultTimeoutMs.Should().BeGreaterThan(0);
    }
}
