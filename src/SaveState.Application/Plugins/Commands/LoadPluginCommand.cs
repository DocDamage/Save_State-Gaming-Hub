using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Plugins.Commands;

/// <summary>
/// Command to load a plugin.
/// </summary>
public record LoadPluginCommand(string PluginPath) : IRequest<Result<bool>>;

/// <summary>
/// Handler for loading plugins.
/// </summary>
public class LoadPluginCommandHandler : IRequestHandler<LoadPluginCommand, Result<bool>>
{
    private readonly Core.Plugins.Services.IPluginManager _pluginManager;

    public LoadPluginCommandHandler(Core.Plugins.Services.IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public async Task<Result<bool>> Handle(LoadPluginCommand request, CancellationToken ct)
    {
        return await _pluginManager.LoadPluginAsync(request.PluginPath, ct);
    }
}