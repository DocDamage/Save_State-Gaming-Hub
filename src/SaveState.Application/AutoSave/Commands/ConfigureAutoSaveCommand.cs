using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Commands;

/// <summary>
/// Command to configure auto-save for a game.
/// </summary>
public sealed record ConfigureAutoSaveCommand(
    Guid GameId,
    bool? IsEnabled = null,
    int? IntervalMinutes = null,
    int? MaxAutoSaves = null,
    bool? SaveOnLevelComplete = null,
    bool? SaveBeforeBoss = null,
    bool? SaveOnCheckpoint = null,
    string? NamingPattern = null,
    List<string>? Tags = null) : IRequest<Result<AutoSaveConfiguration>>;

/// <summary>
/// Handler for ConfigureAutoSaveCommand.
/// </summary>
public sealed class ConfigureAutoSaveCommandHandler : IRequestHandler<ConfigureAutoSaveCommand, Result<AutoSaveConfiguration>>
{
    private readonly IAutoSaveService _autoSaveService;

    public ConfigureAutoSaveCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<AutoSaveConfiguration>> Handle(ConfigureAutoSaveCommand request, CancellationToken cancellationToken)
    {
        var configRequest = new ConfigureAutoSaveRequest
        {
            GameId = request.GameId,
            IsEnabled = request.IsEnabled,
            IntervalMinutes = request.IntervalMinutes,
            MaxAutoSaves = request.MaxAutoSaves,
            SaveOnLevelComplete = request.SaveOnLevelComplete,
            SaveBeforeBoss = request.SaveBeforeBoss,
            SaveOnCheckpoint = request.SaveOnCheckpoint,
            NamingPattern = request.NamingPattern,
            Tags = request.Tags
        };

        return await _autoSaveService.ConfigureAutoSaveAsync(configRequest, cancellationToken);
    }
}
