# Performance Logging Migration Plan

## Overview

This document tracks the migration from runtime `ILogger` extension methods to compile-time **LoggerMessage source generators** for high-performance, zero-allocation logging.

**Created**: 2026-01-07
**Status**: 🚧 In Progress
**Total Warnings**: ~3,000 CA1848/CA2254

---

## Why This Migration?

### Current Pattern (Allocates on every call)

```csharp
_logger.LogInformation("Processing game {GameId}", gameId);
```

### Target Pattern (Zero allocation, compile-time optimized)

```csharp
[LoggerMessage(Level = LogLevel.Information, Message = "Processing game {GameId}")]
private static partial void LogProcessingGame(ILogger logger, Guid gameId);

// Call site:
LogProcessingGame(_logger, gameId);
```

### Benefits

- **6x faster** logging in hot paths
- **Zero allocations** per log call
- **Compile-time validation** of message templates
- **Structured logging** support built-in

---

## Migration Statistics by Project

| Project | CA1848 Count | Priority | Status |
|---------|-------------|----------|--------|
| SaveState.Core | 2 | 🔴 Critical | ✅ Complete |
| SaveState.Application | 522 | 🔴 Critical | 🚧 Not Started |
| SaveState.Infrastructure | 1,766 | 🔴 Critical | 🚧 Not Started |
| SaveState.Presentation | 670 | 🟡 Medium | 🚧 In Progress |
| SaveState.CLI | TBD | 🟢 Low | 🚧 Not Started |
| Plugins (all) | TBD | 🟢 Low | 🚧 Not Started |
| Tests | ~22 | ⚪ Optional | 🚧 Not Started |

---

## Top 20 Files by Warning Count

| File | Warnings | Project | Status |
|------|----------|---------|--------|
| DialogService.cs | 70 | Presentation | ✅ Complete |
| OverlayService.cs | 58 | Presentation | ✅ Complete |
| GameOverviewTabViewModel.cs | 54 | Presentation | ✅ Complete |
| ToolsViewModel.cs | 54 | Presentation | ✅ Complete |
| WorkflowCommandHandlers.cs | 48 | Application | ✅ Complete |
| LiveSyncService.cs | 46 | Application | ✅ Complete |
| DataImportService.cs | 46 | Infrastructure | ✅ Complete |
| PluginManager.cs | 46 | Infrastructure | ✅ Complete |
| GameNotesTabViewModel.cs | 44 | Presentation | ✅ Complete |
| SpeechRecognitionService.cs | 40 | Infrastructure | ✅ Complete |
| GameModsTabViewModel.cs | 40 | Presentation | ✅ Complete |
| BackupCommandHandlers.cs | 40 | Application | ✅ Complete |
| MacroCommandHandlers.cs | 38 | Application | ✅ Complete |
| SystemResourceManager.cs | 38 | Infrastructure | ✅ Complete |
| AudioOptimizer.cs | 36 | Infrastructure | ✅ Complete |
| DataExportService.cs | 36 | Infrastructure | ✅ Complete |
| VoiceCommandService.cs | 34 | Infrastructure | ✅ Complete |
| BacklogService.cs | 34 | Infrastructure | ✅ Complete |
| MacroRecorderViewModel.cs | 34 | Presentation | ⬜ Not Started |
| SharedCollectionService.cs | 32 | Infrastructure | ⬜ Not Started |

---

## Migration Process for Each File

### Step 1: Make Class Partial

```csharp
// Before:
public class MyService : IMyService

// After:
public partial class MyService : IMyService
```

### Step 2: Add LoggerMessage Definitions Region

```csharp
#region LoggerMessage Definitions

[LoggerMessage(Level = LogLevel.Information, Message = "Processing started for {ItemId}")]
private static partial void LogProcessingStarted(ILogger logger, Guid itemId);

[LoggerMessage(Level = LogLevel.Warning, Message = "Item not found: {ItemId}")]
private static partial void LogItemNotFound(ILogger logger, Guid itemId);

[LoggerMessage(Level = LogLevel.Error, Message = "Processing failed for {ItemId}")]
private static partial void LogProcessingFailed(ILogger logger, Exception ex, Guid itemId);

#endregion
```

### Step 3: Replace Call Sites

```csharp
// Before:
_logger.LogInformation("Processing started for {ItemId}", itemId);

// After:
LogProcessingStarted(_logger, itemId);
```

### Naming Conventions

- **Information**: `LogXxxStarted`, `LogXxxCompleted`, `LogXxxFound`
- **Warning**: `LogXxxNotFound`, `LogXxxSkipped`, `LogXxxMissing`
- **Error**: `LogXxxFailed`, `LogXxxError`
- **Debug**: `LogXxxDebug`, `LogXxxTrace`

