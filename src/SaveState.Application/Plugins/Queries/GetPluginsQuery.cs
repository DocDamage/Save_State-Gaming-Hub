using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Application.Plugins.Queries;

/// <summary>
/// Query to get all plugins.
/// </summary>
public record GetPluginsQuery : IRequest<Result<IReadOnlyList<PluginInfo>>>;

/// <summary>
/// Handler for getting plugins.
/// </summary>
public class GetPluginsQueryHandler : IRequestHandler<GetPluginsQuery, Result<IReadOnlyList<PluginInfo>>>
{
    private readonly Core.Plugins.Services.IPluginManager _pluginManager;

    public GetPluginsQueryHandler(Core.Plugins.Services.IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public Task<Result<IReadOnlyList<PluginInfo>>> Handle(GetPluginsQuery request, CancellationToken ct)
    {
        var plugins = _pluginManager.GetLoadedPlugins();
        return Task.FromResult(Result<IReadOnlyList<PluginInfo>>.Success(plugins));
    }
}

/// <summary>
/// Query to discover available plugins.
/// </summary>
public record DiscoverPluginsQuery : IRequest<Result<IReadOnlyList<PluginInfo>>>;

/// <summary>
/// Handler for discovering plugins.
/// </summary>
public class DiscoverPluginsQueryHandler : IRequestHandler<DiscoverPluginsQuery, Result<IReadOnlyList<PluginInfo>>>
{
    private readonly Core.Plugins.Services.IPluginManager _pluginManager;

    public DiscoverPluginsQueryHandler(Core.Plugins.Services.IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public async Task<Result<IReadOnlyList<PluginInfo>>> Handle(DiscoverPluginsQuery request, CancellationToken ct)
    {
        return await _pluginManager.DiscoverPluginsAsync(ct);
    }
}
