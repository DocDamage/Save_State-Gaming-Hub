namespace SaveState.Application.Mugen.Services.NetworkFeatures.Engines;
using Microsoft.Extensions.Logging;
public class RelayServerEngine
{
    private readonly ILogger<RelayServerEngine> _logger;
    public RelayServerEngine(ILogger<RelayServerEngine> logger) => _logger = logger;
}
