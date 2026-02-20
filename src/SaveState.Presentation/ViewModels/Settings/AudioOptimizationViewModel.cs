using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using SaveState.Presentation.Services;
using CoreAudioSettings = SaveState.Core.Performance.Services.AudioSettings;
using CoreAudioProfile = SaveState.Core.Performance.Services.AudioProfile;
using CoreAudioLatencyMode = SaveState.Core.Performance.Services.AudioLatencyMode;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for audio optimization and profile management.
/// </summary>
public partial class AudioOptimizationViewModel : ObservableObject
{
    private readonly IAudioOptimizer _audioOptimizer;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private int sampleRate = 48000;

    [ObservableProperty]
    private int bitDepth = 24;

    [ObservableProperty]
    private int bufferSize = 480;

    [ObservableProperty]
    private int channels = 2;

    [ObservableProperty]
    private bool exclusiveMode;

    [ObservableProperty]
    private string? exclusiveModeWarningMessage;

    [ObservableProperty]
    private bool spatialAudioEnabled;

    [ObservableProperty]
    private string selectedAudioDevice = "Default";

    [ObservableProperty]
    private AudioLatencyMode selectedLatencyMode = AudioLatencyMode.Default;

    [ObservableProperty]
    private float masterVolume = 100.0f;

    [ObservableProperty]
    private float gameVolume = 100.0f;

    [ObservableProperty]
    private float uiVolume = 100.0f;

    [ObservableProperty]
    private float dialogueVolume = 100.0f;

    [ObservableProperty]
    private bool eqEnabled;

    [ObservableProperty]
    private int eqPreset; // 0=Off, 1=Flat, 2=Bass, 3=Treble, 4=Balanced

    [ObservableProperty]
    private ObservableCollection<CoreAudioProfile> savedProfiles = new();

    [ObservableProperty]
    private string? selectedProfileName;

    [ObservableProperty]
    private CoreAudioProfile? selectedProfile;

    [ObservableProperty]
    private string newProfileName = string.Empty;

    [ObservableProperty]
    private float latency = 0.0f;

    [ObservableProperty]
    private float audioQuality = 100.0f;

    [ObservableProperty]
    private bool isOptimizing;

