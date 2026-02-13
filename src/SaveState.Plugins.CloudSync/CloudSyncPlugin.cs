using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.CloudSync;

public sealed class CloudSyncPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "cloud-sync-generic";
    public string Name => "Cloud Save Sync";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Sync saves to Google Drive, Dropbox, or OneDrive.";
    public PluginCapabilities Capabilities => PluginCapabilities.CloudStorage; // Reusing existing capability

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Cloud Sync Initialized");
        }
        return Task.CompletedTask;
    }

    public Task SyncSavesAsync()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Syncing saves to cloud...");
        }
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
