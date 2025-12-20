namespace SaveState.Core.Models;

public class GameMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Developers { get; set; } = new();
    public List<string> Publishers { get; set; } = new();
    public double? Rating { get; set; }
    public string? CoverUrl { get; set; }
    public string? BackgroundUrl { get; set; }
    public long? IgdbId { get; set; }
}
