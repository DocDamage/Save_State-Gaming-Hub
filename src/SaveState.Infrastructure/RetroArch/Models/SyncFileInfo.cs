namespace SaveState.Infrastructure.RetroArch.Models;

/// <summary>
/// Represents a file to be synchronized with cloud storage.
/// </summary>
public sealed class SyncFileInfo
{
    /// <summary>
    /// Gets or sets the full path to the file.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the file contents.
    /// </summary>
    public required string Hash { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp of the file.
    /// </summary>
    public required DateTime Modified { get; set; }
}
