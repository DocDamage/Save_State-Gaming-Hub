# RomValidationService Refactoring Plan

## Overview

**File:** `src/SaveState.Infrastructure/RomManagement/RomValidationService.cs`  
**Current Lines:** 1,032  
**Target Lines:** ~160 lines (coordinator) + 5 managers (~150-200 lines each)  
**Pattern:** Manager Pattern with Coordinator

---

## File Statistics

| Metric | Current | Target |
|--------|---------|--------|
| Total Lines | 1,032 | ~960 (split across 6 files) |
| Public Methods | 13 | 13 (delegated) |
| Private Methods | 11 | 0 (moved to managers) |
| Hash Algorithms | 4 (CRC32, MD5, SHA1, SHA256) | 4 (in HashCalculatorManager) |
| DAT File Formats | 3 (XML, JSON, CSV) | 3 (in DatFileManager) |
| Export Formats | 5 (JSON, CSV, HTML, Markdown, DAT) | 5 (in ReportManager) |
| Responsibilities | 6 | 1 (coordinator only) |

---

## Responsibility Analysis

### Current Responsibilities (Violating SRP)

1. **Hash Calculation**
   - CRC32, MD5, SHA1, SHA256 computation
   - File reading and byte array handling
   - Hash result storage

2. **ROM Validation Orchestration**
   - File integrity verification
   - DAT file matching
   - Issue categorization and status determination

3. **Batch Processing**
   - Multi-ROM job execution
   - Progress reporting
   - Error aggregation

4. **DAT File Management**
   - XML, JSON, CSV parsing
   - Entry matching (exact and partial)
   - Region extraction from filenames

5. **Report Generation & Export**
   - Missing game reports
   - Rename suggestions
   - Bad dump identification
   - Statistics calculation
   - Multi-format export (JSON, CSV, HTML, Markdown, DAT)

6. **File Integrity Verification**
   - Header analysis (iNES, SMC, SMD)
   - Read error detection
   - Format validation

---

## Proposed Manager Classes

### 1. RomHashCalculatorManager

**Responsibility:** Multi-algorithm hash computation for ROM files

**Key Methods:**
```csharp
public sealed class RomHashCalculatorManager
{
    public async Task<Result<RomHashInfo>> CalculateHashesAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default);
    
    private string CalculateCrc32(byte[] data);
    private string CalculateMd5(byte[] data);
    private string CalculateSha1(byte[] data);
    private string CalculateSha256(byte[] data);
    private string GetHashByType(RomHashInfo hashInfo, HashAlgorithmType type);
}
```

**Dependencies:** IFileSystem

---

### 2. RomIntegrityManager

**Responsibility:** File integrity verification and ROM header analysis

**Key Methods:**
```csharp
public sealed class RomIntegrityManager
{
    public async Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(
        string filePath,
        CancellationToken ct = default);
    
    private RomHeaderInfo? AnalyzeRomHeader(byte[] data, string extension);
}
```

**Dependencies:** IFileSystem

---

### 3. DatFileManager

**Responsibility:** DAT file parsing and ROM matching

**Key Methods:**
```csharp
public sealed class DatFileManager
{
    public async Task<Result<List<DatFileEntry>>> LoadDatFileAsync(
        string datFilePath,
        CancellationToken ct = default);
    
    public async Task<Result<RomMatchResult>> MatchAgainstDatFilesAsync(
        RomHashInfo hashInfo,
        IEnumerable<string> datFilePaths,
        CancellationToken ct = default);
    
    private List<DatFileEntry> ParseXmlDat(string content, string sourcePath);
    private List<DatFileEntry> ParseJsonDat(string content, string sourcePath);
    private List<DatFileEntry> ParseCsvDat(string content, string sourcePath);
    private string? ExtractRegion(string? name);
    private bool IsPartialMatch(RomHashInfo hashInfo, DatFileEntry entry);
}
```

**Dependencies:** IFileSystem

---

### 4. RomBatchProcessor

**Responsibility:** Batch validation job execution with progress tracking

**Key Methods:**
```csharp
public sealed class RomBatchProcessor
{
    public async Task<Result<RomValidationJob>> ProcessBatchAsync(
        RomValidationJob job,
        RomValidationOptions options,
        IRomValidationService validationService,
        IProgress<RomValidationProgress>? progress = null,
        CancellationToken ct = default);
}
```

