using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.BoxArtGenerator;

public sealed class BoxArtGeneratorPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "box-art-generator";
    public string Name => "Box Art Generator";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Generate missing cover art using AI.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Box Art Generator Initialized");
        return Task.CompletedTask;
    }

    public Task GenerateCoverAsync(string gameTitle, string style)
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
            _context.Logger.LogInformation("Generating {Style} cover for {Game}", style, gameTitle);
        }
        // Call AI Service (Stable Diffusion / DALL-E)
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
