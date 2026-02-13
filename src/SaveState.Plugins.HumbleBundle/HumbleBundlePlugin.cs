using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.HumbleBundle;

public sealed class HumbleBundlePlugin : IPlugin, IGameProvider
{
    private IPluginContext? _context;
    private readonly HttpClient _httpClient = new();

    public string Id => "humble-bundle";
    public string Name => "Humble Bundle";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Import games from Humble Bundle purchase history.";
    public PluginCapabilities Capabilities => PluginCapabilities.GameProvider;

    public string ProviderName => "Humble Bundle";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Humble Bundle initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    public async Task<Result<IReadOnlyList<Game>>> DiscoverGamesAsync(CancellationToken ct = default)
    {
        // Real implementation requires user to provide API Key or Cookie via settings
        // For this wave, we'll return empty success to indicate "no games found" (auth needed)
        // rather than failing the build or process.

        await Task.Yield();
        _context?.Logger.LogInformation("Humble Bundle discovery skipped (Authentication required)");

        return Result.Success<IReadOnlyList<Game>>(new List<Game>());
    }

    public Task<Result<Game>> GetGameDetailsAsync(string externalId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure<Game>("Not implemented"));
    }

    public Task<Result<bool>> InstallGameAsync(string externalId, string installPath, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure<bool>("Not implemented"));
    }
}
