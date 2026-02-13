using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.SmartPlaylists;

public sealed class SmartPlaylistsPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "smart-playlists";
    public string Name => "Smart Playlist Generator";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Auto-generate playlists like 'Short Games' or 'Highly Rated'.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Smart Playlists Initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
