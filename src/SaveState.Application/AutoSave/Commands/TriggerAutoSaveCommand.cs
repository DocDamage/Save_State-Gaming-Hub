using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Commands;

/// <summary>
/// Command to trigger a manual auto-save.
/// </summary>
public sealed record TriggerAutoSaveCommand(
    Guid GameId,
    AutoSaveTriggerType TriggerType = AutoSaveTriggerType.Manual,
    string? Level = null,
    string? Checkpoint = null,
    int? PlayTimeSeconds = null,
    string? CustomName = null) : IRequest<Result<AutoSaveEntry>>;

/// <summary>
/// Handler for TriggerAutoSaveCommand.
/// </summary>
public sealed class TriggerAutoSaveCommandHandler : IRequestHandler<TriggerAutoSaveCommand, Result<AutoSaveEntry>>
{
    private readonly IAutoSaveService _autoSaveService;

    public TriggerAutoSaveCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<AutoSaveEntry>> Handle(TriggerAutoSaveCommand request, CancellationToken cancellationToken)
    {
        var triggerRequest = new TriggerAutoSaveRequest
        {
            GameId = request.GameId,
            TriggerType = request.TriggerType,
            Level = request.Level,
            Checkpoint = request.Checkpoint,
            PlayTimeSeconds = request.PlayTimeSeconds,
            CustomName = request.CustomName
        };

        return await _autoSaveService.TriggerAutoSaveAsync(triggerRequest, cancellationToken);
    }
}
