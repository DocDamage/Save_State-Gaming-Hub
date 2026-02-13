using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.FpsUnlocker;

public sealed class FpsUnlockerPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "fps-unlocker";
    public string Name => "FPS Unlocker";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Apply performance patches to remove frame rate limits.";
    public PluginCapabilities Capabilities => PluginCapabilities.SystemOptimization;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("FPS Unlocker Initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
