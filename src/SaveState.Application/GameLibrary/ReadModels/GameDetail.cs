using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.ReadModels;

/// <summary>
/// Read model optimized for detailed game views.
/// Contains comprehensive information for game detail pages.
/// </summary>
public class GameDetail
{
    public GameId Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Platform { get; init; } = string.Empty;
    public string? InstallPath { get; init; }
    public string? Source { get; init; }
    public string? SourceId { get; init; }
    public DateTime? LastPlayed { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
    public string? CoverImageUrl { get; init; }
    public GameStatus Status { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<GameFileInfo> Files { get; init; } = Array.Empty<GameFileInfo>();
}

/// <summary>
/// Read model for game file information.
/// </summary>
public class GameFileInfo
{
    public string Path { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
}
