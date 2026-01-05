namespace SaveState.Core.GameLibrary.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.ValueObjects;

/// <summary>
/// Represents a mod or modification installed for a game.
/// Tracks mod metadata, installation status, and compatibility.
/// </summary>
public class GameMod : EntityBase
{
    /// <summary>
    /// Gets the unique identifier for this mod.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the ID of the game this mod is installed for.
    /// </summary>
    public GameId GameId { get; private set; }

    /// <summary>
    /// Gets the name of the mod.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the description of what the mod does.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the mod version.
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// Gets the author or creator of the mod.
    /// </summary>
    public string? Author { get; private set; }

    /// <summary>
    /// Gets the installation path of the mod.
    /// </summary>
    public string InstallPath { get; private set; }

    /// <summary>
    /// Gets the file size of the mod in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets whether the mod is currently enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the mod category (e.g., "Graphics", "Gameplay", "Audio", "UI").
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Gets the tags associated with this mod.
    /// </summary>
    public IReadOnlyList<string> Tags { get; private set; }

    /// <summary>
    /// Gets the URL where the mod was downloaded from.
    /// </summary>
    public string? SourceUrl { get; private set; }

    /// <summary>
    /// Gets the load order priority (lower numbers load first).
    /// </summary>
    public int LoadOrder { get; private set; }

    /// <summary>
    /// Gets whether the mod has configuration files.
    /// </summary>
    public bool HasConfiguration { get; private set; }

    /// <summary>
    /// Gets the date and time when the mod was installed.
    /// </summary>
    public DateTime InstalledAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the mod was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private GameMod()
    {
        Name = string.Empty;
        Version = string.Empty;
        InstallPath = string.Empty;
        Tags = new List<string>();
        GameId = default!;
    }

    /// <summary>
    /// Creates a new game mod.
    /// </summary>
    public static GameMod Create(
        GameId gameId,
        string name,
        string version,
        string installPath,
        long fileSizeBytes,
        string? description = null,
        string? author = null,
        string? category = null,
        IEnumerable<string>? tags = null,
        string? sourceUrl = null,
        int loadOrder = 0)
    {
        var mod = new GameMod
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            Description = description,
            Version = version,
            Author = author,
            InstallPath = installPath,
            FileSizeBytes = fileSizeBytes,
            IsEnabled = true,
            Category = category,
            Tags = tags?.ToList() ?? new List<string>(),
            SourceUrl = sourceUrl,
            LoadOrder = loadOrder,
            HasConfiguration = false,
            InstalledAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return mod;
    }

    /// <summary>
    /// Updates the mod metadata.
    /// </summary>
    public void Update(
        string name,
        string version,
        string? description = null,
        string? author = null,
        string? category = null,
        IEnumerable<string>? tags = null)
    {
        Name = name;
        Version = version;
        Description = description;
        Author = author;
        Category = category;
        Tags = tags?.ToList() ?? new List<string>();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables the mod.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables the mod.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the load order for this mod.
    /// </summary>
    public void SetLoadOrder(int order)
    {
        LoadOrder = order;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks that the mod has configuration files.
    /// </summary>
    public void MarkHasConfiguration()
    {
        HasConfiguration = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
