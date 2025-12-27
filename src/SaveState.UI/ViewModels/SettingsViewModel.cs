using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.IO;
using System.Text.Json;

namespace SaveState.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ILogger _logger = Log.ForContext<SettingsViewModel>();
    private readonly string _configPath;

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

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand LoadCommand { get; }

    public SettingsViewModel()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(appData, "SaveState");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "settings.json");

        SaveCommand = new RelayCommand(Save);
        LoadCommand = new RelayCommand(Load);

        Load();
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

            // Also set environment variables for the current session
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

                // Set environment variables
                if (!string.IsNullOrEmpty(TwitchClientId))
                    Environment.SetEnvironmentVariable("TWITCH_CLIENT_ID", TwitchClientId);
                if (!string.IsNullOrEmpty(TwitchClientSecret))
                    Environment.SetEnvironmentVariable("TWITCH_CLIENT_SECRET", TwitchClientSecret);
                if (!string.IsNullOrEmpty(SteamGridDbApiKey))
                    Environment.SetEnvironmentVariable("STEAMGRIDDB_API_KEY", SteamGridDbApiKey);
                if (!string.IsNullOrEmpty(GeminiApiKey))
                    Environment.SetEnvironmentVariable("GEMINI_API_KEY", GeminiApiKey);

                // Apply theme
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