---

## Phase 1: Core (Priority: Critical)

### SaveState.Core

Already complete. Pattern established in:

- `MugenCharacterParser.cs` ✅

---

## Phase 2: Application Layer (Priority: Critical)

### Files to Migrate (522 warnings)

1. **WorkflowCommandHandlers.cs** (48 warnings)
   - Path: `src/SaveState.Application/Automation/Commands/WorkflowCommandHandlers.cs`
   - Status: ✅ Complete

2. **LiveSyncService.cs** (46 warnings)
   - Path: `src/SaveState.Application/RomManagement/Services/LiveSyncService.cs`
   - Status: ✅ Complete

3. **BackupCommandHandlers.cs** (40 warnings)
   - Path: `src/SaveState.Application/Automation/Commands/BackupCommandHandlers.cs`
   - Status: ✅ Complete

4. **MacroCommandHandlers.cs** (38 warnings)
   - Path: `src/SaveState.Application/Automation/Commands/MacroCommandHandlers.cs`
   - Status: ✅ Complete

5. **DetectCheatsCommandHandler.cs** (TBD warnings)
   - Path: `src/SaveState.Application/AiGaming/Commands/Handlers/DetectCheatsCommandHandler.cs`
   - Status: ⬜ Not Started

*... Continue with remaining Application files*

---

## Phase 3: Infrastructure Layer (Priority: Critical)

### Files to Migrate (1,766 warnings)

1. **DataImportService.cs** (46 warnings)
   - Path: `src/SaveState.Infrastructure/DataPortability/DataImportService.cs`
   - Status: ✅ Complete

2. **PluginManager.cs** (46 warnings)
   - Path: `src/SaveState.Infrastructure/Plugins/PluginManager.cs`
   - Status: ✅ Complete

3. **SpeechRecognitionService.cs** (40 warnings)
   - Path: `src/SaveState.Infrastructure/Ai/SpeechRecognitionService.cs`
   - Status: ✅ Complete

4. **SystemResourceManager.cs** (38 warnings)
   - Path: `src/SaveState.Infrastructure/Gaming/SystemResourceManager.cs`
   - Status: ✅ Complete

5. **AudioOptimizer.cs** (36 warnings)
   - Path: `src/SaveState.Infrastructure/Gaming/AudioOptimizer.cs`
   - Status: ✅ Complete

6. **DataExportService.cs** (36 warnings)
   - Path: `src/SaveState.Infrastructure/DataPortability/DataExportService.cs`
   - Status: ✅ Complete

7. **VoiceCommandService.cs** (34 warnings)
   - Path: `src/SaveState.Infrastructure/Ai/VoiceCommandService.cs`
   - Status: ✅ Complete

8. **BacklogService.cs** (34 warnings)
   - Path: `src/SaveState.Infrastructure/GameLibrary/BacklogService.cs`
   - Status: ✅ Complete

9. **SharedCollectionService.cs** (32 warnings)
    - Path: `src/SaveState.Infrastructure/Community/SharedCollectionService.cs`
    - Status: ⬜ Not Started

*... Continue with remaining Infrastructure files*

---

## Phase 4: Presentation Layer (Priority: Medium)

### Files to Migrate (670 warnings)

1. **DialogService.cs** (70 warnings)
   - Path: `src/SaveState.Presentation/Services/DialogService.cs`
   - Status: ✅ Complete
2. **OverlayService.cs** (58 warnings)
   - Path: `src/SaveState.Presentation/Services/OverlayService.cs`
   - Status: ✅ Complete

3. **GameOverviewTabViewModel.cs** (54 warnings)
   - Path: `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameOverviewTabViewModel.cs`
   - Status: ✅ Complete

4. **ToolsViewModel.cs** (54 warnings)
   - Path: `src/SaveState.Presentation/ViewModels/Shell/ToolsViewModel.cs`
   - Status: ✅ Complete

5. **GameNotesTabViewModel.cs** (44 warnings)
   - Path: `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameNotesTabViewModel.cs`
   - Status: ✅ Complete

6. **GameModsTabViewModel.cs** (40 warnings)
   - Path: `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameModsTabViewModel.cs`
   - Status: ✅ Complete

7. **MacroRecorderViewModel.cs** (34 warnings)
   - Path: `src/SaveState.Presentation/ViewModels/Shell/MacroRecorderViewModel.cs`
   - Status: ⬜ Not Started

*... Continue with remaining Presentation files*

---

