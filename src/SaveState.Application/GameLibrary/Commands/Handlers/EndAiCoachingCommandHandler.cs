namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handler for ending AI coaching sessions.
/// Finalizes coaching data and generates completion reports.
/// </summary>
public class EndAiCoachingCommandHandler : IRequestHandler<EndAiCoachingCommand, Result>
{
    private readonly IAiCoachService _aiCoachService;

    public EndAiCoachingCommandHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    /// <summary>
    /// Handles the command to end an AI coaching session.
    /// </summary>
    /// <param name="request">The end coaching command with session ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(EndAiCoachingCommand request, CancellationToken ct)
    {
        return await _aiCoachService.EndCoachingSessionAsync(request.SessionId, ct);
    }
}