**Dependencies:** IRomFileRepository, ITimeProvider

---

### 5. RomReportManager

**Responsibility:** Report generation, duplicate detection, and export

**Key Methods:**
```csharp
public sealed class RomReportManager
{
    // Duplicate Detection
    public async Task<Result<List<DuplicateRomInfo>>> FindDuplicatesAsync(
        Guid? platformId = null,
        HashAlgorithmType? hashType = null,
        CancellationToken ct = default);
    
    // Missing Games
    public async Task<Result<MissingGameReport>> GenerateMissingGameReportAsync(
        Guid platformId,
        string referenceDatPath,
        CancellationToken ct = default);
    
    // Rename Suggestions
    public async Task<Result<List<RomRenameSuggestion>>> GetRenameSuggestionsAsync(
        Guid? platformId = null,
        CancellationToken ct = default);
    
    // Bad Dumps
    public async Task<Result<List<BadDumpInfo>>> IdentifyBadDumpsAsync(
        Guid? platformId = null,
        CancellationToken ct = default);
    
    // Statistics
    public async Task<Result<RomValidationStatistics>> GetStatisticsAsync(
        CancellationToken ct = default);
    
    // Export
    public async Task<Result<string>> ExportValidationResultsAsync(
        RomValidationExportOptions options,
        CancellationToken ct = default);
    
    // Export format helpers
    private string ExportToJson(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options);
    private string ExportToCsv(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options);
    private string ExportToHtml(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options);
    private string ExportToMarkdown(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options);
    private string ExportToDat(IEnumerable<RomValidationReport> reports, RomValidationExportOptions options);
    
    // Utility
    private static string SanitizeFileName(string name);
}
```

**Dependencies:** IRomFileRepository, IRomHashInfoRepository, IRomValidationReportRepository, IFileSystem, ITimeProvider

---

## Before/After Code Structure

### BEFORE (Current)

```csharp
public class RomValidationService : IRomValidationService
{
    private readonly IFileSystem _fileSystem;
    private readonly IRomFileRepository _romRepository;
    private readonly IRomHashInfoRepository _hashRepository;
    private readonly IRomValidationReportRepository _reportRepository;
    private readonly ILogger<RomValidationService> _logger;
    private readonly ITimeProvider _timeProvider;

    public RomValidationService(...) { ... }

    // Hash Calculation
    public async Task<Result<RomHashInfo>> CalculateHashesAsync(RomFile romFile, RomValidationOptions options, CancellationToken ct) { ... }
    
    // Single ROM Validation
    public async Task<Result<RomValidationReport>> ValidateRomAsync(RomFile romFile, RomValidationOptions options, CancellationToken ct) { ... }
    
    // Batch Processing
    public async Task<Result<RomValidationJob>> ValidateBatchAsync(RomValidationJob job, RomValidationOptions options, IProgress<RomValidationProgress>? progress, CancellationToken ct) { ... }
    
    // DAT File Operations
    public async Task<Result<RomMatchResult>> MatchAgainstDatFilesAsync(RomHashInfo hashInfo, IEnumerable<string> datFilePaths, CancellationToken ct) { ... }
    public async Task<Result<List<DatFileEntry>>> LoadDatFileAsync(string datFilePath, CancellationToken ct) { ... }
    
    // Reports
    public async Task<Result<List<DuplicateRomInfo>>> FindDuplicatesAsync(Guid? platformId, HashAlgorithmType? hashType, CancellationToken ct) { ... }
    public async Task<Result<MissingGameReport>> GenerateMissingGameReportAsync(Guid platformId, string referenceDatPath, CancellationToken ct) { ... }
    public async Task<Result<List<RomRenameSuggestion>>> GetRenameSuggestionsAsync(Guid? platformId, CancellationToken ct) { ... }
    public async Task<Result<List<BadDumpInfo>>> IdentifyBadDumpsAsync(Guid? platformId, CancellationToken ct) { ... }
    public async Task<Result<RomValidationStatistics>> GetStatisticsAsync(CancellationToken ct) { ... }
    public async Task<Result<string>> ExportValidationResultsAsync(RomValidationExportOptions options, CancellationToken ct) { ... }
    
    // Integrity
    public async Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(string filePath, CancellationToken ct) { ... }
    
    // ~11 private helper methods...
}
```

