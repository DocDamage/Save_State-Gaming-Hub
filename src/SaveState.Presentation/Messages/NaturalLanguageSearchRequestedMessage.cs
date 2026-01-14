namespace SaveState.Presentation.Messages;

/// <summary>
/// Message sent when a natural language search is requested.
/// </summary>
public sealed class NaturalLanguageSearchRequestedMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalLanguageSearchRequestedMessage"/> class.
    /// </summary>
    /// <param name="value">The natural language query string.</param>
    public NaturalLanguageSearchRequestedMessage(string value)
    {
        Value = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the natural language query string.
    /// </summary>
    public string Value { get; }
}
