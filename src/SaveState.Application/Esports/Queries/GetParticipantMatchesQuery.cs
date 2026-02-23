using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Queries;

/// <summary>
/// Query to get matches for a specific participant.
/// </summary>
public record GetParticipantMatchesQuery(
    Guid TournamentId,
    Guid ParticipantId
) : IRequest<Result<IReadOnlyList<Match>>>;

/// <summary>
/// Handler for getting participant matches.
/// </summary>
public sealed class GetParticipantMatchesQueryHandler : IRequestHandler<GetParticipantMatchesQuery, Result<IReadOnlyList<Match>>>
{
    private readonly ITournamentService _tournamentService;

    public GetParticipantMatchesQueryHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<IReadOnlyList<Match>>> Handle(GetParticipantMatchesQuery request, CancellationToken cancellationToken)
    {
        var tournamentResult = await _tournamentService.GetTournamentAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
        
        if (tournamentResult.IsFailure)
        {
            return Result<IReadOnlyList<Match>>.Failure(tournamentResult.Error!, tournamentResult.ErrorType);
        }

        var tournament = tournamentResult.Value;
        var participantMatches = tournament.Matches
            .Where(m => m.Player1?.Id == request.ParticipantId || m.Player2?.Id == request.ParticipantId)
            .ToList();

        return Result<IReadOnlyList<Match>>.Success(participantMatches);
    }
}
