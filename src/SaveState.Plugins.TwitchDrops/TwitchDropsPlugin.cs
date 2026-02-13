using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.TwitchDrops;

public sealed class TwitchDropsPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "twitch-drops";
    public string Name => "Twitch Drops Tracker";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Track active Twitch Drop campaigns for your games.";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Twitch Drops Tracker Initialized");
        }
        return Task.CompletedTask;
    }

    public Task UpdateCampaignsAsync()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Updating Twitch Drop campaigns...");
        }
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
