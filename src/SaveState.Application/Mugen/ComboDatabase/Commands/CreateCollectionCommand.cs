using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to create a combo collection.
/// </summary>
public sealed record CreateCollectionCommand(
    string Name,
    string? Description,
    string? CharacterName,
    string Creator,
    bool IsPublic = true) : IRequest<Result<ComboCollection>>;

/// <summary>
/// Handler for CreateCollectionCommand.
/// </summary>
public sealed class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, Result<ComboCollection>>
{
    private readonly IComboDatabaseService _comboService;

    public CreateCollectionCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboCollection>> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        return await _comboService.CreateCollectionAsync(
            request.Name,
            request.Description,
            request.CharacterName,
            request.Creator,
            request.IsPublic,
            cancellationToken);
    }
}
