using SaveState.Core.Common;

namespace SaveState.Core.Search.Models;

/// <summary>
/// Defines the search scope for universal search queries.
/// </summary>
public enum SearchScope
{
    All,
    Games,
    Saves,
    Settings,
    Actions,
    Guides,
    Achievements,
    Commands
}

/// <summary>
/// Represents a search query with filters and options.
/// </summary>
public record SearchQuery
{
    public required string Query { get; init; }
    public required SearchScope Scope { get; init; }
    public required IReadOnlyList<SearchFilter> Filters { get; init; }
    public required int MaxResults { get; init; } = 20;
}

/// <summary>
/// Represents a filter condition for search results.
/// </summary>
public record SearchFilter
{
    public required string Field { get; init; }
    public required string Operator { get; init; } // eq, gt, lt, contains
    public required object Value { get; init; }
}

/// <summary>
/// Represents a search result item.
/// </summary>
public record SearchResult
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required SearchResultType Type { get; init; }
    public required string Icon { get; init; }
    public required float RelevanceScore { get; init; }
    public required IReadOnlyList<string> Highlights { get; init; }
    public required Func<Task<Result>>? Action { get; init; }
    public required string? Shortcut { get; init; }
}

/// <summary>
/// Defines the type of search result.
/// </summary>
public enum SearchResultType
{
    Game,
    SaveState,
    Setting,
    Action,
    Command,
    Guide,
    Achievement
}

/// <summary>
/// Represents an entry in the search index for semantic search.
/// </summary>
public record SearchIndexEntry
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required IReadOnlyList<float> Embedding { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required DateTime LastUpdated { get; init; }
}

/// <summary>
/// Represents a searchable action that can be executed.
/// </summary>
public record SearchableAction
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Icon { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
    public required Func<Task<Result>> Execute { get; init; }
    public string? Shortcut { get; init; }
}

/// <summary>
/// Represents a search suggestion for autocomplete.
/// </summary>
public record SearchSuggestion
{
    public required string Text { get; init; }
    public required SearchResultType Type { get; init; }
    public required float Confidence { get; init; }
}

/// <summary>
/// Options for configuring search behavior.
/// </summary>
public record SearchOptions
{
    public bool IncludeSemanticSearch { get; init; } = true;
    public bool IncludeTextSearch { get; init; } = true;
    public float MinRelevanceScore { get; init; } = 0.3f;
    public int MaxResults { get; init; } = 20;
    public TimeSpan DebounceInterval { get; init; } = TimeSpan.FromMilliseconds(150);
}

/// <summary>
/// Represents search results grouped by category.
/// </summary>
public record GroupedSearchResults
{
    public required string Query { get; init; }
    public required IReadOnlyList<SearchResultGroup> Groups { get; init; }
    public required int TotalResults { get; init; }
    public required TimeSpan SearchDuration { get; init; }
}

/// <summary>
/// Represents a group of search results of the same type.
/// </summary>
public record SearchResultGroup
{
    public required SearchResultType Type { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<SearchResult> Results { get; init; }
}
