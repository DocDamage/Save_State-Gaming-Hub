namespace SaveState.Application.Mugen.Services.WebPortal.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for API functionality in the web portal.
/// </summary>
public class ApiEngine
{
    private readonly ILogger<ApiEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ApiEngine(ILogger<ApiEngine> logger)
    {
        _logger = logger;
    }
}
