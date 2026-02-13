using SaveState.Core.Common;

namespace SaveState.Core.Intelligence.Search.Services;

/// <summary>
/// Universal search service that provides semantic search across games,
/// save states, settings, and application commands.
/// </summary>
public interface IUniversalSearchService
{
    /// <summary>
    /// Performs a universal search across all indexed content.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search results organized by category.</returns>
    Task<Result<UniversalSearchResults>> SearchAsync(
        SearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Searches games with semantic understanding.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="options">Search options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Semantic game search results.</returns>
    Task<Result<IReadOnlyList<SemanticGameResult>>> SearchGamesAsync(
        string query,
        GameSearchOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Searches application settings and commands.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Action search results.</returns>
    Task<Result<IReadOnlyList<ActionSearchResult>>> SearchActionsAsync(
        string query,
        CancellationToken ct = default);

    /// <summary>
    /// Searches within indexed content (descriptions, reviews, notes).
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="contentTypes">Types of content to search.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Content search results.</returns>
    Task<Result<IReadOnlyList<ContentSearchResult>>> SearchContentAsync(
        string query,
        IReadOnlyList<ContentType>? contentTypes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Indexes a game for semantic search.
    /// </summary>
    /// <param name="game">The game to index.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the indexing operation.</returns>
    Task<Result> IndexGameAsync(
        GameSearchIndex game,
        CancellationToken ct = default);

    /// <summary>
    /// Indexes content for semantic search.
    /// </summary>
    /// <param name="content">The content to index.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the indexing operation.</returns>
    Task<Result> IndexContentAsync(
        ContentIndex content,
        CancellationToken ct = default);

    /// <summary>
    /// Registers an action for action search.
    /// </summary>
    /// <param name="action">The action to register.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the registration.</returns>
    Task<Result> RegisterActionAsync(
        SearchableAction action,
        CancellationToken ct = default);

    /// <summary>
    /// Gets search suggestions based on partial query.
    /// </summary>
    /// <param name="partialQuery">The partial query.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search suggestions.</returns>
    Task<Result<IReadOnlyList<SearchSuggestion>>> GetSuggestionsAsync(
        string partialQuery,
        int maxSuggestions = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Gets trending searches.
    /// </summary>
    /// <param name="count">Number of trending searches.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Trending search queries.</returns>
    Task<Result<IReadOnlyList<TrendingSearch>>> GetTrendingSearchesAsync(
        int count = 10,
        CancellationToken ct = default);
}

/// <summary>
/// Universal search query.
/// </summary>
public sealed record SearchQuery(
    string Query,
    int MaxResults = 20,
    IReadOnlyList<SearchCategory>? Categories = null,
    SearchFilters? Filters = null);

/// <summary>
/// Search categories.
/// </summary>
public enum SearchCategory
{
    Games,
    SaveStates,
    Actions,
    Settings,
    Reviews,
    Guides,
    Community,
    All
}

/// <summary>
/// Search filters.
/// </summary>
public sealed record SearchFilters(
    DateTime? DateFrom,
    DateTime? DateTo,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Genres,
    float? MinRelevanceScore = 0.5f);

/// <summary>
/// Universal search results.
/// </summary>
public sealed record UniversalSearchResults(
    string Query,
    IReadOnlyList<GameSearchResult> Games,
    IReadOnlyList<SaveStateSearchResult> SaveStates,
    IReadOnlyList<ActionSearchResult> Actions,
    IReadOnlyList<ContentSearchResult> Content,
    int TotalResults,
    TimeSpan SearchDuration);

/// <summary>
/// Base search result.
/// </summary>
public abstract record SearchResult
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public float RelevanceScore { get; init; }
    public SearchCategory Category { get; init; }

    protected SearchResult(string id, string title, string? description, float relevanceScore, SearchCategory category)
    {
        Id = id;
        Title = title;
        Description = description;
        RelevanceScore = relevanceScore;
        Category = category;
    }
}

/// <summary>
/// Game search result.
/// </summary>
public sealed record GameSearchResult : SearchResult
{
    public string? CoverImageUrl { get; init; }
    public IReadOnlyList<string> Genres { get; init; }
    public IReadOnlyList<string> MatchedTerms { get; init; }
    public GameMatchReason MatchReason { get; init; }

    public GameSearchResult(
        string id,
        string title,
        string? description,
        float relevanceScore,
        string? coverImageUrl,
        IReadOnlyList<string> genres,
        IReadOnlyList<string> matchedTerms,
        GameMatchReason matchReason)
        : base(id, title, description, relevanceScore, SearchCategory.Games)
    {
        CoverImageUrl = coverImageUrl;
        Genres = genres;
        MatchedTerms = matchedTerms;
        MatchReason = matchReason;
    }
}

/// <summary>
/// Reason for game match.
/// </summary>
public enum GameMatchReason
{
    TitleMatch,
    GenreMatch,
    TagMatch,
    DescriptionMatch,
    SemanticMatch,
    SimilarGames,
    Trending,
    RecentlyPlayed
}

/// <summary>
/// Save state search result.
/// </summary>
public sealed record SaveStateSearchResult : SearchResult
{
    public string GameId { get; init; }
    public string GameTitle { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? PreviewImageUrl { get; init; }
    public IReadOnlyList<string> MatchedTerms { get; init; }

    public SaveStateSearchResult(
        string id,
        string title,
        string? description,
        float relevanceScore,
        string gameId,
        string gameTitle,
        DateTime createdAt,
        string? previewImageUrl,
        IReadOnlyList<string> matchedTerms)
        : base(id, title, description, relevanceScore, SearchCategory.SaveStates)
    {
        GameId = gameId;
        GameTitle = gameTitle;
        CreatedAt = createdAt;
        PreviewImageUrl = previewImageUrl;
        MatchedTerms = matchedTerms;
    }
}

/// <summary>
/// Action search result for settings and commands.
/// </summary>
public sealed record ActionSearchResult(
    string Id,
    string Title,
    string? Description,
    float RelevanceScore,
    string ActionType,
    string? ActionCategory,
    string? Icon,
    IReadOnlyList<string> Keywords,
    Func<Task>? Execute)
    : SearchResult(Id, Title, Description, RelevanceScore, SearchCategory.Actions);

/// <summary>
/// Content search result.
/// </summary>
public sealed record ContentSearchResult : SearchResult
{
    public string Content { get; init; }
    public ContentType ContentType { get; init; }
    public string SourceId { get; init; }
    public string SourceTitle { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Author { get; init; }
    public IReadOnlyList<string> MatchedTerms { get; init; }
    public IReadOnlyList<ContentSnippet> Snippets { get; init; }

    public ContentSearchResult(
        string id,
        string title,
        string content,
        float relevanceScore,
        ContentType contentType,
        string sourceId,
        string sourceTitle,
        DateTime createdAt,
        string? author,
        IReadOnlyList<string> matchedTerms,
        IReadOnlyList<ContentSnippet> snippets)
        : base(id, title, content[..Math.Min(200, content.Length)], relevanceScore, SearchCategory.Reviews)
    {
        Content = content;
        ContentType = contentType;
        SourceId = sourceId;
        SourceTitle = sourceTitle;
        CreatedAt = createdAt;
        Author = author;
        MatchedTerms = matchedTerms;
        Snippets = snippets;
    }
}

/// <summary>
/// Content type for search.
/// </summary>
public enum ContentType
{
    Review,
    Guide,
    News,
    ForumPost,
    WikiArticle,
    Description,
    Note,
    AchievementDescription
}

/// <summary>
/// Content snippet with highlighted matches.
/// </summary>
public sealed record ContentSnippet(
    string Text,
    int StartIndex,
    int Length,
    bool IsMatch);

/// <summary>
/// Semantic game search result.
/// </summary>
public sealed record SemanticGameResult(
    Guid GameId,
    string Title,
    string? Description,
    float SemanticScore,
    float TextMatchScore,
    float CombinedScore,
    IReadOnlyList<string> MatchedConcepts,
    string? Explanation);

/// <summary>
/// Game search options.
/// </summary>
public sealed record GameSearchOptions(
    IReadOnlyList<string>? Genres = null,
    IReadOnlyList<string>? Platforms = null,
    DateTime? ReleasedAfter = null,
    DateTime? ReleasedBefore = null,
    float? MinRating = null,
    bool IncludePlayed = true,
    int MaxResults = 20);

/// <summary>
/// Game search index for semantic indexing.
/// </summary>
public sealed record GameSearchIndex(
    Guid Id,
    string Title,
    string? Description,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Features,
    string? Developer,
    string? Publisher,
    DateTime? ReleaseDate,
    float? Rating,
    DateTime IndexedAt);

/// <summary>
/// Content index for semantic indexing.
/// </summary>
public sealed record ContentIndex(
    Guid Id,
    ContentType Type,
    string Title,
    string Content,
    string SourceId,
    string SourceType,
    DateTime CreatedAt,
    string? AuthorId,
    IReadOnlyList<string>? Tags,
    DateTime IndexedAt);

/// <summary>
/// Searchable action for action search.
/// </summary>
public sealed record SearchableAction(
    string Id,
    string Title,
    string? Description,
    string ActionType,
    string? Category,
    string? Icon,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string>? Tags,
    int? ShortcutKey = null,
    string? ShortcutModifiers = null);

/// <summary>
/// Search suggestion.
/// </summary>
public sealed record SearchSuggestion(
    string Text,
    SearchCategory Category,
    string? Icon,
    float Confidence);

/// <summary>
/// Trending search.
/// </summary>
public sealed record TrendingSearch(
    string Query,
    int SearchCount,
    DateTime LastSearched,
    SearchCategory PrimaryCategory);

/// <summary>
/// Semantic embedding for vector search.
/// </summary>
public sealed record SemanticEmbedding(
    string Id,
    string ContentType,
    string ContentId,
    IReadOnlyList<float> Vector,
    DateTime CreatedAt);

/// <summary>
/// Interface for embedding generation service.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates embedding vector for text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Embedding vector.</returns>
    Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string text,
        CancellationToken ct = default);

    /// <summary>
    /// Generates embeddings for multiple texts.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Embedding vectors.</returns>
    Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    /// <param name="vector1">First vector.</param>
    /// <param name="vector2">Second vector.</param>
    /// <returns>Cosine similarity score.</returns>
    float CalculateSimilarity(IReadOnlyList<float> vector1, IReadOnlyList<float> vector2);
}
