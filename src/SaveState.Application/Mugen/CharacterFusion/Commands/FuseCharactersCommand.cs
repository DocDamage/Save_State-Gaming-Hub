using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Commands;

/// <summary>
/// Command to fuse two characters together.
/// </summary>
public sealed record FuseCharactersCommand(
    Guid Parent1Id,
    Guid Parent2Id,
    string? CustomName = null,
    FusionType FusionType = FusionType.Potara,
    FusionCustomizationOptions? Customization = null) : IRequest<Result<FusedCharacter>>;

/// <summary>
/// Handler for FuseCharactersCommand.
/// </summary>
public sealed class FuseCharactersCommandHandler : IRequestHandler<FuseCharactersCommand, Result<FusedCharacter>>
{
    private readonly ICharacterFusionService _fusionService;

    public FuseCharactersCommandHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result<FusedCharacter>> Handle(FuseCharactersCommand request, CancellationToken cancellationToken)
    {
        var fusionRequest = new FusionRequest
        {
            Parent1Id = request.Parent1Id,
            Parent2Id = request.Parent2Id,
            CustomName = request.CustomName,
            FusionType = request.FusionType,
            Customization = request.Customization
        };

        return await _fusionService.FuseCharactersAsync(fusionRequest, cancellationToken);
    }
}
