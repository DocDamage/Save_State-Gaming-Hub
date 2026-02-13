using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.TouchControls;

public sealed class TouchControlsPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "touch-controls";
    public string Name => "Touch Control Overlay";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Virtual gamepad overlay for touch screens.";
    public PluginCapabilities Capabilities => PluginCapabilities.TouchControls;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Touch Controls Initialized");
        }
        return Task.CompletedTask;
    }

    public void ShowOverlay()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Showing Virtual Controller...");
        }
        // Show Avalonia Window
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
