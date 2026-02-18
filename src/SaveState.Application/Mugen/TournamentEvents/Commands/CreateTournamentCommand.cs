using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using TournamentModel = SaveState.Core.Mugen.TournamentEvents.TournamentEvent;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to create a new tournament.
/// </summary>
public sealed record CreateTournamentCommand(
    string Name,
    string? Description,
    TournamentFormat Format,
    int MaxParticipants,
    DateTime? ScheduledStart,
    string Organizer,
    TournamentRules Rules,
    TournamentSettings Settings,
    bool IsPublic = true,
    List<string>? Tags = null) : IRequest<Result<TournamentModel>>;

/// <summary>
/// Handler for CreateTournamentCommand.
/// </summary>
public sealed class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Result<TournamentModel>>
{
    private readonly ITournamentEventService _tournamentService;

    public CreateTournamentCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentModel>> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var createRequest = new CreateTournamentRequest
        {
            Name = request.Name,
            Description = request.Description,
            Format = request.Format,
            MaxParticipants = request.MaxParticipants,
            ScheduledStart = request.ScheduledStart,
            Organizer = request.Organizer,
            Rules = request.Rules,
            Settings = request.Settings,
            IsPublic = request.IsPublic,
            Tags = request.Tags ?? new List<string>()
        };

        return await _tournamentService.CreateTournamentAsync(createRequest, cancellationToken);
    }
}







