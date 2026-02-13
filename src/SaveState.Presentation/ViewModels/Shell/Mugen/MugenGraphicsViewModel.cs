using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// View model for advanced graphics enhancements.
/// </summary>
public partial class MugenGraphicsViewModel : MugenSectionViewModelBase
{
    private readonly IMugenGraphicsEngine _graphicsEngine;

    [ObservableProperty]
    private bool _enableDynamicLighting;

    [ObservableProperty]
    private bool _enableShadows;

    [ObservableProperty]
    private float _ambientIntensity = 0.3f;

    [ObservableProperty]
    private ScreenFilterType _selectedFilterType = ScreenFilterType.None;

    [ObservableProperty]
    private float _filterIntensity = 0.5f;

    [ObservableProperty]
    private bool _enableParallax;

    [ObservableProperty]
    private float _parallaxSpeed = 0.5f;

    [ObservableProperty]
    private bool _enableDynamicCamera;

    [ObservableProperty]
    private CameraMode _cameraMode = CameraMode.Follow;

    [ObservableProperty]
    private ObservableCollection<GraphicsPreset> _availablePresets = new();

    [ObservableProperty]
    private GraphicsPreset? _selectedPreset;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isProcessing;

    public MugenGraphicsViewModel(IMugenGraphicsEngine graphicsEngine)
    {
        _graphicsEngine = graphicsEngine;
        Id = "graphics";
        Name = "Graphics Enhancements";
        Icon = "🎨";
        Title = "Advanced Graphics Engine";
    }

    public override async Task InitializeAsync()
    {
        await LoadAvailablePresetsAsync();
        await UpdateStatusAsync();
    }

    [RelayCommand]
    private async Task ApplyLightingAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Applying dynamic lighting...";

            var lightingConfig = new DynamicLightingConfig
            {
                EnableShadows = EnableShadows,
                AmbientIntensity = AmbientIntensity,
                EnableEnvironmentalLighting = EnableDynamicLighting
            };

            var result = await _graphicsEngine.ApplyDynamicLightingAsync("global", lightingConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "Dynamic lighting applied successfully";
            }
            else
            {
                StatusMessage = $"Failed to apply lighting: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying lighting: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ApplyScreenFilterAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = $"Applying {SelectedFilterType} filter...";

            ScreenFilterConfig config = SelectedFilterType switch
            {
                ScreenFilterType.Crt => new CrtFilterConfig
                {
                    FilterType = ScreenFilterType.Crt,
                    Intensity = FilterIntensity
                },
                ScreenFilterType.Scanlines => new ScanlinesFilterConfig
                {
                    FilterType = ScreenFilterType.Scanlines,
                    Intensity = FilterIntensity
                },
                ScreenFilterType.Bloom => new BloomFilterConfig
                {
                    FilterType = ScreenFilterType.Bloom,
                    Intensity = FilterIntensity
                },
                _ => new ScreenFilterConfig
                {
                    FilterType = SelectedFilterType,
                    Intensity = FilterIntensity
                }
            };

            var result = await _graphicsEngine.ApplyScreenFilterAsync(SelectedFilterType, config);

            if (result.IsSuccess)
            {
                StatusMessage = $"{SelectedFilterType} filter applied successfully";
            }
            else
            {
                StatusMessage = $"Failed to apply filter: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying filter: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ApplyBackgroundEffectsAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Applying background effects...";

            var backgroundConfig = new BackgroundEffectConfig
            {
                EnableParallax = EnableParallax,
                ParallaxSpeed = ParallaxSpeed
            };

            // Would need to specify a stage ID in a real implementation
            var result = await _graphicsEngine.EnhanceBackgroundAsync(1, backgroundConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "Background effects applied successfully";
            }
            else
            {
                StatusMessage = $"Failed to apply background effects: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying background effects: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ConfigureCameraAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Configuring camera system...";

            var cameraConfig = new CameraSystemConfig
            {
                EnableDynamicCamera = EnableDynamicCamera,
                DefaultMode = CameraMode
            };

            var result = await _graphicsEngine.ConfigureCameraSystemAsync(cameraConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "Camera system configured successfully";
            }
            else
            {
                StatusMessage = $"Failed to configure camera: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error configuring camera: {ex.Message}";
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
            EnableDynamicLighting = SelectedPreset.LightingConfig?.EnableEnvironmentalLighting ?? false;
            EnableShadows = SelectedPreset.LightingConfig?.EnableShadows ?? false;
            AmbientIntensity = SelectedPreset.LightingConfig?.AmbientIntensity ?? 0.3f;

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
            StatusMessage = "Resetting all enhancements...";

            var result = await _graphicsEngine.ResetEnhancementsAsync();

            if (result.IsSuccess)
            {
                // Reset UI values
                EnableDynamicLighting = false;
                EnableShadows = false;
                AmbientIntensity = 0.3f;
                SelectedFilterType = ScreenFilterType.None;
                FilterIntensity = 0.5f;
                EnableParallax = false;
                ParallaxSpeed = 0.5f;
                EnableDynamicCamera = false;
                CameraMode = CameraMode.Follow;

                StatusMessage = "All enhancements reset to defaults";
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

    private async Task LoadAvailablePresetsAsync()
    {
        try
        {
            var result = await _graphicsEngine.GetAvailablePresetsAsync();

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
            var statusResult = await _graphicsEngine.GetStatusAsync();

            if (statusResult.IsSuccess && statusResult.Value is not null)
            {
                var status = statusResult.Value;
                StatusMessage = $"Graphics Engine: {(status.IsActive ? "Active" : "Inactive")} - {status.ActiveEnhancements.Count} enhancements applied";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error getting status: {ex.Message}";
        }
    }

    // Available filter types for binding
    public IEnumerable<ScreenFilterType> AvailableFilterTypes => Enum.GetValues<ScreenFilterType>();

    // Available camera modes for binding
    public IEnumerable<CameraMode> AvailableCameraModes => Enum.GetValues<CameraMode>();
}