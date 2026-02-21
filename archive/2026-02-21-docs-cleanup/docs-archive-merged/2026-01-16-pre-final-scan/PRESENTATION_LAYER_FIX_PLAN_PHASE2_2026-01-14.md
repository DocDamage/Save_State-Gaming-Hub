# Presentation Layer Build Error Fix Plan - Phase 2 (LLM-Ready)

**Date**: 2026-01-14 (Updated 9:29 PM)
**Current Errors**: 138
**Target**: 0 errors
**Estimated Time**: ~2 hours

---

## 🤖 INSTRUCTIONS FOR AI ASSISTANT

This document contains exact, copy-paste ready fixes. Execute in order. After each Phase, run:

```powershell
dotnet build SaveStateReborn.sln 2>&1 | Select-String "error CS" | Measure-Object
```

**CRITICAL: Execute Phases 1-6 in exact order. Each phase depends on the previous.**

---

## 📊 CURRENT ERROR SUMMARY

| File | Errors | Fix Phase |
|------|--------|-----------|
| `MoveCreationViewModel.cs` | 34 | Phase 2 |
| `MugenHubViewModel.cs` | 28 | Phase 3 |
| `AudioOptimizationViewModel.cs` | 14 | Phase 4 |
| `MugenRosterViewModel.cs` | 10 | Phase 2 |
| `AnalyticsDashboardViewModel.cs` | 8 | Phase 5 |
| `DialogService.cs` | 6 | Phase 6 |
| `MachineLearningViewModel.cs` | 6 | Phase 6 |
| Other (12 files) | 32 | Phase 6 |

---

## PHASE 1: EXPAND CORE TYPES (Prerequisite for all other fixes)

### Task 1.1: Expand MugenMoveEntry ValueObject

**File**: `src/SaveState.Core/Mugen/ValueObjects/MugenMoveEntry.cs`

**CURRENT CONTENT** (entire file):

```csharp
namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a single command/move entry extracted from a character CMD file.
/// </summary>
public sealed record MugenMoveEntry(string Name, string Command, string? Comment);
```

**REPLACE WITH** (entire file):

```csharp
namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a single command/move entry extracted from a character CMD file.
/// </summary>
public sealed record MugenMoveEntry(
    string Name,
    string Command,
    string? Comment = null,
    string Type = "",
    int Damage = 0,
    int Startup = 0,
    int Active = 0,
    int Recovery = 0,
    int BlockAdvantage = 0,
    int HitAdvantage = 0,
    string Properties = "",
    string? Notes = null)
{
    /// <summary>Alias for Name, used by Presentation layer.</summary>
    public string MoveName => Name;
}
```

**VERIFICATION**: `dotnet build src/SaveState.Core` should pass.

---

### Task 1.2: Expand MugenCompatibilityIssue and MugenCompatibilityFix

**File**: `src/SaveState.Core/Mugen/ValueObjects/MugenCompatibilityReport.cs`

**CURRENT CONTENT** (entire file):

```csharp
namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents compatibility analysis and applied fixes for a character.
/// </summary>
public sealed record MugenCompatibilityReport(
    IReadOnlyList<MugenCompatibilityIssue> Issues,
    IReadOnlyList<MugenCompatibilityFix> Fixes);

public sealed record MugenCompatibilityIssue(string Code, string Message);

public sealed record MugenCompatibilityFix(string Code, string Message);
```

**REPLACE WITH** (entire file):

