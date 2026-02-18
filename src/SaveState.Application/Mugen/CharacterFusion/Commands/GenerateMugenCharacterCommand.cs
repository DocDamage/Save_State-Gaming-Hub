using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Commands;

/// <summary>
/// Command to generate MUGEN character files for a fusion.
/// </summary>
public sealed record GenerateMugenCharacterCommand(
    Guid FusionId,
    string OutputDirectory) : IRequest<Result<string>>;

/// <summary>
/// Handler for GenerateMugenCharacterCommand.
/// </summary>
public sealed class GenerateMugenCharacterCommandHandler : IRequestHandler<GenerateMugenCharacterCommand, Result<string>>
{
    private readonly ICharacterFusionService _fusionService;

    public GenerateMugenCharacterCommandHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result<string>> Handle(GenerateMugenCharacterCommand request, CancellationToken cancellationToken)
    {
        return await _fusionService.GenerateMugenCharacterAsync(
            request.FusionId, request.OutputDirectory, cancellationToken);
    }
}
