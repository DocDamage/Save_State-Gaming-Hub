namespace SaveState.Core.RetroArch.Models;

/// <summary>
/// Information about a save state.
/// </summary>
public class SaveStateInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int Slot { get; init; } = -1;
    public bool IsAutoSave { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public long FileSize { get; init; }
    public SaveStateMetadata? Metadata { get; init; }
    public string? ThumbnailPath { get; init; }
    public SaveStateFormat Format { get; init; }
}

/// <summary>
/// Metadata for a save state.
/// </summary>
public class SaveStateMetadata
{
    public string? GameName { get; init; }
    public string? CoreName { get; init; }
    public string? GamePath { get; init; }
    public TimeSpan PlayTime { get; init; }
    public DateTime SaveDate { get; init; }
    public string? Region { get; init; }
    public Dictionary<string, string> ExtraData { get; init; } = new();
}

/// <summary>
/// Options for creating a save state.
/// </summary>
public class SaveStateOptions
{
    public int Slot { get; init; } = -1;
    public string? Label { get; init; }
    public bool IncludeScreenshot { get; init; }
    public Dictionary<string, string>? ExtraMetadata { get; init; }
}

/// <summary>
/// Result of a save state operation.
/// </summary>
public class SaveStateResult
{
    public bool Success { get; init; }
    public string? FilePath { get; init; }
    public string? ErrorMessage { get; init; }
    public SaveStateInfo? Info { get; init; }
}
