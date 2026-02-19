using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Infrastructure.RomManagement;

namespace SaveState.Infrastructure.Tests.RomManagement;

public class RomValidationBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<ILogger<RomValidationBackgroundService>> _loggerMock;
    private readonly Mock<IOptions<RomValidationOptions>> _optionsMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IRomFileRepository> _romFileRepositoryMock;
    private readonly Mock<IRomValidationService> _validationServiceMock;

    public RomValidationBackgroundServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<RomValidationBackgroundService>>();
        _optionsMock = new Mock<IOptions<RomValidationOptions>>();
        _mediatorMock = new Mock<IMediator>();
        _romFileRepositoryMock = new Mock<IRomFileRepository>();
        _validationServiceMock = new Mock<IRomValidationService>();

        _optionsMock.Setup(o => o.Value).Returns(new RomValidationOptions());

        // Setup scope factory
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        
        _serviceScopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_serviceScopeMock.Object);
        
        _serviceScopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        // Setup services in scope
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(MediatR.IMediator)))
            .Returns(_mediatorMock.Object);
        
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IRomFileRepository)))
            .Returns(_romFileRepositoryMock.Object);
        
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IRomValidationService)))
            .Returns(_validationServiceMock.Object);
    }

    [Fact]
    public async Task ValidateRomOnImportAsync_RomNotFound_ShouldLogWarning()
    {
        // Arrange
        var service = new RomValidationBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);

        var romId = Guid.NewGuid();
        _romFileRepositoryMock
            .Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RomFile?)null);

        // Act
        await service.ValidateRomOnImportAsync(romId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ROM not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateRomOnImportAsync_ValidationSuccessful_ShouldLogSuccess()
    {
        // Arrange
        var service = new RomValidationBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);

        var romId = Guid.NewGuid();
        var romFile = new RomFile(
            title: "test.rom",
            platformId: Guid.NewGuid(),
            filePath: new SaveState.Core.RomManagement.ValueObjects.FilePath("/roms/test.rom"),
            fileSize: 1024
        );

        _romFileRepositoryMock
            .Setup(r => r.GetByIdAsync(romId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);

        var validationReport = new RomValidationReport
        {
            RomFileId = romId,
            Status = ValidationStatus.Valid
        };

        _validationServiceMock
            .Setup(v => v.ValidateRomAsync(romFile, It.IsAny<RomValidationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RomValidationReport>.Success(validationReport));

        // Act
        await service.ValidateRomOnImportAsync(romId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_Success_ShouldLogResults()
    {
        // Arrange
        var service = new RomValidationBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);

        var duplicateInfo = new DuplicateRomInfo
        {
            Hash = "abc123",
            HashType = HashAlgorithmType.Sha1,
            Duplicates = new List<RomDuplicateEntry>
            {
                new() { RomFileId = Guid.NewGuid(), FileName = "test1.rom", Directory = "/roms", FileSize = 1024 },
                new() { RomFileId = Guid.NewGuid(), FileName = "test2.rom", Directory = "/roms/backup", FileSize = 1024 }
            }
        };

        _validationServiceMock
            .Setup(v => v.FindDuplicatesAsync(null, HashAlgorithmType.Sha1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<DuplicateRomInfo>>.Success(new List<DuplicateRomInfo> { duplicateInfo }));

        // Act
        await service.ScanForDuplicatesAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Duplicate scan completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RomValidationBackgroundOptions_Defaults_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new RomValidationBackgroundOptions();

        // Assert
        options.ValidateOnImport.Should().BeTrue();
        options.EnableScheduledValidation.Should().BeTrue();
        options.ValidationIntervalHours.Should().Be(24);
        options.EnableDuplicateScanning.Should().BeTrue();
        options.DuplicateScanIntervalHours.Should().Be(168);
    }

    [Fact]
    public void RomValidationBackgroundOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new RomValidationBackgroundOptions
        {
            ValidateOnImport = false,
            EnableScheduledValidation = false,
            ValidationIntervalHours = 12,
            EnableDuplicateScanning = false,
            DuplicateScanIntervalHours = 72
        };

        // Assert
        options.ValidateOnImport.Should().BeFalse();
        options.EnableScheduledValidation.Should().BeFalse();
        options.ValidationIntervalHours.Should().Be(12);
        options.EnableDuplicateScanning.Should().BeFalse();
        options.DuplicateScanIntervalHours.Should().Be(72);
    }
}
