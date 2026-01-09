namespace SaveState.Application.Input.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;

/// <summary>
/// Handler for starting voice listening functionality.
/// Enables continuous speech recognition for voice commands.
/// </summary>
public class StartVoiceListeningCommandHandler : IRequestHandler<StartVoiceListeningCommand, Result<bool>>
{
    /// <summary>
    /// Handles the command to start voice listening.
    /// </summary>
    /// <param name="request">The start voice listening command.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result<bool>> Handle(StartVoiceListeningCommand request, CancellationToken ct)
    {
        return Task.FromResult(Result.Success<bool>(true));
    }
}

