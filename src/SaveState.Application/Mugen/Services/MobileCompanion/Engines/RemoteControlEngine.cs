namespace SaveState.Application.Mugen.Services.MobileCompanion.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

/// <summary>
/// Engine for executing remote control commands.
/// </summary>
public class RemoteControlEngine
{
    private readonly ILogger<RemoteControlEngine> _logger;

    public RemoteControlEngine(ILogger<RemoteControlEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a remote command.
    /// </summary>
    public Task<Result> ExecuteCommandAsync(
        MobileCompanionServiceMobileSession session,
        MobileCompanionServiceRemoteCommand command,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Executing command {CommandType} for session {SessionId}",
            command.MobileCompanionServiceCommandType, session.SessionId);

        // Simulate command execution
        var success = true;
        var error = success ? null : "Command execution failed";

        return Task.FromResult(success ? Result.Success() : Result.Failure(error!));
    }
}
