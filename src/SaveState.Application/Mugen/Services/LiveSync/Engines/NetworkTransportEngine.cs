namespace SaveState.Application.Mugen.Services.LiveSync.Engines;
using Microsoft.Extensions.Logging;
public class NetworkTransportEngine
{
    private readonly ILogger<NetworkTransportEngine> _logger;
    public NetworkTransportEngine(ILogger<NetworkTransportEngine> logger) => _logger = logger;
}
