using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;
using System.Text.Json;

namespace SaveState.Plugins.Leaderboards;

public sealed class LeaderboardsPlugin : IPlugin
{
    private IPluginContext? _context;
    // In future, this would hold leaderboards data
    private Dictionary<string, double> _localPlaytimeStats = new();

    public string Id => "playtime-leaderboards";
    public string Name => "Playtime Leaderboards";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Compare playtime with friends and community.";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Leaderboards plugin initialized");

        LoadLocalStats();
        _context.EventReceived += OnEventReceived;

        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        // Track game closing to update local "leaderboard" stats
        if (e.EventType == PluginEventType.GameClosed && e.Data is string gameTitle)
        {
             // Mock update
             var logger = _context?.Logger;
             if (logger?.IsEnabled(LogLevel.Debug) == true)
             {
                 logger.LogDebug("Updating playtime stats for {Game}", gameTitle);
             }

             // Real implementation would sync to backend here
        }
    }

    private void LoadLocalStats()
    {
         // Mock
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        if (_context != null)
        {
            _context.EventReceived -= OnEventReceived;
        }
        return Task.CompletedTask;
    }
}
