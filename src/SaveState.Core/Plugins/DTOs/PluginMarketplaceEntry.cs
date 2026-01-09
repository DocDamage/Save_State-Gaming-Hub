namespace SaveState.Core.Plugins.DTOs;

/// <summary>
/// Represents a plugin available in the marketplace.
/// </summary>
public record PluginMarketplaceEntry(
    string Id,
    string Name,
    string Description,
    string Author,
    string Version,
    string Category,
    string IconUrl,
    string DownloadUrl,
    long DownloadCount,
    double AverageRating,
    int ReviewCount,
    DateTime PublishedAt,
    DateTime UpdatedAt,
    List<string> Tags,
    List<string> Screenshots,
    string? HomepageUrl,
    string? SourceCodeUrl,
    bool IsVerified,
    bool IsCompatible,
    string MinimumAppVersion,
    long SizeInBytes);

/// <summary>
/// Categories for organizing plugins in the marketplace.
/// </summary>
public static class PluginCategories
{
    public const string GameDetection = "Game Detection";
    public const string Themes = "Themes & UI";
    public const string Integration = "External Integration";
    public const string Analytics = "Analytics & Stats";
    public const string Social = "Social & Streaming";
    public const string Emulation = "Emulation";
    public const string Utilities = "Utilities";
    public const string All = "All";
}
