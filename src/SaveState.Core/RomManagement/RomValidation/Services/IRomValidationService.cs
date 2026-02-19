using SaveState.Core.Common;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.RomManagement.RomValidation.Services;

/// <summary>
/// Service for comprehensive ROM validation including hash calculation,
/// DAT file matching, duplicate detection, and integrity verification.
/// </summary>
public interface IRomValidationService
{
    /// <summary>
    /// Calculates all requested hashes for a ROM file.
    /// </summary>
    Task<Result<RomHashInfo>> CalculateHashesAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a single ROM file against DAT files and integrity checks.
    /// </summary>
    Task<Result<RomValidationReport>> ValidateRomAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple ROM files in batch.
    /// </summary>
    Task<Result<RomValidationJob>> ValidateBatchAsync(
        RomValidationJob job,
        RomValidationOptions options,
        IProgress<RomValidationProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Attempts to match a ROM against known DAT file entries.
    /// </summary>
    Task<Result<RomMatchResult>> MatchAgainstDatFilesAsync(
        RomHashInfo hashInfo,
        IEnumerable<string> datFilePaths,
        CancellationToken ct = default);

    /// <summary>
    /// Finds duplicate ROM files in the library.
    /// </summary>
    Task<Result<List<DuplicateRomInfo>>> FindDuplicatesAsync(
        Guid? platformId = null,
        HashAlgorithmType? hashType = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a missing game report for a platform by comparing against a DAT file.
    /// </summary>
    Task<Result<MissingGameReport>> GenerateMissingGameReportAsync(
        Guid platformId,
        string referenceDatPath,
        CancellationToken ct = default);

    /// <summary>
    /// Suggests standardized names for ROM files based on DAT matches.
    /// </summary>
    Task<Result<List<RomRenameSuggestion>>> GetRenameSuggestionsAsync(
        Guid? platformId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Identifies bad dumps by checking against DAT file entries.
    /// </summary>
    Task<Result<List<BadDumpInfo>>> IdentifyBadDumpsAsync(
        Guid? platformId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets comprehensive validation statistics.
    /// </summary>
    Task<Result<RomValidationStatistics>> GetStatisticsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Exports validation results to a file.
    /// </summary>
    Task<Result<string>> ExportValidationResultsAsync(
        RomValidationExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Loads a DAT file (No-Intro, Redump format).
    /// </summary>
    Task<Result<List<DatFileEntry>>> LoadDatFileAsync(
        string datFilePath,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies file integrity (checks for corruption beyond hash mismatch).
    /// </summary>
    Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(
        string filePath,
        CancellationToken ct = default);
}

/// <summary>
/// Progress information for ROM validation operations.
/// </summary>
public class RomValidationProgress
{
    /// <summary>
    /// Current ROM file being processed.
    /// </summary>
    public string CurrentFile { get; set; } = string.Empty;

    /// <summary>
    /// Number of ROMs processed so far.
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Total ROMs to process.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current operation.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Percentage complete (0-100).
    /// </summary>
    public int PercentageComplete => TotalCount > 0 ? (ProcessedCount * 100) / TotalCount : 0;

    /// <summary>
    /// Current job status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// Suggestion for renaming a ROM file.
/// </summary>
public class RomRenameSuggestion
{
    /// <summary>
    /// ROM file ID.
    /// </summary>
    public Guid RomFileId { get; set; }

    /// <summary>
    /// Current file name.
    /// </summary>
    public string CurrentName { get; set; } = string.Empty;

    /// <summary>
    /// Suggested new name.
    /// </summary>
    public string SuggestedName { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the suggestion.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// DAT file source.
    /// </summary>
    public string SourceDat { get; set; } = string.Empty;

    /// <summary>
    /// Match confidence.
    /// </summary>
    public MatchConfidence Confidence { get; set; }

    /// <summary>
    /// Whether this would be a significant change.
    /// </summary>
    public bool IsSignificantChange => !string.Equals(
        Path.GetFileNameWithoutExtension(CurrentName),
        Path.GetFileNameWithoutExtension(SuggestedName),
        StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Information about a bad dump.
/// </summary>
public class BadDumpInfo
{
    /// <summary>
    /// ROM file ID.
    /// </summary>
    public Guid RomFileId { get; set; }

    /// <summary>
    /// Current file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Platform name.
    /// </summary>
    public string PlatformName { get; set; } = string.Empty;

    /// <summary>
    /// Detected dump status.
    /// </summary>
    public RomDumpStatus DumpStatus { get; set; }

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string IssueDescription { get; set; } = string.Empty;

    /// <summary>
    /// Expected hash from DAT.
    /// </summary>
    public string? ExpectedHash { get; set; }

    /// <summary>
    /// Actual calculated hash.
    /// </summary>
    public string? ActualHash { get; set; }

    /// <summary>
    /// Whether a correct version is available elsewhere in the library.
    /// </summary>
    public bool CorrectVersionAvailable { get; set; }

    /// <summary>
    /// Location of correct version if available.
    /// </summary>
    public string? CorrectVersionLocation { get; set; }

    /// <summary>
    /// Recommended action.
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// File integrity verification result.
/// </summary>
public class FileIntegrityResult
{
    /// <summary>
    /// Whether the file passed integrity checks.
    /// </summary>
    public bool IsIntact { get; set; }

    /// <summary>
    /// File size on disk.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Whether the file can be read completely.
    /// </summary>
    public bool IsReadable { get; set; }

    /// <summary>
    /// Any read errors encountered.
    /// </summary>
    public List<string> ReadErrors { get; set; } = new();

    /// <summary>
    /// File format validity check.
    /// </summary>
    public bool IsValidFormat { get; set; }

    /// <summary>
    /// Format-specific issues.
    /// </summary>
    public List<string> FormatIssues { get; set; } = new();

    /// <summary>
    /// Header information (if applicable).
    /// </summary>
    public RomHeaderInfo? HeaderInfo { get; set; }
}

/// <summary>
/// ROM header information for format validation.
/// </summary>
public class RomHeaderInfo
{
    /// <summary>
    /// Whether the ROM has a header.
    /// </summary>
    public bool HasHeader { get; set; }

    /// <summary>
    /// Header size in bytes.
    /// </summary>
    public int HeaderSize { get; set; }

    /// <summary>
    /// Header type (if known).
    /// </summary>
    public string? HeaderType { get; set; }

    /// <summary>
    /// Whether the header is valid.
    /// </summary>
    public bool IsValidHeader { get; set; }

    /// <summary>
    /// Internal name from header (if available).
    /// </summary>
    public string? InternalName { get; set; }

    /// <summary>
    /// Game code from header (if available).
    /// </summary>
    public string? GameCode { get; set; }

    /// <summary>
    /// Region code from header.
    /// </summary>
    public string? RegionCode { get; set; }

    /// <summary>
    /// Version from header.
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// Options for exporting validation results.
/// </summary>
public class RomValidationExportOptions
{
    /// <summary>
    /// Output file path.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Export format.
    /// </summary>
    public ValidationExportFormat Format { get; set; } = ValidationExportFormat.Json;

    /// <summary>
    /// Platform ID filter (null = all platforms).
    /// </summary>
    public Guid? PlatformId { get; set; }

    /// <summary>
    /// Only include ROMs with specific statuses.
    /// </summary>
    public List<ValidationStatus>? IncludeStatuses { get; set; }

    /// <summary>
    /// Include hash information.
    /// </summary>
    public bool IncludeHashes { get; set; } = true;

    /// <summary>
    /// Include DAT match information.
    /// </summary>
    public bool IncludeDatMatches { get; set; } = true;

    /// <summary>
    /// Include duplicates.
    /// </summary>
    public bool IncludeDuplicates { get; set; } = true;
}

/// <summary>
/// Export format options.
/// </summary>
public enum ValidationExportFormat
{
    Json,
    Csv,
    Html,
    Markdown,
    Dat
}

/// <summary>
/// Repository interface for RomHashInfo entities.
/// </summary>
public interface IRomHashInfoRepository
{
    Task<RomHashInfo?> GetByRomFileIdAsync(Guid romFileId, CancellationToken ct = default);
    Task<IEnumerable<RomHashInfo>> GetByHashAsync(string hash, HashAlgorithmType type, CancellationToken ct = default);
    Task<IEnumerable<RomHashInfo>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RomHashInfo hashInfo, CancellationToken ct = default);
    Task UpdateAsync(RomHashInfo hashInfo, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository interface for RomValidationReport entities.
/// </summary>
public interface IRomValidationReportRepository
{
    Task<RomValidationReport?> GetByRomFileIdAsync(Guid romFileId, CancellationToken ct = default);
    Task<IEnumerable<RomValidationReport>> GetByStatusAsync(ValidationStatus status, CancellationToken ct = default);
    Task<IEnumerable<RomValidationReport>> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default);
    Task<IEnumerable<RomValidationReport>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RomValidationReport report, CancellationToken ct = default);
    Task UpdateAsync(RomValidationReport report, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