## Phase 5: CLI & Plugins (Priority: Low)

These are lower priority as they are less performance-critical.

---

## Phase 6: Test Projects (Priority: Optional)

Consider adding `.editorconfig` rule to suppress CA1848 in test projects:

```ini
# In tests .editorconfig
[*.cs]
dotnet_diagnostic.CA1848.severity = none
dotnet_diagnostic.CA2254.severity = none
```

---

## Other Code Analysis Warnings to Address

### Quick Performance Wins (Should Fix)

| Warning | Count | Description | Fix Effort |
|---------|-------|-------------|------------|
| CA1860 | 112 | Use `.Length > 0` instead of `.Any()` | Easy |
| CA1861 | 178 | Use static readonly arrays | Easy |
| CA1866 | 24 | Use `string.StartsWith(char)` | Easy |
| CA1847 | ~20 | Use `string.Contains(char)` | Easy |
| CA1850 | ~10 | Use static `SHA256.HashData` | Easy |

### Style/Naming (Should Suppress in Tests)

| Warning | Count | Description | Action |
|---------|-------|-------------|--------|
| CA1707 | 1,108 | Underscores in test names | Suppress in tests |

### Other Improvements

| Warning | Count | Description | Fix Effort |
|---------|-------|-------------|------------|
| CA1725 | 290 | Parameter name mismatch | Medium |
| CA1305 | 184 | Culture-aware formatting | Medium |
| CA1310 | 88 | StringComparison | Medium |
| CA1852 | 62 | Seal types | Easy |
| CA2016 | 68 | Forward CancellationToken | Medium |

---

## Commands for Tracking Progress

### Count Remaining CA1848 Warnings

```powershell
dotnet build --no-incremental 2>&1 | Select-String -Pattern "warning CA1848" | Measure-Object
```

### Count by Project

```powershell
dotnet build --no-incremental 2>&1 | Out-File build.txt
Get-Content build.txt | Select-String -Pattern "warning CA1848" | `
  ForEach-Object { if ($_.Line -match '\\src\\([^\\]+)\\') { $matches[1] } } | `
  Group-Object | Sort-Object Count -Descending
```

### Count by File

```powershell
Get-Content build.txt | Select-String -Pattern "warning CA1848" | `
  ForEach-Object { if ($_.Line -match '([^\\]+\.cs)\(\d+') { $matches[1] } } | `
  Group-Object | Sort-Object Count -Descending | Select-Object -First 20
```

---

## Completion Tracking

### Summary

- **Total Files to Migrate**: ~150+
- **Estimated Time per File**: 10-30 minutes depending on complexity
- **Total Estimated Effort**: 40-80 hours

### Progress Log

| Date | Files Completed | Warnings Fixed | Notes |
|------|-----------------|----------------|-------|
| 2026-01-07 | MugenCharacterParser.cs | 1 | Initial pattern established |
| 2026-01-07 | DialogService.cs (partial) | ~10 | LoggerMessage definitions added |
| 2026-01-07 | DialogService.cs | 70 | Complete migration |
| 2026-01-07 | WorkflowCommandHandlers.cs | 48 | Complete migration |
| 2026-01-07 | LiveSyncService.cs | 46 | Complete migration |
| 2026-01-07 | DataImportService.cs | 46 | Complete migration |
| 2026-01-07 | PluginManager.cs | 46 | Complete migration |
| 2026-01-07 | BackupCommandHandlers.cs | 40 | Complete migration |
| 2026-01-07 | OverlayService.cs | 58 | Complete migration |
| 2026-01-07 | GameOverviewTabViewModel.cs | 54 | Complete migration |
| 2026-01-07 | ToolsViewModel.cs | 54 | Complete migration |
| 2026-01-07 | GameNotesTabViewModel.cs | 44 | Complete migration |
| 2026-01-07 | SpeechRecognitionService.cs | 40 | Complete migration |
| 2026-01-07 | GameModsTabViewModel.cs | 40 | Complete migration |
| 2026-01-07 | MacroCommandHandlers.cs | 38 | Complete migration |
| 2026-01-07 | SystemResourceManager.cs | 38 | Complete migration |
| 2026-01-07 | AudioOptimizer.cs | 36 | Complete migration |
| 2026-01-07 | DataExportService.cs | 36 | Complete migration |
| 2026-01-07 | VoiceCommandService.cs | 34 | Complete migration |
| 2026-01-07 | BacklogService.cs | 34 | Complete migration |

---

## References

- [LoggerMessage Source Generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- [CA1848: Use LoggerMessage Delegates](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1848)
- [High-Performance Logging in .NET](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage)
