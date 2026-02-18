using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using TournamentModel = SaveState.Core.Mugen.TournamentEvents.TournamentEvent;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to generate the tournament bracket.
/// </summary>
public sealed record GenerateBracketCommand(
    Guid TournamentId,
    SeedingMethod SeedingMethod = SeedingMethod.Random) : IRequest<Result<TournamentModel>>;

/// <summary>
/// Handler for GenerateBracketCommand.
/// </summary>
public sealed class GenerateBracketCommandHandler : IRequestHandler<GenerateBracketCommand, Result<TournamentModel>>
{
    private readonly ITournamentEventService _tournamentService;

    public GenerateBracketCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentModel>> Handle(GenerateBracketCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GenerateBracketAsync(
            request.TournamentId,
            request.SeedingMethod,
            cancellationToken);
    }
}







