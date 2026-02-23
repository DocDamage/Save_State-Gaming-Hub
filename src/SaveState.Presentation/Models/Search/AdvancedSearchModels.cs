namespace SaveState.Presentation.Models.Search;

/// <summary>
/// Represents a comprehensive search query with filters, sorting, and pagination.
/// </summary>
public record SearchQuery
{
    public string Text { get; set; } = string.Empty;
    public List<SearchFilter> Filters { get; set; } = new();
    public List<SearchSort> Sorting { get; set; } = new();
    public int PageSize { get; set; } = 25;
    public int PageNumber { get; set; } = 1;
}

/// <summary>
/// Represents a filter criterion for advanced search.
/// </summary>
public record SearchFilter
{
    public SearchFilterType Type { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "="; // =, !=, <, >, contains, startsWith
    public object Value { get; set; } = null!;
}

/// <summary>
/// Types of filters available for advanced search.
/// </summary>
public enum SearchFilterType
{
    Game,
    Platform,
    Genre,
    Tag,
    Date,
    Rating,
    PlayTime,
    SaveState,
    Achievement,
    Collection,
    Status
}

/// <summary>
/// Represents a sorting criterion for search results.
/// </summary>
public record SearchSort
{
    public string Field { get; set; } = string.Empty;
    public bool Descending { get; set; }
}

/// <summary>
/// Represents a single search result item.
/// </summary>
public record SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public SearchResultType Type { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public float RelevanceScore { get; set; }
}

/// <summary>
/// Types of items that can appear in search results.
/// </summary>
public enum SearchResultType
{
    Game,
    SaveState,
    Achievement,
    Screenshot,
    Replay,
    Collection,
    Setting,
    Command
}
