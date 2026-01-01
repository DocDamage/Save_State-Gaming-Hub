using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.Plugins;

/// <summary>
/// Interface for plugins that can provide games from external sources.
/// </summary>
public interface IGameProvider
{
    /// <summary>
    /// Gets the name of this game provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Discovers games from this provider.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of discovered games.</returns>
    Task<Result<IReadOnlyList<Game>>> DiscoverGamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information about a specific game.
    /// </summary>
    /// <param name="externalId">The external ID of the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Detailed game information.</returns>
    Task<Result<Game>> GetGameDetailsAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Installs a game from this provider.
    /// </summary>
    /// <param name="externalId">The external ID of the game.</param>
    /// <param name="installPath">Where to install the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if installation was successful.</returns>
    Task<Result<bool>> InstallGameAsync(string externalId, string installPath, CancellationToken ct = default);
}

/// <summary>
/// Interface for plugins that can scrape metadata from external sources.
/// </summary>
public interface IMetadataScraper
{
    /// <summary>
    /// Gets the name of this metadata scraper.
    /// </summary>
    string ScraperName { get; }

    /// <summary>
    /// Searches for games by title.
    /// </summary>
    /// <param name="title">The game title to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching games.</returns>
    Task<Result<IReadOnlyList<MetadataSearchResult>>> SearchGamesAsync(string title, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed metadata for a game.
    /// </summary>
    /// <param name="externalId">The external ID of the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Detailed metadata.</returns>
    Task<Result<GameMetadata>> GetGameMetadataAsync(string externalId, CancellationToken ct = default);
}

/// <summary>
/// Result of a metadata search.
/// </summary>
public sealed record MetadataSearchResult(
    string ExternalId,
    string Title,
    string? Description,
    int? ReleaseYear,
    string? ImageUrl);

/// <summary>
/// Game metadata returned by scrapers.
/// </summary>
public sealed record GameMetadata(
    string Title,
    string? Description,
    string? Developer,
    string? Publisher,
    int? ReleaseYear,
    string? Genre,
    TimeSpan? TimeToBeatMain,
    TimeSpan? TimeToBeatPlus,
    TimeSpan? TimeToBeat100,
    string? CoverImageUrl,
    string? BackgroundImageUrl,
    IReadOnlyList<string> Screenshots,
    float? UserScore);

/// <summary>
/// Interface for plugins that provide UI themes.
/// </summary>
public interface ITheme
{
    /// <summary>
    /// Gets the name of this theme.
    /// </summary>
    string ThemeName { get; }

    /// <summary>
    /// Gets the display name of this theme.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the author of this theme.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the version of this theme.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Applies this theme to the application.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> ApplyAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes this theme from the application.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> RemoveAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the theme's resource dictionary (Avalonia-specific).
    /// </summary>
    object? GetResourceDictionary();
}

/// <summary>
/// Interface for plugins that can import data from other applications.
/// </summary>
public interface IImporter
{
    /// <summary>
    /// Gets the name of this importer.
    /// </summary>
    string ImporterName { get; }

    /// <summary>
    /// Gets the display name of this importer.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the applications this importer can import from.
    /// </summary>
    IReadOnlyList<string> SupportedApplications { get; }

    /// <summary>
    /// Analyzes an import file and returns what can be imported.
    /// </summary>
    /// <param name="filePath">Path to the import file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Analysis of what can be imported.</returns>
    Task<Result<ImportAnalysis>> AnalyzeImportAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Performs the import operation.
    /// </summary>
    /// <param name="filePath">Path to the import file.</param>
    /// <param name="options">Import options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import result.</returns>
    Task<Result<ImportResult>> ImportAsync(string filePath, ImportOptions options, CancellationToken ct = default);
}

/// <summary>
/// Analysis of what can be imported.
/// </summary>
public sealed record ImportAnalysis(
    int GamesCount,
    int CollectionsCount,
    int PlaytimeRecordsCount,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Options for import operations.
/// </summary>
public sealed record ImportOptions(
    bool ImportGames = true,
    bool ImportCollections = true,
    bool ImportPlaytime = true,
    bool OverwriteExisting = false);

/// <summary>
/// Result of an import operation.
/// </summary>
public sealed record ImportResult(
    int GamesImported,
    int CollectionsImported,
    int PlaytimeRecordsImported,
    IReadOnlyList<string> Errors);

/// <summary>
/// Interface for plugins that can export data to various formats.
/// </summary>
public interface IExporter
{
    /// <summary>
    /// Gets the name of this exporter.
    /// </summary>
    string ExporterName { get; }

    /// <summary>
    /// Gets the display name of this exporter.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the supported export formats.
    /// </summary>
    IReadOnlyList<string> SupportedFormats { get; }

    /// <summary>
    /// Exports data to the specified format.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <param name="options">Export options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result.</returns>
    Task<Result<ExportResult>> ExportAsync(string format, ExportOptions options, CancellationToken ct = default);
}

/// <summary>
/// Options for export operations.
/// </summary>
public sealed record ExportOptions(
    string OutputPath,
    bool IncludeGames = true,
    bool IncludeCollections = true,
    bool IncludePlaytime = true,
    bool IncludeReviews = true,
    DateTime? FromDate = null,
    DateTime? ToDate = null);

/// <summary>
/// Result of an export operation.
/// </summary>
public sealed record ExportResult(
    string OutputPath,
    int GamesExported,
    int CollectionsExported,
    long FileSizeBytes);

/// <summary>
/// Interface for plugins that provide UI extensions.
/// </summary>
public interface IUIPanel
{
    /// <summary>
    /// Gets the name of this UI panel.
    /// </summary>
    string PanelName { get; }

    /// <summary>
    /// Gets the display name of this UI panel.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the icon for this UI panel.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets the Avalonia UserControl for this panel.
    /// </summary>
    object GetControl();

    /// <summary>
    /// Called when the panel becomes visible.
    /// </summary>
    Task OnActivatedAsync();

    /// <summary>
    /// Called when the panel becomes hidden.
    /// </summary>
    Task OnDeactivatedAsync();
}