using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.IkemenGo;
using SaveState.Infrastructure.Mugen.IkemenGo.Managers;

namespace SaveState.Infrastructure.Tests.Mugen;

public class IkemenGoServiceFacadeTests
{
    private static IkemenGoService CreateService()
    {
        var timeProvider = SystemTimeProvider.Instance;

        // Create all managers with their dependencies
        var installationManager = new IkemenGoInstallationManager(
            NullLogger<IkemenGoInstallationManager>.Instance,
            timeProvider);

        var configurationManager = new IkemenGoConfigurationManager(
            NullLogger<IkemenGoConfigurationManager>.Instance,
            timeProvider);

        var launchManager = new IkemenGoLaunchManager(
            NullLogger<IkemenGoLaunchManager>.Instance,
            timeProvider);

        var networkManager = new IkemenGoNetworkManager(
            NullLogger<IkemenGoNetworkManager>.Instance,
            timeProvider,
            new HttpClient());

        var moduleManager = new IkemenGoModuleManager(
            NullLogger<IkemenGoModuleManager>.Instance,
            timeProvider);

        var replayManager = new IkemenGoReplayManager(
            NullLogger<IkemenGoReplayManager>.Instance,
            timeProvider);

        var analyticsManager = new IkemenGoAnalyticsManager(
            NullLogger<IkemenGoAnalyticsManager>.Instance,
            timeProvider);

        var migrationManager = new IkemenGoMigrationManager(
            NullLogger<IkemenGoMigrationManager>.Instance,
            timeProvider);

        return new IkemenGoService(
            installationManager,
            migrationManager,
            configurationManager,
            networkManager,
            moduleManager,
            launchManager,
            replayManager,
            analyticsManager);
    }

    [Fact]
    public async Task LoadConfigAsync_WhenConfigFileMissing_ReturnsDefaultConfig()
    {
        var sut = CreateService();
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-missing-config.json");

        var result = await sut.LoadConfigAsync(missingPath);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Video.Width.Should().Be(1280);
        result.Value.Video.Height.Should().Be(720);
        result.Value.Network.ListenPort.Should().Be(7500);
    }

    [Fact]
    public async Task ValidateSelectDefAsync_WhenFileMissing_ReturnsNotFoundFailure()
    {
        var sut = CreateService();
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-select.def");

        var result = await sut.ValidateSelectDefAsync(missingPath);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ValidateConfigAsync_WhenValuesOutOfRange_ReturnsInvalidWithErrors()
    {
        var sut = CreateService();
        var invalidConfig = new IkemenGoConfig(
            new IkemenGoVideoSettings(100, 120, false, true, 60, "OpenGL"),
            new IkemenGoAudioSettings(200, 80, 100, true),
            new IkemenGoGameplaySettings(4, 0, 99, 2, false, Array.Empty<string>()),
            new IkemenGoNetworkSettings("Player", 7500, 300, true, null, new RollbackNetcodeSettings(true, 1, 8, true)),
            new IkemenGoDebugSettings(false, false, false, false),
            new IkemenGoModuleSettings(true, Array.Empty<string>(), Array.Empty<string>()));

        var result = await sut.ValidateConfigAsync(invalidConfig);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.Errors.Should().NotBeEmpty();
    }
}
