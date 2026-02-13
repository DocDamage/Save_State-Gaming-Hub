using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.GenreDeepDive;

public sealed class GenreDeepDivePlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "genre-deep-dive";
    public string Name => "Genre Deep Dive";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Analyze your playing habits with AI-powered genre insights.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Genre Deep Dive Initialized");
        }
        return Task.CompletedTask;
    }

    public Task AnalyzeLibraryAsync()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Analyzing genre distribution...");
        }
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