```csharp
namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents compatibility analysis and applied fixes for a character.
/// </summary>
public sealed record MugenCompatibilityReport(
    IReadOnlyList<MugenCompatibilityIssue> Issues,
    IReadOnlyList<MugenCompatibilityFix> Fixes);

/// <summary>
/// Represents a compatibility issue found during analysis.
/// </summary>
public sealed record MugenCompatibilityIssue(
    string Code,
    string Message,
    string IssueType = "Compatibility",
    string Severity = "Medium",
    string? SuggestedFix = null)
{
    /// <summary>Alias for Message, used by Presentation layer.</summary>
    public string Description => Message;
}

/// <summary>
/// Represents a compatibility fix that was applied.
/// </summary>
public sealed record MugenCompatibilityFix(
    string Code,
    string Message,
    string FixType = "AutoFix",
    bool Success = true,
    string? Details = null)
{
    /// <summary>Alias for Message, used by Presentation layer.</summary>
    public string Description => Message;
}
```

**VERIFICATION**: `dotnet build src/SaveState.Core` should pass.

---

### Task 1.3: Verify Core AudioProfile/AudioSettings Types

**File**: `src/SaveState.Core/Performance/Services/IAudioOptimizer.cs`

Check that these types exist with these properties (they should already exist):

```csharp
public record AudioProfile(
    Guid Id,                    // NOT ProfileId
    string Name,                // NOT ProfileName
    AudioSettings Settings,
    DateTime CreatedAt);

public record AudioSettings(
    int SampleRate,
    int BitDepth,
    int BufferSize,
    int Channels,
    bool ExclusiveMode,
    bool SpatialAudio,
    AudioLatencyMode LatencyMode,
    string? PreferredDeviceId);

public enum AudioLatencyMode
{
    Default,
    Low,
    UltraLow    // NOT Ultra
}
```

If the Core types have `Id` and `Name` (not `ProfileId`/`ProfileName`), you need to update the Presentation references in Phase 4.

---

## PHASE 2: FIX MoveCreationViewModel (34 errors)

**File**: `src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs`

### Task 2.1: Change Using Statements (Lines 1-12)

**CURRENT** (lines 1-12):

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;
// Use ValueObjects as canonical type for ambiguous MugenMoveEntry
using MugenMoveEntry = SaveState.Core.Mugen.ValueObjects.MugenMoveEntry;
```

**REPLACE WITH**:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Core.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;
```

**REASON**: Use Core DTOs instead of Application DTOs. Remove the alias since we'll use the expanded MugenMoveEntry now.

---

### Task 2.2: Change Type References

**FIND AND REPLACE** throughout the file:

| Find | Replace With |
|------|--------------|
| `MugenCharacterSummaryDto` | `MugenCharacterSummary` |

This affects:

- Line 77: `private MugenCharacterSummaryDto? _selectedCharacter;`
- Line 82: `public ObservableCollection<MugenCharacterSummaryDto> AvailableCharacters`
- Line 131: `partial void OnSelectedCharacterChanged(MugenCharacterSummaryDto? value)`

---

### Task 2.3: Fix Line 152 Type Mismatch

**CURRENT** (line ~146-154):

```csharp
var result = await _moveCreationService.GetCharacterMovesAsync(SelectedCharacter.Id);
if (result.IsSuccess && result.Value != null)
{
    ExistingMoves.Clear();
    foreach (var move in result.Value)
    {
        ExistingMoves.Add(move);
    }
}
```

The service returns `MugenMoveEntryDto` but `ExistingMoves` is `ObservableCollection<MugenMoveEntry>`.

**FIX**: Change the collection type at line 83:

**CURRENT**:

```csharp
public ObservableCollection<MugenMoveEntry> ExistingMoves { get; } = new();
```

**REPLACE WITH**:

```csharp
public ObservableCollection<MugenMoveEntryDto> ExistingMoves { get; } = new();
```

Also change line 80:

```csharp
private MugenMoveEntryDto? _selectedMove;
```

And line 162:

```csharp
partial void OnSelectedMoveChanged(MugenMoveEntryDto? value)
```

---

### Task 2.4: OR Alternative - Keep MugenMoveEntry and Map

If you prefer to keep `MugenMoveEntry`, map the DTO in the loop:

