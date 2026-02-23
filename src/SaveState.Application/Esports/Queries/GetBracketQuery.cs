using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Queries;

/// <summary>
/// Query to get a tournament bracket.
/// </summary>
public record GetBracketQuery(Guid TournamentId) : IRequest<Result<Bracket>>;

/// <summary>
/// Handler for getting a bracket.
/// </summary>
public sealed class GetBracketQueryHandler : IRequestHandler<GetBracketQuery, Result<Bracket>>
{
    private readonly ITournamentService _tournamentService;

    public GetBracketQueryHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Bracket>> Handle(GetBracketQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetBracketAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
