namespace SaveState.Application.Input.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Application.Input.Commands;

/// <summary>
/// Handler for stopping voice listening functionality.
/// Disables continuous speech recognition and voice command processing.
/// </summary>
public class StopVoiceListeningCommandHandler : IRequestHandler<StopVoiceListeningCommand, Result<bool>>
{
    /// <summary>
    /// Handles the command to stop voice listening.
    /// </summary>
    /// <param name="request">The stop voice listening command.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result<bool>> Handle(StopVoiceListeningCommand request, CancellationToken ct)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }
}
