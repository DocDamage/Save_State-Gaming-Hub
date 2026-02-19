using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.RomManagement.RomValidation.Queries;
using SaveState.Application.RomManagement.RomValidation.Queries.Handlers;
using SaveState.Core.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.Tests.RomManagement.RomValidation;

public class GetRomValidationStatisticsQueryHandlerTests
{
    private readonly Mock<IRomValidationService> _mockValidationService = new();
    private readonly Mock<ILogger<GetRomValidationStatisticsQueryHandler>> _mockLogger = new();
    private readonly GetRomValidationStatisticsQueryHandler _sut;

    public GetRomValidationStatisticsQueryHandlerTests()
    {
        _sut = new GetRomValidationStatisticsQueryHandler(
            _mockValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ReturnsStatistics()
    {
        // Arrange
        var query = new GetRomValidationStatisticsQuery();
        var expectedStats = new RomValidationStatistics
        {
            TotalRoms = 100,
            ValidatedRoms = 75,
            VerifiedRoms = 60,
            BadDumps = 5,
            CorruptedRoms = 2,
            DuplicateRoms = 10
        };

        _mockValidationService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationStatistics>.Success(expectedStats));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRoms.Should().Be(100);
        result.Value.ValidatedRoms.Should().Be(75);
        result.Value.VerifiedRoms.Should().Be(60);
        result.Value.BadDumps.Should().Be(5);
        result.Value.ValidationPercentage.Should().Be(75m);
    }

    [Fact]
    public async Task Handle_CalculatesPercentageCorrectly()
    {
        // Arrange
        var query = new GetRomValidationStatisticsQuery();
        var expectedStats = new RomValidationStatistics
        {
            TotalRoms = 200,
            ValidatedRoms = 50
        };

        _mockValidationService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationStatistics>.Success(expectedStats));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ValidationPercentage.Should().Be(25m);
    }

    [Fact]
    public async Task Handle_WithZeroTotalRoms_ReturnsZeroPercentage()
    {
        // Arrange
        var query = new GetRomValidationStatisticsQuery();
        var expectedStats = new RomValidationStatistics
        {
            TotalRoms = 0,
            ValidatedRoms = 0
        };

        _mockValidationService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationStatistics>.Success(expectedStats));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ValidationPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WhenServiceFails_ReturnsFailure()
    {
        // Arrange
        var query = new GetRomValidationStatisticsQuery();

        _mockValidationService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationStatistics>.Failure("Service error"));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Service error");
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var query = new GetRomValidationStatisticsQuery();

        _mockValidationService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Test exception");
    }

    [Fact]
    public async Task Handle_WithPlatformStats_IncludesPlatformBreakdown()
    {
        // Arrange
        var query = new GetRomValidationStatisticsQuery();
        var platformStats = new Dictionary<string, PlatformValidationStats>
        {
            ["NES"] = new() { PlatformName = "NES", TotalRoms = 50, ValidatedRoms = 40 },
            ["SNES"] = new() { PlatformName = "SNES", TotalRoms = 30, ValidatedRoms = 15 }
        };
        var expectedStats = new RomValidationStatistics
        {
            TotalRoms = 80,
            ValidatedRoms = 55,
            PlatformStats = platformStats
        };

        _mockValidationService.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationStatistics>.Success(expectedStats));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlatformStats.Should().HaveCount(2);
        result.Value.PlatformStats["NES"].CompletionPercentage.Should().Be(80m);
        result.Value.PlatformStats["SNES"].CompletionPercentage.Should().Be(50m);
    }
}
