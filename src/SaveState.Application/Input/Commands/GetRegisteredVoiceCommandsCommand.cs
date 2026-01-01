namespace SaveState.Application.Input.Commands;

using MediatR;
using SaveState.Core.Common;

/// <summary>
/// Command to get all registered voice commands.
/// </summary>
public record GetRegisteredVoiceCommandsCommand() : IRequest<Result<IReadOnlyList<VoiceCommandInfo>>>;

/// <summary>
/// Information about a registered voice command.
/// </summary>
public record VoiceCommandInfo(
    string CommandPhrase,
    string Description,
    string Action);