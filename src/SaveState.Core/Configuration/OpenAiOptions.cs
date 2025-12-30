using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class OpenAiOptions
{
    public const string Section = "OpenAi";

    [Required(ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    [Required(ErrorMessage = "ApiKey is required")]
    [MinLength(1, ErrorMessage = "ApiKey cannot be empty")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "DefaultModel is required")]
    [MinLength(1, ErrorMessage = "DefaultModel cannot be empty")]
    public string DefaultModel { get; set; } = "gpt-4";
}
