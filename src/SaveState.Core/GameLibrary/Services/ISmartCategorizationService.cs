using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

public interface ISmartCategorizationService
{
    Task<Result<GameTags>> AnalyzeGameAsync(Guid gameId, CancellationToken ct = default);
    Task<Result> AutoTagLibraryAsync(IProgress<TaggingProgress>? progress = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> SuggestTagsAsync(string gameTitle, string? description, CancellationToken ct = default);
}

public sealed record GameTags(
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Themes,
    IReadOnlyList<string> Moods,
    IReadOnlyList<string> Mechanics,
    string? SuggestedRating,
    float Confidence);

public sealed record TaggingProgress(int Processed, int Total, string CurrentGame);