using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Common;
using SaveState.Application.RomManagement.RomValidation.Queries;
using SaveState.Application.RomManagement.RomValidation.Queries.Handlers;
using SaveState.Core.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.Tests.RomManagement.RomValidation;

public class GetDuplicateRomsQueryHandlerTests
{
    private readonly Mock<IRomValidationService> _mockValidationService = new();
    private readonly Mock<ILogger<GetDuplicateRomsQueryHandler>> _mockLogger = new();
    private readonly GetDuplicateRomsQueryHandler _sut;

    public GetDuplicateRomsQueryHandlerTests()
    {
        _sut = new GetDuplicateRomsQueryHandler(
            _mockValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithNoPlatformFilter_CallsServiceWithNullPlatform()
    {
        // Arrange
        var query = new GetDuplicateRomsQuery();
        var expectedDuplicates = new List<DuplicateRomInfo>();

        _mockValidationService.Setup(s => s.FindDuplicatesAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DuplicateRomInfo>>.Success(expectedDuplicates));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockValidationService.Verify(s => s.FindDuplicatesAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPlatformFilter_CallsServiceWithPlatformId()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var query = new GetDuplicateRomsQuery(platformId);
        var expectedDuplicates = new List<DuplicateRomInfo>();

        _mockValidationService.Setup(s => s.FindDuplicatesAsync(platformId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DuplicateRomInfo>>.Success(expectedDuplicates));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockValidationService.Verify(s => s.FindDuplicatesAsync(platformId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithHashTypeFilter_CallsServiceWithHashType()
    {
        // Arrange
        var query = new GetDuplicateRomsQuery(null, HashAlgorithmType.Md5);
        var expectedDuplicates = new List<DuplicateRomInfo>();

        _mockValidationService.Setup(s => s.FindDuplicatesAsync(null, HashAlgorithmType.Md5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DuplicateRomInfo>>.Success(expectedDuplicates));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockValidationService.Verify(s => s.FindDuplicatesAsync(null, HashAlgorithmType.Md5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicates_ReturnsDuplicates()
    {
        // Arrange
        var query = new GetDuplicateRomsQuery();
        var expectedDuplicates = new List<DuplicateRomInfo>
        {
            new()
            {
                Hash = "abc123",
                HashType = HashAlgorithmType.Sha1,
                Duplicates = new List<RomDuplicateEntry>
                {
                    new() { FileName = "rom1.nes", FileSize = 4096 },
                    new() { FileName = "rom2.nes", FileSize = 4096 }
                }
            }
        };

        _mockValidationService.Setup(s => s.FindDuplicatesAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DuplicateRomInfo>>.Success(expectedDuplicates));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Hash.Should().Be("abc123");
        result.Value[0].Count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenServiceFails_ReturnsFailure()
    {
        // Arrange
        var query = new GetDuplicateRomsQuery();

        _mockValidationService.Setup(s => s.FindDuplicatesAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DuplicateRomInfo>>.Failure("Service error"));

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
        var query = new GetDuplicateRomsQuery();

        _mockValidationService.Setup(s => s.FindDuplicatesAsync(null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Test exception");
    }
}
