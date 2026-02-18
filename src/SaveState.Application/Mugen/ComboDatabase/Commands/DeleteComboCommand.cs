using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to delete a combo.
/// </summary>
public sealed record DeleteComboCommand(Guid ComboId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteComboCommand.
/// </summary>
public sealed class DeleteComboCommandHandler : IRequestHandler<DeleteComboCommand, Result>
{
    private readonly IComboDatabaseService _comboService;

    public DeleteComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result> Handle(DeleteComboCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.DeleteComboAsync(request.ComboId, cancellationToken);
    }
}
