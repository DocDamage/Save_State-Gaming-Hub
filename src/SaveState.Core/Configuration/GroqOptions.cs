namespace SaveState.Core.Configuration;

public class GroqOptions
{
    public const string Section = "Groq";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "mixtral-8x7b-32768";
}
