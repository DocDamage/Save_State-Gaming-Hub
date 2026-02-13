using SaveState.Core.Common;

namespace SaveState.Core.Input.Services.DTOs;

/// <summary>
/// A command definition that can be surfaced and executed by the command palette.
/// </summary>
public sealed record CommandDefinition
{
    /// <summary>
    /// Unique command identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// User-facing command title.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// User-facing command description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Logical grouping/category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Search keywords used for fuzzy matching.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>
    /// Optional icon hint.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Optional keyboard shortcut hint.
    /// </summary>
    public string? Shortcut { get; init; }

    /// <summary>
    /// Optional source identifier (e.g., plugin id).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Delegate used to execute the command.
    /// </summary>
    public required Func<CancellationToken, Task<Result>> ExecuteAsync { get; init; }
}

/// <summary>
/// Search result entry returned by command palette lookups.
/// </summary>
public sealed record CommandItem(
    string Id,
    string Name,
    string Description,
    string Category,
    float Score,
    string? Icon = null,
    string? Shortcut = null,
    string? Source = null);

/// <summary>
/// Context used to constrain command palette searches.
/// </summary>
public sealed record CommandContext
{
    /// <summary>
    /// Shared default context.
    /// </summary>
    public static CommandContext Default { get; } = new();

    /// <summary>
    /// Optional category allow list.
    /// Empty list means all categories are eligible.
    /// </summary>
    public IReadOnlyList<string> AllowedCategories { get; init; } = [];

    /// <summary>
    /// Maximum number of search results returned.
    /// </summary>
    public int MaxResults { get; init; } = 25;
}
