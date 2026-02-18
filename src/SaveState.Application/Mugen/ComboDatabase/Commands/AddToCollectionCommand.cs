using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to add a combo to a collection.
/// </summary>
public sealed record AddToCollectionCommand(
    Guid CollectionId,
    Guid ComboId) : IRequest<Result>;

/// <summary>
/// Handler for AddToCollectionCommand.
/// </summary>
public sealed class AddToCollectionCommandHandler : IRequestHandler<AddToCollectionCommand, Result>
{
    private readonly IComboDatabaseService _comboService;

    public AddToCollectionCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result> Handle(AddToCollectionCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.AddToCollectionAsync(
            request.CollectionId,
            request.ComboId,
            cancellationToken);
    }
}
