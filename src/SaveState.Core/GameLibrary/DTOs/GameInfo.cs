namespace SaveState.Core.GameLibrary.DTOs;

public class GameInfo
{
    public string Source { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? InstallPath { get; set; }
    public DateTimeOffset? LastPlayed { get; set; }
    public int? PlayTimeMinutes { get; set; }
    public string? Platform { get; set; }
}