```csharp
foreach (var move in result.Value)
{
    ExistingMoves.Add(new MugenMoveEntry(
        Name: move.MoveName,
        Command: move.Command,
        Comment: move.Notes,
        Type: move.Type,
        Damage: move.Damage,
        Startup: move.Startup,
        Active: move.Active,
        Recovery: move.Recovery,
        BlockAdvantage: move.BlockAdvantage,
        HitAdvantage: move.HitAdvantage,
        Properties: move.Properties,
        Notes: move.Notes));
}
```

---

## PHASE 3: FIX MugenHubViewModel (28 errors)

**File**: `src/SaveState.Presentation/ViewModels/Shell/MugenHubViewModel.cs`

### Task 3.1: Fix MugenCompatibilityIssue Construction (Lines 464-468)

**CURRENT**:

```csharp
foreach (var issue in result.Value.Issues)
    CompatibilityIssues.Add(new MugenCompatibilityIssue(
        issue.IssueType,
        issue.Description,
        issue.Severity,
        issue.SuggestedFix));
```

**ISSUE**: The constructor order doesn't match. Core type is: `(Code, Message, IssueType, Severity, SuggestedFix)`

**REPLACE WITH**:

```csharp
foreach (var issue in result.Value.Issues)
    CompatibilityIssues.Add(new MugenCompatibilityIssue(
        Code: issue.Code,
        Message: issue.Message,
        IssueType: issue.IssueType,
        Severity: issue.Severity,
        SuggestedFix: issue.SuggestedFix));
```

**OR** if `issue` already IS a `MugenCompatibilityIssue`, just add it directly:

```csharp
foreach (var issue in result.Value.Issues)
    CompatibilityIssues.Add(issue);
```

---

### Task 3.2: Fix MugenCompatibilityFix Construction (Lines 511-516)

**CURRENT**:

```csharp
foreach (var fix in result.Value.Fixes)
    CompatibilityFixes.Add(new MugenCompatibilityFix(
        fix.FixType,
        fix.Description,
        fix.Success,
        fix.Details));
```

**REPLACE WITH**:

```csharp
foreach (var fix in result.Value.Fixes)
    CompatibilityFixes.Add(new MugenCompatibilityFix(
        Code: fix.Code,
        Message: fix.Message,
        FixType: fix.FixType,
        Success: fix.Success,
        Details: fix.Details));
```

**OR** if `fix` already IS a `MugenCompatibilityFix`, just add it directly:

```csharp
foreach (var fix in result.Value.Fixes)
    CompatibilityFixes.Add(fix);
```

---

### Task 3.3: Fix Second MugenCompatibilityIssue (Lines 518-523)

Apply same fix as Task 3.1.

---

### Task 3.4: Add MugenEloRating Type If Missing

Check if `MugenEloRating` exists. If not, add at end of file or in a separate file:

```csharp
public class MugenEloRating
{
    public string CharacterName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}
```

**OR** check if it exists in Core and import it.

---

## PHASE 4: FIX AudioOptimizationViewModel (14 errors)

**File**: `src/SaveState.Presentation/ViewModels/Settings/AudioOptimizationViewModel.cs`

### Task 4.1: DELETE Local Types (Lines 339-370)

**DELETE** these lines at the end of the file:

```csharp
/// <summary>
/// Represents an audio profile.
/// </summary>
public record AudioProfile(
    Guid ProfileId,
    string ProfileName,
    AudioSettings Settings,
    DateTime CreatedAt);

/// <summary>
/// Audio settings configuration.
/// </summary>
public record AudioSettings(
    int SampleRate,
    int BitDepth,
    int BufferSize,
    int Channels,
    bool ExclusiveMode,
    bool SpatialAudio,
    AudioLatencyMode LatencyMode,
    string? PreferredDeviceId);

/// <summary>
/// Audio latency modes.
/// </summary>
public enum AudioLatencyMode
{
    Default,
    Low,
    Ultra
}
```

---

### Task 4.2: Verify Using Statement

Confirm line 5 has:

```csharp
using SaveState.Core.Performance.Services;
```

