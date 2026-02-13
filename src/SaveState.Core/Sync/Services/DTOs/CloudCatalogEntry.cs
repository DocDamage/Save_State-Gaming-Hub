namespace SaveState.Core.Sync.Services.DTOs;

/// <summary>
/// Represents an entry in the cloud gaming catalog.
/// </summary>
public sealed record CloudCatalogEntry(
    string Title,
    int SteamAppId,
    IReadOnlyList<CloudGamingProvider> Providers);

/// <summary>
/// Represents the cloud gaming catalog with version and metadata.
/// </summary>
public sealed record CloudCatalog(
    string Version,
    DateTimeOffset LastUpdated,
    IReadOnlyList<CloudCatalogEntry> Games);
