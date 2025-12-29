namespace SaveState.Core.Configuration;

public class AiOptions
{
    public const string Section = "Ai";

    public string DefaultModel { get; set; } = "gpt-4";
    public int CacheTtlMinutes { get; set; } = 30;
    public bool EnableFallback { get; set; } = true;
    public int MaxConcurrentRequests { get; set; } = 10;
}