**Problems:**
- 1,032 lines in single file
- Mixes hashing, parsing, validation, and reporting
- Complex dependencies for simple operations
- Difficult to test individual components
- File I/O scattered throughout

---

### AFTER (Refactored)

#### Coordinator: RomValidationService

```csharp
public sealed class RomValidationService : IRomValidationService
{
    private readonly RomHashCalculatorManager _hashCalculator;
    private readonly RomIntegrityManager _integrityManager;
    private readonly DatFileManager _datFileManager;
    private readonly RomBatchProcessor _batchProcessor;
    private readonly RomReportManager _reportManager;
    private readonly IRomValidationReportRepository _reportRepository;
    private readonly ILogger<RomValidationService> _logger;
    private readonly ITimeProvider _timeProvider;

    public RomValidationService(
        RomHashCalculatorManager hashCalculator,
        RomIntegrityManager integrityManager,
        DatFileManager datFileManager,
        RomBatchProcessor batchProcessor,
        RomReportManager reportManager,
        IRomValidationReportRepository reportRepository,
        ILogger<RomValidationService> logger,
        ITimeProvider timeProvider)
    {
        _hashCalculator = hashCalculator;
        _integrityManager = integrityManager;
        _datFileManager = datFileManager;
        _batchProcessor = batchProcessor;
        _reportManager = reportManager;
        _reportRepository = reportRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RomHashInfo>> CalculateHashesAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Calculating hashes for ROM: {RomTitle}", romFile.Title);
        var result = await _hashCalculator.CalculateHashesAsync(romFile, options, ct).ConfigureAwait(false);
        if (result.IsSuccess)
            _logger.LogInformation("Hash calculation completed for {RomTitle}", romFile.Title);
        return result;
    }

    public async Task<Result<RomValidationReport>> ValidateRomAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating ROM: {RomTitle}", romFile.Title);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var report = new RomValidationReport
        {
            RomFileId = (Guid)romFile.Id,
            Status = ValidationStatus.Validating
        };

        // File integrity check
        if (options.VerifyFileIntegrity)
        {
            var integrityResult = await _integrityManager.VerifyFileIntegrityAsync(
                romFile.FilePath.Value, ct).ConfigureAwait(false);
            ApplyIntegrityResult(report, integrityResult);
        }

        // Hash calculation
        RomHashInfo? hashInfo = null;
        if (report.Status != ValidationStatus.Corrupted)
        {
            var hashResult = await CalculateHashesAsync(romFile, options, ct).ConfigureAwait(false);
            if (hashResult.IsSuccess)
            {
                hashInfo = hashResult.Value;
                romFile.SetChecksum(hashInfo.GetPrimaryHash());
            }
            else
            {
                AddHashError(report, hashResult.Error);
            }
        }

        report.HashInfo = hashInfo;

        // DAT matching
        if (options.MatchAgainstDatFiles && hashInfo != null && options.DatFilePaths.Any())
        {
            var matchResult = await _datFileManager.MatchAgainstDatFilesAsync(
                hashInfo, options.DatFilePaths, ct).ConfigureAwait(false);
            ApplyMatchResult(report, matchResult);
        }
        else if (report.Status != ValidationStatus.Corrupted)
        {
            report.Status = ValidationStatus.Valid;
        }

        stopwatch.Stop();
        report.ValidationDuration = stopwatch.Elapsed;
        await _reportRepository.AddAsync(report, ct).ConfigureAwait(false);

        _logger.LogInformation("Validation completed for {RomTitle} with status {Status}", 
            romFile.Title, report.Status);

        return Result<RomValidationReport>.Success(report);
    }

    public async Task<Result<RomValidationJob>> ValidateBatchAsync(
        RomValidationJob job,
        RomValidationOptions options,
        IProgress<RomValidationProgress>? progress = null,
        CancellationToken ct = default)
    {
        return await _batchProcessor.ProcessBatchAsync(job, options, this, progress, ct).ConfigureAwait(false);
    }

    public async Task<Result<RomMatchResult>> MatchAgainstDatFilesAsync(
        RomHashInfo hashInfo,
        IEnumerable<string> datFilePaths,
        CancellationToken ct = default)
    {
        return await _datFileManager.MatchAgainstDatFilesAsync(hashInfo, datFilePaths, ct).ConfigureAwait(false);
    }

    public async Task<Result<List<DatFileEntry>>> LoadDatFileAsync(
        string datFilePath,
        CancellationToken ct = default)
    {
        return await _datFileManager.LoadDatFileAsync(datFilePath, ct).ConfigureAwait(false);
    }

    public async Task<Result<List<DuplicateRomInfo>>> FindDuplicatesAsync(
        Guid? platformId = null,
        HashAlgorithmType? hashType = null,
        CancellationToken ct = default)
    {
        return await _reportManager.FindDuplicatesAsync(platformId, hashType, ct).ConfigureAwait(false);
    }

    public async Task<Result<MissingGameReport>> GenerateMissingGameReportAsync(
        Guid platformId,
        string referenceDatPath,
        CancellationToken ct = default)
    {
        return await _reportManager.GenerateMissingGameReportAsync(platformId, referenceDatPath, ct).ConfigureAwait(false);
    }

    public async Task<Result<List<RomRenameSuggestion>>> GetRenameSuggestionsAsync(
        Guid? platformId = null,
        CancellationToken ct = default)
    {
        return await _reportManager.GetRenameSuggestionsAsync(platformId, ct).ConfigureAwait(false);
    }

    public async Task<Result<List<BadDumpInfo>>> IdentifyBadDumpsAsync(
        Guid? platformId = null,
        CancellationToken ct = default)
    {
        return await _reportManager.IdentifyBadDumpsAsync(platformId, ct).ConfigureAwait(false);
    }

    public async Task<Result<RomValidationStatistics>> GetStatisticsAsync(
        CancellationToken ct = default)
    {
        return await _reportManager.GetStatisticsAsync(ct).ConfigureAwait(false);
    }

    public async Task<Result<string>> ExportValidationResultsAsync(
        RomValidationExportOptions options,
        CancellationToken ct = default)
    {
        return await _reportManager.ExportValidationResultsAsync(options, ct).ConfigureAwait(false);
    }

    public async Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(
        string filePath,
        CancellationToken ct = default)
    {
        return await _integrityManager.VerifyFileIntegrityAsync(filePath, ct).ConfigureAwait(false);
    }

    // Private helper methods for report building
    private void ApplyIntegrityResult(RomValidationReport report, Result<FileIntegrityResult> result) { ... }
    private void AddHashError(RomValidationReport report, string? error) { ... }
    private void ApplyMatchResult(RomValidationReport report, Result<RomMatchResult> matchResult) { ... }
}
```

