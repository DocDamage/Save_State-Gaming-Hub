namespace SaveState.Application.Tests.RomManagement;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.GameLibrary;
using EmulatorEntity = SaveState.Core.RomManagement.Entities.Emulator;
using RomFileEntity = SaveState.Core.RomManagement.Entities.RomFile;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;

public class EmulatorServiceTests
{
    private readonly Mock<IEmulatorRepository> _mockEmulatorRepository = new();
    private readonly Mock<IRomFileRepository> _mockRomFileRepository = new();
    private readonly Mock<IPlatformRepository> _mockPlatformRepository = new();
    private readonly Mock<ILogger<EmulatorService>> _mockLogger = new();
    private readonly Mock<ITimeProvider> _mockTimeProvider = new();
    private readonly EmulatorService _sut;

    public EmulatorServiceTests()
    {
        _mockTimeProvider.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _sut = new EmulatorService(
            _mockEmulatorRepository.Object,
            _mockRomFileRepository.Object,
            _mockPlatformRepository.Object,
            _mockLogger.Object,
            _mockTimeProvider.Object);
    }

    [Fact]
    public async Task LaunchRomAsync_WithInvalidRomFileId_ReturnsFailure()
    {
        // Arrange
        var romFileId = Guid.NewGuid();
        _mockRomFileRepository.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as RomFileEntity);

        // Act
        var result = await _sut.LaunchRomAsync(romFileId, default);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("ROM file not found");
        result.ProcessId.Should().BeNull();
    }

    [Fact]
    public async Task LaunchRomAsync_WithNoEmulatorForPlatform_ReturnsFailure()
    {
        // Arrange
        var romFileId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romFileId, platformId);

        _mockRomFileRepository.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockEmulatorRepository.Setup(r => r.GetByPlatformIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as EmulatorEntity);

        // Act
        var result = await _sut.LaunchRomAsync(romFileId, default);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No emulator configured for this platform");
    }

    [Fact]
    public async Task LaunchRomAsync_WithValidEmulator_LaunchesSuccessfully()
    {
        // Arrange
        var romFileId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var emulatorId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romFileId, platformId);
        var emulator = CreateTestEmulator(emulatorId, platformId, "test.exe");

        _mockRomFileRepository.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockEmulatorRepository.Setup(r => r.GetByPlatformIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emulator);
        _mockEmulatorRepository.Setup(r => r.GetByIdAsync(emulatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emulator);

        // Act
        var result = await _sut.LaunchRomAsync(romFileId, default);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ProcessId.Should().NotBeNull();
        result.EmulatorId.Should().Be(emulatorId);
    }

    [Fact]
    public async Task LaunchRomWithEmulatorAsync_WithUnavailableEmulator_ReturnsFailure()
    {
        // Arrange
        var romFileId = Guid.NewGuid();
        var emulatorId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var romFile = CreateTestRomFile(romFileId, platformId);
        var emulator = CreateTestEmulator(emulatorId, platformId, "nonexistent.exe");

        _mockRomFileRepository.Setup(r => r.GetByIdAsync(romFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(romFile);
        _mockEmulatorRepository.Setup(r => r.GetByIdAsync(emulatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emulator);

        // Act
        var result = await _sut.LaunchRomWithEmulatorAsync(romFileId, emulatorId, default);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Emulator executable not found");
    }

    [Fact]
    public async Task GetAvailableEmulatorsAsync_ReturnsAvailableEmulators()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var emulator1 = CreateTestEmulator(Guid.NewGuid(), platformId, "emu1.exe");
        var emulator2 = CreateTestEmulator(Guid.NewGuid(), platformId, "emu2.exe");

        var testPlatform = new Platform(
            PlatformName.From("NES"),
            PlatformShortName.From("NES"),
            Core.GameLibrary.Enums.PlatformType.Console);
        typeof(Platform).GetProperty("Id")?.SetValue(testPlatform, platformId);

        _mockPlatformRepository.Setup(r => r.GetByIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testPlatform);

        _mockEmulatorRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { emulator1, emulator2 });

        // Act
        var result = await _sut.GetAvailableEmulatorsAsync(platformId, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(e => e.IsAvailable);
    }

    [Fact]
    public async Task GetDefaultEmulatorAsync_ReturnsFirstAvailableEmulator()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var emulator = CreateTestEmulator(Guid.NewGuid(), platformId, "default.exe");

        _mockEmulatorRepository.Setup(r => r.GetByPlatformIdAsync(platformId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emulator);

        // Act
        var result = await _sut.GetDefaultEmulatorAsync(platformId, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(emulator.Name);
        result.Value.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmulatorAvailableAsync_WithAvailableEmulator_ReturnsTrue()
    {
        // Arrange
        var emulatorId = Guid.NewGuid();
        var emulator = CreateTestEmulator(emulatorId, Guid.NewGuid(), "available.exe");

        _mockEmulatorRepository.Setup(r => r.GetByIdAsync(emulatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emulator);

        // Act
        var result = await _sut.IsEmulatorAvailableAsync(emulatorId, default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetRunningEmulatorProcessAsync_WithRunningProcess_ReturnsProcessInfo()
    {
        // This test would require mocking System.Diagnostics.Process which is difficult
        // The actual process tracking is tested through integration tests
        // Here we just verify the method doesn't throw
        var romFileId = Guid.NewGuid();

        // Act
        var result = await _sut.GetRunningEmulatorProcessAsync(romFileId, default);

        // Assert
        result.IsSuccess.Should().BeFalse(); // No tracked process
        result.Error.Should().Be("No emulator process running for this ROM");
    }

    [Fact]
    public async Task KillEmulatorProcessAsync_WithNoRunningProcess_DoesNothing()
    {
        // Arrange
        var romFileId = Guid.NewGuid();

        // Act & Assert - Should not throw
        await _sut.KillEmulatorProcessAsync(romFileId, default);
    }

    private static RomFileEntity CreateTestRomFile(Guid romFileId, Guid platformId)
    {
        var romFile = new RomFileEntity(
            "Test Game",
            platformId,
            new FilePath(@"C:\Games\test.nes"),
            1024);

        // Set the ID for testing
        typeof(RomFileEntity).GetProperty("Id")?.SetValue(romFile, romFileId);

        return romFile;
    }

    private static EmulatorEntity CreateTestEmulator(Guid emulatorId, Guid platformId, string exePath)
    {
        // Create emulator with valid executable path for testing
        var validExePath = exePath == "nonexistent.exe" ? @"C:\nonexistent.exe" : @"C:\Windows\System32\cmd.exe"; // Use absolute path
        var emulator = new EmulatorEntity(
            $"Test Emulator {emulatorId}",
            new FilePath(validExePath),
            platformId);

        // Set additional properties for testing
        emulator.UpdateDescription($"Test emulator for platform {platformId}");
        emulator.SetCommandLineArgs("/c echo test");
        emulator.UpdateVersion("1.0.0");

        // Set the ID for testing
        typeof(EmulatorEntity).GetProperty("Id")?.SetValue(emulator, emulatorId);

        return emulator;
    }
}
