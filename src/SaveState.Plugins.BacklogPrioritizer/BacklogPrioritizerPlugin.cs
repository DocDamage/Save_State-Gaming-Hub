using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.BacklogPrioritizer;

public sealed class BacklogPrioritizerPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "backlog-prioritizer";
    public string Name => "AI Backlog Prioritizer";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Rank your backlog using weighted scoring.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Backlog Prioritizer Initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
