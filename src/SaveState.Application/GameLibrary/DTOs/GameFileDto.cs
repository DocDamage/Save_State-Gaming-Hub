namespace SaveState.Application.GameLibrary.DTOs;

public class GameFileDto
{
    public string Path { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public DateTime AddedAt { get; set; }
}
