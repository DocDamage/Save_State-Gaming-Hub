using Grpc.Net.Client;
using SaveState.Core.Ipc;
using Serilog;

namespace SaveState.Core.Infrastructure;

public class SingleInstanceLock : IDisposable
{
    private readonly string _mutexName;
    private Mutex? _mutex;
    private readonly ILogger _logger;

    public SingleInstanceLock(string appName)
    {
        _mutexName = $"Global\\{appName}_SingleInstanceMutex";
        _logger = Log.ForContext<SingleInstanceLock>();
    }

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, _mutexName, out bool createdNew);
        if (!createdNew)
        {
            _logger.Information("Another instance is already running.");
            _mutex.Dispose();
            _mutex = null;
            return false;
        }
        return true;
    }

    public async Task SendCommandToInstance(string command, string[] args)
    {
        try
        {
            // Use Named Pipes for gRPC on Windows
            using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    ConnectCallback = async (context, token) =>
                    {
                        var pipeName = "SaveStateIpcPipe"; // This should match server config
                        var pipe = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous);
                        await pipe.ConnectAsync(token);
                        return pipe;
                    }
                }
            });

            var client = new SaveStateIpc.SaveStateIpcClient(channel);
            var request = new CommandRequest { Command = command };
            request.Args.AddRange(args);

            _logger.Information("Sending command to existing instance...");
            var response = await client.SendCommandAsync(request);
            _logger.Information("Instance responded: {Message}", response.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send command to existing instance.");
        }
    }

    public void Dispose()
    {
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
