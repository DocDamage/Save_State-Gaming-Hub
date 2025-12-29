namespace SaveState.Application.Tests.RomManagement;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Core.RomManagement;

public class RomScannerServiceTests
{
    private readonly Mock<IPlatformRepository> _mockPlatformRepository = new();
    private readonly Mock<IPlatformExtensionRegistry> _mockExtensionRegistry = new();
    private readonly Mock<ILogger<RomScannerService>> _mockLogger = new();
    private readonly RomScannerService _sut;

    public RomScannerServiceTests()
    {
        // Setup extension registry to return NES extensions by default
        _mockExtensionRegistry.Setup(r => r.GetExtensions("NES"))
            .Returns(new[] { ".nes", ".unf", ".unif" });

        _mockExtensionRegistry.Setup(r => r.GetExtensions("SNES"))
            .Returns(new[] { ".sfc", ".smc", ".fig", ".swc" });

        _mockExtensionRegistry.Setup(r => r.GetExtensions("PlayStation"))
            .Returns(new[] { ".bin", ".cue", ".iso", ".img", ".pbp" });

        _mockExtensionRegistry.Setup(r => r.GetExtensions("Nintendo 64"))
            .Returns(new[] { ".n64", ".z64", ".v64", ".rom" });

        _sut = new RomScannerService(
            _mockPlatformRepository.Object,
            _mockExtensionRegistry.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ScanFolderAsync_WithInvalidFolderPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ScanFolderAsync("", TestPlatformId, false, null, default));
    }

    [Fact]
    public async Task ScanFolderAsync_WithInvalidPlatformId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ScanFolderAsync(@"C:\Games", Guid.Empty, false, null, default));
    }

    [Fact]
    public async Task ScanFolderAsync_WithNonExistentFolder_ReturnsEmptyList()
    {
        // Arrange
        _mockPlatformRepository.Setup(r => r.GetByIdAsync(TestPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform("NES"));

        // Act
        var result = await _sut.ScanFolderAsync(@"C:\NonExistentFolder", TestPlatformId, false, null, default);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanFolderAsync_WithUnknownPlatform_ReturnsEmptyList()
    {
        // Arrange
        var unknownPlatformId = Guid.NewGuid();
        _mockPlatformRepository.Setup(r => r.GetByIdAsync(unknownPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        // Act
        var result = await _sut.ScanFolderAsync(@"C:\Games", unknownPlatformId, false, null, default);

        // Assert
        result.Should().BeEmpty();
    }


    [Fact]
    public async Task ScanFolderAsync_ReportsProgress()
    {
        // Arrange
        var progressReports = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(p => progressReports.Add(p));

        _mockPlatformRepository.Setup(r => r.GetByIdAsync(TestPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform("NES"));

        // Create a temporary directory with test files
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create some test ROM files
            var romFile1 = Path.Combine(tempDir, "game1.nes");
            var romFile2 = Path.Combine(tempDir, "game2.nes");
            await File.WriteAllTextAsync(romFile1, "dummy rom 1");
            await File.WriteAllTextAsync(romFile2, "dummy rom 2");

            // Act
            var result = await _sut.ScanFolderAsync(tempDir, TestPlatformId, false, progress, default);

            // Assert
            result.Should().HaveCount(2);
            progressReports.Should().NotBeEmpty();
            progressReports.Should().AllSatisfy(p =>
            {
                p.FilesScanned.Should().BeGreaterThan(0);
                p.FilesTotal.Should().Be(2);
                p.CurrentFile.Should().NotBeNullOrEmpty();
                p.RomsFound.Should().BeGreaterThanOrEqualTo(0);
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_CreatesRomFileEntities()
    {
        // Arrange
        _mockPlatformRepository.Setup(r => r.GetByIdAsync(TestPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform("NES"));

        // Create a temporary directory with a test file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var romFile = Path.Combine(tempDir, "Super Mario Bros.nes");
            await File.WriteAllTextAsync(romFile, "dummy rom content");

            // Act
            var result = await _sut.ScanFolderAsync(tempDir, TestPlatformId, false, null, default);

            // Assert
            result.Should().HaveCount(1);
            var rom = result[0];
            rom.Title.Should().Be("Super Mario Bros");
            rom.PlatformId.Should().Be(TestPlatformId);
            rom.FilePath.Value.Should().Be(romFile);
            rom.FileSize.Should().BeGreaterThan(0);
            rom.Status.Should().Be(Core.RomManagement.Enums.RomStatus.Scanned);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_HandlesRecursiveScanning()
    {
        // Arrange
        _mockPlatformRepository.Setup(r => r.GetByIdAsync(TestPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform("NES"));

        // Create a temporary directory structure
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(subDir);

        try
        {
            var romFile1 = Path.Combine(tempDir, "game1.nes");
            var romFile2 = Path.Combine(subDir, "game2.nes");
            await File.WriteAllTextAsync(romFile1, "dummy rom 1");
            await File.WriteAllTextAsync(romFile2, "dummy rom 2");

            // Act - recursive scan
            var result = await _sut.ScanFolderAsync(tempDir, TestPlatformId, true, null, default);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(r => r.Title == "game1");
            result.Should().Contain(r => r.Title == "game2");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_IgnoresNonRomFiles()
    {
        // Arrange
        _mockPlatformRepository.Setup(r => r.GetByIdAsync(TestPlatformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform("NES"));

        // Create a temporary directory with mixed files
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var romFile = Path.Combine(tempDir, "game.nes");
            var textFile = Path.Combine(tempDir, "readme.txt");
            var imageFile = Path.Combine(tempDir, "cover.jpg");
            await File.WriteAllTextAsync(romFile, "dummy rom");
            await File.WriteAllTextAsync(textFile, "dummy text");
            await File.WriteAllTextAsync(imageFile, "dummy image");

            // Act
            var result = await _sut.ScanFolderAsync(tempDir, TestPlatformId, false, null, default);

            // Assert
            result.Should().HaveCount(1);
            result[0].Title.Should().Be("game");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [InlineData("NES", new[] { ".nes" })]
    [InlineData("SNES", new[] { ".sfc", ".smc", ".fig" })]
    [InlineData("Nintendo 64", new[] { ".n64", ".z64", ".v64", ".rom" })]
    [InlineData("PlayStation", new[] { ".bin", ".cue", ".iso", ".img" })]
    public async Task ScanFolderAsync_SupportsMultiplePlatforms(string platformName, string[] expectedExtensions)
    {
        // Arrange
        var platformId = Guid.NewGuid();
        _mockPlatformRepository.Setup(r => r.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform(platformName));

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files with expected extensions
            var testFiles = expectedExtensions
                .Select(ext => Path.Combine(tempDir, $"test{ext}"))
                .ToList();

            foreach (var file in testFiles)
            {
                await File.WriteAllTextAsync(file, "dummy content");
            }

            // Act
            var result = await _sut.ScanFolderAsync(tempDir, platformId, false, null, default);

            // Assert
            result.Should().HaveCount(expectedExtensions.Length);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static readonly Guid TestPlatformId = Guid.NewGuid();

    private static Platform CreateTestPlatform(string name)
    {
        var platform = new Platform(
            PlatformName.From(name),
            PlatformShortName.From(name.ToUpper().Substring(0, Math.Min(3, name.Length))),
            Core.GameLibrary.Enums.PlatformType.Console);

        // Set the ID for testing
        typeof(Platform).GetProperty("Id")?.SetValue(platform, TestPlatformId);

        return platform;
    }
}
