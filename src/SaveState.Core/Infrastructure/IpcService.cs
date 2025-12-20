using Grpc.Core;
using SaveState.Core.Ipc;
using Serilog;

namespace SaveState.Core.Infrastructure;

public class IpcService : SaveStateIpc.SaveStateIpcBase
{
    private readonly ILogger _logger;

    public IpcService()
    {
        _logger = Log.ForContext<IpcService>();
    }

    public override Task<CommandResponse> SendCommand(CommandRequest request, ServerCallContext context)
    {
        _logger.Information("Received IPC command: {Command} with args: {Args}", 
            request.Command, string.Join(", ", request.Args));

        // Logic to handle commands (e.g., focus window, launch game)
        // This will likely trigger an event that the UI or App listens to.

        return Task.FromResult(new CommandResponse
        {
            Success = true,
            Message = $"Command '{request.Command}' received."
        });
    }
}
