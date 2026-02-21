using SaveState.Core.Common;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Core.ContentGeneration.Services;

/// <summary>
/// Service for searching save states using natural language queries.
/// </summary>
public interface INaturalLanguageSaveSearch
{
    /// <summary>
    /// Searches save states using a natural language query.
    /// </summary>
    /// <param name="naturalLanguageQuery">Query like "my save before the final boss in Elden Ring"</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing matching save states.</returns>
    Task<Result<IReadOnlyList<SaveStateEntity>>> SearchAsync(
        string naturalLanguageQuery,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes a natural language query to extract structured information.
    /// </summary>
    /// <param name="query">The natural language query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the parsed query analysis.</returns>
    Task<Result<SearchQueryAnalysis>> AnalyzeQueryAsync(
        string query,
        CancellationToken ct = default);
}

/// <summary>
/// Analysis result from parsing a natural language search query.
/// </summary>
public record SearchQueryAnalysis
{
    public required string OriginalQuery { get; init; }
    public required string Intent { get; init; } // "find", "compare", "restore", etc.
    public required IReadOnlyList<string> ExtractedKeywords { get; init; }
    public required DateTime? ReferencedDate { get; init; }
    public required string? ReferencedGame { get; init; }
    public required string? ReferencedLocation { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
}
