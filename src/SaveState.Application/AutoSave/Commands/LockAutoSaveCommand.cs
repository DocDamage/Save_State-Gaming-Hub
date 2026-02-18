using MediatR;
using SaveState.Core.Common;
using SaveState.Core.AutoSave.Services;

namespace SaveState.Application.AutoSave.Commands;

/// <summary>
/// Command to lock an auto-save.
/// </summary>
public sealed record LockAutoSaveCommand(Guid AutoSaveId) : IRequest<Result>;

/// <summary>
/// Handler for LockAutoSaveCommand.
/// </summary>
public sealed class LockAutoSaveCommandHandler : IRequestHandler<LockAutoSaveCommand, Result>
{
    private readonly IAutoSaveService _autoSaveService;

    public LockAutoSaveCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result> Handle(LockAutoSaveCommand request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.LockAutoSaveAsync(request.AutoSaveId, cancellationToken);
    }
}

/// <summary>
/// Command to unlock an auto-save.
/// </summary>
public sealed record UnlockAutoSaveCommand(Guid AutoSaveId) : IRequest<Result>;

/// <summary>
/// Handler for UnlockAutoSaveCommand.
/// </summary>
public sealed class UnlockAutoSaveCommandHandler : IRequestHandler<UnlockAutoSaveCommand, Result>
{
    private readonly IAutoSaveService _autoSaveService;

    public UnlockAutoSaveCommandHandler(IAutoSaveService autoSaveService)
    {
        _autoSaveService = autoSaveService;
    }

    public async Task<Result> Handle(UnlockAutoSaveCommand request, CancellationToken cancellationToken)
    {
        return await _autoSaveService.UnlockAutoSaveAsync(request.AutoSaveId, cancellationToken);
    }
}
