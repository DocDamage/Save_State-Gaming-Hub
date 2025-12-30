using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Infrastructure.Repositories.Projections;

/// <summary>
/// Database projection for game summaries - optimized for list views.
/// Contains only the fields needed for game library browsing.
/// </summary>
public class GameSummaryProjection
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PlatformName { get; init; } = string.Empty;
    public GameStatus Status { get; init; }
    public string? CoverImageUrl { get; init; }
}
