using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Interface for importing Cheat Engine table files (.CT) into the memory signature database.
/// </summary>
public interface ICheatEngineImporter
{
    /// <summary>
    /// Imports Cheat Engine entries from a single .CT file.
    /// </summary>
    /// <param name="filePath">Path to the .CT file.</param>
    /// <param name="options">Import options for controlling behavior.</param>
    /// <returns>Result containing import statistics and imported signatures.</returns>
    Result<CheatEngineImportResult> ImportFromFile(string filePath, CheatEngineImportOptions? options = null);

    /// <summary>
    /// Imports all .CT files from a directory.
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing .CT files.</param>
    /// <param name="recursive">Whether to search subdirectories recursively.</param>
    /// <param name="options">Import options for controlling behavior.</param>
    /// <returns>Result containing combined import statistics.</returns>
    Result<CheatEngineImportResult> ImportFromDirectory(string directoryPath, bool recursive = false, CheatEngineImportOptions? options = null);

    /// <summary>
    /// Checks if a file can be parsed as a Cheat Engine table.
    /// Validates XML structure and basic Cheat Table format.
    /// </summary>
    /// <param name="filePath">Path to the file to check.</param>
    /// <returns>True if the file appears to be a valid CT file.</returns>
    bool CanParseFile(string filePath);

    /// <summary>
    /// Previews the contents of a .CT file without importing.
    /// Useful for showing users what will be imported before confirmation.
    /// </summary>
    /// <param name="filePath">Path to the .CT file.</param>
    /// <returns>Result containing preview entries.</returns>
    Result<CheatEngineTablePreview> PreviewFile(string filePath);
}

/// <summary>
/// Options for controlling Cheat Engine table import behavior.
/// </summary>
public class CheatEngineImportOptions
{
    /// <summary>
    /// The game title to associate with imported signatures.
    /// If null, attempts to extract from filename or CT metadata.
    /// </summary>
    public string? GameTitle { get; set; }

    /// <summary>
    /// Whether to skip entries that already exist in the database.
    /// </summary>
    public bool SkipDuplicates { get; set; } = true;

    /// <summary>
    /// Whether to overwrite existing entries with the same name.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Whether to include Lua script entries (typically advanced cheats).
    /// </summary>
    public bool IncludeScripts { get; set; } = false;

    /// <summary>
    /// Whether to only import entries with specific tags.
    /// </summary>
    public List<string>? RequiredTags { get; set; }

    /// <summary>
    /// Tags to apply to all imported signatures.
    /// </summary>
    public List<string> DefaultTags { get; set; } = new() { "cheat-engine", "imported" };

    /// <summary>
    /// Minimum priority for imported entries.
    /// </summary>
    public int MinimumPriority { get; set; } = 0;

    /// <summary>
    /// Callback for progress updates during batch imports.
    /// </summary>
    public Action<CheatEngineImportProgress>? ProgressCallback { get; set; }
}

/// <summary>
/// Result of a Cheat Engine table import operation.
/// </summary>
public class CheatEngineImportResult
{
    /// <summary>
    /// Total number of entries found in the source file(s).
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Number of entries successfully imported.
    /// </summary>
    public int SuccessfullyImported { get; set; }

    /// <summary>
    /// Number of entries skipped (duplicates, filtered, etc.).
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Number of entries that failed to import.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// List of errors that occurred during import.
    /// </summary>
    public List<ImportError> Errors { get; set; } = new();

    /// <summary>
    /// List of successfully imported signatures.
    /// </summary>
    public List<GameMemorySignature> ImportedSignatures { get; set; } = new();

    /// <summary>
    /// List of entries that were skipped and why.
    /// </summary>
    public List<SkippedEntry> SkippedEntries { get; set; } = new();

    /// <summary>
    /// Source files that were processed.
    /// </summary>
    public List<string> ProcessedFiles { get; set; } = new();

    /// <summary>
    /// Returns a summary string of the import operation.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();
        if (SuccessfullyImported > 0) parts.Add($"{SuccessfullyImported} imported");
        if (Skipped > 0) parts.Add($"{Skipped} skipped");
        if (Failed > 0) parts.Add($"{Failed} failed");
        return string.Join(", ", parts);
    }
}

/// <summary>
/// Represents an error that occurred during import.
/// </summary>
public class ImportError
{
    /// <summary>
    /// The entry description or identifier that caused the error.
    /// </summary>
    public string EntryName { get; set; } = "";

    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// The type of error.
    /// </summary>
    public ImportErrorType ErrorType { get; set; }

    /// <summary>
    /// Optional exception details.
    /// </summary>
    public string? ExceptionDetails { get; set; }
}

/// <summary>
/// Types of import errors.
/// </summary>
public enum ImportErrorType
{
    ParseError,
    ValidationError,
    ConversionError,
    DuplicateEntry,
    UnsupportedType,
    FileError
}

/// <summary>
/// Represents an entry that was skipped during import.
/// </summary>
public class SkippedEntry
{
    /// <summary>
    /// The entry description.
    /// </summary>
    public string EntryName { get; set; } = "";

    /// <summary>
    /// The reason the entry was skipped.
    /// </summary>
    public string Reason { get; set; } = "";
}

/// <summary>
/// Progress information during batch import operations.
/// </summary>
public class CheatEngineImportProgress
{
    /// <summary>
    /// Current file being processed.
    /// </summary>
    public string CurrentFile { get; set; } = "";

    /// <summary>
    /// Index of current file.
    /// </summary>
    public int CurrentFileIndex { get; set; }

    /// <summary>
    /// Total number of files to process.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double PercentComplete => TotalFiles > 0 ? (CurrentFileIndex / (double)TotalFiles) * 100 : 0;

    /// <summary>
    /// Current operation description.
    /// </summary>
    public string StatusMessage { get; set; } = "";
}

/// <summary>
/// Preview of a Cheat Engine table file contents.
/// </summary>
public class CheatEngineTablePreview
{
    /// <summary>
    /// The file path.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Detected or extracted game title.
    /// </summary>
    public string? GameTitle { get; set; }

    /// <summary>
    /// Entries found in the table.
    /// </summary>
    public List<CheatEngineEntryPreview> Entries { get; set; } = new();

    /// <summary>
    /// Whether the table contains Lua scripts.
    /// </summary>
    public bool HasScripts { get; set; }

    /// <summary>
    /// Number of script entries.
    /// </summary>
    public int ScriptCount { get; set; }

    /// <summary>
    /// Whether the file is compressed/encoded.
    /// </summary>
    public bool IsCompressed { get; set; }
}

/// <summary>
/// Preview of a single cheat entry.
/// </summary>
public class CheatEngineEntryPreview
{
    /// <summary>
    /// Entry description (name).
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Memory address or pointer path.
    /// </summary>
    public string Address { get; set; } = "";

    /// <summary>
    /// Variable type (4 Bytes, Float, etc.).
    /// </summary>
    public string VariableType { get; set; } = "";

    /// <summary>
    /// Whether this entry is a pointer (has offsets).
    /// </summary>
    public bool IsPointer { get; set; }

    /// <summary>
    /// Whether this entry is a Lua script.
    /// </summary>
    public bool IsScript { get; set; }

    /// <summary>
    /// Whether this entry can be imported (valid conversion available).
    /// </summary>
    public bool CanImport { get; set; }

    /// <summary>
    /// Reason for import restriction, if any.
    /// </summary>
    public string? ImportRestriction { get; set; }

    /// <summary>
    /// The converted value type for our system.
    /// </summary>
    public string? ConvertedValueType { get; set; }
}
