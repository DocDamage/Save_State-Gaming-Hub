using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.GameSummarizer;

public sealed class GameSummarizerPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "game-summarizer";
    public string Name => "Game Summary Generator";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Create 'What happened last time' recaps.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Game Summarizer Initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
