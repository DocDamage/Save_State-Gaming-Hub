using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Commands;

/// <summary>
/// Command to delete an auto-save.
/// </summary>
public sealed record DeleteAutoSaveCommand(Guid AutoSaveId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteAutoSaveCommand.
/// </summary>
public sealed class DeleteAutoSaveCommandHandler : IRequestHandler<DeleteAutoSaveCommand, Result>
{
    private readonly IAutoSaveService _autoSaveService;

    public DeleteAutoSaveCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result> Handle(DeleteAutoSaveCommand request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.DeleteAutoSaveAsync(request.AutoSaveId, cancellationToken);
    }
}

/// <summary>
/// Command to clean up old auto-saves.
/// </summary>
public sealed record CleanupOldAutoSavesCommand(Guid GameId) : IRequest<Result<int>>;

/// <summary>
/// Handler for CleanupOldAutoSavesCommand.
/// </summary>
public sealed class CleanupOldAutoSavesCommandHandler : IRequestHandler<CleanupOldAutoSavesCommand, Result<int>>
{
    private readonly IAutoSaveService _autoSaveService;

    public CleanupOldAutoSavesCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result<int>> Handle(CleanupOldAutoSavesCommand request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.CleanupOldSavesAsync(request.GameId, cancellationToken);
    }
}
