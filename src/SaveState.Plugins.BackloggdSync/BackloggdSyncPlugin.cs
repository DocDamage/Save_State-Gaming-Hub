using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.BackloggdSync;

public sealed class BackloggdSyncPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "backloggd-sync";
    public string Name => "Backloggd Sync";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Sync your library and played status to Backloggd.";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Backloggd Sync Initialized");
        }
        return Task.CompletedTask;
    }

    public Task SyncLibraryAsync()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
            _context.Logger.LogInformation("Starting Backloggd Sync...");
        }
        // Mock sync
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
