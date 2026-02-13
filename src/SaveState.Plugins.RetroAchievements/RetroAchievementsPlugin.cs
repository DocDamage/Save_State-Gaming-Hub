using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.RetroAchievements;

public sealed class RetroAchievementsPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "retro-achievements";
    public string Name => "RetroAchievements Overlay";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Display achievement pop-ups in-game.";
    public PluginCapabilities Capabilities => PluginCapabilities.Emulation;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("RetroAchievements Overlay Initialized");

        // Subscribe to mock achievement event
        // context.EventReceived += OnAchievementUnlocked;

        return Task.CompletedTask;
    }

    public void ShowUnlockNotification(string title, string points)
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
            _context.Logger.LogInformation("Achievement Unlocked: {Title} ({Points})", title, points);
        }
        // Show Avalonia Window
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
