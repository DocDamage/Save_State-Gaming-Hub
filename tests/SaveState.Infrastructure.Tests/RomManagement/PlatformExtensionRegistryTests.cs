using FluentAssertions;
using Xunit;
using SaveState.Infrastructure.RomManagement.Services;

namespace SaveState.Infrastructure.Tests.RomManagement;

public class PlatformExtensionRegistryTests
{
    private readonly PlatformExtensionRegistry _sut = new();

    [Theory]
    [InlineData("NES", new[] { ".nes", ".unf", ".unif" })]
    [InlineData("SNES", new[] { ".sfc", ".smc", ".fig", ".swc" })]
    [InlineData("PlayStation", new[] { ".bin", ".cue", ".iso", ".img", ".pbp" })]
    [InlineData("Nintendo 64", new[] { ".n64", ".z64", ".v64", ".rom" })]
    public void GetExtensions_WithValidPlatformName_ReturnsCorrectExtensions(string platformName, string[] expectedExtensions)
    {
        // Act
        var result = _sut.GetExtensions(platformName);

        // Assert
        result.Should().BeEquivalentTo(expectedExtensions);
    }

    [Fact]
    public void GetExtensions_WithUnknownPlatformName_ReturnsEmptyArray()
    {
        // Act
        var result = _sut.GetExtensions("UnknownPlatform");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetExtensions_WithNullOrEmptyPlatformName_ReturnsEmptyArray()
    {
        // Act & Assert
        _sut.GetExtensions(null!).Should().BeEmpty();
        _sut.GetExtensions("").Should().BeEmpty();
        _sut.GetExtensions("   ").Should().BeEmpty();
    }

    [Theory]
    [InlineData("game.nes", "NES")]
    [InlineData("game.sfc", "SNES")]
    [InlineData("game.bin", "PlayStation")]
    [InlineData("game.n64", "Nintendo 64")]
    public void DetectPlatformName_WithValidFilePath_ReturnsCorrectPlatform(string filePath, string expectedPlatform)
    {
        // Act
        var result = _sut.DetectPlatformName(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedPlatform);
    }

    [Fact]
    public void DetectPlatformName_WithUnknownExtension_ReturnsFailure()
    {
        // Act
        var result = _sut.DetectPlatformName("game.unknown");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("extension '.unknown'");
    }

    [Theory]
    [InlineData("Super Mario Bros.nes", "NES", true)]
    [InlineData("Chrono Trigger.sfc", "SNES", true)]
    [InlineData("Final Fantasy VII.bin", "PlayStation", true)]
    [InlineData("Super Mario 64.n64", "Nintendo 64", true)]
    [InlineData("game.unknown", "NES", false)]
    [InlineData("game.nes", "SNES", false)]
    public void IsValidExtension_WithFilePathAndPlatform_ReturnsCorrectResult(string filePath, string platformName, bool expectedResult)
    {
        // Act
        var result = _sut.IsValidExtension(platformName, filePath);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void GetExtensions_IsCaseInsensitive()
    {
        // Act
        var result1 = _sut.GetExtensions("nes");
        var result2 = _sut.GetExtensions("NES");
        var result3 = _sut.GetExtensions("Nes");

        // Assert
        result1.Should().BeEquivalentTo(result2);
        result2.Should().BeEquivalentTo(result3);
    }

    [Fact]
    public void DetectPlatformName_IsCaseInsensitive()
    {
        // Act
        var result1 = _sut.DetectPlatformName("game.NES");
        var result2 = _sut.DetectPlatformName("game.nes");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(result2.Value);
    }

    [Fact]
    public void IsValidExtension_IsCaseInsensitive()
    {
        // Act
        var result1 = _sut.IsValidExtension("NES", "game.NES");
        var result2 = _sut.IsValidExtension("nes", "game.nes");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}
