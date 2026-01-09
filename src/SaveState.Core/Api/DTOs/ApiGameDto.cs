namespace SaveState.Core.Api.DTOs;

/// <summary>
/// Data transfer object for Game entity in API responses.
/// </summary>
public record ApiGameDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverImagePath,
    string? Platform,
    string Status,
    DateTime? LastPlayedAt,
    string TotalPlayTime,
    double? UserRating,
    bool IsCompleted,
    List<string> Tags);

/// <summary>
/// Data transfer object for game session in API responses.
/// </summary>
public record ApiGameSessionDto(
    Guid Id,
    Guid GameId,
    string GameTitle,
    DateTime StartedAt,
    DateTime? EndedAt,
    string Duration,
    bool IsActive);

/// <summary>
/// Data transfer object for library statistics in API responses.
/// </summary>
public record ApiLibraryStatsDto(
    int TotalGames,
    int GamesPlayed,
    int GamesCompleted,
    string TotalPlayTime,
    Dictionary<string, int> GamesByPlatform,
    List<ApiGameDto> RecentlyPlayed);
