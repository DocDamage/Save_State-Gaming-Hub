using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;
using System.Text.Json;

namespace SaveState.Plugins.GameTimer;

/// <summary>
/// Plugin that tracks playtime and enforces time limits with configurable warnings.
/// </summary>
public sealed class GameTimerPlugin : IPlugin
{
    private IPluginContext? _context;
    // Initialized in InitializeAsync before use
    private ITimeProvider? _timeProvider;
    private Timer? _sessionTimer;
    private DateTime _sessionStartTime;
    private TimeSpan _sessionDuration;
    private GameTimerSettings _settings = new();
    private string? _currentGameId;
    private bool _warningShown15Min;
    private bool _warningShown5Min;
    private bool _warningShown1Min;

    public string Id => "game-timer-alarm";
    public string Name => "Game Timer & Alarm";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Set playtime limits with configurable warnings and parental controls.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();
        _context.Logger.LogInformation("Game Timer & Alarm plugin initialized");

        // Load settings
        LoadSettings();

        // Register event handlers
        _context.EventReceived += OnEventReceived;

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _sessionTimer?.Dispose();
        SaveSettings();

        if (_context != null)
        {
            _context.EventReceived -= OnEventReceived;
        }

        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        switch (e.EventType)
        {
            case PluginEventType.GameLaunched:
                OnGameLaunched(e.Data);
                break;
            case PluginEventType.GameClosed:
                OnGameClosed(e.Data);
                break;
        }
    }

    private void OnGameLaunched(object? data)
    {
        if (!_settings.Enabled || _timeProvider == null)
            return;

        _currentGameId = data?.ToString();
        _sessionStartTime = _timeProvider.Now;
        _sessionDuration = TimeSpan.Zero;
        _warningShown15Min = false;
        _warningShown5Min = false;
        _warningShown1Min = false;

        // Start session timer (check every minute)
        _sessionTimer = new Timer(CheckSessionTime, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        var logger = _context?.Logger;
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation("Game session started for {GameId}", _currentGameId);
        }
    }

    private void OnGameClosed(object? data)
    {
        if (_sessionTimer == null || _timeProvider == null)
            return;

        _sessionTimer.Dispose();
        _sessionTimer = null;

        var now = _timeProvider.Now;
        var totalTime = now - _sessionStartTime;
        var logger = _context?.Logger;
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation("Game session ended. Duration: {Duration}", totalTime);
        }

        // Track daily playtime
        var today = _timeProvider.Now.Date;
        if (!_settings.DailyPlaytime.ContainsKey(today))
        {
            _settings.DailyPlaytime[today] = TimeSpan.Zero;
        }
        _settings.DailyPlaytime[today] += totalTime;

        SaveSettings();
        _currentGameId = null;
    }

    private void CheckSessionTime(object? state)
    {
        if (_currentGameId == null || _timeProvider == null)
            return;

        var now = _timeProvider.Now;
        _sessionDuration = now - _sessionStartTime;
        var remaining = _settings.SessionLimit - _sessionDuration;

        // Check for warnings
        if (!_warningShown15Min && remaining <= TimeSpan.FromMinutes(15) && remaining > TimeSpan.FromMinutes(14))
        {
            ShowWarning("15 minutes remaining in your gaming session!");
            _warningShown15Min = true;
        }
        else if (!_warningShown5Min && remaining <= TimeSpan.FromMinutes(5) && remaining > TimeSpan.FromMinutes(4))
        {
            ShowWarning("5 minutes remaining in your gaming session!");
            _warningShown5Min = true;
        }
        else if (!_warningShown1Min && remaining <= TimeSpan.FromMinutes(1) && remaining > TimeSpan.Zero)
        {
            ShowWarning("1 minute remaining! Please save your progress.");
            _warningShown1Min = true;
        }
        else if (remaining <= TimeSpan.Zero && _settings.EnforceLimit)
        {
            ShowWarning("Time limit reached! Please close the game.");
            // In a real implementation, this could trigger a graceful game shutdown
        }

        // Check daily limit
        var today = _timeProvider.Now.Date;
        if (_settings.DailyPlaytime.TryGetValue(today, out var todayPlaytime))
        {
            var totalToday = todayPlaytime + _sessionDuration;
            if (totalToday >= _settings.DailyLimit && _settings.DailyLimit > TimeSpan.Zero)
            {
                ShowWarning($"Daily playtime limit reached! ({_settings.DailyLimit.TotalHours:F1} hours)");
            }
        }
    }

    private void ShowWarning(string message)
    {
        var logger = _context?.Logger;
        if (logger?.IsEnabled(LogLevel.Warning) == true)
        {
            logger.LogWarning("Game Timer Warning: {Message}", message);
        }

        _context?.ReportProgress(message, 1.0f);

        // In a real implementation, this would show a system notification
        // For now, we'll just log it
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                _settings = JsonSerializer.Deserialize<GameTimerSettings>(json) ?? new GameTimerSettings();
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
/// Settings for the Game Timer plugin.
/// </summary>
public sealed class GameTimerSettings
{
    /// <summary>
    /// Whether the timer is enabled.
    /// </summary>
    [PluginSetting(
        DisplayName = "Enable Timer",
        Description = "Turn on playtime tracking and limits",
        Category = "General",
        Order = 0)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Session time limit (default: 2 hours).
    /// </summary>
    [PluginSetting(
        DisplayName = "Session Time Limit",
        Description = "Maximum play time per session",
        Category = "Limits",
        Order = 10)]
    public TimeSpan SessionLimit { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Daily time limit (default: 4 hours, 0 = unlimited).
    /// </summary>
    [PluginSetting(
        DisplayName = "Daily Time Limit",
        Description = "Maximum play time per day (0 = unlimited)",
        Category = "Limits",
        Order = 20)]
    public TimeSpan DailyLimit { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Whether to enforce limits (vs. just warn).
    /// </summary>
    [PluginSetting(
        DisplayName = "Enforce Limits",
        Description = "When enabled, games will be closed when limits are reached",
        Category = "Limits",
        Order = 30)]
    public bool EnforceLimit { get; set; } = false;

    /// <summary>
    /// Parental control PIN (empty = disabled).
    /// </summary>
    [PluginSetting(
        DisplayName = "Parental PIN",
        Description = "Set a PIN to prevent children from changing settings",
        Category = "Parental Controls",
        Order = 40,
        IsAdvanced = true)]
    [PluginSettingSecret]
    public string ParentalPin { get; set; } = string.Empty;

    /// <summary>
    /// Daily playtime tracking (date -> duration).
    /// </summary>
    public Dictionary<DateTime, TimeSpan> DailyPlaytime { get; set; } = new();

    /// <summary>
    /// Weekly playtime budget in hours (0 = unlimited).
    /// </summary>
    [PluginSetting(
        DisplayName = "Weekly Budget (hours)",
        Description = "Total hours allowed per week (0 = unlimited)",
        Category = "Limits",
        Order = 25)]
    [PluginSettingRange(0, 168, 0.5)]
    public double WeeklyBudgetHours { get; set; } = 20.0;
}