**Benefits:**
- ~160 lines (85% reduction)
- Clear delegation pattern
- Easy to trace validation flow
- Each manager independently testable
- Logging only in coordinator

---

## New File Structure

```
src/SaveState.Infrastructure/RomManagement/
├── RomValidationService.cs                      # Coordinator (~160 lines)
├── Managers/
│   ├── RomHashCalculatorManager.cs              # Hash computation (~180 lines)
│   ├── RomIntegrityManager.cs                   # File integrity (~140 lines)
│   ├── DatFileManager.cs                        # DAT parsing/matching (~220 lines)
│   ├── RomBatchProcessor.cs                     # Batch processing (~140 lines)
│   └── RomReportManager.cs                      # Reports/export (~280 lines)
└── (existing files unchanged)
```

---

## Key Challenges and Edge Cases

### 1. Cyclic Dependency Risk

**Challenge:** `RomBatchProcessor` needs to call `ValidateRomAsync` on the service.

**Solution:** Pass service interface to batch processor:
```csharp
public sealed class RomBatchProcessor
{
    public async Task<Result<RomValidationJob>> ProcessBatchAsync(
        RomValidationJob job,
        RomValidationOptions options,
        IRomValidationService validationService,  // Interface, not concrete
        IProgress<RomValidationProgress>? progress,
        CancellationToken ct)
    {
        // Use validationService.ValidateRomAsync() for each ROM
    }
}
```

---

### 2. Shared Repository Access

**Challenge:** Multiple managers need the same repositories.

