using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Common;
using SaveState.Application.RomManagement.RomValidation.Commands;
using SaveState.Application.RomManagement.RomValidation.Commands.Handlers;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Application.Tests.RomManagement.RomValidation;

public class ValidateRomCommandHandlerTests
{
    private readonly Mock<IRomValidationService> _mockValidationService = new();
    private readonly Mock<IRomFileRepository> _mockRomRepository = new();
    private readonly Mock<ILogger<ValidateRomCommandHandler>> _mockLogger = new();
    private readonly ValidateRomCommandHandler _sut;

    public ValidateRomCommandHandlerTests()
    {
        _sut = new ValidateRomCommandHandler(
            _mockValidationService.Object,
            _mockRomRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithNonExistentRom_ReturnsFailure()
    {
        // Arrange
        var romId = Guid.NewGuid();
        var command = new ValidateRomCommand(romId);

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RomFile?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithExistingRom_CallsValidationService()
    {
        // Arrange
        var romId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romId);
        var command = new ValidateRomCommand(romId);
        var expectedReport = new RomValidationReport
        {
            RomFileId = romId,
            Status = ValidationStatus.Verified
        };

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockValidationService.Setup(s => s.ValidateRomAsync(romFile, It.IsAny<RomValidationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationReport>.Success(expectedReport));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ValidationStatus.Verified);
        _mockValidationService.Verify(s => s.ValidateRomAsync(romFile, It.IsAny<RomValidationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomOptions_PassesOptionsToService()
    {
        // Arrange
        var romId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romId);
        var customOptions = new RomValidationOptions
        {
            CalculateCrc32 = false,
            CalculateSha1 = true,
            MatchAgainstDatFiles = false
        };
        var command = new ValidateRomCommand(romId, customOptions);

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockValidationService.Setup(s => s.ValidateRomAsync(romFile, customOptions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationReport>.Success(new RomValidationReport()));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _mockValidationService.Verify(s => s.ValidateRomAsync(romFile, customOptions, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        var romId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romId);
        var command = new ValidateRomCommand(romId, null);

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockValidationService.Setup(s => s.ValidateRomAsync(romFile, It.IsAny<RomValidationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationReport>.Success(new RomValidationReport()));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _mockValidationService.Verify(s => s.ValidateRomAsync(romFile, It.Is<RomValidationOptions>(o =>
            o.CalculateCrc32 && o.CalculateMd5 && o.CalculateSha1 && o.MatchAgainstDatFiles),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsFailure()
    {
        // Arrange
        var romId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romId);
        var command = new ValidateRomCommand(romId);

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockValidationService.Setup(s => s.ValidateRomAsync(romFile, It.IsAny<RomValidationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationReport>.Failure("Validation error"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Validation error");
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var romId = Guid.NewGuid();
        var command = new ValidateRomCommand(romId);

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Test exception");
    }

    private static RomFile CreateTestRomFile(Guid id)
    {
        var romFile = new RomFile(
            "Test ROM",
            Guid.NewGuid(),
            new FilePath(@"C:\Roms\test.nes"),
            4096);

        typeof(RomFile).GetProperty("Id")?.SetValue(romFile, id);
        return romFile;
    }
}
