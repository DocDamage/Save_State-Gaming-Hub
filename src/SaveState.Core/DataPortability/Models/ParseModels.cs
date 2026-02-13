using System.Text.Json;

namespace SaveState.Core.DataPortability.Models;

/// <summary>
/// Container for parsed data from an import file.
/// </summary>
public class ParsedData
{
    public ImportFormat Format { get; init; } = ImportFormat.Unknown;
    public JsonDocument? RootDocument { get; init; }
    public string? RawContent { get; init; }
    public Dictionary<string, JsonElement> Sections { get; init; } = new();
    public List<ParseError> Errors { get; init; } = new();
    public bool IsValid => Errors.Count == 0 && RootDocument != null;
}

/// <summary>
/// Represents an error that occurred during parsing.
/// </summary>
public record ParseError(
    string Message,
    string? Section = null,
    int? LineNumber = null,
    Exception? Exception = null);
