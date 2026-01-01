using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.DTOs;

public class GameDetailsDto
{
    public required GameId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? InstallPath { get; set; }
    public string? Source { get; set; }
    public string? SourceId { get; set; }
    public DateTime? LastPlayed { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public string? CoverImageUrl { get; set; }
    public GameStatus Status { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<GameFileDto> Files { get; set; } = Array.Empty<GameFileDto>();
}
