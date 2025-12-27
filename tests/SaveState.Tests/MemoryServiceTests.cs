using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using SaveState.Core.Services.Memory;

namespace SaveState.Tests;

/// <summary>
/// MemoryProfileService Tests
/// </summary>
public class MemoryProfileServiceTests
{
    [Fact]
    public void MemoryProfileService_Constructor_CreatesSuccessfully()
    {
        // Arrange & Act
        var service = new MemoryProfileService();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task MemoryProfileService_GetProfileAsync_ReturnsNullForUnknownGame()
    {
        // Arrange
        var service = new MemoryProfileService();
        var unknownGameId = Guid.NewGuid();

        // Act
        var profile = await service.GetProfileAsync(unknownGameId);

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task MemoryProfileService_SaveProfileAsync_PersistsProfile()
    {
        // Arrange
        var service = new MemoryProfileService();
        var gameId = Guid.NewGuid();
        var profile = new GameMemoryProfile
        {
            GameId = gameId,
            GameTitle = "Test Game"
        };

        // Act
        await service.SaveProfileAsync(profile);
        var retrieved = await service.GetProfileAsync(gameId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(gameId, retrieved.GameId);
        Assert.Equal("Test Game", retrieved.GameTitle);
    }

    [Fact]
    public async Task MemoryProfileService_SaveProfileAsync_OverwritesExisting()
    {
        // Arrange
        var service = new MemoryProfileService();
        var gameId = Guid.NewGuid();
        var originalProfile = new GameMemoryProfile
        {
            GameId = gameId,
            GameTitle = "Original Name"
        };
        var updatedProfile = new GameMemoryProfile
        {
            GameId = gameId,
            GameTitle = "Updated Name"
        };

        // Act
        await service.SaveProfileAsync(originalProfile);
        await service.SaveProfileAsync(updatedProfile);
        var retrieved = await service.GetProfileAsync(gameId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.GameTitle);
    }
}

/// <summary>
/// GameMemoryProfile Tests
/// </summary>
public class GameMemoryProfileTests
{
    [Fact]
    public void GameMemoryProfile_CreateWithMemoryMap()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var profile = new GameMemoryProfile
        {
            GameId = gameId,
            GameTitle = "Final Fantasy VI"
        };

        // Act
        profile.MemoryMap["GOLD"] = new MemoryValueDefinition
        {
            BaseAddress = "0x7E1860",
            Type = MemoryValueType.Int
        };

        profile.MemoryMap["HP"] = new MemoryValueDefinition
        {
            BaseAddress = "0x7E1600",
            Type = MemoryValueType.Int
        };

        // Assert
        Assert.Equal(2, profile.MemoryMap.Count);
        Assert.True(profile.MemoryMap.ContainsKey("GOLD"));
        Assert.True(profile.MemoryMap.ContainsKey("HP"));
    }

    [Fact]
    public void MemoryValueDefinition_SupportsPointerChains()
    {
        // Arrange
        var definition = new MemoryValueDefinition
        {
            BaseAddress = "game.exe+0x12345",
            Offsets = new int[] { 0x10, 0x20, 0x30 },
            Type = MemoryValueType.Float
        };

        // Assert
        Assert.Equal(3, definition.Offsets.Length);
        Assert.Equal(MemoryValueType.Float, definition.Type);
    }

    [Fact]
    public void MemoryValueType_HasExpectedValues()
    {
        // Assert all expected MemoryValueTypes exist
        Assert.True(Enum.IsDefined(typeof(MemoryValueType), MemoryValueType.Int));
        Assert.True(Enum.IsDefined(typeof(MemoryValueType), MemoryValueType.Float));
        Assert.True(Enum.IsDefined(typeof(MemoryValueType), MemoryValueType.String));
        Assert.True(Enum.IsDefined(typeof(MemoryValueType), MemoryValueType.Byte));
    }
}
