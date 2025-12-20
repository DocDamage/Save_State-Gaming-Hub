namespace SaveState.Core.Entities;

public enum ImageType
{
    Cover,
    Background,
    Icon,
    Logo
}

public class GameImage
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    public ImageType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
