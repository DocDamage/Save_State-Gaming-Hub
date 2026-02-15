using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.AiRecommender;

public sealed class AiRecommenderPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "ai-recommender";
    public string Name => "AI Game Recommender";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Intelligent game recommendations based on mood and simple heuristics.";
    public PluginCapabilities Capabilities => PluginCapabilities.AIService;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("AI Recommender Initialized");
        return Task.CompletedTask;
    }

    // Future feature: Public method exposed to host via reflection or future API
    public async Task<List<string>> GetRecommendationsAsync(string mood)
    {
         await Task.Delay(100); // Simulate AI thought
         var logger = _context?.Logger;
         if (logger?.IsEnabled(LogLevel.Information) == true)
         {
             logger.LogInformation("Generating recommendations for mood: {Mood}", mood);
         }

         // Mock logic
         if (mood.ToLower().Contains("chill"))
             return new List<string> { "Stardew Valley", "Minecraft", "Animal Crossing" };

         return new List<string> { "Elden Ring", "Doom Eternal" };
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
