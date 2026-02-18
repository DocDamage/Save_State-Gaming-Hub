using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to add a new combo to the database.
/// </summary>
public sealed record AddComboCommand(
    string CharacterName,
    string Name,
    string? Description,
    ComboDifficulty Difficulty,
    int HitCount,
    int Damage,
    string StartingPosition,
    int MeterRequired,
    List<ComboMoveEntry> Moves,
    string InputNotation,
    string? VideoUrl,
    string? Creator,
    List<string> Tags,
    bool IsTouchOfDeath = false,
    string? GameVersion = null) : IRequest<Result<ComboEntryModel>>;

/// <summary>
/// Handler for AddComboCommand.
/// </summary>
public sealed class AddComboCommandHandler : IRequestHandler<AddComboCommand, Result<ComboEntryModel>>
{
    private readonly IComboDatabaseService _comboService;

    public AddComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboEntryModel>> Handle(AddComboCommand request, CancellationToken cancellationToken)
    {
        var addRequest = new AddComboRequest
        {
            CharacterName = request.CharacterName,
            Name = request.Name,
            Description = request.Description,
            Difficulty = request.Difficulty,
            HitCount = request.HitCount,
            Damage = request.Damage,
            StartingPosition = request.StartingPosition,
            MeterRequired = request.MeterRequired,
            Moves = request.Moves,
            InputNotation = request.InputNotation,
            VideoUrl = request.VideoUrl,
            Creator = request.Creator,
            Tags = request.Tags,
            IsTouchOfDeath = request.IsTouchOfDeath,
            GameVersion = request.GameVersion
        };

        return await _comboService.AddComboAsync(addRequest, cancellationToken);
    }
}
