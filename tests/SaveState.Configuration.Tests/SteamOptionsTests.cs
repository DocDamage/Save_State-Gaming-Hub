using FluentAssertions;
using SaveState.Core.Configuration;
using Xunit;

namespace SaveState.Configuration.Tests;

/// <summary>
/// Tests for Steam configuration options.
/// Validates API key and Steam ID configuration.
/// </summary>
public class SteamOptionsTests
{
    [Fact]
    public void SteamOptions_DefaultValues_AreEmpty()
    {
        // Arrange & Act
        var options = new SteamOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
        options.SteamId.Should().BeEmpty();
    }

    [Fact]
    public void SteamOptions_CanBeModified()
    {
        // Arrange
        var options = new SteamOptions();

        // Act
        options.ApiKey = "ABC123DEF456";
        options.SteamId = "76561198000000000";

        // Assert
        options.ApiKey.Should().Be("ABC123DEF456");
        options.SteamId.Should().Be("76561198000000000");
    }

    [Fact]
    public void SteamOptions_ApiKey_HandlesDifferentFormats()
    {
        // Arrange
        var options = new SteamOptions();

        // Act & Assert
        options.ApiKey = "ABC123";
        options.ApiKey.Should().Be("ABC123");

        options.ApiKey = "1234567890ABCDEF";
        options.ApiKey.Should().Be("1234567890ABCDEF");

        options.ApiKey = "";
        options.ApiKey.Should().BeEmpty();

        options.ApiKey = null!;
        options.ApiKey.Should().BeNull();
    }

    [Fact]
    public void SteamOptions_SteamId_HandlesDifferentFormats()
    {
        // Arrange
        var options = new SteamOptions();

        // Act & Assert
        options.SteamId = "76561198000000000";
        options.SteamId.Should().Be("76561198000000000");

        options.SteamId = "12345";
        options.SteamId.Should().Be("12345");

        options.SteamId = "";
        options.SteamId.Should().BeEmpty();

        options.SteamId = null!;
        options.SteamId.Should().BeNull();
    }

    [Fact]
    public void SteamOptions_SteamId_AcceptsValidSteamIdFormats()
    {
        // Arrange
        var options = new SteamOptions();

        // Act & Assert - Valid SteamID64 formats
        var validSteamIds = new[]
        {
            "76561198000000000", // Valid SteamID64
            "76561197960265728", // Another valid SteamID64
            "76561198888888888"  // Another valid SteamID64
        };

        foreach (var steamId in validSteamIds)
        {
            options.SteamId = steamId;
            options.SteamId.Should().Be(steamId);
            options.SteamId.Should().MatchRegex(@"^\d{17}$"); // SteamID64 are 17 digits
        }
    }
}
