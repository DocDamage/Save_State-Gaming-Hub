using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;

namespace SaveState.Application.Mugen.Queries.Handlers;

public class GetMugenTournamentsQueryHandler : IRequestHandler<GetMugenTournamentsQuery, Result<IReadOnlyList<MugenTournament>>>
{
    private readonly IMugenTournamentRepository _repository;

    public GetMugenTournamentsQueryHandler(IMugenTournamentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<MugenTournament>>> Handle(GetMugenTournamentsQuery request, CancellationToken ct)
    {
        try
        {
            var tournaments = await _repository.GetAllAsync(ct);
            return Result.Success<IReadOnlyList<MugenTournament>>(tournaments);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenTournament>>($"Failed to get tournaments: {ex.Message}");
        }
    }
}
