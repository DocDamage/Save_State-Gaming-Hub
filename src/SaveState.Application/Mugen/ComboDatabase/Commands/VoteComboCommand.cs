using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to upvote a combo.
/// </summary>
public sealed record UpvoteComboCommand(Guid ComboId) : IRequest<Result>;

/// <summary>
/// Handler for UpvoteComboCommand.
/// </summary>
public sealed class UpvoteComboCommandHandler : IRequestHandler<UpvoteComboCommand, Result>
{
    private readonly IComboDatabaseService _comboService;

    public UpvoteComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result> Handle(UpvoteComboCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.UpvoteComboAsync(request.ComboId, cancellationToken);
    }
}

/// <summary>
/// Command to downvote a combo.
/// </summary>
public sealed record DownvoteComboCommand(Guid ComboId) : IRequest<Result>;

/// <summary>
/// Handler for DownvoteComboCommand.
/// </summary>
public sealed class DownvoteComboCommandHandler : IRequestHandler<DownvoteComboCommand, Result>
{
    private readonly IComboDatabaseService _comboService;

    public DownvoteComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result> Handle(DownvoteComboCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.DownvoteComboAsync(request.ComboId, cancellationToken);
    }
}
