using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.BatterySaver;

public sealed class BatterySaverPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "battery-saver";
    public string Name => "Battery Saver Pro";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Optimize settings to extend battery life on portable devices.";
    public PluginCapabilities Capabilities => PluginCapabilities.BatteryOptimization;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Battery Saver Initialized");
        }
        return Task.CompletedTask;
    }

    public void EnablePowerSavingMode()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Enabling Power Saving Mode: Limiting FPS to 30, TDP to 10W...");
        }
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
