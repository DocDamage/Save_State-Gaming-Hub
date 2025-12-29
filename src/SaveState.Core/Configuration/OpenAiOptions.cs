namespace SaveState.Core.Configuration;

public class OpenAiOptions
{
    public const string Section = "OpenAi";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "gpt-4";
}
