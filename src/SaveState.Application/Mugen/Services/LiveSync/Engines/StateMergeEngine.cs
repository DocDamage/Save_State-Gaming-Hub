namespace SaveState.Application.Mugen.Services.LiveSync.Engines;
using Microsoft.Extensions.Logging;
public class StateMergeEngine
{
    private readonly ILogger<StateMergeEngine> _logger;
    public StateMergeEngine(ILogger<StateMergeEngine> logger) => _logger = logger;
}
