namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;

/// <summary>
/// Handler for creating MUGEN tournaments.
/// </summary>
public class CreateMugenTournamentCommandHandler : IRequestHandler<CreateMugenTournamentCommand, Result<MugenTournament>>
{
    private readonly IMugenTournamentService _tournamentService;

    public CreateMugenTournamentCommandHandler(IMugenTournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<MugenTournament>> Handle(CreateMugenTournamentCommand request, CancellationToken ct)
    {
        // Parse tournament format
        if (!Enum.TryParse<TournamentFormat>(request.Format, true, out var format))
        {
            return Result<MugenTournament>.Failure($"Invalid tournament format: {request.Format}");
        }

        var createRequest = new CreateTournamentRequest(
            request.Name,
            format,
            request.ParticipantIds);

        return await _tournamentService.CreateTournamentAsync(createRequest, ct);
    }
}