using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;
using Tweetinvi;

namespace SaveState.Plugins.TwitterShare;

public sealed class TwitterSharePlugin : IPlugin
{
    private IPluginContext? _context;
    private TwitterClient? _client = null;

    public string Id => "twitter-share";
    public string Name => "Twitter/X Share";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Post screenshots and achievements to X.";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Twitter Plugin Initialized");
        // API Key setup would be here
        return Task.CompletedTask;
    }

    public async Task ShareStatusAsync(string text)
    {
        if (_client == null) return;
        try
        {
            await _client.Tweets.PublishTweetAsync(text);
            _context?.Logger.LogInformation("Tweet posted!");
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to tweet");
        }
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