**Solution:** Accept repositories in constructor, DI handles singletons:
```csharp
// All managers receive same repository instances from DI
public RomHashCalculatorManager(IFileSystem fileSystem, IRomHashInfoRepository hashRepository) { }
public RomIntegrityManager(IFileSystem fileSystem) { }
public DatFileManager(IFileSystem fileSystem) { }
public RomReportManager(
    IRomFileRepository romRepository,
    IRomHashInfoRepository hashRepository,
    IRomValidationReportRepository reportRepository,
    IFileSystem fileSystem,
    ITimeProvider timeProvider) { }
```

---

### 3. Security Suppression Migration

**Challenge:** Hash methods have `[SuppressMessage]` attributes for MD5/SHA1.

**Solution:** Keep suppressions in manager:
```csharp
public sealed class RomHashCalculatorManager
{
    [SuppressMessage("Security", "CA5351:DoNotUseBrokenCryptographicAlgorithms", 
        Justification = "MD5 required for No-Intro/Redump ROM database compatibility")]
    [SuppressMessage("Security", "CA5350:DoNotUseWeakCryptographicAlgorithms", 
        Justification = "SHA1 required for No-Intro/Redump ROM database compatibility")]
    public async Task<Result<RomHashInfo>> CalculateHashesAsync(...) { ... }
}
```

---

### 4. DAT Parsing Complexity

**Challenge:** `ParseXmlDat` has `[SuppressMessage("Maintainability", "CA1502")]`.

**Solution:** Keep in DatFileManager or split further if needed:
```csharp
public sealed class DatFileManager
{
    [SuppressMessage("Maintainability", "CA1502:AvoidExcessiveComplexity", 
        Justification = "XML parsing requires multiple conditional checks")]
    private List<DatFileEntry> ParseXmlDat(string content, string sourcePath) { ... }
}
```

---

### 5. Error Aggregation in Batch Processing

**Challenge:** Batch processor collects individual ROM errors.

**Solution:** Maintain error collection in batch processor:
```csharp
public async Task<Result<RomValidationJob>> ProcessBatchAsync(...)
{
    for (int i = 0; i < romList.Count; i++)
    {
        try
        {
            var result = await validationService.ValidateRomAsync(rom, options, ct);
            if (result.IsSuccess)
                job.Results.Add(result.Value);
            else
                job.Errors.Add($"Failed to validate {rom.Title}: {result.Error}");
        }
        catch (Exception ex)
        {
            job.Errors.Add($"Exception validating {rom.Title}: {ex.Message}");
        }
    }
}
```

---

## Migration Steps

1. **Create RomHashCalculatorManager**
   - Move `CalculateHashesAsync` and hash calculation private methods
   - Move hash-related suppressions
   - Add unit tests

2. **Create RomIntegrityManager**
   - Move `VerifyFileIntegrityAsync` and `AnalyzeRomHeader`
   - Add unit tests

3. **Create DatFileManager**
   - Move DAT file methods and parsing logic
   - Move parsing suppressions
   - Add unit tests

4. **Create RomBatchProcessor**
   - Move `ValidateBatchAsync` and related logic
   - Add unit tests

5. **Create RomReportManager**
   - Move all report-related methods
   - Move export format methods
   - Add unit tests

6. **Refactor RomValidationService**
   - Inject managers via constructor
   - Simplify to coordination only
   - Keep logging and report building helpers

7. **Update Tests**
   - Create unit tests for each manager
   - Update integration tests
   - Verify batch processing still works

---

## Estimated Effort

| Task | Estimated Time |
|------|----------------|
| Create RomHashCalculatorManager | 3 hours |
| Create RomIntegrityManager | 2 hours |
| Create DatFileManager | 3 hours |
| Create RomBatchProcessor | 2 hours |
| Create RomReportManager | 4 hours |
| Refactor RomValidationService | 2 hours |
| Update Unit Tests | 4 hours |
| Integration Testing | 2 hours |
| **Total** | **22 hours** |

---

## Success Criteria

- [ ] RomValidationService under 200 lines
- [ ] All managers under 300 lines each
- [ ] Existing tests pass without modification
- [ ] New manager unit tests achieve 80%+ coverage
- [ ] No regression in validation accuracy
- [ ] All hash algorithms still work (CRC32, MD5, SHA1, SHA256)
- [ ] All export formats still work (JSON, CSV, HTML, Markdown, DAT)
- [ ] Build succeeds with 0 warnings
