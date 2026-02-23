using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;

namespace SaveState.Infrastructure.RgbSync;

/// <summary>
/// Implementation of the RGB Sync Service for managing RGB devices, effects, and profiles.
/// </summary>
public sealed class RgbSyncService : IRgbSyncService
{
    private readonly ILogger<RgbSyncService> _logger;
    private readonly List<IRgbProvider> _providers;
    private readonly HashSet<string> _enabledProviders = new();
    private readonly List<RgbDevice> _devices = new();
    private readonly List<RgbProfile> _profiles = new();
    private readonly List<RgbSyncGroup> _syncGroups = new();
    private readonly Dictionary<Guid, RgbEffect> _activeEffects = new();
    private readonly Dictionary<GameStateRgbTrigger, GameStateRgbConfig> _gameStateConfigs = new();
    private bool _isRunning;

    public RgbSyncService(
        ILogger<RgbSyncService> logger,
        IEnumerable<IRgbProvider> providers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers?.ToList() ?? new List<IRgbProvider>();
    }

    #region Device Management

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RgbDevice>>> GetDevicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<RgbDevice>>(_devices.AsReadOnly()));
    }

    /// <inheritdoc />
    public Task<Result<RgbDevice>> GetDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        var device = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
        {
            return Task.FromResult(Result.Failure<RgbDevice>($"Device {deviceId} not found", ErrorType.NotFound));
        }
        return Task.FromResult(Result.Success(device));
    }

    /// <inheritdoc />
    public async Task<Result> RefreshDevicesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing RGB devices from all providers");
        _devices.Clear();

        foreach (var provider in _providers.Where(p => _enabledProviders.Contains(p.Id)))
        {
            try
            {
                var providerDevices = await provider.GetDevicesAsync(ct).ConfigureAwait(false);
                _devices.AddRange(providerDevices);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get devices from provider {ProviderId}", provider.Id);
            }
        }

        _logger.LogInformation("Discovered {DeviceCount} RGB devices", _devices.Count);
        return Result.Success();
    }

    #endregion

    #region Effect Control

    /// <inheritdoc />
    public Task<Result> SetDeviceColorAsync(Guid deviceId, RgbColor color, CancellationToken ct = default)
    {
        _logger.LogDebug("Setting device {DeviceId} color to RGB({R},{G},{B})", deviceId, color.R, color.G, color.B);
        
        var device = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
        {
            return Task.FromResult(Result.Failure($"Device {deviceId} not found", ErrorType.NotFound));
        }

        // Apply color to all LEDs
        foreach (var led in device.Leds)
        {
            led.Color = color;
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetDeviceLedsAsync(Guid deviceId, Dictionary<int, RgbColor> ledColors, CancellationToken ct = default)
    {
        _logger.LogDebug("Setting individual LEDs for device {DeviceId}", deviceId);
        
        var device = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
        {
            return Task.FromResult(Result.Failure($"Device {deviceId} not found", ErrorType.NotFound));
        }

        foreach (var (ledIndex, color) in ledColors)
        {
            if (ledIndex >= 0 && ledIndex < device.Leds.Count)
            {
                device.Leds[ledIndex].Color = color;
            }
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetDeviceEffectAsync(Guid deviceId, RgbEffect effect, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying effect {EffectName} ({EffectType}) to device {DeviceId}", 
            effect.Name, effect.Type, deviceId);
        
        var device = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
        {
            return Task.FromResult(Result.Failure($"Device {deviceId} not found", ErrorType.NotFound));
        }

        _activeEffects[deviceId] = effect;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetAllDevicesColorAsync(RgbColor color, CancellationToken ct = default)
    {
        _logger.LogInformation("Setting all devices color to RGB({R},{G},{B})", color.R, color.G, color.B);
        
        foreach (var device in _devices.Where(d => d.IsConnected))
        {
            SetDeviceColorAsync(device.Id, color, ct);
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetAllDevicesEffectAsync(RgbEffect effect, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying effect {EffectName} to all devices", effect.Name);
        
        foreach (var device in _devices.Where(d => d.IsConnected))
        {
            _activeEffects[device.Id] = effect;
        }

        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Profile Management

    /// <inheritdoc />
    public Task<Result<RgbProfile>> CreateProfileAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(Result.Failure<RgbProfile>("Profile name cannot be empty", ErrorType.Validation));
        }

        var profile = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            IsDefault = false
        };

        _profiles.Add(profile);
        _logger.LogInformation("Created RGB profile '{ProfileName}' with ID {ProfileId}", name, profile.Id);

        return Task.FromResult(Result.Success(profile));
    }

    /// <inheritdoc />
    public Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
        {
            return Task.FromResult(Result.Failure($"Profile {profileId} not found", ErrorType.NotFound));
        }

        _logger.LogInformation("Applying RGB profile '{ProfileName}'", profile.Name);

        foreach (var deviceEffect in profile.DeviceEffects)
        {
            SetDeviceEffectAsync(deviceEffect.Key, deviceEffect.Value, ct);
        }

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RgbProfile>>> GetProfilesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<RgbProfile>>(_profiles.AsReadOnly()));
    }

    /// <inheritdoc />
    public Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
        {
            return Task.FromResult(Result.Failure($"Profile {profileId} not found", ErrorType.NotFound));
        }

        _profiles.Remove(profile);
        _logger.LogInformation("Deleted RGB profile '{ProfileName}'", profile.Name);

        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Sync Groups

    /// <inheritdoc />
    public Task<Result<RgbSyncGroup>> CreateSyncGroupAsync(string name, List<Guid> deviceIds, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(Result.Failure<RgbSyncGroup>("Group name cannot be empty", ErrorType.Validation));
        }

        if (deviceIds == null || deviceIds.Count == 0)
        {
            return Task.FromResult(Result.Failure<RgbSyncGroup>("At least one device is required", ErrorType.Validation));
        }

        // Validate all devices exist
        foreach (var deviceId in deviceIds)
        {
            if (!_devices.Any(d => d.Id == deviceId))
            {
                return Task.FromResult(Result.Failure<RgbSyncGroup>($"Device {deviceId} not found", ErrorType.NotFound));
            }
        }

        var group = new RgbSyncGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            DeviceIds = new List<Guid>(deviceIds),
            SharedEffect = new RgbEffect
            {
                Id = Guid.NewGuid(),
                Name = "Default",
                Type = RgbEffectType.Static,
                Colors = new List<RgbColor> { RgbColor.White }
            }
        };

        _syncGroups.Add(group);
        _logger.LogInformation("Created RGB sync group '{GroupName}' with {DeviceCount} devices", name, deviceIds.Count);

        return Task.FromResult(Result.Success(group));
    }

    /// <inheritdoc />
    public Task<Result> UpdateSyncGroupEffectAsync(Guid groupId, RgbEffect effect, CancellationToken ct = default)
    {
        var group = _syncGroups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return Task.FromResult(Result.Failure($"Sync group {groupId} not found", ErrorType.NotFound));
        }

        group.SharedEffect = effect;

        // Apply effect to all devices in the group
        foreach (var deviceId in group.DeviceIds)
        {
            SetDeviceEffectAsync(deviceId, effect, ct);
        }

        _logger.LogDebug("Updated effect for sync group '{GroupName}'", group.Name);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> DeleteSyncGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = _syncGroups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            return Task.FromResult(Result.Failure($"Sync group {groupId} not found", ErrorType.NotFound));
        }

        _syncGroups.Remove(group);
        _logger.LogInformation("Deleted RGB sync group '{GroupName}'", group.Name);

        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Game State Integration

    /// <inheritdoc />
    public Task<Result> TriggerGameStateEffectAsync(GameStateRgbTrigger trigger, CancellationToken ct = default)
    {
        _logger.LogInformation("Triggering RGB effect for game state: {Trigger}", trigger);

        if (!_gameStateConfigs.TryGetValue(trigger, out var config))
        {
            _logger.LogDebug("No RGB configuration found for trigger {Trigger}", trigger);
            return Task.FromResult(Result.Success());
        }

        // Apply the configured effect to all devices
        return SetAllDevicesEffectAsync(config.Effect, ct);
    }

    /// <inheritdoc />
    public Task<Result> ConfigureGameStateEffectsAsync(List<GameStateRgbConfig> configs, CancellationToken ct = default)
    {
        if (configs == null)
        {
            return Task.FromResult(Result.Success());
        }

        _gameStateConfigs.Clear();

        foreach (var config in configs)
        {
            _gameStateConfigs[config.Trigger] = config;
        }

        _logger.LogInformation("Configured {Count} game state RGB effects", configs.Count);
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Provider Management

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RgbProviderInfo>>> GetProvidersAsync(CancellationToken ct = default)
    {
        var providerInfos = new List<RgbProviderInfo>();

        foreach (var provider in _providers)
        {
            var info = new RgbProviderInfo
            {
                Id = provider.Id,
                Name = provider.Name,
                Version = provider.Version,
                IsAvailable = provider.IsAvailable,
                IsEnabled = _enabledProviders.Contains(provider.Id),
                DeviceCount = _devices.Count(d => d.ProviderId == provider.Id),
                ConnectionStatus = provider.IsAvailable ? "Connected" : "Not Available"
            };
            providerInfos.Add(info);
        }

        return Task.FromResult(Result.Success<IReadOnlyList<RgbProviderInfo>>(providerInfos.AsReadOnly()));
    }

    /// <inheritdoc />
    public async Task<Result> EnableProviderAsync(string providerId, CancellationToken ct = default)
    {
        var provider = _providers.FirstOrDefault(p => p.Id == providerId);
        if (provider == null)
        {
            return Result.Failure($"Provider {providerId} not found", ErrorType.NotFound);
        }

        if (!provider.IsAvailable)
        {
            return Result.Failure($"Provider {providerId} is not available", ErrorType.Validation);
        }

        try
        {
            await provider.InitializeAsync(ct).ConfigureAwait(false);
            _enabledProviders.Add(providerId);
            _logger.LogInformation("Enabled RGB provider {ProviderId}", providerId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable provider {ProviderId}", providerId);
            return Result.Failure($"Failed to enable provider: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DisableProviderAsync(string providerId, CancellationToken ct = default)
    {
        var provider = _providers.FirstOrDefault(p => p.Id == providerId);
        if (provider == null)
        {
            return Result.Failure($"Provider {providerId} not found", ErrorType.NotFound);
        }

        try
        {
            await provider.ShutdownAsync(ct).ConfigureAwait(false);
            _enabledProviders.Remove(providerId);
            
            // Remove devices from this provider
            _devices.RemoveAll(d => d.ProviderId == providerId);
            
            _logger.LogInformation("Disabled RGB provider {ProviderId}", providerId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable provider {ProviderId}", providerId);
            return Result.Failure($"Failed to disable provider: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Control

    /// <inheritdoc />
    public async Task<Result> StartAsync(CancellationToken ct = default)
    {
        if (_isRunning)
        {
            return Result.Success();
        }

        _logger.LogInformation("Starting RGB Sync Service");

        // Initialize all available providers
        foreach (var provider in _providers.Where(p => p.IsAvailable))
        {
            try
            {
                await provider.InitializeAsync(ct).ConfigureAwait(false);
                _enabledProviders.Add(provider.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize provider {ProviderId}", provider.Id);
            }
        }

        // Discover devices
        await RefreshDevicesAsync(ct).ConfigureAwait(false);

        _isRunning = true;
        _logger.LogInformation("RGB Sync Service started with {DeviceCount} devices from {ProviderCount} providers", 
            _devices.Count, _enabledProviders.Count);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> StopAsync(CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return Result.Success();
        }

        _logger.LogInformation("Stopping RGB Sync Service");

        // Clear all effects
        _activeEffects.Clear();

        // Shutdown all enabled providers
        foreach (var providerId in _enabledProviders.ToList())
        {
            var provider = _providers.FirstOrDefault(p => p.Id == providerId);
            if (provider != null)
            {
                try
                {
                    await provider.ShutdownAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error shutting down provider {ProviderId}", providerId);
                }
            }
        }

        _enabledProviders.Clear();
        _devices.Clear();
        _isRunning = false;

        _logger.LogInformation("RGB Sync Service stopped");
        return Result.Success();
    }

    /// <inheritdoc />
    public Task<bool> IsRunningAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_isRunning);
    }

    #endregion
}
