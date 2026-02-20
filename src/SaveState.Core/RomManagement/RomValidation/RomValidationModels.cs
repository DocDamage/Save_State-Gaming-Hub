using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaveState.Core.RomManagement.RomValidation;

/// <summary>
/// Represents comprehensive hash information for a ROM file.
/// Supports multiple hash algorithms for compatibility with various databases.
/// </summary>
public class RomHashInfo : EntityBase
{
    /// <summary>
    /// Associated ROM file ID.
    /// </summary>
    public Guid RomFileId { get; set; }

    /// <summary>
    /// CRC32 hash (common in older ROM databases).
    /// </summary>
    public string? Crc32 { get; set; }

    /// <summary>
    /// MD5 hash (standard verification).
    /// </summary>
    public string? Md5 { get; set; }

    /// <summary>
    /// SHA1 hash (No-Intro/Redump standard).
    /// </summary>
    public string? Sha1 { get; set; }

    /// <summary>
    /// SHA256 hash (modern verification).
    /// </summary>
    public string? Sha256 { get; set; }

    /// <summary>
    /// When the hashes were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// Time taken to calculate hashes.
    /// </summary>
    public TimeSpan CalculationTime { get; set; }

    /// <summary>
    /// Whether all hash algorithms completed successfully.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Any errors encountered during hashing.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a new RomHashInfo with calculated hashes.
    /// </summary>
    public static RomHashInfo Create(
        Guid romFileId,
        string? crc32 = null,
        string? md5 = null,
        string? sha1 = null,
        string? sha256 = null)
    {
        return new RomHashInfo
        {
            RomFileId = romFileId,
            Crc32 = crc32?.ToLowerInvariant(),
            Md5 = md5?.ToLowerInvariant(),
            Sha1 = sha1?.ToLowerInvariant(),
            Sha256 = sha256?.ToLowerInvariant(),
            IsComplete = !string.IsNullOrEmpty(sha1) || !string.IsNullOrEmpty(md5),
            CalculatedAt = SystemTimeProvider.Instance.UtcNow
        };
    }

