namespace SaveState.Application.Mugen.Services.WebPortal.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for authentication functionality in the web portal.
/// </summary>
public class AuthenticationEngine
{
    private readonly ILogger<AuthenticationEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AuthenticationEngine(ILogger<AuthenticationEngine> logger)
    {
        _logger = logger;
    }
}
