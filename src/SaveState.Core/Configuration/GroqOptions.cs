using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for Groq AI service.
/// </summary>
public class GroqOptions : IValidatableObject
{
    public const string Section = "Groq";

    /// <summary>
    /// Base URL for the Groq API.
    /// </summary>
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";

    /// <summary>
    /// API key for authentication. Required for actual usage.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default model to use for requests.
    /// </summary>
    public string DefaultModel { get; set; } = "mixtral-8x7b-32768";

    /// <summary>
    /// Validates the configuration. API key may be supplied later from user preferences.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate BaseUrl format if provided.
        if (!string.IsNullOrWhiteSpace(BaseUrl) && !Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute))
        {
            results.Add(new ValidationResult("BaseUrl must be a valid URL", [nameof(BaseUrl)]));
        }

        return results;
    }
}
