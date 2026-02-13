using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.ScreenshotCaptioner;

public sealed class ScreenshotCaptionerPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "screenshot-captioner";
    public string Name => "Screenshot Caption Generator";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Generate descriptions for your screenshots.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Screenshot Captioner Initialized");
        _context.EventReceived += OnEventReceived;
        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        // React to new screenshot event if added in future
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
