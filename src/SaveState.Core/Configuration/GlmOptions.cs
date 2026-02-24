using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

/// <summary>
/// Configuration options for GLM AI service.
/// </summary>
public class GlmOptions : IValidatableObject
{
    public const string SectionName = "GLM";

    /// <summary>
    /// Base URL for the GLM API.
    /// </summary>
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4/";

    /// <summary>
    /// API key for authentication. Required for actual usage.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default model to use for requests.
    /// </summary>
    public string DefaultModel { get; set; } = "glm-4";

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