    /// <summary>
    /// Checks if this hash matches any hash in the provided database entry.
    /// </summary>
    public bool Matches(DatFileEntry entry)
    {
        if (!string.IsNullOrEmpty(Crc32) && !string.IsNullOrEmpty(entry.Crc32))
            return Crc32.Equals(entry.Crc32, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(Md5) && !string.IsNullOrEmpty(entry.Md5))
            return Md5.Equals(entry.Md5, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(Sha1) && !string.IsNullOrEmpty(entry.Sha1))
            return Sha1.Equals(entry.Sha1, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    /// <summary>
    /// Gets the best available hash for display.
    /// </summary>
    public string GetPrimaryHash()
    {
        return Sha1 ?? Md5 ?? Crc32 ?? Sha256 ?? string.Empty;
    }
}

/// <summary>
/// Hash algorithms supported for ROM verification.
/// </summary>
public enum HashAlgorithmType
{
    Crc32,
    Md5,
    Sha1,
    Sha256
}

/// <summary>
/// Represents a matched entry from a DAT file (No-Intro, Redump, etc.).
/// </summary>
public class DatFileEntry
{
    /// <summary>
    /// Game/ROM name from the DAT file.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Standardized game title.
    /// </summary>
    public string? GameTitle { get; set; }

    /// <summary>
    /// Region code (USA, EUR, JPN, etc.).
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Language codes.
    /// </summary>
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// CRC32 hash.
    /// </summary>
    public string? Crc32 { get; set; }

    /// <summary>
    /// MD5 hash.
    /// </summary>
    public string? Md5 { get; set; }

    /// <summary>
    /// SHA1 hash.
    /// </summary>
    public string? Sha1 { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Source DAT file name.
    /// </summary>
    public string SourceDat { get; set; } = string.Empty;

    /// <summary>
    /// DAT file version/date.
    /// </summary>
    public string? DatVersion { get; set; }

    /// <summary>
    /// Whether this is a verified good dump.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Clone/variant information.
    /// </summary>
    public string? CloneOf { get; set; }

    /// <summary>
    /// ROM status (good dump, bad dump, etc.).
    /// </summary>
    public RomDumpStatus DumpStatus { get; set; } = RomDumpStatus.Good;

    /// <summary>
    /// Additional notes about this ROM.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// ROM dump status classifications.
/// </summary>
public enum RomDumpStatus
{
    Good,
    Bad,
    Verified,
    Overdump,
    Underdump,
    Corrupt,
    Unknown
}

/// <summary>
/// Result of matching a ROM against a DAT file.
/// </summary>
public class RomMatchResult
{
    /// <summary>
    /// Whether a match was found.
    /// </summary>
    public bool IsMatchFound { get; set; }

    /// <summary>
    /// The matched DAT entry (if found).
    /// </summary>
    public DatFileEntry? MatchedEntry { get; set; }

    /// <summary>
    /// Confidence level of the match.
    /// </summary>
    public MatchConfidence Confidence { get; set; }

    /// <summary>
    /// Alternative matches (similar ROMs).
    /// </summary>
    [NotMapped]
    public List<DatFileEntry> AlternativeMatches { get; set; } = new();

    /// <summary>
    /// Whether the ROM is considered a good dump.
    /// </summary>
    public bool IsGoodDump => MatchedEntry?.DumpStatus == RomDumpStatus.Good ||
                              MatchedEntry?.DumpStatus == RomDumpStatus.Verified;

    /// <summary>
    /// Match date.
    /// </summary>
    public DateTime MatchedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// Source DAT database used for matching.
    /// </summary>
    public string? SourceDat { get; set; }
}

/// <summary>
/// Match confidence levels.
/// </summary>
public enum MatchConfidence
{
    Exact,      // All hashes match
    High,       // Primary hash matches
    Medium,     // Secondary hash matches
    Low,        // Size and name match
    None        // No match
}

/// <summary>
/// Comprehensive validation report for a ROM file.
/// </summary>
public class RomValidationReport : EntityBase
{
    /// <summary>
    /// Associated ROM file ID.
    /// </summary>
    public Guid RomFileId { get; set; }

    /// <summary>
    /// Validation status.
    /// </summary>
    public ValidationStatus Status { get; set; }

    /// <summary>
    /// Hash information ID.
    /// </summary>
    public Guid? HashInfoId { get; set; }

    /// <summary>
    /// Hash information.
    /// </summary>
    public RomHashInfo? HashInfo { get; set; }

    /// <summary>
    /// DAT file matching result.
    /// </summary>
    [NotMapped]
    public RomMatchResult? MatchResult { get; set; }

    /// <summary>
    /// When validation was performed.
    /// </summary>
    public DateTime ValidatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// Validation duration.
    /// </summary>
    public TimeSpan ValidationDuration { get; set; }

    /// <summary>
    /// Issues found during validation.
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Recommended actions.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Whether the ROM passed all validation checks.
    /// </summary>
    public bool IsValid => Status == ValidationStatus.Valid ||
                           Status == ValidationStatus.Verified;

    /// <summary>
    /// Suggested standardized name based on DAT match.
    /// </summary>
    public string? SuggestedName { get; set; }
}

/// <summary>
/// ROM validation status.
/// </summary>
public enum ValidationStatus
{
    Pending,
    Validating,
    Valid,
    Invalid,
    Verified,
    Corrupted,
    Unknown,
    BadDump
}

/// <summary>
/// Individual validation issue.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Issue severity.
    /// </summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>
    /// Issue category.
    /// </summary>
    public IssueCategory Category { get; set; }

    /// <summary>
    /// Issue description.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Suggested fix.
    /// </summary>
    public string? SuggestedFix { get; set; }
}

/// <summary>
/// Issue severity levels.
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Issue categories.
/// </summary>
public enum IssueCategory
{
    Hash,
    File,
    Metadata,
    Database,
    Format,
    Size,
    Unknown
}

/// <summary>
/// Duplicate ROM detection result.
/// </summary>
public class DuplicateRomInfo
{
    /// <summary>
    /// Hash that identifies these ROMs as duplicates.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Hash algorithm used.
    /// </summary>
    public HashAlgorithmType HashType { get; set; }

    /// <summary>
    /// ROM files with this hash.
    /// </summary>
    public List<RomDuplicateEntry> Duplicates { get; set; } = new();

    /// <summary>
    /// Number of duplicates.
    /// </summary>
    public int Count => Duplicates.Count;

    /// <summary>
    /// Whether duplicates are in different locations.
    /// </summary>
    public bool AreInDifferentLocations => Duplicates.Select(d => d.Directory).Distinct().Count() > 1;

    /// <summary>
    /// Total space used by duplicates (excluding the original).
    /// </summary>
    public long WastedSpace => Duplicates.Count > 1
        ? Duplicates.Sum(d => d.FileSize) - Duplicates.Max(d => d.FileSize)
        : 0;
}

/// <summary>
/// Individual duplicate ROM entry.
/// </summary>
public class RomDuplicateEntry
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
    /// Directory containing the ROM.
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// Full file path.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// File size.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// When the ROM was added to the library.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// When the ROM was last played (if applicable).
    /// </summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>
    /// Whether this is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Whether this ROM is in a playlist/collection.
    /// </summary>
    public bool InCollection { get; set; }

    /// <summary>
    /// Suggested action for this duplicate.
    /// </summary>
    public DuplicateAction SuggestedAction { get; set; }
}

/// <summary>
/// Actions for handling duplicates.
/// </summary>
public enum DuplicateAction
{
    Keep,
    Delete,
    Move,
    Review
}

/// <summary>
/// Batch validation job.
/// </summary>
public class RomValidationJob : EntityBase
{
    /// <summary>
    /// Job name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Platform IDs to validate (empty = all platforms).
    /// </summary>
    public List<Guid> PlatformIds { get; set; } = new();

    /// <summary>
    /// Specific ROM file IDs to validate (empty = all ROMs).
    /// </summary>
    public List<Guid> RomFileIds { get; set; } = new();

    /// <summary>
    /// Current job status.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// When the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// When the job started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total ROMs to validate.
    /// </summary>
    public int TotalRoms { get; set; }

    /// <summary>
    /// Number of ROMs processed.
    /// </summary>
    public int ProcessedRoms { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage => TotalRoms > 0 ? (ProcessedRoms * 100) / TotalRoms : 0;

    /// <summary>
    /// Validation results.
    /// </summary>
    public List<RomValidationReport> Results { get; set; } = new();

    /// <summary>
    /// Errors encountered.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether to match against DAT files.
    /// </summary>
    public bool MatchAgainstDatFiles { get; set; } = true;

    /// <summary>
    /// DAT file paths to use for matching.
    /// </summary>
    public List<string> DatFilePaths { get; set; } = new();
}

/// <summary>
/// Job execution status.
/// </summary>
public enum JobStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Missing game report for a platform.
/// </summary>
public class MissingGameReport
{
    /// <summary>
    /// Platform ID.
    /// </summary>
    public Guid PlatformId { get; set; }

    /// <summary>
    /// Platform name.
    /// </summary>
    public string PlatformName { get; set; } = string.Empty;

    /// <summary>
    /// DAT file used as reference.
    /// </summary>
    public string? ReferenceDatFile { get; set; }

    /// <summary>
    /// Games in the library.
    /// </summary>
    public List<string> OwnedGames { get; set; } = new();

    /// <summary>
    /// Games missing from the library.
    /// </summary>
    public List<MissingGameEntry> MissingGames { get; set; } = new();

    /// <summary>
    /// Completion percentage.
    /// </summary>
    public decimal CompletionPercentage => TotalGames > 0
        ? (decimal)(TotalGames - MissingCount) / TotalGames * 100
        : 0;

    /// <summary>
    /// Total games in the reference DAT.
    /// </summary>
    public int TotalGames { get; set; }

    /// <summary>
    /// Number of games owned.
    /// </summary>
    public int OwnedCount => OwnedGames.Count;

    /// <summary>
    /// Number of games missing.
    /// </summary>
    public int MissingCount => MissingGames.Count;
}

/// <summary>
/// Individual missing game entry.
/// </summary>
public class MissingGameEntry
{
    /// <summary>
    /// Game name from DAT.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Region.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Whether this is a high-priority want.
    /// </summary>
    public bool IsPriority { get; set; }

    /// <summary>
    /// Whether this game has multiple versions.
    /// </summary>
    public bool HasMultipleVersions { get; set; }

    /// <summary>
    /// Alternative regions available.
    /// </summary>
    public List<string> AlternativeRegions { get; set; } = new();
}

/// <summary>
/// Options for ROM validation operations.
/// </summary>
public class RomValidationOptions
{
    /// <summary>
    /// Calculate CRC32 hash.
    /// </summary>
    public bool CalculateCrc32 { get; set; } = true;

    /// <summary>
    /// Calculate MD5 hash.
    /// </summary>
    public bool CalculateMd5 { get; set; } = true;

    /// <summary>
    /// Calculate SHA1 hash.
    /// </summary>
    public bool CalculateSha1 { get; set; } = true;

    /// <summary>
    /// Calculate SHA256 hash.
    /// </summary>
    public bool CalculateSha256 { get; set; } = false;

    /// <summary>
    /// Match against DAT files.
    /// </summary>
    public bool MatchAgainstDatFiles { get; set; } = true;

    /// <summary>
    /// Paths to DAT files for matching.
    /// </summary>
    public List<string> DatFilePaths { get; set; } = new();

    /// <summary>
    /// Skip ROMs that already have hash information.
    /// </summary>
    public bool SkipValidated { get; set; } = false;

    /// <summary>
    /// Verify file integrity (check for corruption).
    /// </summary>
    public bool VerifyFileIntegrity { get; set; } = true;
}

/// <summary>
/// Summary statistics for ROM validation.
/// </summary>
public class RomValidationStatistics
{
    /// <summary>
    /// Total ROMs in library.
    /// </summary>
    public int TotalRoms { get; set; }

    /// <summary>
    /// Number of validated ROMs.
    /// </summary>
    public int ValidatedRoms { get; set; }

    /// <summary>
    /// Number of verified ROMs (matched to DAT).
    /// </summary>
    public int VerifiedRoms { get; set; }

    /// <summary>
    /// Number of bad dumps detected.
    /// </summary>
    public int BadDumps { get; set; }

    /// <summary>
    /// Number of corrupted ROMs.
    /// </summary>
    public int CorruptedRoms { get; set; }

    /// <summary>
    /// Number of duplicate ROMs.
    /// </summary>
    public int DuplicateRoms { get; set; }

    /// <summary>
    /// Total space used by duplicates.
    /// </summary>
    public long DuplicateSpaceWasted { get; set; }

    /// <summary>
    /// ROMs by validation status.
    /// </summary>
    public Dictionary<ValidationStatus, int> RomsByStatus { get; set; } = new();

    /// <summary>
    /// Platform breakdown.
    /// </summary>
    public Dictionary<string, PlatformValidationStats> PlatformStats { get; set; } = new();

    /// <summary>
    /// Last validation run date.
    /// </summary>
    public DateTime? LastValidationRun { get; set; }

    /// <summary>
    /// Validation completion percentage.
    /// </summary>
    public decimal ValidationPercentage => TotalRoms > 0
        ? (decimal)ValidatedRoms / TotalRoms * 100
        : 0;
}

/// <summary>
/// Validation stats per platform.
/// </summary>
public class PlatformValidationStats
{
    /// <summary>
    /// Platform ID.
    /// </summary>
    public Guid PlatformId { get; set; }

    /// <summary>
    /// Platform name.
    /// </summary>
    public string PlatformName { get; set; } = string.Empty;

    /// <summary>
    /// Total ROMs.
    /// </summary>
    public int TotalRoms { get; set; }

    /// <summary>
    /// Validated ROMs.
    /// </summary>
    public int ValidatedRoms { get; set; }

    /// <summary>
    /// Verified ROMs.
    /// </summary>
    public int VerifiedRoms { get; set; }

    /// <summary>
    /// Bad dumps.
    /// </summary>
    public int BadDumps { get; set; }

    /// <summary>
    /// Completion percentage.
    /// </summary>
    public decimal CompletionPercentage => TotalRoms > 0
        ? (decimal)ValidatedRoms / TotalRoms * 100
        : 0;
}
