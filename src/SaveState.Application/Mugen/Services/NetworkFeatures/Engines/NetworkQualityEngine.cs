namespace SaveState.Application.Mugen.Services.NetworkFeatures.Engines;
using Microsoft.Extensions.Logging;
public class NetworkQualityEngine
{
    private readonly ILogger<NetworkQualityEngine> _logger;
    public NetworkQualityEngine(ILogger<NetworkQualityEngine> logger) => _logger = logger;
}
