namespace SaveState.Application.Mugen.Commands;

using MediatR;
using SaveState.Application.RomManagement.Services;

/// <summary>
/// Command to launch IKEMEN in versus mode with two characters.
/// </summary>
public record LaunchIkemenVersusCommand(
    string Player1Character,
    string Player2Character,
    int Rounds = 3
) : IRequest<ProcessInfo>;
