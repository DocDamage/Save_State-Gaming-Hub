using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.ReadModels;

/// <summary>
/// Read model optimized for game list views and search results.
/// Contains essential information for displaying games in lists or grids.
/// </summary>
public class GameSummary
{
    public required GameId Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public GameStatus Status { get; init; }
    public string? CoverImageUrl { get; init; }
    public DateTime? LastPlayed { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
}
