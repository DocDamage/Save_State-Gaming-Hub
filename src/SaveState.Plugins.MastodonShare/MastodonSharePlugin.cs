using Mastonet;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.MastodonShare;

public sealed class MastodonSharePlugin : IPlugin
{
    private IPluginContext? _context;
    private MastodonClient? _client = null;

    public string Id => "mastodon-share";
    public string Name => "Mastodon Share";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Post updates to the Fediverse.";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Mastodon Plugin Initialized");
        return Task.CompletedTask;
    }

    public async Task PostStatusAsync(string text)
    {
        if (_client == null) return;
        try
        {
            await _client.PublishStatus(text, visibility: Visibility.Public);
            _context?.Logger.LogInformation("Toot posted!");
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to post to Mastodon");
        }
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
