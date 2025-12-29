namespace SaveState.Core.GameLibrary.DTOs;

public class GameMetadata
{
    public static GameMetadata Empty => new() { Title = string.Empty };

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? ReleaseDate { get; set; }
    public string[] Genres { get; set; } = Array.Empty<string>();
    public string? CoverImageUrl { get; set; }
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public decimal? UserRating { get; set; }
    public int? MetacriticScore { get; set; }
}
