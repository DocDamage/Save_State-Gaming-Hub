# ROM Validation & Management - Feature 12

## Overview

Feature 12 provides comprehensive ROM validation and management capabilities including multi-hash verification, DAT file matching, duplicate detection, and integrity checking.

## Features

### 1. Multi-Hash Support
- **CRC32** - For legacy ROM database compatibility
- **MD5** - Standard verification hash
- **SHA1** - No-Intro/Redump standard
- **SHA256** - Modern secure hash (optional)

### 2. DAT File Matching
- Supports No-Intro XML format
- Supports Redump XML format
- Automatic bad dump detection
- Standardized naming suggestions

### 3. Duplicate Detection
- Detects duplicates using any hash algorithm
- Calculates wasted storage space
- Shows duplicate locations
- Batch duplicate removal

### 4. Validation Reports
- Per-ROM validation status
- Hash information storage
- DAT match results
- Issue tracking with severity levels

## User Interface

The ROM Validation features are fully integrated into the **ROMs** tab (🎮) in the main application window.

### Accessing ROM Validation
- **Navigation:** Click the "ROMs" tab or press `Ctrl+D3`
- **Auto-initialization:** The ViewModel automatically loads platforms and statistics on startup

### UI Features

#### Left Sidebar - Validation Options
- **Hash Algorithms:** Checkboxes for CRC32, MD5, SHA1
- **DAT File Matching:** Option to match against No-Intro/Redump DAT files
- **Validation Actions:**
  - ✅ Validate Selected ROM
  - ✅ Validate All ROMs
  - #️⃣ Calculate Hashes Only
  - 🔍 Find Duplicates
  - ⚠️ Identify Bad Dumps
  - 📊 View Statistics
  - 📤 Export Results

#### Main Content Areas

**Statistics Panel** (toggle with "View Statistics")
- Total ROMs count
- Validated/Verified counts
- Bad dumps count
- Corrupted ROMs count
- Duplicates count
- Wasted space calculation

**Duplicates Panel** (opens after "Find Duplicates")
- List of duplicate sets by hash
- File count and wasted space per set
- Individual file listings
- "Remove Duplicates" button per set

**Bad Dumps Panel** (opens after "Identify Bad Dumps")
- Table of bad dump ROMs
- Platform, status, issue description
- Recommended actions

**ROM Library Grid**
- Title, Platform, File, Size columns
- **Validation Status column** - shows current status
- Action buttons per row:
  - ▶️ Launch ROM
  - ℹ️ Show Details
  - ✅ Validate this ROM
  - 🏷️ Get Rename Suggestion

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Ctrl+D3` | Navigate to ROMs tab |

## CLI Commands

For power users and automation, all features are available via CLI:

### Validate a Single ROM
```bash
dotnet run --project src/SaveState.CLI -- validate rom <rom-id>
```

### Batch Validate
```bash
dotnet run --project src/SaveState.CLI -- validate batch --platform-id <guid>
```

### Find Duplicates
```bash
dotnet run --project src/SaveState.CLI -- validate duplicates --platform-id <guid>
```

### View Statistics
```bash
dotnet run --project src/SaveState.CLI -- validate stats
```

### Identify Bad Dumps
```bash
dotnet run --project src/SaveState.CLI -- validate bad-dumps --platform-id <guid>
```

### Generate Missing Games Report
```bash
dotnet run --project src/SaveState.CLI -- validate missing --platform-id <guid> --reference-dat <path>
```

### Export Results
```bash
dotnet run --project src/SaveState.CLI -- validate export --output report.html --format Html
```

## API Usage

### Validate a ROM via MediatR

```csharp
var result = await mediator.Send(new ValidateRomCommand(
    romFileId: guid,
    options: new RomValidationOptions
    {
        CalculateCrc32 = true,
        CalculateMd5 = true,
        CalculateSha1 = true,
        MatchAgainstDatFiles = true,
        DatFilePaths = new[] { "nointro.xml" }
    }));

if (result.IsSuccess)
{
    Console.WriteLine($"Status: {result.Value.Status}");
    Console.WriteLine($"SHA1: {result.Value.HashInfo?.Sha1}");
}
```

### Batch Validation

```csharp
var job = await mediator.Send(new BatchValidateRomsCommand(
    jobName: "Full Library Validation",
    platformIds: new[] { nesPlatformId, snesPlatformId },
    options: new RomValidationOptions()));
```

### Find Duplicates

```csharp
var duplicates = await mediator.Send(
    new GetDuplicateRomsQuery(platformId: nesId, hashType: HashAlgorithmType.Sha1));

