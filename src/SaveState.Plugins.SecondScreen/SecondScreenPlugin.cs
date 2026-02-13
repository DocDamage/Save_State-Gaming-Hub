using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.SecondScreen;

public sealed class SecondScreenPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "second-screen";
    public string Name => "Second Screen Companion";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "View stats and inventory on your phone.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Second Screen Companion Initialized");
        }
        StartWebServer();
        return Task.CompletedTask;
    }

    private void StartWebServer()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Starting internal web server on port 8080...");
        }
        // Mock server start
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
