namespace SaveState.Core.Configuration;

public class GeminiOptions
{
    public const string SectionName = "Gemini";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public string DefaultModel { get; set; } = "gemini-pro";
}
