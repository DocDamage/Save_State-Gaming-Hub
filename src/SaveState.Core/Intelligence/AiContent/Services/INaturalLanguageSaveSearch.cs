using SaveState.Core.Common;

namespace SaveState.Core.Intelligence.AiContent.Services;

/// <summary>
/// Interface for natural language save state search.
/// </summary>
public interface INaturalLanguageSaveSearch
{
    /// <summary>
    /// Searches save states using natural language query.
    /// </summary>
    /// <param name="query">Natural language query (e.g., "find my save before the final boss").</param>
    /// <param name="gameId">Optional game ID to limit search.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching save states with relevance scores.</returns>
    Task<Result<IReadOnlyList<SemanticSaveResult>>> SearchSavesAsync(
        string query,
        Guid? gameId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Indexes a save state for semantic search.
    /// </summary>
    /// <param name="saveStateId">The save state ID.</param>
    /// <param name="context">Contextual information about the save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the indexing operation.</returns>
    Task<Result> IndexSaveStateAsync(
        Guid saveStateId,
        SaveStateContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a natural language description of a save state.
    /// </summary>
    /// <param name="saveStateId">The save state ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generated description.</returns>
    Task<Result<string>> GenerateDescriptionAsync(
        Guid saveStateId,
        CancellationToken ct = default);
}

/// <summary>
/// Semantic save search result.
/// </summary>
public sealed record SemanticSaveResult(
    Guid SaveStateId,
    Guid GameId,
    string GameTitle,
    string? Description,
    float RelevanceScore,
    DateTime CreatedAt,
    string? PreviewImageUrl);

/// <summary>
/// Contextual information for save state indexing.
/// </summary>
public sealed record SaveStateContext(
    string? PlayerNotes,
    string? GameLocation,
    string? CharacterLevel,
    TimeSpan? PlayTimeAtSave,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? UnlockedAchievements,
    Guid? GameId = null,
    string? GameTitle = null);
