using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.ControllerRemapper;

public sealed class ControllerRemapperPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "controller-remapper";
    public string Name => "Controller Remapper";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Apply per-game controller profiles automatically.";
    public PluginCapabilities Capabilities => PluginCapabilities.InputProvider;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Controller Remapper Initialized");
        _context.EventReceived += OnEventReceived;
        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        if (e.EventType == PluginEventType.GameLaunched && e.Data is string gameTitle)
        {
            ApplyProfile(gameTitle);
        }
    }

    private void ApplyProfile(string game)
    {
        _context?.Logger.LogInformation("Applying controller profile for {Game}", game);
        // SDL_GameControllerAddMapping(...)
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
