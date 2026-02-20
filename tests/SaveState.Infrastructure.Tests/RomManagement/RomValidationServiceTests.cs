using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Infrastructure.RomManagement;

namespace SaveState.Infrastructure.Tests.RomManagement;

public class RomValidationServiceTests
{
    private readonly Mock<IFileSystem> _mockFileSystem = new();
    private readonly Mock<IRomFileRepository> _mockRomRepository = new();
    private readonly Mock<IRomHashInfoRepository> _mockHashRepository = new();
    private readonly Mock<IRomValidationReportRepository> _mockReportRepository = new();
    private readonly Mock<ILogger<RomValidationService>> _mockLogger = new();
    private readonly Mock<ITimeProvider> _mockTimeProvider = new();
    private readonly RomValidationService _sut;

    public RomValidationServiceTests()
    {
        _sut = new RomValidationService(
            _mockFileSystem.Object,
            _mockRomRepository.Object,
            _mockHashRepository.Object,
            _mockReportRepository.Object,
            _mockLogger.Object,
            _mockTimeProvider.Object);
    }

    [Fact]
    public async Task CalculateHashesAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var romFile = CreateTestRomFile();
        _mockFileSystem.Setup(f => f.FileExistsAsync(romFile.FilePath.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CalculateHashesAsync(romFile, new RomValidationOptions());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CalculateHashesAsync_WithValidFile_ReturnsHashInfo()
    {
        // Arrange
        var romFile = CreateTestRomFile();
        var fileContent = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        _mockFileSystem.Setup(f => f.FileExistsAsync(romFile.FilePath.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFileSystem.Setup(f => f.ReadAllBytesAsync(romFile.FilePath.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        // Act
        var result = await _sut.CalculateHashesAsync(romFile, new RomValidationOptions());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Crc32.Should().NotBeNullOrEmpty();
        result.Value.Md5.Should().NotBeNullOrEmpty();
        result.Value.Sha1.Should().NotBeNullOrEmpty();
        result.Value.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateHashesAsync_SavesToRepository()
    {
        // Arrange
        var romFile = CreateTestRomFile();
        var fileContent = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        _mockFileSystem.Setup(f => f.FileExistsAsync(romFile.FilePath.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFileSystem.Setup(f => f.ReadAllBytesAsync(romFile.FilePath.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        // Act
        await _sut.CalculateHashesAsync(romFile, new RomValidationOptions());

        // Assert
        _mockHashRepository.Verify(r => r.AddAsync(It.IsAny<RomHashInfo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyFileIntegrityAsync_WithNonExistentFile_ReturnsNotIntact()
    {
        // Arrange
        const string filePath = @"C:\NonExistent\rom.nes";
        _mockFileSystem.Setup(f => f.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.VerifyFileIntegrityAsync(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeFalse();
        result.Value.ReadErrors.Should().Contain("File does not exist");
    }

    [Fact]
    public async Task VerifyFileIntegrityAsync_WithValidFile_ReturnsIntact()
    {
        // Arrange
        const string filePath = @"C:\Roms\test.rom";
        var fileContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

        _mockFileSystem.Setup(f => f.FileExistsAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFileSystem.Setup(f => f.GetFileSizeAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent.Length);
        _mockFileSystem.Setup(f => f.ReadAllBytesAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        // Act
        var result = await _sut.VerifyFileIntegrityAsync(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithNoHashInfos_ReturnsEmptyList()
    {
        // Arrange
        _mockHashRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RomHashInfo>());

        // Act
        var result = await _sut.FindDuplicatesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithDuplicates_ReturnsDuplicateInfo()
    {
        // Arrange
        var romId1 = Guid.NewGuid();
        var romId2 = Guid.NewGuid();
        const string hash = "abc123";

        var hashInfos = new List<RomHashInfo>
        {
            new() { RomFileId = romId1, Sha1 = hash },
            new() { RomFileId = romId2, Sha1 = hash }
        };

        _mockHashRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hashInfos);

        var rom1 = CreateTestRomFile(romId1, "rom1.nes");
        var rom2 = CreateTestRomFile(romId2, "rom2.nes");

        _mockRomRepository.Setup(r => r.GetByIdAsync(romId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rom1);
        _mockRomRepository.Setup(r => r.GetByIdAsync(romId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rom2);

        // Act
        var result = await _sut.FindDuplicatesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Hash.Should().Be(hash);
        result.Value[0].Count.Should().Be(2);
    }

    [Fact]
    public async Task LoadDatFileAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        const string datPath = @"C:\DATs\nointro.xml";
        _mockFileSystem.Setup(f => f.FileExistsAsync(datPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.LoadDatFileAsync(datPath);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task LoadDatFileAsync_WithValidXml_ReturnsEntries()
    {
        // Arrange
        const string datPath = @"C:\DATs\nointro.xml";
        var xmlLines = new[]
        {
            "<?xml version=\"1.0\"?>",
            "<datafile>",
            "  <header>",
            "    <version>1.0</version>",
            "  </header>",
            "  <game name=\"Test Game\">",
            "    <description>Test Game (USA)</description>",
            "    <rom name=\"test.nes\" size=\"4096\" crc=\"a1b2c3d4\" md5=\"d41d8cd98f00b204e9800998ecf8427e\" sha1=\"da39a3ee5e6b4b0d3255bfef95601890afd80709\"/>",
            "  </game>",
            "</datafile>"
        };
        var xmlContent = string.Join("\n", xmlLines);

        _mockFileSystem.Setup(f => f.FileExistsAsync(datPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockFileSystem.Setup(f => f.ReadAllTextAsync(datPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(xmlContent);

        // Act
        var result = await _sut.LoadDatFileAsync(datPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Test Game");
        result.Value[0].GameTitle.Should().Be("Test Game (USA)");
        result.Value[0].Crc32.Should().Be("a1b2c3d4");
        result.Value[0].Md5.Should().Be("d41d8cd98f00b204e9800998ecf8427e");
        result.Value[0].Sha1.Should().Be("da39a3ee5e6b4b0d3255bfef95601890afd80709");
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var roms = new List<RomFile> { CreateTestRomFile(), CreateTestRomFile() };
        var reports = new List<RomValidationReport>
        {
            new() { Status = ValidationStatus.Verified },
            new() { Status = ValidationStatus.BadDump }
        };
        var platforms = new List<Platform>();

        _mockRomRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roms);
        _mockReportRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);
        _mockHashRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RomHashInfo>());
        _mockRomRepository.Setup(r => r.GetAllPlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(platforms);

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRoms.Should().Be(2);
        result.Value.ValidatedRoms.Should().Be(2);
        result.Value.VerifiedRoms.Should().Be(1);
        result.Value.BadDumps.Should().Be(1);
    }

    [Fact]
    public async Task ExportValidationResultsAsync_WithJsonFormat_WritesFile()
    {
        // Arrange
        const string outputPath = @"C:\Reports\validation.json";
        var reports = new List<RomValidationReport>
        {
            new() { RomFileId = Guid.NewGuid(), Status = ValidationStatus.Verified, ValidatedAt = DateTime.UtcNow }
        };

        _mockReportRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        string? capturedContent = null;
        _mockFileSystem.Setup(f => f.WriteAllTextAsync(outputPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((path, content, ct) => capturedContent = content)
            .Returns(Task.CompletedTask);

        var options = new RomValidationExportOptions
        {
            OutputPath = outputPath,
            Format = ValidationExportFormat.Json
        };

        // Act
        var result = await _sut.ExportValidationResultsAsync(options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(outputPath);
        capturedContent.Should().NotBeNull();
        capturedContent.Should().Contain("reports");
    }

    private static RomFile CreateTestRomFile(Guid? id = null, string fileName = "test.nes")
    {
        var romFile = new RomFile(
            "Test ROM",
            Guid.NewGuid(),
            new FilePath($@"C:\Roms\{fileName}"),
            4096);

        if (id.HasValue)
        {
            typeof(RomFile).GetProperty("Id")?.SetValue(romFile, id.Value);
        }

        return romFile;
    }
}