foreach (var dup in duplicates.Value)
{
    Console.WriteLine($"Hash: {dup.Hash}, Count: {dup.Count}");
}
```

### Generate Missing Games Report

```csharp
var report = await mediator.Send(
    new GetMissingGamesReportQuery(platformId, "nointro_nes.dat"));

Console.WriteLine($"Completion: {report.Value.CompletionPercentage:F1}%");
Console.WriteLine($"Missing: {report.Value.MissingCount} games");
```

## Configuration

### Dependency Injection

```csharp
services.AddScoped<IRomValidationService, RomValidationService>();
services.AddScoped<IRomHashInfoRepository, RomHashInfoRepository>();
services.AddScoped<IRomValidationReportRepository, RomValidationReportRepository>();
```

### Background Service

```csharp
services.AddHostedService<RomValidationBackgroundService>();
```

Options:
```json
{
  "RomValidation": {
    "ValidateOnImport": true,
    "EnableScheduledValidation": true,
    "ValidationIntervalHours": 24,
    "EnableDuplicateScanning": true,
    "DuplicateScanIntervalHours": 168
  }
}
```

## Performance Optimizations

### 1. Parallel Hash Calculation
Multiple hash algorithms (CRC32, MD5, SHA1) are calculated in parallel using `Parallel.Invoke()`.

### 2. DAT File Caching
Parsed DAT files are cached in memory with:
- 100 MB cache limit
- 1-hour expiration
- File change detection

### 3. Buffered File Reading
Large ROM files are processed using buffered streams to minimize memory usage.

## Database Schema

### RomHashInfo Table
```sql
CREATE TABLE RomHashInfos (
    Id TEXT PRIMARY KEY,
    RomFileId TEXT NOT NULL,
    Crc32 TEXT(8),
    Md5 TEXT(32),
    Sha1 TEXT(40),
    Sha256 TEXT(64),
    CalculatedAt DATETIME NOT NULL,
    IsComplete BOOLEAN NOT NULL
);
```

### RomValidationReport Table
```sql
CREATE TABLE RomValidationReports (
    Id TEXT PRIMARY KEY,
    RomFileId TEXT NOT NULL,
    Status INTEGER NOT NULL,
    ValidatedAt DATETIME NOT NULL,
    ValidationDuration TEXT,
    SuggestedName TEXT(500)
);
```

## Architecture

```
Presentation (Avalonia)
    └── RomManagementView
        └── RomManagementViewModel
            ├── ValidateSelectedRomCommand
            ├── ValidateAllRomsCommand
            ├── FindDuplicatesCommand
            ├── IdentifyBadDumpsCommand
            └── ExportValidationResultsCommand

Application (CQRS)
    ├── Commands: ValidateRom, BatchValidate, ExportResults
    │   └── RemoveDuplicateRoms, RenameRomToStandard, CalculateRomHashes
    └── Queries: GetDuplicates, GetStatistics, GetBadDumps
        └── GetRenameSuggestions, GetMissingGamesReport

Infrastructure
    ├── RomValidationService
    ├── RomHashInfoRepository
    ├── RomValidationReportRepository
    ├── RomValidationBackgroundService (Hosted)
    ├── DatFileCache
    └── ParallelHashCalculator

Core (Domain)
    ├── RomHashInfo
    ├── RomValidationReport
    ├── DuplicateRomInfo
    ├── BadDumpInfo
    ├── DatFileEntry
    └── IRomValidationService
```

## Testing

### Run Tests
```bash
dotnet test tests/SaveState.Core.Tests --filter "FullyQualifiedName~RomValidation"
dotnet test tests/SaveState.Application.Tests --filter "FullyQualifiedName~RomValidation"
dotnet test tests/SaveState.Infrastructure.Tests --filter "FullyQualifiedName~RomValidation"
```

### Test Coverage
- **91 tests** covering all validation scenarios
  - Core Tests: 57
  - Application Tests: 18
  - Infrastructure Tests: 16
- 100% pass rate
- Tests for hash calculation, DAT matching, duplicates, bad dumps, rename suggestions

## Troubleshooting

### DAT File Not Found
Ensure the DAT file path is absolute and the file exists:
```csharp
var datPath = Path.GetFullPath("dat/nointro_nes.dat");
```

### Hash Calculation Slow
- Enable parallel processing (default)
- Use SSD for ROM storage
- Consider skipping SHA256 for large files

### Memory Issues
- Reduce batch size for validation
- Clear DAT file cache periodically
- Use buffered file streams

## License

This feature is part of SaveStateReborn and follows the project's license.
