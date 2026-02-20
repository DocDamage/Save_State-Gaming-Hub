using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.IkemenGo;

namespace SaveState.Infrastructure.Tests.Mugen;

public class IkemenGoServiceFacadeTests
{
    [Fact]
    public async Task LoadConfigAsync_WhenConfigFileMissing_ReturnsDefaultConfig()
    {
        var sut = new IkemenGoService(NullLogger<IkemenGoService>.Instance, SystemTimeProvider.Instance);
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
        var sut = new IkemenGoService(NullLogger<IkemenGoService>.Instance, SystemTimeProvider.Instance);
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-select.def");

        var result = await sut.ValidateSelectDefAsync(missingPath);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ValidateConfigAsync_WhenValuesOutOfRange_ReturnsInvalidWithErrors()
    {
        var sut = new IkemenGoService(NullLogger<IkemenGoService>.Instance, SystemTimeProvider.Instance);
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
