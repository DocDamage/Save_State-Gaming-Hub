using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to generate a tournament bracket.
/// </summary>
public record GenerateBracketCommand(
    Guid TournamentId,
    bool RandomizeSeeds = false,
    IReadOnlyList<string>? SeededPlayers = null
) : IRequest<Result<Bracket>>;

/// <summary>
/// Handler for generating a bracket.
/// </summary>
public sealed class GenerateBracketCommandHandler : IRequestHandler<GenerateBracketCommand, Result<Bracket>>
{
    private readonly ITournamentService _tournamentService;

    public GenerateBracketCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Bracket>> Handle(GenerateBracketCommand request, CancellationToken cancellationToken)
    {
        var options = new BracketOptions(
            request.RandomizeSeeds,
            request.SeededPlayers
        );

        return await _tournamentService.GenerateBracketAsync(request.TournamentId, options, cancellationToken).ConfigureAwait(false);
    }
}
