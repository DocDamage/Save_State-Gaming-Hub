namespace SaveState.Application.Mugen.Services.ContentMarketplace.Engines;

using Microsoft.Extensions.Logging;

public class RevenueManager
{
    private readonly ILogger<RevenueManager> _logger;

    public RevenueManager(ILogger<RevenueManager> logger) => _logger = logger;
}
