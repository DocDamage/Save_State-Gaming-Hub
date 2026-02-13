using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.ModManagerPro;

public sealed class ModManagerProPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "mod-manager-pro";
    public string Name => "Mod Manager Pro";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Manage mods via Nexus Mods integration.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Mod Manager Pro Initialized");
        return Task.CompletedTask;
    }

    // Future public API
    public Task CheckForUpdatesAsync()
    {
        _context?.Logger.LogInformation("Checking for mod updates...");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
