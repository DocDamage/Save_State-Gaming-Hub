using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// View model for the RGB Sync Control Panel.
/// Manages devices, effects, profiles, and game state integration.
/// </summary>
public partial class RgbControlPanelViewModel : ObservableObject, IDisposable
{
    private readonly IRgbSyncService _rgbService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RgbControlPanelViewModel> _logger;
    private readonly ITimeProvider _timeProvider;
    private System.Timers.Timer? _previewTimer;

    [ObservableProperty]
    private ObservableCollection<RgbDevice> _devices = new();

    [ObservableProperty]
    private ObservableCollection<RgbProfile> _profiles = new();

    [ObservableProperty]
    private ObservableCollection<RgbProviderInfo> _providers = new();

    [ObservableProperty]
    private RgbDevice? _selectedDevice;

    [ObservableProperty]
    private RgbProfile? _selectedProfile;

    [ObservableProperty]
    private RgbEffect _currentEffect = new() { Name = "New Effect" };

    [ObservableProperty]
    private bool _isSyncEnabled;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPreviewActive;

    // Effect parameters
    [ObservableProperty]
    private RgbEffectType _selectedEffectType = RgbEffectType.Static;

    [ObservableProperty]
    private ObservableCollection<RgbColor> _effectColors = new();

    [ObservableProperty]
    private float _effectSpeed = 1.0f;

    [ObservableProperty]
    private float _effectBrightness = 1.0f;

    [ObservableProperty]
    private RgbDirection _effectDirection = RgbDirection.Forward;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GameStateRgbConfig> _gameStateConfigs = new();

    [ObservableProperty]
    private ObservableCollection<RgbSyncGroup> _syncGroups = new();

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public RgbControlPanelViewModel(
        IRgbSyncService rgbService,
        IDialogService dialogService,
        ILogger<RgbControlPanelViewModel> logger,
        ITimeProvider timeProvider)
    {
        _rgbService = rgbService;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;

        // Initialize with default colors
        EffectColors.Add(RgbColor.Red);
        EffectColors.Add(RgbColor.Green);
        EffectColors.Add(RgbColor.Blue);

        // Initialize game state configs
        InitializeGameStateConfigs();

        // Start preview timer
        _previewTimer = new System.Timers.Timer(50); // 20fps
        _previewTimer.Elapsed += (s, e) => UpdatePreview();

        _ = InitializeAsync();
    }

