using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for accessibility settings and features.
/// </summary>
public partial class AccessibilityViewModel : ObservableObject
{
    private readonly IAccessibilityService _accessibilityService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool screenReaderEnabled;

    [ObservableProperty]
    private bool textToSpeechEnabled;

    [ObservableProperty]
    private float textToSpeechRate = 1.0f;

    [ObservableProperty]
    private float textToSpeechVolume = 100.0f;

    [ObservableProperty]
    private bool highContrastModeEnabled;

    [ObservableProperty]
    private bool colorBlindModeEnabled;

    [ObservableProperty]
    private ColorBlindMode selectedColorBlindMode = ColorBlindMode.Normal;

    [ObservableProperty]
    private float uiScalePercentage = 100.0f;

    [ObservableProperty]
    private float fontSizeMultiplier = 1.0f;

    [ObservableProperty]
    private bool focusIndicatorsEnabled = true;

    [ObservableProperty]
    private bool animationsEnabled = true;

    [ObservableProperty]
    private bool reduceMotionEnabled;

    [ObservableProperty]
    private bool keyboardNavigationEnabled = true;

    [ObservableProperty]
    private bool mousePointerEnlargementEnabled;

    [ObservableProperty]
    private float pointerSize = 1.0f;

    [ObservableProperty]
    private bool stickyKeysEnabled;

    [ObservableProperty]
    private bool toggleKeysEnabled;

    [ObservableProperty]
    private bool filterKeysEnabled;

    [ObservableProperty]
    private bool captionsEnabled;

    [ObservableProperty]
    private string? selectedCaptionLanguage = "en-US";

    [ObservableProperty]
    private bool soundVisualizationEnabled;

    [ObservableProperty]
    private bool monoAudioEnabled;

    public AccessibilityViewModel(
        IAccessibilityService accessibilityService,
        INotificationService notificationService)
    {
        _accessibilityService = accessibilityService;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadAccessibilitySettingsAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load accessibility settings: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleScreenReaderAsync()
    {
        try
        {
            if (ScreenReaderEnabled)
            {
                await _accessibilityService.EnableScreenReaderAsync();
                await _notificationService.ShowNotificationAsync("Screen reader enabled", "Success");
            }
            else
            {
                await _accessibilityService.DisableScreenReaderAsync();
                await _notificationService.ShowNotificationAsync("Screen reader disabled", "Success");
            }
        }
        catch (Exception ex)
        {
            ScreenReaderEnabled = !ScreenReaderEnabled;
            await _notificationService.ShowErrorAsync($"Failed to toggle screen reader: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleTextToSpeechAsync()
    {
        try
        {
            if (TextToSpeechEnabled)
            {
                await _accessibilityService.EnableTextToSpeechAsync();
                await _notificationService.ShowNotificationAsync("Text-to-speech enabled", "Success");
            }
            else
            {
                await _accessibilityService.DisableTextToSpeechAsync();
                await _notificationService.ShowNotificationAsync("Text-to-speech disabled", "Success");
            }
        }
        catch (Exception ex)
        {
            TextToSpeechEnabled = !TextToSpeechEnabled;
            await _notificationService.ShowErrorAsync($"Failed to toggle text-to-speech: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleHighContrastAsync()
    {
        try
        {
            if (HighContrastModeEnabled)
            {
                await _accessibilityService.EnableHighContrastAsync();
                await _notificationService.ShowNotificationAsync("High contrast mode enabled", "Success");
            }
            else
            {
                await _accessibilityService.DisableHighContrastAsync();
                await _notificationService.ShowNotificationAsync("High contrast mode disabled", "Success");
            }
        }
        catch (Exception ex)
        {
            HighContrastModeEnabled = !HighContrastModeEnabled;
            await _notificationService.ShowErrorAsync($"Failed to toggle high contrast: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ApplyColorBlindModeAsync()
    {
        try
        {
            if (ColorBlindModeEnabled)
            {
                await _accessibilityService.ApplyColorBlindModeAsync((int)SelectedColorBlindMode);
                await _notificationService.ShowNotificationAsync($"Color blind mode ({SelectedColorBlindMode}) applied", "Success");
            }
            else
            {
                await _accessibilityService.DisableColorBlindModeAsync();
                await _notificationService.ShowNotificationAsync("Color blind mode disabled", "Success");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to apply color blind mode: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ApplyUIScaleAsync()
    {
        try
        {
            await _accessibilityService.SetUIScaleAsync(UiScalePercentage / 100.0f);
            await _notificationService.ShowNotificationAsync($"UI scaled to {UiScalePercentage}%", "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to apply UI scale: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ApplyFontSizeAsync()
    {
        try
        {
            await _accessibilityService.SetFontSizeMultiplierAsync(FontSizeMultiplier);
            await _notificationService.ShowNotificationAsync($"Font size adjusted to {FontSizeMultiplier}x", "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to apply font size: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleReduceMotionAsync()
    {
        try
        {
            if (ReduceMotionEnabled)
            {
                await _accessibilityService.EnableReduceMotionAsync();
                await _notificationService.ShowNotificationAsync("Reduce motion enabled", "Success");
            }
            else
            {
                await _accessibilityService.DisableReduceMotionAsync();
                await _notificationService.ShowNotificationAsync("Reduce motion disabled", "Success");
            }
        }
        catch (Exception ex)
        {
            ReduceMotionEnabled = !ReduceMotionEnabled;
            await _notificationService.ShowErrorAsync($"Failed to toggle reduce motion: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ResetToDefaultsAsync()
    {
        try
        {
            ScreenReaderEnabled = false;
            TextToSpeechEnabled = false;
            TextToSpeechRate = 1.0f;
            HighContrastModeEnabled = false;
            ColorBlindModeEnabled = false;
            SelectedColorBlindMode = ColorBlindMode.Normal;
            UiScalePercentage = 100.0f;
            FontSizeMultiplier = 1.0f;
            ReduceMotionEnabled = false;
            PointerSize = 1.0f;
            MousePointerEnlargementEnabled = false;

            await _notificationService.ShowNotificationAsync("Accessibility settings reset to defaults", "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to reset settings: {ex.Message}");
        }
    }

    private async Task LoadAccessibilitySettingsAsync()
    {
        try
        {
            var settings = await _accessibilityService.GetCurrentSettingsAsync();
            if (settings.IsSuccess)
            {
                // Apply loaded settings to properties
                // This would be implemented based on the actual settings structure
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to load settings: {ex.Message}");
        }
    }
}

/// <summary>
/// Enumeration of color blind modes.
/// </summary>
public enum ColorBlindMode
{
    /// <summary>Normal color vision (no filter).</summary>
    Normal,

    /// <summary>Protanopia (red-blind).</summary>
    Protanopia,

    /// <summary>Deuteranopia (green-blind).</summary>
    Deuteranopia,

    /// <summary>Tritanopia (blue-blind).</summary>
    Tritanopia,

    /// <summary>Achromatopsia (complete color blindness).</summary>
    Achromatopsia
}
