using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Commands;

/// <summary>
/// Command to delete a fused character.
/// </summary>
public sealed record DeleteFusionCommand(Guid FusionId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteFusionCommand.
/// </summary>
public sealed class DeleteFusionCommandHandler : IRequestHandler<DeleteFusionCommand, Result>
{
    private readonly ICharacterFusionService _fusionService;

    public DeleteFusionCommandHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result> Handle(DeleteFusionCommand request, CancellationToken cancellationToken)
    {
        return await _fusionService.DeleteFusionAsync(request.FusionId, cancellationToken);
    }
}
