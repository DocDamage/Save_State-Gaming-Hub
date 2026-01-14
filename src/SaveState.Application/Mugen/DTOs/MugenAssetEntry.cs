namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN asset information (characters, stages, add-ons).
/// </summary>
public sealed class MugenAssetEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public float Rating { get; set; }
    public bool IsFeatured { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ThumbnailUrl { get; set; }
    public string? DownloadUrl { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsInstalled { get; set; }
    public string? PreviewImagePath { get; set; }
}
