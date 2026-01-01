namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class EndAiCoachingCommandHandler : IRequestHandler<EndAiCoachingCommand, Result>
{
    private readonly IAiCoachService _aiCoachService;

    public EndAiCoachingCommandHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    public async Task<Result> Handle(EndAiCoachingCommand request, CancellationToken ct)
    {
        return await _aiCoachService.EndCoachingSessionAsync(request.SessionId, ct);
    }
}