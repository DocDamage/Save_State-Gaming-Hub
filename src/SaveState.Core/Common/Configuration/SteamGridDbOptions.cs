namespace SaveState.Core.Common.Configuration;

public class SteamGridDbOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.steamgriddb.com/api/v2";
    public string[] PreferredImageTypes { get; set; } = ["grid", "hero", "logo"];
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentRequests { get; set; } = 3;
    public int CacheDurationHours { get; set; } = 24;
}