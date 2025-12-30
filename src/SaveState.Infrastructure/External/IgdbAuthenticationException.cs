namespace SaveState.Infrastructure.External;

/// <summary>
/// Exception thrown when IGDB authentication with Twitch fails.
/// </summary>
public class IgdbAuthenticationException : Exception
{
    public IgdbAuthenticationException(string message) : base(message)
    {
    }

    public IgdbAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
