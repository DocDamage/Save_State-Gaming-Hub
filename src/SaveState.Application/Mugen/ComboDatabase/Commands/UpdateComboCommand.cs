using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Commands;

/// <summary>
/// Command to update an existing combo.
/// </summary>
public sealed record UpdateComboCommand(
    Guid ComboId,
    string? Name = null,
    string? Description = null,
    ComboDifficulty? Difficulty = null,
    int? HitCount = null,
    int? Damage = null,
    List<ComboMoveEntry>? Moves = null,
    string? InputNotation = null,
    string? VideoUrl = null,
    List<string>? Tags = null,
    bool? IsVerified = null,
    bool? IsOptimal = null) : IRequest<Result<ComboEntryModel>>;

/// <summary>
/// Handler for UpdateComboCommand.
/// </summary>
public sealed class UpdateComboCommandHandler : IRequestHandler<UpdateComboCommand, Result<ComboEntryModel>>
{
    private readonly IComboDatabaseService _comboService;

    public UpdateComboCommandHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboEntryModel>> Handle(UpdateComboCommand request, CancellationToken cancellationToken)
    {
        var updateRequest = new UpdateComboRequest
        {
            Name = request.Name,
            Description = request.Description,
            Difficulty = request.Difficulty,
            HitCount = request.HitCount,
            Damage = request.Damage,
            Moves = request.Moves,
            InputNotation = request.InputNotation,
            VideoUrl = request.VideoUrl,
            Tags = request.Tags,
            IsVerified = request.IsVerified,
            IsOptimal = request.IsOptimal
        };

        return await _comboService.UpdateComboAsync(request.ComboId, updateRequest, cancellationToken);
    }
}
