namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Handler for getting MUGEN matchup advice.
/// </summary>
public class GetMugenMatchupAdviceCommandHandler : IRequestHandler<GetMugenMatchupAdviceCommand, Result<MatchupAdvice>>
{
    private readonly IMugenCoachService _coachService;

    public GetMugenMatchupAdviceCommandHandler(IMugenCoachService coachService)
    {
        _coachService = coachService;
    }

    public async Task<Result<MatchupAdvice>> Handle(GetMugenMatchupAdviceCommand request, CancellationToken ct)
    {
        return await _coachService.GetMatchupAdviceAsync(
            request.CharacterId,
            request.OpponentId,
            ct);
    }
}