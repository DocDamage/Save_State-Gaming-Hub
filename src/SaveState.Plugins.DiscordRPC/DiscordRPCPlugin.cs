using DiscordRPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;
using System.Text.Json;

namespace SaveState.Plugins.DiscordRPC;

/// <summary>
/// Enhanced Discord Rich Presence plugin with custom status messages and artwork.
/// </summary>
public sealed class DiscordRPCPlugin : IPlugin
{
    private IPluginContext? _context;
    private ITimeProvider? _timeProvider;
    private DiscordRpcClient? _client;
    private DiscordRPCSettings _settings = new();
    private DateTime _sessionStartTime;
    private string? _currentGameTitle;
    private int _achievementCount;
    private int _totalAchievements;

    public string Id => "discord-rpc-pro";
    public string Name => "Discord Rich Presence Pro";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Enhanced Discord status with custom messages, artwork, and achievement progress.";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _timeProvider = context.Services.GetService<ITimeProvider>();
        _context.Logger.LogInformation("Discord RPC Pro plugin initialized");

        LoadSettings();

        if (_settings.Enabled)
        {
            InitializeDiscordClient();
        }

        _context.EventReceived += OnEventReceived;

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _client?.Dispose();
        SaveSettings();

        if (_context != null)
        {
            _context.EventReceived -= OnEventReceived;
        }

        return Task.CompletedTask;
    }

    private void InitializeDiscordClient()
    {
        try
        {
            _client = new DiscordRpcClient(_settings.ApplicationId);
            _client.Initialize();
            var logger = _context?.Logger;
            if (logger?.IsEnabled(LogLevel.Information) == true)
            {
                logger.LogInformation("Discord RPC client initialized with App ID: {AppId}", _settings.ApplicationId);
            }

            // Set initial presence
            UpdatePresence("Browsing Library", "Choosing a game to play");
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to initialize Discord RPC client");
        }
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        switch (e.EventType)
        {
            case PluginEventType.GameLaunched:
                OnGameLaunched(e.Data);
                break;
            case PluginEventType.GameClosed:
                OnGameClosed();
                break;
        }
    }

    private void OnGameLaunched(object? data)
    {
        _currentGameTitle = data?.ToString() ?? "Unknown Game";
        _sessionStartTime = _timeProvider?.UtcNow ?? SystemTimeProvider.Instance.UtcNow; // Uses injected ITimeProvider
        _achievementCount = 0;
        _totalAchievements = 0;

        UpdateGamePresence();
    }

    private void OnGameClosed()
    {
        _currentGameTitle = null;
        UpdatePresence("Browsing Library", "Just finished playing");
    }

    private void UpdateGamePresence()
    {
        if (_client == null || string.IsNullOrEmpty(_currentGameTitle))
            return;

        try
        {
            var details = GetCustomMessage(_currentGameTitle) ?? $"Playing {_currentGameTitle}";
            var state = _totalAchievements > 0
                ? $"{_achievementCount}/{_totalAchievements} Achievements"
                : "Enjoying the game";

            var presence = new RichPresence
            {
                Details = details,
                State = state,
                Timestamps = new Timestamps
                {
                    Start = _sessionStartTime
                },
                Assets = new Assets
                {
                    LargeImageKey = "savestate_logo",
                    LargeImageText = _currentGameTitle,
                    SmallImageKey = "controller",
                    SmallImageText = "SaveState"
                }
            };

            // Add buttons if configured
            if (_settings.ShowInviteButton)
            {
                presence.Buttons = new[]
                {
                    new Button { Label = "View on SaveState", Url = $"https://savestate.app/games/{Uri.EscapeDataString(_currentGameTitle)}" }
                };
            }

            _client.SetPresence(presence);
            var logger = _context?.Logger;
            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                logger.LogDebug("Updated Discord presence for: {Game}", _currentGameTitle);
            }
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to update Discord presence");
        }
    }

    private void UpdatePresence(string details, string state)
    {
        if (_client == null)
            return;

        try
        {
            var presence = new RichPresence
            {
                Details = details,
                State = state,
                Assets = new Assets
                {
                    LargeImageKey = "savestate_logo",
                    LargeImageText = "SaveState",
                    SmallImageKey = "idle",
                    SmallImageText = "Idle"
                }
            };

            _client.SetPresence(presence);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to update Discord presence");
        }
    }

    private string? GetCustomMessage(string gameTitle)
    {
        if (_settings.CustomMessages.TryGetValue(gameTitle, out var message))
        {
            return message;
        }

        return null;
    }

    /// <summary>
    /// Updates achievement progress (called externally).
    /// </summary>
    public void UpdateAchievementProgress(int unlocked, int total)
    {
        _achievementCount = unlocked;
        _totalAchievements = total;
        UpdateGamePresence();
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                _settings = JsonSerializer.Deserialize<DiscordRPCSettings>(json) ?? new DiscordRPCSettings();
            }
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to load settings");
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to save settings");
        }
    }
}

/// <summary>
/// Settings for Discord RPC Pro plugin.
/// </summary>
public sealed class DiscordRPCSettings
{
    /// <summary>
    /// Whether Discord RPC is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Discord Application ID (default is SaveState's app ID).
    /// </summary>
    public string ApplicationId { get; set; } = "1234567890123456789"; // Replace with actual SaveState Discord App ID

    /// <summary>
    /// Custom status messages per game.
    /// </summary>
    public Dictionary<string, string> CustomMessages { get; set; } = new();

    /// <summary>
    /// Whether to show "View on SaveState" button.
    /// </summary>
    public bool ShowInviteButton { get; set; } = true;

    /// <summary>
    /// Whether to show playtime in session.
    /// </summary>
    public bool ShowPlaytime { get; set; } = true;

    /// <summary>
    /// Whether to show achievement progress.
    /// </summary>
    public bool ShowAchievements { get; set; } = true;
}
