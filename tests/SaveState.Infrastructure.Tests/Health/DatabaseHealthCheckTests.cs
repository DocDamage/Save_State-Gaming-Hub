using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SaveState.Infrastructure.Health;
using SaveState.Infrastructure.Persistence;
using Xunit;

namespace SaveState.Infrastructure.Tests.Health;

public class DatabaseHealthCheckTests : IAsyncDisposable
{
    private readonly SaveStateDbContext _dbContext;
    private readonly DatabaseHealthCheck _healthCheck;

    public DatabaseHealthCheckTests()
    {
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _dbContext = new SaveStateDbContext(options);
        _healthCheck = new DatabaseHealthCheck(_dbContext);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthyDatabase_ReturnsHealthyResult()
    {
        // Arrange
        await _dbContext.Database.EnsureCreatedAsync();

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database is healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WithConnectionFailure_ReturnsUnhealthyResult()
    {
        // Arrange - Create a mock context that can't connect
        var mockContext = new Mock<SaveStateDbContext>();
        var mockDatabase = new Mock<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>();
        mockContext.SetupGet(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var healthCheck = new DatabaseHealthCheck(mockContext.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Cannot connect to database");
    }

    [Fact]
    public async Task CheckHealthAsync_WithPendingMigrations_ReturnsDegradedResult()
    {
        // Arrange - Mock pending migrations
        var mockContext = new Mock<SaveStateDbContext>();
        var mockDatabase = new Mock<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>();
        mockContext.SetupGet(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockDatabase.Setup(d => d.GetPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Migration1", "Migration2" });

        var healthCheck = new DatabaseHealthCheck(mockContext.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("2 pending migrations");
        result.Data.Should().ContainKey("PendingMigrations");
        result.Data["PendingMigrations"].Should().BeEquivalentTo(new[] { "Migration1", "Migration2" });
    }

    [Fact]
    public async Task CheckHealthAsync_WithException_ReturnsUnhealthyResult()
    {
        // Arrange - Mock an exception during health check
        var mockContext = new Mock<SaveStateDbContext>();
        var mockDatabase = new Mock<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>();
        mockContext.SetupGet(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var healthCheck = new DatabaseHealthCheck(mockContext.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database check failed");
        result.Exception.Should().NotBeNull();
        result.Exception!.Message.Should().Be("Database connection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new HealthCheckContext();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _healthCheck.CheckHealthAsync(context, cts.Token));
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoPendingMigrations_ReturnsHealthyResult()
    {
        // Arrange - Mock no pending migrations
        var mockContext = new Mock<SaveStateDbContext>();
        var mockDatabase = new Mock<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>();
        mockContext.SetupGet(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockDatabase.Setup(d => d.GetPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var healthCheck = new DatabaseHealthCheck(mockContext.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database is healthy");
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync().ConfigureAwait(false);
    }
}