This imports the Core `AudioProfile`, `AudioSettings`, and `AudioLatencyMode`.

---

### Task 4.3: Fix Property Name Mismatches

**FIND AND REPLACE**:

| Find | Replace With | Lines |
|------|--------------|-------|
| `ProfileId` | `Id` | 193 |
| `ProfileName` | `Name` | 194, 220 |
| `AudioLatencyMode.Ultra` | `AudioLatencyMode.UltraLow` | (if used) |

---

### Task 4.4: Fix Missing ApplyCurrentSettingsAsync Method

**Line 236** calls `ApplyCurrentSettingsAsync()` but the method is named `ApplyCurrentSettings()`.

**Option A**: Rename the method:

```csharp
[RelayCommand]
public async Task ApplyCurrentSettingsAsync()  // Add Async suffix
```

**Option B**: Change the call:

```csharp
await ApplyCurrentSettings();  // Remove Async suffix from call
```

**Apply same fix to line 284**.

---

## PHASE 5: FIX AnalyticsDashboardViewModel (8 errors)

**File**: `src/SaveState.Presentation/ViewModels/Analytics/AnalyticsDashboardViewModel.cs`

### Task 5.1: Fix CompletionPrediction Property Names

**FIND AND REPLACE**:

| Find | Replace With |
|------|--------------|
| `.GameTitle` | `.GameName` |
| `.EstimatedTimeToCompletion` | `.EstimatedTimeRemaining` |
| `.ConfidenceLevel` | `.ConfidenceScore` |

---

## PHASE 6: FIX REMAINING FILES (44 errors)

### Task 6.1: DialogService.cs (6 errors)

**File**: `src/SaveState.Presentation/Services/DialogService.cs`

1. Add using if missing:

   ```csharp
   using SaveState.Presentation.ViewModels.Emulators;
   ```

2. Fix MacroPlaybackService signature (line ~1050):

   ```csharp
   // CURRENT:
   await _macroService.PlayMacroAsync(macroId, cancellationToken);

   // REPLACE WITH:
   await _macroService.PlayMacroAsync(macroId, new MacroPlaybackOptions(), cancellationToken);
   ```

---

### Task 6.2: MachineLearningViewModel.cs (6 errors)

**File**: `src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs`

1. DELETE local `TrainingModel` class if it exists at end of file
2. ADD using: `using SaveState.Core.Mugen.DTOs;`
3. Fix method name if needed: `PredictMatchOutcomeAsync` → `PredictMatchAsync`

---

### Task 6.3: EmulatorEditorDialogViewModel.cs (4 errors)

**File**: `src/SaveState.Presentation/ViewModels/Dialogs/EmulatorEditorDialogViewModel.cs`

1. Fix line 96: `emulator.DisplayName` → `emulator.Name`
2. Fix line 120: Add `.ToString()` for char to string conversion

---

### Task 6.4: EmulatorEditorDialog.axaml.cs (2 errors)

**File**: `src/SaveState.Presentation/Views/Dialogs/EmulatorEditorDialog.axaml.cs`

Wrap DevTools in debug conditional:

```csharp
#if DEBUG
    this.AttachDevTools();
#endif
```

---

### Task 6.5: GameDetailViewModel.cs, GameOverviewTabViewModel.cs, QuickSearchViewModel.cs, OverlayContainerViewModel.cs (10 errors total)

Fix `GameId` to `Game?` parameter conversion. Either:

1. Change method signature to accept `GameId`
2. Or resolve `Game` from `GameId` before calling

---

### Task 6.6: MacroMarketplaceViewModel.cs (2 errors)

**File**: `src/SaveState.Presentation/ViewModels/Automation/MacroMarketplaceViewModel.cs`

Line ~196: Access `.Value` before iterating:

```csharp
// CURRENT:
foreach (var macro in result)

// REPLACE WITH:
foreach (var macro in result.Value)
```

---

