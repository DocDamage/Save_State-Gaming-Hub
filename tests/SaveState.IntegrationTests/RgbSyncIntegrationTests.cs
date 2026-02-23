using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;

namespace SaveState.IntegrationTests;

/// <summary>
/// Integration tests for RGB synchronization functionality.
/// Tests device discovery, effect application, profile management, and game state triggers.
/// </summary>
public class RgbSyncIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IRgbSyncService _rgbSyncService;

    public RgbSyncIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _rgbSyncService = _fixture.ServiceProvider.GetRequiredService<IRgbSyncService>();
    }

    #region Device Discovery Tests

    [Fact]
    public async Task DiscoverDevices_ReturnsListOfDevices()
    {
        // Act
        var result = await _rgbSyncService.DiscoverDevicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConnectedDevices_ReturnsOnlyConnectedDevices()
    {
        // Act
        var result = await _rgbSyncService.GetConnectedDevicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().OnlyContain(d => d.IsConnected);
    }

    [Fact]
    public async Task GetDevicesByType_ReturnsFilteredDevices()
    {
        // Act
        var result = await _rgbSyncService.GetDevicesByTypeAsync(RgbDeviceType.Keyboard);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().OnlyContain(d => d.Type == RgbDeviceType.Keyboard);
    }

    [Fact]
    public async Task GetDevice_ById_ReturnsDevice()
    {
        // Arrange
        var devices = await _rgbSyncService.DiscoverDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;

            // Act
            var result = await _rgbSyncService.GetDeviceAsync(deviceId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(deviceId);
        }
    }

    [Fact]
    public async Task ConnectDevice_ConnectsSuccessfully()
    {
        // Arrange
        var devices = await _rgbSyncService.DiscoverDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var device = devices.Value.First(d => !d.IsConnected);

            // Act
            var result = await _rgbSyncService.ConnectDeviceAsync(device.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Verify connection
            var deviceResult = await _rgbSyncService.GetDeviceAsync(device.Id);
            deviceResult.Value.IsConnected.Should().BeTrue();
        }
    }

    [Fact]
    public async Task DisconnectDevice_DisconnectsSuccessfully()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;

            // Act
            var result = await _rgbSyncService.DisconnectDeviceAsync(deviceId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    #endregion

    #region Effect Application Tests

    [Fact]
    public async Task ApplyEffect_ToDevice_AppliesSuccessfully()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var device = devices.Value.First();
            var effect = CreateSampleRgbEffect("Test Static Effect");
            effect = effect with { Type = RgbEffectType.Static };

            // Act
            var result = await _rgbSyncService.ApplyEffectAsync(device.Id, effect);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(RgbEffectType.Static)]
    [InlineData(RgbEffectType.Breathing)]
    [InlineData(RgbEffectType.ColorCycle)]
    [InlineData(RgbEffectType.Rainbow)]
    [InlineData(RgbEffectType.Wave)]
    [InlineData(RgbEffectType.Reactive)]
    [InlineData(RgbEffectType.Starlight)]
    public async Task ApplyEffect_WithDifferentTypes_AppliesSuccessfully(RgbEffectType effectType)
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var device = devices.Value.First();
            var effect = CreateSampleRgbEffect($"Test {effectType}");
            effect = effect with { Type = effectType };

            // Act
            var result = await _rgbSyncService.ApplyEffectAsync(device.Id, effect);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ApplyEffect_ToMultipleDevices_AppliesToAll()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var deviceIds = devices.Value.Take(2).Select(d => d.Id).ToList();
            var effect = CreateSampleRgbEffect("Multi-Device Effect");

            // Act
            var result = await _rgbSyncService.ApplyEffectToMultipleAsync(deviceIds, effect);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ClearEffect_RemovesEffect()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;
            var effect = CreateSampleRgbEffect();
            await _rgbSyncService.ApplyEffectAsync(deviceId, effect);

            // Act
            var result = await _rgbSyncService.ClearEffectAsync(deviceId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SetDeviceColor_SetsStaticColor()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;
            var color = RgbColor.Blue;

            // Act
            var result = await _rgbSyncService.SetDeviceColorAsync(deviceId, color);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SetDeviceBrightness_SetsBrightnessLevel()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;
            var brightness = 0.75f;

            // Act
            var result = await _rgbSyncService.SetDeviceBrightnessAsync(deviceId, brightness);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    [InlineData(0.75f)]
    [InlineData(1f)]
    public async Task SetDeviceBrightness_WithDifferentLevels_WorksCorrectly(float brightness)
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;

            // Act
            var result = await _rgbSyncService.SetDeviceBrightnessAsync(deviceId, brightness);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SetEffectSpeed_UpdatesAnimationSpeed()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;
            var speed = 1.5f;

            // Act
            var result = await _rgbSyncService.SetEffectSpeedAsync(deviceId, speed);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    #endregion

    #region Profile Management Tests

    [Fact]
    public async Task CreateProfile_CreatesNewProfile()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Test Profile");

        // Act
        var result = await _rgbSyncService.CreateProfileAsync(profile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(profile.Name);
    }

    [Fact]
    public async Task GetProfile_ById_ReturnsProfile()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Get Profile Test");
        var createResult = await _rgbSyncService.CreateProfileAsync(profile);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _rgbSyncService.GetProfileAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(createResult.Value.Id);
    }

    [Fact]
    public async Task GetAllProfiles_ReturnsAllProfiles()
    {
        // Arrange - Create a few profiles
        for (int i = 0; i < 3; i++)
        {
            var profile = CreateSampleRgbProfile($"Profile {i}");
            await _rgbSyncService.CreateProfileAsync(profile);
        }

        // Act
        var result = await _rgbSyncService.GetAllProfilesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task UpdateProfile_UpdatesProfileData()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Original Name");
        var createResult = await _rgbSyncService.CreateProfileAsync(profile);
        createResult.IsSuccess.Should().BeTrue();

        var updatedProfile = createResult.Value with { Name = "Updated Name" };

        // Act
        var result = await _rgbSyncService.UpdateProfileAsync(updatedProfile);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _rgbSyncService.GetProfileAsync(createResult.Value.Id);
        getResult.Value.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteProfile_RemovesProfile()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Delete Profile Test");
        var createResult = await _rgbSyncService.CreateProfileAsync(profile);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _rgbSyncService.DeleteProfileAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _rgbSyncService.GetProfileAsync(createResult.Value.Id);
        getResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyProfile_AppliesProfileToDevices()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var profile = CreateSampleRgbProfile("Apply Test Profile");
            foreach (var device in devices.Value)
            {
                profile.DeviceEffects[device.Id] = CreateSampleRgbEffect();
            }

            var createResult = await _rgbSyncService.CreateProfileAsync(profile);
            createResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _rgbSyncService.ApplyProfileAsync(createResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SetDefaultProfile_SetsAsDefault()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Default Profile");
        profile = profile with { IsDefault = false };
        var createResult = await _rgbSyncService.CreateProfileAsync(profile);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _rgbSyncService.SetDefaultProfileAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _rgbSyncService.GetProfileAsync(createResult.Value.Id);
        getResult.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task DuplicateProfile_CreatesCopy()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Original Profile");
        var createResult = await _rgbSyncService.CreateProfileAsync(profile);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _rgbSyncService.DuplicateProfileAsync(createResult.Value.Id, "Copied Profile");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Copied Profile");
        result.Value.Id.Should().NotBe(createResult.Value.Id);
    }

    [Fact]
    public async Task ExportProfile_ReturnsProfileData()
    {
        // Arrange
        var profile = CreateSampleRgbProfile("Export Test Profile");
        var createResult = await _rgbSyncService.CreateProfileAsync(profile);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _rgbSyncService.ExportProfileAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ImportProfile_CreatesProfileFromData()
    {
        // Arrange
        var profileData = @"{
            ""Name"": ""Imported Profile"",
            ""DeviceEffects"": {}
        }";

        // Act
        var result = await _rgbSyncService.ImportProfileAsync(profileData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Imported Profile");
    }

    #endregion

    #region Sync Groups Tests

    [Fact]
    public async Task CreateSyncGroup_CreatesNewGroup()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var group = new RgbSyncGroup
            {
                Name = "Test Sync Group",
                DeviceIds = devices.Value.Take(2).Select(d => d.Id).ToList(),
                SharedEffect = CreateSampleRgbEffect("Group Effect")
            };

            // Act
            var result = await _rgbSyncService.CreateSyncGroupAsync(group);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(group.Name);
        }
    }

    [Fact]
    public async Task GetSyncGroup_ReturnsGroup()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var group = new RgbSyncGroup
            {
                Name = "Get Group Test",
                DeviceIds = devices.Value.Take(2).Select(d => d.Id).ToList(),
                SharedEffect = CreateSampleRgbEffect()
            };
            var createResult = await _rgbSyncService.CreateSyncGroupAsync(group);
            createResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _rgbSyncService.GetSyncGroupAsync(createResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(createResult.Value.Id);
        }
    }

    [Fact]
    public async Task UpdateSyncGroup_UpdatesGroup()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var group = new RgbSyncGroup
            {
                Name = "Update Group Test",
                DeviceIds = devices.Value.Take(2).Select(d => d.Id).ToList(),
                SharedEffect = CreateSampleRgbEffect()
            };
            var createResult = await _rgbSyncService.CreateSyncGroupAsync(group);
            createResult.IsSuccess.Should().BeTrue();

            var updatedGroup = createResult.Value with { Name = "Updated Group Name" };

            // Act
            var result = await _rgbSyncService.UpdateSyncGroupAsync(updatedGroup);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var getResult = await _rgbSyncService.GetSyncGroupAsync(createResult.Value.Id);
            getResult.Value.Name.Should().Be("Updated Group Name");
        }
    }

    [Fact]
    public async Task DeleteSyncGroup_RemovesGroup()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var group = new RgbSyncGroup
            {
                Name = "Delete Group Test",
                DeviceIds = devices.Value.Take(2).Select(d => d.Id).ToList(),
                SharedEffect = CreateSampleRgbEffect()
            };
            var createResult = await _rgbSyncService.CreateSyncGroupAsync(group);
            createResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _rgbSyncService.DeleteSyncGroupAsync(createResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var getResult = await _rgbSyncService.GetSyncGroupAsync(createResult.Value.Id);
            getResult.IsFailure.Should().BeTrue();
        }
    }

    [Fact]
    public async Task AddDeviceToSyncGroup_AddsDevice()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 3)
        {
            var group = new RgbSyncGroup
            {
                Name = "Add Device Test",
                DeviceIds = devices.Value.Take(2).Select(d => d.Id).ToList(),
                SharedEffect = CreateSampleRgbEffect()
            };
            var createResult = await _rgbSyncService.CreateSyncGroupAsync(group);
            createResult.IsSuccess.Should().BeTrue();

            var newDeviceId = devices.Value[2].Id;

            // Act
            var result = await _rgbSyncService.AddDeviceToSyncGroupAsync(createResult.Value.Id, newDeviceId);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var getResult = await _rgbSyncService.GetSyncGroupAsync(createResult.Value.Id);
            getResult.Value.DeviceIds.Should().Contain(newDeviceId);
        }
    }

    [Fact]
    public async Task RemoveDeviceFromSyncGroup_RemovesDevice()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var deviceIds = devices.Value.Take(2).Select(d => d.Id).ToList();
            var group = new RgbSyncGroup
            {
                Name = "Remove Device Test",
                DeviceIds = deviceIds,
                SharedEffect = CreateSampleRgbEffect()
            };
            var createResult = await _rgbSyncService.CreateSyncGroupAsync(group);
            createResult.IsSuccess.Should().BeTrue();

            var deviceToRemove = deviceIds.First();

            // Act
            var result = await _rgbSyncService.RemoveDeviceFromSyncGroupAsync(createResult.Value.Id, deviceToRemove);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var getResult = await _rgbSyncService.GetSyncGroupAsync(createResult.Value.Id);
            getResult.Value.DeviceIds.Should().NotContain(deviceToRemove);
        }
    }

    [Fact]
    public async Task ApplySyncGroupEffect_SyncsEffectToAllDevices()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count >= 2)
        {
            var group = new RgbSyncGroup
            {
                Name = "Apply Effect Test",
                DeviceIds = devices.Value.Take(2).Select(d => d.Id).ToList(),
                SharedEffect = CreateSampleRgbEffect("Synced Effect")
            };
            var createResult = await _rgbSyncService.CreateSyncGroupAsync(group);
            createResult.IsSuccess.Should().BeTrue();

            // Act
            var result = await _rgbSyncService.ApplySyncGroupEffectAsync(createResult.Value.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    #endregion

    #region Game State Trigger Tests

    [Fact]
    public async Task SetGameStateTrigger_SetsTrigger()
    {
        // Arrange
        var config = new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.HealthLow,
            Effect = CreateSampleRgbEffect("Health Low Effect"),
            DurationMs = 3000,
            Interruptible = true,
            Priority = 1
        };

        // Act
        var result = await _rgbSyncService.SetGameStateTriggerAsync(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetGameStateTriggers_ReturnsAllTriggers()
    {
        // Arrange - Set up some triggers
        var triggers = new[]
        {
            GameStateRgbTrigger.HealthLow,
            GameStateRgbTrigger.HealthCritical,
            GameStateRgbTrigger.LevelUp
        };

        foreach (var trigger in triggers)
        {
            var config = new GameStateRgbConfig
            {
                Trigger = trigger,
                Effect = CreateSampleRgbEffect($"{trigger} Effect"),
                DurationMs = 3000,
                Interruptible = true,
                Priority = 1
            };
            await _rgbSyncService.SetGameStateTriggerAsync(config);
        }

        // Act
        var result = await _rgbSyncService.GetGameStateTriggersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task RemoveGameStateTrigger_RemovesTrigger()
    {
        // Arrange
        var config = new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.BossEncounter,
            Effect = CreateSampleRgbEffect("Boss Effect"),
            DurationMs = 5000,
            Interruptible = false,
            Priority = 2
        };
        await _rgbSyncService.SetGameStateTriggerAsync(config);

        // Act
        var result = await _rgbSyncService.RemoveGameStateTriggerAsync(GameStateRgbTrigger.BossEncounter);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TriggerGameState_TriggersEffect()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var config = new GameStateRgbConfig
            {
                Trigger = GameStateRgbTrigger.Victory,
                Effect = CreateSampleRgbEffect("Victory Effect"),
                DurationMs = 5000,
                Interruptible = true,
                Priority = 1
            };
            await _rgbSyncService.SetGameStateTriggerAsync(config);

            // Act
            var result = await _rgbSyncService.TriggerGameStateAsync(GameStateRgbTrigger.Victory);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(GameStateRgbTrigger.HealthLow)]
    [InlineData(GameStateRgbTrigger.HealthCritical)]
    [InlineData(GameStateRgbTrigger.ManaLow)]
    [InlineData(GameStateRgbTrigger.LevelUp)]
    [InlineData(GameStateRgbTrigger.BossEncounter)]
    [InlineData(GameStateRgbTrigger.Victory)]
    [InlineData(GameStateRgbTrigger.Defeat)]
    public async Task SetGameStateTrigger_ForAllTriggerTypes_WorksCorrectly(GameStateRgbTrigger trigger)
    {
        // Arrange
        var config = new GameStateRgbConfig
        {
            Trigger = trigger,
            Effect = CreateSampleRgbEffect($"{trigger} Effect"),
            DurationMs = 3000,
            Interruptible = true,
            Priority = 1
        };

        // Act
        var result = await _rgbSyncService.SetGameStateTriggerAsync(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Provider Management Tests

    [Fact]
    public async Task GetAvailableProviders_ReturnsListOfProviders()
    {
        // Act
        var result = await _rgbSyncService.GetAvailableProvidersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task EnableProvider_EnablesProvider()
    {
        // Arrange
        var providers = await _rgbSyncService.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();

        if (providers.Value.Count > 0)
        {
            var providerId = providers.Value.First().Id;

            // Act
            var result = await _rgbSyncService.EnableProviderAsync(providerId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task DisableProvider_DisablesProvider()
    {
        // Arrange
        var providers = await _rgbSyncService.GetAvailableProvidersAsync();
        providers.IsSuccess.Should().BeTrue();

        if (providers.Value.Count > 0)
        {
            var providerId = providers.Value.First().Id;

            // Act
            var result = await _rgbSyncService.DisableProviderAsync(providerId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ApplyEffect_ToDisconnectedDevice_ReturnsFailure()
    {
        // Arrange
        var effect = CreateSampleRgbEffect("Test Effect");
        var disconnectedDeviceId = Guid.NewGuid();

        // Act
        var result = await _rgbSyncService.ApplyEffectAsync(disconnectedDeviceId, effect);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetNonExistentProfile_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _rgbSyncService.GetProfileAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteNonExistentSyncGroup_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _rgbSyncService.DeleteSyncGroupAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task SetInvalidBrightness_ReturnsValidationError()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var deviceId = devices.Value.First().Id;
            var invalidBrightness = 1.5f; // > 1.0 is invalid

            // Act
            var result = await _rgbSyncService.SetDeviceBrightnessAsync(deviceId, invalidBrightness);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorType.Should().Be(ErrorType.Validation);
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ApplyEffect_ToManyDevices_PerformsEfficiently()
    {
        // Arrange
        var devices = await _rgbSyncService.GetConnectedDevicesAsync();
        devices.IsSuccess.Should().BeTrue();

        if (devices.Value.Count > 0)
        {
            var effect = CreateSampleRgbEffect("Performance Test");
            var deviceIds = devices.Value.Select(d => d.Id).ToList();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _rgbSyncService.ApplyEffectToMultipleAsync(deviceIds, effect);

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
    }

    [Fact]
    public async Task CreateManyProfiles_PerformsEfficiently()
    {
        // Arrange
        var profiles = Enumerable.Range(0, 50).Select(i => CreateSampleRgbProfile($"Bulk Profile {i}")).ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        foreach (var profile in profiles)
        {
            await _rgbSyncService.CreateProfileAsync(profile);
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    #endregion

    #region Helper Methods

    private static RgbEffect CreateSampleRgbEffect(string name = "Test Effect")
    {
        return new RgbEffect
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = RgbEffectType.Static,
            Colors = new List<RgbColor> { RgbColor.Red, RgbColor.Green, RgbColor.Blue },
            Speed = 1.0f,
            Brightness = 1.0f,
            Direction = RgbDirection.Forward,
            Parameters = new Dictionary<string, object>(),
            IsEnabled = true
        };
    }

    private static RgbProfile CreateSampleRgbProfile(string name = "Test Profile")
    {
        return new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsDefault = false,
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    #endregion
}


