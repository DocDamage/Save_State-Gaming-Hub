using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.CheatManager;

public sealed class CheatManagerPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "cheat-manager";
    public string Name => "Cheat Code Manager";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Manage GameShark and Action Replay codes.";
    public PluginCapabilities Capabilities => PluginCapabilities.Emulation;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Cheat Manager Initialized");
        return Task.CompletedTask;
    }

    public Task EnableCheatAsync(string game, string code)
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
            _context.Logger.LogInformation("Enabling cheat for {Game}: {Code}", game, code);
        }
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
