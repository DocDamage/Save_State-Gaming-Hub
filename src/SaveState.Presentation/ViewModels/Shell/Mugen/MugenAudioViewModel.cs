using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// View model for sound design studio.
/// </summary>
public partial class MugenAudioViewModel : MugenSectionViewModelBase
{
    private readonly IMugenSoundDesignStudio _soundStudio;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private float _masterVolume = 1.0f;

    [ObservableProperty]
    private bool _enableDynamicMusic;

    [ObservableProperty]
    private bool _enableSpatialization;

    [ObservableProperty]
    private GameState _selectedMusicState = GameState.Fighting;

    [ObservableProperty]
    private float _analysisProgress;

    [ObservableProperty]
    private AudioAnalysisResult? _currentAnalysis;

    [ObservableProperty]
    private ObservableCollection<AudioPreset> _availablePresets = new();

    [ObservableProperty]
    private AudioPreset? _selectedPreset;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _selectedAudioFile = string.Empty;

    public MugenAudioViewModel(IMugenSoundDesignStudio soundStudio, ITimeProvider timeProvider)
    {
        _soundStudio = soundStudio;
        _timeProvider = timeProvider;
        Id = "audio";
        Name = "Sound Design Studio";
        Icon = "🔊";
        Title = "Advanced Audio Enhancements";
    }

    public override async Task InitializeAsync()
    {
        await LoadAvailablePresetsAsync();
        await UpdateStatusAsync();
    }

    [RelayCommand]
    private async Task AnalyzeAudioAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedAudioFile))
        {
            StatusMessage = "Please select an audio file to analyze";
            return;
        }

        try
        {
            IsProcessing = true;
            StatusMessage = "Analyzing audio file...";
            AnalysisProgress = 0;

            // Simulate progress updates
            for (int i = 0; i <= 100; i += 10)
            {
                AnalysisProgress = i;
                await Task.Delay(50);
            }

            var result = await _soundStudio.AnalyzeAudioAsync(SelectedAudioFile);

            if (result.IsSuccess && result.Value is not null)
            {
                CurrentAnalysis = result.Value;
                StatusMessage = $"Analysis complete - Peak: {CurrentAnalysis.PeakLevelDb:F1}dBFS, RMS: {CurrentAnalysis.RmsLevelDb:F1}dBFS";
            }
            else
            {
                StatusMessage = $"Analysis failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error analyzing audio: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            AnalysisProgress = 0;
        }
    }

    [RelayCommand]
    private async Task ApplyAudioMixAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Applying audio mix configuration...";

            var mixConfig = new AudioMixConfig
            {
                MasterVolume = MasterVolume,
                // Would load full configuration in real implementation
            };

            var result = await _soundStudio.ApplyAudioMixAsync(mixConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "Audio mix applied successfully";
            }
            else
            {
                StatusMessage = $"Failed to apply audio mix: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying audio mix: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ConfigureDynamicMusicAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Configuring dynamic music system...";

            var musicConfig = new DynamicMusicConfig
            {
                Enabled = EnableDynamicMusic,
                // Would configure state tracks in real implementation
            };

            var result = await _soundStudio.ConfigureDynamicMusicAsync(musicConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "Dynamic music configured successfully";
            }
            else
            {
                StatusMessage = $"Failed to configure dynamic music: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error configuring dynamic music: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ApplySpatializationAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Applying 3D spatialization...";

            var spatialConfig = new SoundSpatializationConfig
            {
                Enabled = EnableSpatialization,
                // Would configure spatial settings in real implementation
            };

            var result = await _soundStudio.ApplySoundSpatializationAsync(spatialConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "3D spatialization applied successfully";
            }
            else
            {
                StatusMessage = $"Failed to apply spatialization: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying spatialization: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task LoadPresetAsync()
    {
        if (SelectedPreset == null) return;

        try
        {
            IsProcessing = true;
            StatusMessage = $"Loading preset {SelectedPreset.Name}...";

            // Apply preset settings to UI
            MasterVolume = SelectedPreset.MixConfig?.MasterVolume ?? 1.0f;
            EnableDynamicMusic = SelectedPreset.MusicConfig?.Enabled ?? false;
            EnableSpatialization = SelectedPreset.SpatialConfig?.Enabled ?? false;

            StatusMessage = $"Preset {SelectedPreset.Name} loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading preset: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ResetEnhancementsAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Resetting all audio enhancements...";

            var result = await _soundStudio.ResetEnhancementsAsync();

            if (result.IsSuccess)
            {
                // Reset UI values
                MasterVolume = 1.0f;
                EnableDynamicMusic = false;
                EnableSpatialization = false;
                CurrentAnalysis = null;
                SelectedAudioFile = string.Empty;

                StatusMessage = "All audio enhancements reset to defaults";
            }
            else
            {
                StatusMessage = $"Failed to reset enhancements: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resetting enhancements: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ExportConfigurationAsync()
    {
        try
        {
            var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                        $"MugenAudioConfig_{_timeProvider.Now:yyyyMMdd_HHmmss}.json");

            var result = await _soundStudio.ExportConfigurationAsync(exportPath);

            if (result.IsSuccess)
            {
                StatusMessage = $"Configuration exported to {exportPath}";
            }
            else
            {
                StatusMessage = $"Failed to export configuration: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting configuration: {ex.Message}";
        }
    }

    private async Task LoadAvailablePresetsAsync()
    {
        try
        {
            var result = await _soundStudio.GetAvailablePresetsAsync();

            if (result.IsSuccess && result.Value is not null)
            {
                AvailablePresets.Clear();
                foreach (var preset in result.Value)
                {
                    AvailablePresets.Add(preset);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading presets: {ex.Message}";
        }
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            var statusResult = await _soundStudio.GetStatusAsync();

            if (statusResult.IsSuccess && statusResult.Value is not null)
            {
                var status = statusResult.Value;
                StatusMessage = $"Sound Studio: {(status.IsActive ? "Active" : "Inactive")} - {status.ActiveEnhancements.Count} enhancements applied";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error getting status: {ex.Message}";
        }
    }

    // Available game states for binding
    public IEnumerable<GameState> AvailableGameStates => Enum.GetValues<GameState>();
}