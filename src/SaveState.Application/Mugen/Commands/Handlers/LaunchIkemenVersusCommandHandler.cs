namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.Mugen.Services;

/// <summary>
/// Handles the LaunchIkemenVersusCommand.
/// </summary>
public class LaunchIkemenVersusCommandHandler : IRequestHandler<LaunchIkemenVersusCommand, ProcessInfo>
{
    private readonly IMugenLauncher _launcher;

    /// <summary>
    /// Initializes a new instance of the LaunchIkemenVersusCommandHandler.
    /// </summary>
    /// <param name="launcher">The MUGEN launcher.</param>
    public LaunchIkemenVersusCommandHandler(IMugenLauncher launcher)
    {
        _launcher = launcher;
    }

    /// <summary>
    /// Handles the launch IKEMEN versus command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process information for the launched game.</returns>
    public async Task<ProcessInfo> Handle(LaunchIkemenVersusCommand request, CancellationToken cancellationToken)
    {
        var process = await _launcher.LaunchVersusAsync(request.Player1Character, request.Player2Character, request.Rounds);

        return new ProcessInfo(
            Id: process.Id,
            ProcessName: process.ProcessName,
            StartTime: process.StartTime,
            MemoryUsage: process.WorkingSet64
        );
    }
}
