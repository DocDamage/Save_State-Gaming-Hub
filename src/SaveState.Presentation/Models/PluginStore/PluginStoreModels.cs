using SaveState.Core.Common.Services;

namespace SaveState.Presentation.Models.PluginStore;

/// <summary>
/// Represents a plugin listing in the plugin store.
/// </summary>
public record PluginListing
{
    /// <summary>Unique identifier for the plugin.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the plugin.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the plugin.</summary>
    public string? Description { get; set; }

    /// <summary>Author name.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>URL to the author's avatar.</summary>
    public string? AuthorAvatar { get; set; }

    /// <summary>Current version.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>URL to the plugin icon.</summary>
    public string? Icon { get; set; }

    /// <summary>List of screenshot URLs.</summary>
    public List<string> Screenshots { get; set; } = new();

    /// <summary>Categories the plugin belongs to.</summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>Tags for search and filtering.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Number of downloads.</summary>
    public int DownloadCount { get; set; }

    /// <summary>Average rating (0-5).</summary>
    public float Rating { get; set; }

    /// <summary>Number of reviews.</summary>
    public int ReviewCount { get; set; }

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Publication date.</summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>Last update date.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Pricing information.</summary>
    public PluginPricing Pricing { get; set; } = new();

    /// <summary>List of plugin dependencies.</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>Minimum app version required.</summary>
    public string? MinimumAppVersion { get; set; }

    /// <summary>Whether the plugin is currently installed.</summary>
    public bool IsInstalled { get; set; }

    /// <summary>Whether an update is available.</summary>
    public bool IsUpdateAvailable { get; set; }

    /// <summary>Currently installed version.</summary>
    public string? InstalledVersion { get; set; }

    /// <summary>Whether the plugin is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the formatted download count.
    /// </summary>
    public string FormattedDownloads => DownloadCount switch
    {
        >= 1_000_000 => $"{DownloadCount / 1_000_000.0:F1}M",
        >= 1_000 => $"{DownloadCount / 1_000.0:F1}K",
        _ => DownloadCount.ToString()
    };

    /// <summary>
    /// Gets the formatted file size.
    /// </summary>
    public string FormattedFileSize => FileSize switch
    {
        >= 1_073_741_824 => $"{FileSize / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{FileSize / 1_048_576.0:F1} MB",
        >= 1_024 => $"{FileSize / 1_024.0:F1} KB",
        _ => $"{FileSize} B"
    };

    /// <summary>
    /// Gets the rating as star display string.
    /// </summary>
    public string StarRating => new string('★', (int)Rating) + new string('☆', 5 - (int)Rating);
}

/// <summary>
/// Represents the pricing model for a plugin.
/// </summary>
public record PluginPricing
{
    /// <summary>Type of pricing.</summary>
    public PricingType Type { get; set; } = PricingType.Free;

    /// <summary>Regular price.</summary>
    public decimal? Price { get; set; }

    /// <summary>Sale price (if on sale).</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>When the sale ends.</summary>
    public DateTime? SaleEndsAt { get; set; }

    /// <summary>Currency code (e.g., USD).</summary>
    public string? Currency { get; set; } = "USD";

    /// <summary>
    /// Gets the current display price.
    /// </summary>
    public string DisplayPrice => Type switch
    {
        PricingType.Free => "Free",
        PricingType.Paid when SalePrice.HasValue && SaleEndsAt > SystemTimeProvider.Instance.Now => $"${SalePrice.Value:F2}",
        PricingType.Paid when Price.HasValue => $"${Price.Value:F2}",
        PricingType.Subscription => $"${Price:F2}/mo",
        _ => "Free"
    };

    /// <summary>
    /// Gets whether the plugin is currently on sale.
    /// </summary>
    public bool IsOnSale => Type == PricingType.Paid && SalePrice.HasValue && SaleEndsAt > SystemTimeProvider.Instance.Now;
}

/// <summary>
/// Pricing types for plugins.
/// </summary>
public enum PricingType
{
    /// <summary>Free plugin.</summary>
    Free,

    /// <summary>One-time purchase.</summary>
    Paid,

    /// <summary>Subscription-based.</summary>
    Subscription
}

/// <summary>
/// Represents a plugin review.
/// </summary>
public record PluginReview
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Plugin being reviewed.</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Reviewer name.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Reviewer avatar URL.</summary>
    public string? AuthorAvatar { get; set; }

    /// <summary>Rating (1-5).</summary>
    public int Rating { get; set; }

    /// <summary>Review title.</summary>
    public string? Title { get; set; }

    /// <summary>Review content.</summary>
    public string? Content { get; set; }

    /// <summary>Review creation date.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Number of helpful votes.</summary>
    public int HelpfulCount { get; set; }

    /// <summary>Whether this is a developer response.</summary>
    public bool IsDeveloperResponse { get; set; }

    /// <summary>Developer response content.</summary>
    public string? DeveloperResponse { get; set; }

    /// <summary>
    /// Gets the rating as star display string.
    /// </summary>
    public string StarRating => new string('★', Rating) + new string('☆', 5 - Rating);
}

/// <summary>
/// Represents a plugin category.
/// </summary>
public record PluginCategory
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Icon emoji or character.</summary>
    public string? Icon { get; set; }

    /// <summary>Category description.</summary>
    public string? Description { get; set; }

    /// <summary>Number of plugins in this category.</summary>
    public int PluginCount { get; set; }
}

/// <summary>
/// Represents the installation status of a plugin.
/// </summary>
public enum PluginInstallationStatus
{
    /// <summary>Not installed.</summary>
    NotInstalled,

    /// <summary>Download in progress.</summary>
    Downloading,

    /// <summary>Verifying downloaded files.</summary>
    Verifying,

    /// <summary>Installing.</summary>
    Installing,

    /// <summary>Activating plugin.</summary>
    Activating,

    /// <summary>Successfully installed.</summary>
    Completed,

    /// <summary>Installation failed.</summary>
    Failed
}

/// <summary>
/// Represents installation progress for a plugin.
/// </summary>
public record PluginInstallationProgress
{
    /// <summary>Current installation status.</summary>
    public PluginInstallationStatus Status { get; set; }

    /// <summary>Progress percentage (0-100).</summary>
    public double ProgressPercent { get; set; }

    /// <summary>Current step description.</summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>Bytes downloaded so far.</summary>
    public long BytesDownloaded { get; set; }

    /// <summary>Total bytes to download.</summary>
    public long TotalBytes { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the changelog for a plugin version.
/// </summary>
public record PluginChangelogEntry
{
    /// <summary>Version number.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Release date.</summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>List of changes.</summary>
    public List<string> Changes { get; set; } = new();

    /// <summary>Whether this is a major release.</summary>
    public bool IsMajorRelease { get; set; }
}