    private void InitializeGameStateConfigs()
    {
        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.HealthLow,
            Effect = new RgbEffect { Type = RgbEffectType.Breathing, Colors = new List<RgbColor> { RgbColor.Red } },
            DurationMs = 3000
        });
        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.AchievementUnlocked,
            Effect = new RgbEffect { Type = RgbEffectType.Flashing, Colors = new List<RgbColor> { RgbColor.Gold } },
            DurationMs = 5000
        });
        GameStateConfigs.Add(new GameStateRgbConfig
        {
            Trigger = GameStateRgbTrigger.LevelUp,
            Effect = new RgbEffect { Type = RgbEffectType.Rainbow, Colors = new List<RgbColor> { RgbColor.Red, RgbColor.Green, RgbColor.Blue } },
            DurationMs = 10000
        });
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Initializing RGB service...";

        try
        {
            var config = new RgbSyncConfiguration { Enabled = true };
            var result = await _rgbService.InitializeAsync(config);

            if (result.IsSuccess)
            {
                IsSyncEnabled = true;
                await RefreshDevicesAsync();
                await LoadProvidersAsync();
                await LoadProfilesAsync();
                StatusMessage = "RGB Sync ready";
            }
            else
            {
                StatusMessage = $"Failed to initialize: {result.Error}";
                _logger.LogWarning("RGB service initialization failed: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error initializing RGB service";
            _logger.LogError(ex, "Error during RGB service initialization");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        IsLoading = true;
        StatusMessage = "Scanning for devices...";

        try
        {
            var result = await _rgbService.RefreshDevicesAsync();
            if (result.IsSuccess)
            {
                var devicesResult = await _rgbService.GetDevicesAsync();
                if (devicesResult.IsSuccess)
                {
                    Devices.Clear();
                    foreach (var device in devicesResult.Value)
                    {
                        Devices.Add(device);
                    }
                    StatusMessage = $"Found {Devices.Count} devices";
                }
            }
            else
            {
                StatusMessage = $"Failed to refresh: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error refreshing devices";
            _logger.LogError(ex, "Error refreshing RGB devices");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyEffectAsync()
    {
        if (SelectedDevice == null)
        {
            StatusMessage = "No device selected";
            return;
        }

        try
        {
            CurrentEffect.Type = SelectedEffectType;
            CurrentEffect.Speed = EffectSpeed;
            CurrentEffect.Brightness = EffectBrightness;
            CurrentEffect.Direction = EffectDirection;
            CurrentEffect.Colors = EffectColors.ToList();

            var result = await _rgbService.ApplyEffectAsync(SelectedDevice.Id.ToString(), CurrentEffect);

            if (result.IsSuccess)
            {
                StatusMessage = "Effect applied successfully";
                _logger.LogInformation("Applied {EffectType} effect to {DeviceName}", SelectedEffectType, SelectedDevice.Name);
            }
            else
            {
                StatusMessage = $"Failed to apply effect: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error applying effect";
            _logger.LogError(ex, "Error applying RGB effect");
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var profileName = await _dialogService.ShowInputDialogAsync("Save Profile", "Enter profile name:", CurrentEffect.Name);
        if (string.IsNullOrWhiteSpace(profileName))
            return;

        try
        {
            var profile = new RgbProfile
            {
                Id = Guid.NewGuid(),
                Name = profileName,
                DeviceEffects = new Dictionary<Guid, RgbEffect>(),
                CreatedAt = _timeProvider.Now,
                ModifiedAt = _timeProvider.Now
            };

            if (SelectedDevice != null)
            {
                profile.DeviceEffects[SelectedDevice.Id] = CurrentEffect;
            }

            Profiles.Add(profile);
            SelectedProfile = profile;
            StatusMessage = $"Profile '{profileName}' saved";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error saving profile";
            _logger.LogError(ex, "Error saving RGB profile");
        }
    }

    [RelayCommand]
    private async Task LoadProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        try
        {
            SelectedProfile = profile;

            // Load effect from profile
            if (profile.DeviceEffects.Count > 0)
            {
                var effect = profile.DeviceEffects.Values.First();
                CurrentEffect = effect;
                SelectedEffectType = effect.Type;
                EffectSpeed = effect.Speed;
                EffectBrightness = effect.Brightness;
                EffectDirection = effect.Direction;
                EffectColors = new ObservableCollection<RgbColor>(effect.Colors);
            }

            // Apply to all devices in profile
            foreach (var (deviceId, effect) in profile.DeviceEffects)
            {
                await _rgbService.ApplyEffectAsync(deviceId.ToString(), effect);
            }

            StatusMessage = $"Profile '{profile.Name}' loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error loading profile";
            _logger.LogError(ex, "Error loading RGB profile");
        }
    }

    [RelayCommand]
    private async Task ToggleProviderAsync(RgbProviderInfo? provider)
    {
        if (provider == null) return;

        try
        {
            provider.IsEnabled = !provider.IsEnabled;
            StatusMessage = $"{provider.Name} {(provider.IsEnabled ? "enabled" : "disabled")}";

            // Refresh devices after toggling provider
            await RefreshDevicesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Error toggling provider";
            _logger.LogError(ex, "Error toggling RGB provider");
        }
    }

    [RelayCommand]
    private async Task PickColorAsync()
    {
        try
        {
            // Show color picker dialog
            var dialog = new RgbColorPickerViewModel(_dialogService);
            var result = await _dialogService.ShowDialogAsync<RgbColor?>(dialog);

            if (result != null)
            {
                EffectColors.Add(result);
                StatusMessage = $"Color {result.ToHex()} added";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error picking color";
            _logger.LogError(ex, "Error showing color picker");
        }
    }

    [RelayCommand]
    private async Task PreviewEffectAsync()
    {
        try
        {
            IsPreviewActive = !IsPreviewActive;

            if (IsPreviewActive)
            {
                _previewTimer?.Start();
                StatusMessage = "Preview active";
            }
            else
            {
                _previewTimer?.Stop();
                StatusMessage = "Preview stopped";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error with preview";
            _logger.LogError(ex, "Error in effect preview");
        }
    }

    [RelayCommand]
    private async Task CreateSyncGroupAsync()
    {
        try
        {
            var groupName = await _dialogService.ShowInputDialogAsync("Create Sync Group", "Enter group name:", "New Group");
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            var group = new RgbSyncGroup
            {
                Id = Guid.NewGuid(),
                Name = groupName,
                SharedEffect = CurrentEffect
            };

            SyncGroups.Add(group);
            StatusMessage = $"Sync group '{groupName}' created";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error creating sync group";
            _logger.LogError(ex, "Error creating sync group");
        }
    }

    [RelayCommand]
    private async Task ConfigureGameStateEffectsAsync()
    {
        try
        {
            SelectedTabIndex = 3; // Switch to Game State tab
            StatusMessage = "Configure game state effects";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error configuring game state effects";
            _logger.LogError(ex, "Error configuring game state effects");
        }
    }

    [RelayCommand]
    private void RemoveColor(RgbColor? color)
    {
        if (color != null && EffectColors.Contains(color))
        {
            EffectColors.Remove(color);
        }
    }

    [RelayCommand]
    private void AddColor()
    {
        EffectColors.Add(RgbColor.White);
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Profile",
            $"Are you sure you want to delete '{profile.Name}'?");

        if (confirmed)
        {
            Profiles.Remove(profile);
            StatusMessage = $"Profile '{profile.Name}' deleted";
        }
    }

    [RelayCommand]
    private async Task SetAsDefaultProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        foreach (var p in Profiles)
        {
            p.IsDefault = p.Id == profile.Id;
        }

        StatusMessage = $"'{profile.Name}' set as default";
    }

    [RelayCommand]
    private void ToggleSync()
    {
        IsSyncEnabled = !IsSyncEnabled;
        StatusMessage = IsSyncEnabled ? "RGB Sync enabled" : "RGB Sync disabled";
    }

    private async Task LoadProvidersAsync()
    {
        try
        {
            var sdkResult = await _rgbService.GetSdkInfoAsync();
            if (sdkResult.IsSuccess)
            {
                Providers.Clear();
                foreach (var sdk in sdkResult.Value)
                {
                    Providers.Add(new RgbProviderInfo
                    {
                        Id = sdk.Vendor.ToString(),
                        Name = sdk.Vendor.ToString(),
                        Version = sdk.Version,
                        IsAvailable = sdk.IsAvailable,
                        IsEnabled = sdk.IsAvailable
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RGB providers");
        }
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            // Load from service or create defaults
            var configResult = await _rgbService.GetConfigurationAsync();
            if (configResult.IsSuccess)
            {
                // Create some default profiles if none exist
                if (Profiles.Count == 0)
                {
                    Profiles.Add(new RgbProfile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Gaming",
                        IsDefault = true,
                        DeviceEffects = new Dictionary<Guid, RgbEffect>(),
                        CreatedAt = _timeProvider.Now,
                        ModifiedAt = _timeProvider.Now
                    });

                    Profiles.Add(new RgbProfile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Movie",
                        DeviceEffects = new Dictionary<Guid, RgbEffect>(),
                        CreatedAt = _timeProvider.Now,
                        ModifiedAt = _timeProvider.Now
                    });

                    Profiles.Add(new RgbProfile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Work",
                        DeviceEffects = new Dictionary<Guid, RgbEffect>(),
                        CreatedAt = _timeProvider.Now,
                        ModifiedAt = _timeProvider.Now
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RGB profiles");
        }
    }

    private void UpdatePreview()
    {
        // This would update the preview animation
        // Called by the timer when preview is active
    }

    public void Dispose()
    {
        _previewTimer?.Stop();
        _previewTimer?.Dispose();
    }
}