    public AudioOptimizationViewModel(
        IAudioOptimizer audioOptimizer,
        INotificationService notificationService)
    {
        _audioOptimizer = audioOptimizer;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadCurrentSettingsAsync();
            await LoadSavedProfilesAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to initialize audio settings: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ApplyCurrentSettingsAsync()
    {
        try
        {
            IsOptimizing = true;

            var settings = new CoreAudioSettings(
                SampleRate: SampleRate,
                BitDepth: BitDepth,
                BufferSize: BufferSize,
                Channels: Channels,
                ExclusiveMode: ExclusiveMode,
                SpatialAudio: SpatialAudioEnabled,
                LatencyMode: MapToCoreLatencyMode(SelectedLatencyMode),
                PreferredDeviceId: SelectedAudioDevice);

            var result = await _audioOptimizer.ApplySettingsAsync(settings);
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync("Audio settings applied successfully", "Success");
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to apply settings: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsOptimizing = false;
        }
    }

    [RelayCommand]
    public async Task OptimizeForGameAsync(Guid gameId)
    {
        try
        {
            IsOptimizing = true;

            var settings = new CoreAudioSettings(
                SampleRate: SampleRate,
                BitDepth: BitDepth,
                BufferSize: BufferSize,
                Channels: Channels,
                ExclusiveMode: ExclusiveMode,
                SpatialAudio: SpatialAudioEnabled,
                LatencyMode: MapToCoreLatencyMode(SelectedLatencyMode),
                PreferredDeviceId: SelectedAudioDevice);

            var result = await _audioOptimizer.CreateGameProfileAsync(gameId, settings);
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync("Audio profile created for game", "Success");
                SelectedProfileName = result.Value.Name;
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to create profile: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsOptimizing = false;
        }
    }

    [RelayCommand]
    public async Task SaveProfileAsync(string profileName)
    {
        try
        {
            var finalName = string.IsNullOrWhiteSpace(profileName) ? NewProfileName : profileName;
            if (string.IsNullOrWhiteSpace(finalName))
            {
                await _notificationService.ShowErrorAsync("Profile name cannot be empty");
                return;
            }

            var settings = new CoreAudioSettings(
                SampleRate: SampleRate,
                BitDepth: BitDepth,
                BufferSize: BufferSize,
                Channels: Channels,
                ExclusiveMode: ExclusiveMode,
                SpatialAudio: SpatialAudioEnabled,
                LatencyMode: MapToCoreLatencyMode(SelectedLatencyMode),
                PreferredDeviceId: SelectedAudioDevice);

            var profile = CoreAudioProfile.Create(null, finalName, settings);

            var result = await _audioOptimizer.SaveProfileAsync(profile);
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync($"Profile '{finalName}' saved", "Success");
                NewProfileName = string.Empty;
                await LoadSavedProfilesAsync();
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to save profile: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadProfileAsync(string profileName)
    {
        try
        {
            var profile = SavedProfiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null)
            {
                await _notificationService.ShowErrorAsync("Profile not found");
                return;
            }

            SampleRate = profile.Settings.SampleRate;
            BitDepth = profile.Settings.BitDepth;
            BufferSize = profile.Settings.BufferSize;
            Channels = profile.Settings.Channels;
            ExclusiveMode = profile.Settings.ExclusiveMode;
            SpatialAudioEnabled = profile.Settings.SpatialAudio;
            SelectedLatencyMode = MapToViewModelLatencyMode(profile.Settings.LatencyMode);

            SelectedProfileName = profileName;
            await ApplyCurrentSettingsAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteProfileAsync(string profileName)
    {
        try
        {
            var result = await _audioOptimizer.DeleteProfileAsync(profileName);
            if (result.IsSuccess)
            {
                await _notificationService.ShowNotificationAsync($"Profile '{profileName}' deleted", "Success");
                await LoadSavedProfilesAsync();
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Failed to delete profile: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ResetToDefaultsAsync()
    {
        try
        {
            SampleRate = 48000;
            BitDepth = 24;
            BufferSize = 480;
            Channels = 2;
            ExclusiveMode = false;
            SpatialAudioEnabled = false;
            SelectedLatencyMode = AudioLatencyMode.Default;
            MasterVolume = 100.0f;
            GameVolume = 100.0f;
            UiVolume = 100.0f;
            DialogueVolume = 100.0f;
            EqEnabled = false;

            await ApplyCurrentSettingsAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    private async Task LoadCurrentSettingsAsync()
    {
        try
        {
            var result = await _audioOptimizer.GetCurrentSettingsAsync();
            if (result.IsSuccess)
            {
                SampleRate = result.Value.SampleRate;
                BitDepth = result.Value.BitDepth;
                BufferSize = result.Value.BufferSize;
                Channels = result.Value.Channels;
                ExclusiveMode = result.Value.ExclusiveMode;
                SpatialAudioEnabled = result.Value.SpatialAudio;
                SelectedLatencyMode = MapToViewModelLatencyMode(result.Value.LatencyMode);
                if (!string.IsNullOrEmpty(result.Value.PreferredDeviceId))
                {
                    SelectedAudioDevice = result.Value.PreferredDeviceId;
                }
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load audio settings: {ex.Message}");
        }
    }

    private async Task LoadSavedProfilesAsync()
    {
        try
        {
            var result = await _audioOptimizer.GetSavedProfilesAsync();
            if (result.IsSuccess)
            {
                SavedProfiles.Clear();
                foreach (var profile in result.Value)
                {
                    SavedProfiles.Add(profile);
                }
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load profiles: {ex.Message}");
        }
    }

    partial void OnExclusiveModeChanged(bool value)
    {
        ExclusiveModeWarningMessage = value
            ? "Exclusive Mode bypasses the Windows mixer and can prevent other apps from playing audio. Enable only when you need low-latency or DRM support."
            : null;
    }

    partial void OnSelectedProfileChanged(CoreAudioProfile? value)
    {
        if (value == null)
        {
            SelectedProfileName = null;
            return;
        }

        SelectedProfileName = value.Name;
    }
}

// Helper methods for latency mode mapping
public partial class AudioOptimizationViewModel
{
    private static CoreAudioLatencyMode MapToCoreLatencyMode(AudioLatencyMode mode) => mode switch
    {
        AudioLatencyMode.Default => CoreAudioLatencyMode.Default,
        AudioLatencyMode.Low => CoreAudioLatencyMode.Low,
        AudioLatencyMode.Ultra => CoreAudioLatencyMode.UltraLow,
        _ => CoreAudioLatencyMode.Default
    };

    private static AudioLatencyMode MapToViewModelLatencyMode(CoreAudioLatencyMode mode) => mode switch
    {
        CoreAudioLatencyMode.Default => AudioLatencyMode.Default,
        CoreAudioLatencyMode.Balanced => AudioLatencyMode.Default,
        CoreAudioLatencyMode.Low => AudioLatencyMode.Low,
        CoreAudioLatencyMode.UltraLow => AudioLatencyMode.Ultra,
        _ => AudioLatencyMode.Default
    };
}

/// <summary>
/// Audio latency modes (ViewModel version).
/// </summary>
public enum AudioLatencyMode
{
    Default,
    Low,
    Ultra
}
