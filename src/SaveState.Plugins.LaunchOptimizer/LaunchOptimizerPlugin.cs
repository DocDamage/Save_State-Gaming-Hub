using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;
using System.Diagnostics;

namespace SaveState.Plugins.LaunchOptimizer;

public sealed class LaunchOptimizerPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "launch-optimizer";
    public string Name => "Launch Time Optimizer";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Optimize system resources before game launch.";
    public PluginCapabilities Capabilities => PluginCapabilities.LaunchExperience;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Launch Optimizer Initialized");
        _context.EventReceived += OnEventReceived;
        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        if (e.EventType == PluginEventType.GameLaunched)
        {
             OptimizeSystem();
        }
    }

    private void OptimizeSystem()
    {
        _context?.Logger.LogInformation("Optimizing system resources...");
        // Example: Set priority of background tasks to Low
        // Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