### Task 6.7: MemoryMonitorOverlayViewModel.cs (2 errors)

**File**: `src/SaveState.Presentation/ViewModels/Overlays/MemoryMonitorOverlayViewModel.cs`

Add using alias:

```csharp
using MemoryDataType = SaveState.Core.Performance.ValueObjects.MemoryDataType;
```

---

### Task 6.8: Program.cs (4 errors)

**File**: `src/SaveState.Presentation/Program.cs`

Fix DI registrations:

```csharp
// CURRENT:
builder.Services.AddTransient<IMatchDataRepository, MatchDataRepository>();
builder.Services.AddTransient<ICharacterDataRepository, CharacterDataRepository>();

// REPLACE WITH:
builder.Services.AddTransient<IMatchDataRepository, SaveState.Infrastructure.Mugen.Repositories.MugenMatchDataRepository>();
builder.Services.AddTransient<ICharacterDataRepository, SaveState.Infrastructure.Mugen.Repositories.MugenCharacterDataRepository>();
```

---

### Task 6.9: MugenTournamentViewModel.cs (4 errors)

**File**: `src/SaveState.Presentation/ViewModels/Shell/Mugen/MugenTournamentViewModel.cs`

Fix constructor arguments at line ~139 to match `MugenTournament` record definition.

---

### Task 6.10: PerformanceDashboardViewModel.cs (2 errors)

**File**: `src/SaveState.Presentation/ViewModels/Shell/PerformanceDashboardViewModel.cs`

DELETE local `OperationMetrics` record if it exists, use Core version.

---

## ✅ VERIFICATION CHECKLIST

After completing all phases:

```powershell
# 1. Clean and rebuild
dotnet clean SaveStateReborn.sln
dotnet build SaveStateReborn.sln

# 2. Check error count (should be 0)
dotnet build SaveStateReborn.sln 2>&1 | Select-String "error CS" | Measure-Object

# 3. Run tests
dotnet test SaveStateReborn.sln --no-build
```

---

## 🔄 CASCADING IMPACT SUMMARY

| Core Type Change | Infrastructure Impact | Presentation Impact |
|------------------|----------------------|---------------------|
| `MugenMoveEntry` expanded | ✅ None (uses defaults) | ✅ Fixes 34 errors |
| `MugenCompatibilityIssue` expanded | ⚠️ Optional: add new fields | ✅ Fixes 28 errors |
| `MugenCompatibilityFix` expanded | ⚠️ Optional: add new fields | ✅ Fixes 28 errors |
| Delete Presentation `AudioProfile` | ✅ None | ✅ Fixes 14 errors |

---

## 📁 FILES TO MODIFY (Summary)

### Core Layer (2 files)

1. `src/SaveState.Core/Mugen/ValueObjects/MugenMoveEntry.cs` - EXPAND
2. `src/SaveState.Core/Mugen/ValueObjects/MugenCompatibilityReport.cs` - EXPAND

### Presentation Layer (15+ files)

1. `MoveCreationViewModel.cs` - MAJOR CHANGES
2. `MugenHubViewModel.cs` - MAJOR CHANGES
3. `AudioOptimizationViewModel.cs` - DELETE local types
4. `MugenRosterViewModel.cs` - Type changes
5. `AnalyticsDashboardViewModel.cs` - Property renames
6. `DialogService.cs` - Using + signature
7. `MachineLearningViewModel.cs` - DELETE local type
8. `EmulatorEditorDialogViewModel.cs` - Property renames
9. `EmulatorEditorDialog.axaml.cs` - DEBUG wrap
10. `GameDetailViewModel.cs` - Parameter fix
11. `MacroMarketplaceViewModel.cs` - .Value access
12. `MemoryMonitorOverlayViewModel.cs` - Using alias
13. `Program.cs` - DI registrations
14. `MugenTournamentViewModel.cs` - Constructor args
15. `PerformanceDashboardViewModel.cs` - DELETE local type
