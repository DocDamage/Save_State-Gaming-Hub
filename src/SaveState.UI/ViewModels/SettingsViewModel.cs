using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.Accessibility;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ILogger _logger = Log.ForContext<SettingsViewModel>();
    private readonly string _configPath;
    private readonly AccessibilityService _accessibilityService;
    private readonly ThemeService _themeService;

    // API Keys
    [ObservableProperty]
    private string _twitchClientId = string.Empty;

    [ObservableProperty]
    private string _twitchClientSecret = string.Empty;

    [ObservableProperty]
    private string _steamGridDbApiKey = string.Empty;

    [ObservableProperty]
    private string _geminiApiKey = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Theme
    [ObservableProperty]
    private ObservableCollection<ThemeDisplayInfo> _themes = new();

    [ObservableProperty]
    private string _currentThemeName = "SaveState Dark";

    // Accessibility
    [ObservableProperty]
    private bool _highContrastMode;

    [ObservableProperty]
    private double _textScale = 1.0;

    [ObservableProperty]
    private bool _reducedMotion;

    [ObservableProperty]
    private bool _screenReaderSupport;

    [ObservableProperty]
    private string _selectedColorBlindMode = "None";

    [ObservableProperty]
    private bool _useDyslexiaFont;

    [ObservableProperty]
    private bool _showSubtitles;

    [ObservableProperty]
    private bool _audioCues = true;

    public ObservableCollection<string> ColorBlindModes { get; } = new()
    {
        "None", "Protanopia", "Deuteranopia", "Tritanopia", "Monochromacy"
    };

    public ObservableCollection<double> TextScaleOptions { get; } = new()
    {
        0.75, 0.9, 1.0, 1.1, 1.25, 1.5, 1.75, 2.0
    };

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand LoadCommand { get; }
    public IRelayCommand<string> ApplyThemeCommand { get; }
    public IRelayCommand SaveAccessibilityCommand { get; }
    public IRelayCommand ResetAccessibilityCommand { get; }

    public SettingsViewModel()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(appData, "SaveState");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "settings.json");

        _accessibilityService = AccessibilityService.Instance;
        _themeService = ThemeService.Instance;

        SaveCommand = new RelayCommand(Save);
        LoadCommand = new RelayCommand(Load);
        ApplyThemeCommand = new RelayCommand<string>(ApplyTheme);
        SaveAccessibilityCommand = new RelayCommand(SaveAccessibility);
        ResetAccessibilityCommand = new RelayCommand(ResetAccessibility);

        Load();
        LoadThemes();
        LoadAccessibilitySettings();
    }

    private void LoadThemes()
    {
        Themes.Clear();
        foreach (var theme in _themeService.AvailableThemes)
        {
            Themes.Add(new ThemeDisplayInfo
            {
                Id = theme.Id,
                Name = theme.Name,
                Description = theme.Description,
                IsDark = theme.IsDark,
                IsBuiltIn = theme.IsBuiltIn,
                PrimaryColor = theme.Colors.Primary,
                SecondaryColor = theme.Colors.Secondary,
                IsSelected = theme.Id == _themeService.CurrentTheme.Id
            });
        }
        CurrentThemeName = _themeService.CurrentTheme.Name;
    }

    private void LoadAccessibilitySettings()
    {
        var settings = _accessibilityService.Settings;
        HighContrastMode = settings.HighContrastMode;
        TextScale = settings.TextScale;
        ReducedMotion = settings.ReducedMotion;
        ScreenReaderSupport = settings.ScreenReaderSupport;
        SelectedColorBlindMode = settings.ColorBlindMode.ToString();
        UseDyslexiaFont = settings.UseDyslexiaFont;
        ShowSubtitles = settings.ShowSubtitles;
        AudioCues = settings.AudioCues;
    }

    private void ApplyTheme(string? themeId)
    {
        if (string.IsNullOrEmpty(themeId)) return;
        _themeService.ApplyTheme(themeId);
        foreach (var theme in Themes) theme.IsSelected = theme.Id == themeId;
        CurrentThemeName = _themeService.CurrentTheme.Name;
        StatusMessage = $"Theme applied: {CurrentThemeName}";
    }

    private void SaveAccessibility()
    {
        var settings = new AccessibilitySettings
        {
            HighContrastMode = HighContrastMode,
            TextScale = TextScale,
            ReducedMotion = ReducedMotion,
            ScreenReaderSupport = ScreenReaderSupport,
            ColorBlindMode = Enum.TryParse<ColorBlindType>(SelectedColorBlindMode, out var cbm) 
                ? cbm : ColorBlindType.None,
            UseDyslexiaFont = UseDyslexiaFont,
            ShowSubtitles = ShowSubtitles,
            AudioCues = AudioCues
        };
        _accessibilityService.UpdateSettings(settings);
        StatusMessage = "Accessibility settings saved!";
    }

    private void ResetAccessibility()
    {
        _accessibilityService.ResetToDefaults();
        LoadAccessibilitySettings();
        StatusMessage = "Accessibility settings reset to defaults";
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        App.SetTheme(value);
    }

    private void Save()
    {
        try
        {
            var config = new
            {
                TwitchClientId,
                TwitchClientSecret,
                SteamGridDbApiKey,
                GeminiApiKey,
                IsDarkTheme
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);

            Environment.SetEnvironmentVariable("TWITCH_CLIENT_ID", TwitchClientId);
            Environment.SetEnvironmentVariable("TWITCH_CLIENT_SECRET", TwitchClientSecret);
            Environment.SetEnvironmentVariable("STEAMGRIDDB_API_KEY", SteamGridDbApiKey);
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", GeminiApiKey);

            StatusMessage = "Settings saved successfully!";
            _logger.Information("Settings saved to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save: {ex.Message}";
            _logger.Error(ex, "Failed to save settings");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                TwitchClientId = root.TryGetProperty("TwitchClientId", out var tc) ? tc.GetString() ?? "" : "";
                TwitchClientSecret = root.TryGetProperty("TwitchClientSecret", out var ts) ? ts.GetString() ?? "" : "";
                SteamGridDbApiKey = root.TryGetProperty("SteamGridDbApiKey", out var sg) ? sg.GetString() ?? "" : "";
                GeminiApiKey = root.TryGetProperty("GeminiApiKey", out var oa) ? oa.GetString() ?? "" : "";
                IsDarkTheme = root.TryGetProperty("IsDarkTheme", out var dt) ? dt.GetBoolean() : true;

                if (!string.IsNullOrEmpty(TwitchClientId))
                    Environment.SetEnvironmentVariable("TWITCH_CLIENT_ID", TwitchClientId);
                if (!string.IsNullOrEmpty(TwitchClientSecret))
                    Environment.SetEnvironmentVariable("TWITCH_CLIENT_SECRET", TwitchClientSecret);
                if (!string.IsNullOrEmpty(SteamGridDbApiKey))
                    Environment.SetEnvironmentVariable("STEAMGRIDDB_API_KEY", SteamGridDbApiKey);
                if (!string.IsNullOrEmpty(GeminiApiKey))
                    Environment.SetEnvironmentVariable("GEMINI_API_KEY", GeminiApiKey);

                App.SetTheme(IsDarkTheme);
                _logger.Information("Settings loaded from {Path}", _configPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load settings");
        }
    }
}

public partial class ThemeDisplayInfo : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDark { get; set; }
    public bool IsBuiltIn { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

