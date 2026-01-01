using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.DTOs;

public class GameSummaryDto
{
    public required GameId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public GameStatus Status { get; set; }
    public DateTime AddedAt { get; set; }
}
