using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Theme.Models;
using SaveState.Core.Theme.Services;
using SaveState.Infrastructure.Theme.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;

// Use Core IThemeService for theme builder functionality
using IThemeService = SaveState.Core.Theme.Services.IThemeService;

namespace SaveState.Presentation.ViewModels.Theme;

/// <summary>
/// ViewModel for the Theme Builder feature.
/// </summary>
public partial class ThemeBuilderViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IMaterialYouService _materialYouService;
    private readonly IThemeImportExportService _importExportService;
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<ThemeBuilderViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ThemeDefinition> _themes = new();

    [ObservableProperty]
    private ThemeDefinition _selectedTheme = new();

    [ObservableProperty]
    private ThemeColors _colors = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _newThemeName = string.Empty;

    [ObservableProperty]
    private bool _isPreviewMode;

    [ObservableProperty]
    private string _seedColor = "#6750A4";

    [ObservableProperty]
    private bool _isDarkTheme;

    // Color pickers for each role
    [ObservableProperty]
    private Color _primaryColor = Color.Parse("#6750A4");

    [ObservableProperty]
    private Color _secondaryColor = Color.Parse("#625B71");

    [ObservableProperty]
    private Color _tertiaryColor = Color.Parse("#7D5260");

    [ObservableProperty]
    private Color _errorColor = Color.Parse("#B3261E");

    [ObservableProperty]
    private Color _backgroundColor = Color.Parse("#FFFBFE");

    [ObservableProperty]
    private Color _surfaceColor = Color.Parse("#FFFBFE");

    [ObservableProperty]
    private Color _outlineColor = Color.Parse("#79747E");

    // Contrast information
    [ObservableProperty]
    private double _primaryContrastRatio;

    [ObservableProperty]
    private string _primaryComplianceLevel = "AA";

    [ObservableProperty]
    private bool _isColorBlindnessEnabled;

    [ObservableProperty]
    private ColorBlindnessType _selectedColorBlindnessType = ColorBlindnessType.None;

    public ThemeBuilderViewModel(
        IThemeService themeService,
        IMaterialYouService materialYouService,
        IThemeImportExportService importExportService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        ILogger<ThemeBuilderViewModel> logger)
    {
        _themeService = themeService;
        _materialYouService = materialYouService;
        _importExportService = importExportService;
        _dialogService = dialogService;
        _clipboardService = clipboardService;
        _logger = logger;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var result = await _themeService.GetAllThemesAsync();
            if (result.IsSuccess)
            {
                Themes.Clear();
                foreach (var theme in result.Value!)
                {
                    Themes.Add(theme);
                }

                if (Themes.Count > 0)
                {
                    SelectedTheme = Themes[0];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ThemeBuilder");
        }
    }

    partial void OnSelectedThemeChanged(ThemeDefinition value)
    {
        if (value != null)
        {
            Colors = value.Colors.Copy();
            IsDarkTheme = value.IsDark;
            UpdateColorProperties();
            UpdateContrastInfo();
        }
    }

    partial void OnPrimaryColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Primary), value);
    partial void OnSecondaryColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Secondary), value);
    partial void OnTertiaryColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Tertiary), value);
    partial void OnErrorColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Error), value);
    partial void OnBackgroundColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Background), value);
    partial void OnSurfaceColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Surface), value);
    partial void OnOutlineColorChanged(Color value) => UpdateThemeColor(nameof(Colors.Outline), value);

    private void UpdateThemeColor(string propertyName, Color color)
    {
        var hex = ColorToHex(color);
        var property = typeof(ThemeColors).GetProperty(propertyName);
        if (property != null)
        {
            property.SetValue(Colors, hex);
            UpdateContrastInfo();
        }
    }

    private void UpdateColorProperties()
    {
        PrimaryColor = Color.Parse(Colors.Primary);
        SecondaryColor = Color.Parse(Colors.Secondary);
        TertiaryColor = Color.Parse(Colors.Tertiary);
        ErrorColor = Color.Parse(Colors.Error);
        BackgroundColor = Color.Parse(Colors.Background);
        SurfaceColor = Color.Parse(Colors.Surface);
        OutlineColor = Color.Parse(Colors.Outline);
    }

    private void UpdateContrastInfo()
    {
        var contrast = ContrastInfo.Calculate(Colors.OnPrimary, Colors.Primary);
        PrimaryContrastRatio = contrast.Ratio;
        PrimaryComplianceLevel = contrast.ComplianceLevel;
    }

    [RelayCommand]
    private async Task CreateNewThemeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewThemeName))
        {
            await _dialogService.ShowErrorAsync("Please enter a theme name.");
            return;
        }

        var result = await _themeService.CreateThemeAsync(NewThemeName, SelectedTheme);
        if (result.IsSuccess)
        {
            Themes.Add(result.Value!);
            SelectedTheme = result.Value!;
            NewThemeName = string.Empty;
            IsEditing = false;

            await _dialogService.ShowSuccessAsync($"Theme '{result.Value!.Name}' created successfully!");
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to create theme.");
        }
    }

    [RelayCommand]
    private async Task SaveThemeAsync()
    {
        if (SelectedTheme == null || SelectedTheme.IsBuiltIn)
        {
            await _dialogService.ShowErrorAsync("Cannot save built-in themes. Create a copy instead.");
            return;
        }

        // Update colors from current selection
        SelectedTheme.Colors = Colors.Copy();
        SelectedTheme.IsDark = IsDarkTheme;

        var result = await _themeService.UpdateThemeAsync(SelectedTheme);
        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync($"Theme '{SelectedTheme.Name}' saved successfully!");
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to save theme.");
        }
    }

    [RelayCommand]
    private async Task DeleteThemeAsync(ThemeDefinition? theme)
    {
        if (theme == null) return;

        if (theme.IsBuiltIn)
        {
            await _dialogService.ShowErrorAsync("Cannot delete built-in themes.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Theme",
            $"Are you sure you want to delete '{theme.Name}'?");

        if (!confirmed) return;

        var result = await _themeService.DeleteThemeAsync(theme.Id);
        if (result.IsSuccess)
        {
            Themes.Remove(theme);
            if (Themes.Count > 0)
            {
                SelectedTheme = Themes[0];
            }
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to delete theme.");
        }
    }

    [RelayCommand]
    private async Task ApplyThemeAsync(ThemeDefinition? theme)
    {
        if (theme == null) return;

        var result = await _themeService.ApplyThemeAsync(theme);
        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync($"Theme '{theme.Name}' applied!");
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to apply theme.");
        }
    }

    [RelayCommand]
    private async Task DuplicateThemeAsync(ThemeDefinition? theme)
    {
        if (theme == null) return;

        var newName = $"{theme.Name} Copy";
        var result = await _themeService.DuplicateThemeAsync(theme.Id, newName);
        if (result.IsSuccess)
        {
            Themes.Add(result.Value!);
            SelectedTheme = result.Value!;
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to duplicate theme.");
        }
    }

    [RelayCommand]
    private async Task ExportThemeAsync(ThemeDefinition? theme)
    {
        if (theme == null) return;

        // Show export dialog with format selection
        var format = ThemeFormat.Json; // Default

        var result = await _themeService.ExportThemeAsync(theme.Id, format);
        if (result.IsSuccess)
        {
            // Copy to clipboard
            await _clipboardService.SetTextAsync(result.Value!);
            await _dialogService.ShowSuccessAsync("Theme exported to clipboard!");
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to export theme.");
        }
    }

    [RelayCommand]
    private async Task ImportThemeAsync()
    {
        // Show import dialog (would typically use file picker)
        // For now, show a text input dialog for JSON
        var json = await _dialogService.ShowTextInputAsync("Import Theme", "Paste theme JSON:");
        if (string.IsNullOrWhiteSpace(json)) return;

        var result = await _themeService.ImportThemeAsync(json);
        if (result.IsSuccess)
        {
            Themes.Add(result.Value!);
            SelectedTheme = result.Value!;
            await _dialogService.ShowSuccessAsync($"Theme '{result.Value!.Name}' imported!");
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.Error ?? "Failed to import theme.");
        }
    }

    [RelayCommand]
    private void GenerateFromSeed()
    {
        var colors = _materialYouService.GenerateColorScheme(SeedColor, IsDarkTheme);
        Colors = colors;
        UpdateColorProperties();
        UpdateContrastInfo();
    }

    [RelayCommand]
    private async Task GenerateFromImageAsync()
    {
        // Show file picker dialog
        var filePath = await _dialogService.ShowOpenFileDialogAsync("Select Image", "Images", new[] { "*.png", "*.jpg", "*.jpeg" });
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            await using var stream = File.OpenRead(filePath);
            var result = await _themeService.ImportFromImageAsync(stream, $"Image Theme {Themes.Count + 1}");
            if (result.IsSuccess)
            {
                Themes.Add(result.Value!);
                SelectedTheme = result.Value!;
                await _dialogService.ShowSuccessAsync($"Theme generated from image!");
            }
            else
            {
                await _dialogService.ShowErrorAsync(result.Error ?? "Failed to generate theme from image.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate theme from image");
            await _dialogService.ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PreviewThemeAsync()
    {
        IsPreviewMode = !IsPreviewMode;

        if (IsPreviewMode)
        {
            // Create temporary theme for preview
            var previewTheme = new ThemeDefinition
            {
                Id = SelectedTheme.Id,
                Name = SelectedTheme.Name,
                Colors = Colors.Copy(),
                IsDark = IsDarkTheme
            };

            await _themeService.PreviewThemeAsync(previewTheme);
        }
        else
        {
            // Restore original theme
            await _themeService.ApplyThemeAsync(SelectedTheme);
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        if (IsDarkTheme)
        {
            Colors = _themeService.GetDefaultDarkTheme().Colors.Copy();
        }
        else
        {
            Colors = _themeService.GetDefaultLightTheme().Colors.Copy();
        }
        UpdateColorProperties();
        UpdateContrastInfo();
    }

    [RelayCommand]
    private void ToggleEditing()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task AnalyzeAccessibilityAsync()
    {
        var suggestions = _materialYouService.SuggestAccessibilityImprovements(Colors.OnPrimary, Colors.Primary);
        var message = string.Join("\n", suggestions);
        await _dialogService.ShowMessageAsync("Accessibility Analysis", message);
    }

    [RelayCommand]
    private void ApplyColorBlindnessFilter()
    {
        if (SelectedColorBlindnessType == ColorBlindnessType.None)
        {
            // Restore original colors
            UpdateColorProperties();
        }
        else
        {
            // Apply color blindness simulation
            PrimaryColor = Color.Parse(_themeService.SimulateColorBlindness(Colors.Primary, SelectedColorBlindnessType));
            SecondaryColor = Color.Parse(_themeService.SimulateColorBlindness(Colors.Secondary, SelectedColorBlindnessType));
            TertiaryColor = Color.Parse(_themeService.SimulateColorBlindness(Colors.Tertiary, SelectedColorBlindnessType));
        }
    }

    [RelayCommand]
    private void GenerateAnalogousColors()
    {
        var colors = _materialYouService.GenerateAnalogousColors(SeedColor, 5);
        // Show color suggestions (could open a dialog or update UI)
        _logger.LogInformation("Generated {Count} analogous colors", colors.Count);
    }

    [RelayCommand]
    private void GenerateComplementaryColors()
    {
        var colors = _materialYouService.GenerateComplementaryColors(SeedColor);
        _logger.LogInformation("Generated {Count} complementary colors", colors.Count);
    }

    [RelayCommand]
    private void GenerateTriadicColors()
    {
        var colors = _materialYouService.GenerateTriadicColors(SeedColor);
        _logger.LogInformation("Generated {Count} triadic colors", colors.Count);
    }

    [RelayCommand]
    private void HarmonizeColors()
    {
        // Harmonize secondary and tertiary towards primary
        Colors.Secondary = _materialYouService.Harmonize(Colors.Secondary, Colors.Primary, 0.3);
        Colors.Tertiary = _materialYouService.Harmonize(Colors.Tertiary, Colors.Primary, 0.3);
        UpdateColorProperties();
    }

    public IEnumerable<ColorBlindnessType> ColorBlindnessTypes =>
        Enum.GetValues<ColorBlindnessType>();

    public string Title => "Theme Builder";

    public string Subtitle => "Create and customize your own color themes";

    public bool CanDeleteSelectedTheme => SelectedTheme != null && !SelectedTheme.IsBuiltIn;

    public bool CanEditSelectedTheme => SelectedTheme != null && !SelectedTheme.IsBuiltIn;

    private static string ColorToHex(Color color)
    {
        return $"#{(uint)color.ToUInt32():X8}";
    }
}
