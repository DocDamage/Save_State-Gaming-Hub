namespace SaveState.Application.Input.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Application.Input.Commands;

/// <summary>
/// Handler for getting registered voice commands.
/// </summary>
public class GetRegisteredVoiceCommandsCommandHandler : IRequestHandler<GetRegisteredVoiceCommandsCommand, Result<IReadOnlyList<VoiceCommandInfo>>>
{
    public Task<Result<IReadOnlyList<VoiceCommandInfo>>> Handle(GetRegisteredVoiceCommandsCommand request, CancellationToken ct)
    {
        // Return some stub voice commands for testing
        var commands = new List<VoiceCommandInfo>
        {
            new VoiceCommandInfo("launch game", "Launch a game by name", "LaunchGame"),
            new VoiceCommandInfo("save game", "Save current game", "SaveGame"),
            new VoiceCommandInfo("exit game", "Exit current game", "ExitGame")
        };

        return Task.FromResult(Result.Success<IReadOnlyList<VoiceCommandInfo>>(commands));
    }
}
