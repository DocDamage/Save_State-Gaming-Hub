# Presentation Layer Fix Plan - Phase 4 (Actual Errors)

**Date:** 2026-01-15
**Based on:** Actual build output from `src/SaveState.Presentation`
**Total Errors:** ~70 errors across 19 files

## Executive Summary

The original Phase 2 plan was based on 18 errors that no longer exist. The actual build has approximately 70+ errors across 19 files. The errors fall into several categories:

1. **Type mismatches between DTOs and ValueObjects** - ViewModels use wrong types
2. **Missing properties on ValueObjects** - Core types don't have expected properties
3. **Missing repository types** - Program.cs references non-existent types
4. **Method signature issues** - Methods don't match expected signatures
5. **Type conversion issues** - Various implicit/explicit conversion problems

## Error Analysis

### Root Cause Analysis

| Error Category | Root Cause | Files Affected | Error Count |
|---------------|-------------|----------------|--------------|
| MugenMoveEntry type mismatch | ViewModel uses `MugenMoveEntry` (ValueObject) but service returns `MugenMoveEntryDto` | MoveCreationViewModel, MugenHubViewModel | 15+ |
| MugenCharacterSummary type mismatch | ViewModel uses obsolete `MugenCharacterSummaryDto` but service returns `MugenCharacterSummary` | MoveCreationViewModel, MachineLearningViewModel, MugenRosterViewModel | 3+ |
| Missing properties on MugenMoveEntry | ValueObject only has Name, Command, Comment but ViewModel expects MoveName, Type, Damage, etc. | MoveCreationViewModel | 10+ |
| Missing repository types | MatchDataRepository, ICharacterDataRepository, etc. don't exist in Infrastructure | Program.cs | 5 |
| Missing methods on interfaces | IMugenRosterService missing LoadRosterAsync, GetSelectDefPath, SaveRosterAsync | MugenRosterViewModel | 4 |
| GameId to Game conversion | GameId ValueObject can't be implicitly converted to Game entity | GameDetailViewModel, OverlayContainerViewModel, QuickSearchViewModel | 3 |
| Missing properties on compatibility types | MugenCompatibilityIssue and MugenCompatibilityFix missing properties | MugenHubViewModel | 8 |
| AudioSettings type mismatch | Presentation AudioSettings vs Core AudioSettings | AudioOptimizationViewModel | 5 |
| Missing methods in AudioOptimizationViewModel | ApplyCurrentSettingsAsync doesn't exist | AudioOptimizationViewModel | 2 |
| Other type mismatches | Various other conversion issues | Multiple files | 10+ |

## Fix Plan

### Phase 1: Fix MugenMoveEntry Type Mismatch (15+ errors)

**Problem:** ViewModels use `MugenMoveEntry` (ValueObject) which only has Name, Command, Comment, but services return `MugenMoveEntryDto` which has MoveName, Type, Damage, Startup, Active, Recovery, BlockAdvantage, HitAdvantage, Properties, Notes.

**Files:**

- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MugenHubViewModel.cs`

**Solution Options:**

#### Option A: Update ViewModels to use MugenMoveEntryDto (RECOMMENDED)

Change ViewModels to use `MugenMoveEntryDto` instead of `MugenMoveEntry` (ValueObject).

**Changes for MoveCreationViewModel.cs:**

```csharp
// Line 11: Change alias
// FROM: using MugenMoveEntry = SaveState.Core.Mugen.ValueObjects.MugenMoveEntry;
// TO: using MugenMoveEntry = SaveState.Core.Mugen.DTOs.MugenMoveEntryDto;

// Line 80: Change property type (already correct)
public ObservableCollection<MugenMoveEntry> ExistingMoves { get; } = new();

// Lines 166-176: Update property access
// The MugenMoveEntryDto has these properties, so this should work:
MoveName = value.MoveName;  // Was value.MoveName (but ValueObject doesn't have it)
MoveCommand = value.Command;
MoveType = value.Type;
MoveDamage = value.Damage;
Startup = value.Startup;
Active = value.Active;
Recovery = value.Recovery;
BlockAdvantage = value.BlockAdvantage;
HitAdvantage = value.HitAdvantage;
Properties = value.Properties;
Notes = value.Notes ?? string.Empty;
```

**Changes for MugenHubViewModel.cs:**

```csharp
// Line 736: Change parameter type
// FROM: MugenMoveEntry
// TO: MugenMoveEntryDto
```

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"MugenMoveEntry" /C:"MoveName" /C:"CS1061"
```

**Rollback:**

- Revert the alias change on line 11
- Revert property access changes

---

### Phase 2: Fix MugenCharacterSummary Type Mismatch (3+ errors)

**Problem:** ViewModels use obsolete `MugenCharacterSummaryDto` but services return `MugenCharacterSummary` from Core.

**Files:**

- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MugenRosterViewModel.cs`

**Solution:** Update ViewModels to use `MugenCharacterSummary` from Core instead of `MugenCharacterSummaryDto`.

**Changes for MoveCreationViewModel.cs:**

```csharp
// Line 5: Remove obsolete using
// FROM: using SaveState.Application.Mugen.DTOs;
// TO: (remove this line)

// Line 6: Add Core DTOs using
// FROM: using SaveState.Core.Mugen.Services;
// TO: using SaveState.Core.Mugen.Services;
//      using SaveState.Core.Mugen.DTOs;

// Line 77: Change property type
// FROM: private MugenCharacterSummaryDto? _selectedCharacter;
// TO: private MugenCharacterSummary? _selectedCharacter;

// Line 82: Change collection type
// FROM: public ObservableCollection<MugenCharacterSummaryDto> AvailableCharacters { get; } = new();
// TO: public ObservableCollection<MugenCharacterSummary> AvailableCharacters { get; } = new();

// Lines 93-96: Update constructor parameter types
// FROM: public MoveCreationViewModel(IMediator mediator, IMoveCreationService moveCreationService, INotificationService notificationService)
// TO: public MoveCreationViewModel(IMediator mediator, IMoveCreationService moveCreationService, INotificationService notificationService)
// (No change needed - service returns correct type now)
```

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"MugenCharacterSummary" /C:"CS1503"
```

**Rollback:**

- Revert the using statements
- Revert property type changes

---

### Phase 3: Fix Missing Repository Types in Program.cs (5 errors)

**Problem:** Program.cs references repository types that don't exist in `SaveState.Infrastructure.Mugen`.

**File:** `src/SaveState.Presentation/Program.cs`

**Errors:**

- Line 140: `MatchDataRepository` does not exist
- Line 141: `ICharacterDataRepository` not found
- Line 141: `CharacterDataRepository` does not exist
- Line 142: `IPlayerDataRepository` not found
- Line 142: `PlayerDataRepository` type mismatch

**Solution:** Remove or comment out the non-existent repository registrations.

**Changes:**

```csharp
// Lines 140-142: Comment out or remove these lines
// services.AddTransient<MatchDataRepository>();
// services.AddTransient<ICharacterDataRepository, CharacterDataRepository>();
// services.AddTransient<IPlayerDataRepository, PlayerDataRepository>();
```

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"Program.cs" /C:"CS0234" /C:"CS0246"
```

**Rollback:**

- Uncomment the lines if needed later

---

### Phase 4: Fix Missing Methods on IMugenRosterService (4 errors)

**Problem:** `IMugenRosterService` interface is missing methods that MugenRosterViewModel expects.

**File:** `src/SaveState.Core/Mugen/Services/IMugenRosterService.cs`

**Errors:**

- Line 174: `LoadRosterAsync` method not found
- Line 181: `GetSelectDefPath` method not found
- Line 188: `GetSelectDefPath` method not found
- Line 219: `SaveRosterAsync` method not found

**Solution:** Add missing methods to the interface.

**Changes:**

```csharp
// Add these methods to IMugenRosterService interface:
Task<Result<IReadOnlyList<MugenCharacterSummary>>> LoadRosterAsync(CancellationToken cancellationToken = default);
Task<Result<string>> GetSelectDefPath(CancellationToken cancellationToken = default);
Task<Result> SaveRosterAsync(IReadOnlyList<MugenCharacterSummary> roster, CancellationToken cancellationToken = default);
```

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"IMugenRosterService" /C:"CS1061"
```

**Rollback:**

- Remove the added methods from the interface

---

### Phase 5: Fix GameId to Game Conversion (3 errors)

**Problem:** `GameId` ValueObject cannot be implicitly converted to `Game` entity.

**Files:**

- `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameDetailViewModel.cs` (lines 153, 290)
- `src/SaveState.Presentation/ViewModels/Shell/OverlayContainerViewModel.cs` (line 247)
- `src/SaveState.Presentation/ViewModels/Shell/QuickSearchViewModel.cs` (line 139)

**Solution:** Change method calls to pass GameId instead of Game, or update the service methods to accept GameId.

**Example Changes for GameDetailViewModel.cs:**

```csharp
// Lines 153, 290: Need to understand what method is being called and what it expects
// This requires looking at the service method signatures
```

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"GameId" /C:"CS1503"
```

**Rollback:**

- Revert the changes

---

### Phase 6: Fix Missing Properties on Compatibility Types (8 errors)

**Problem:** `MugenCompatibilityIssue` and `MugenCompatibilityFix` are missing properties.

**File:** `src/SaveState.Presentation/ViewModels/Shell/Mugen/MugenHubViewModel.cs`

**Errors:**

- Lines 465-468: Missing IssueType, Description, Severity, SuggestedFix on MugenCompatibilityIssue
- Lines 513-516: Missing FixType, Description, Success, Details on MugenCompatibilityFix
- Lines 520-523: Missing IssueType, Description, Severity, SuggestedFix on MugenCompatibilityIssue

**Solution:** Update the ValueObject or DTO classes to include missing properties, or update the ViewModel to use correct property names.

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"Compatibility" /C:"CS1061"
```

**Rollback:**

- Revert the changes

---

### Phase 7: Fix AudioSettings Type Mismatch (5 errors)

**Problem:** Presentation layer has its own `AudioSettings` type but services expect Core's `AudioSettings`.

**File:** `src/SaveState.Presentation/ViewModels/Settings/AudioOptimizationViewModel.cs`

**Errors:**

- Line 113: AudioSettings type mismatch
- Line 150: AudioSettings type mismatch
- Line 198: AudioProfile type mismatch
- Line 305: AudioLatencyMode conversion
- Line 328: AudioProfile type mismatch

**Solution:** Either:

1. Remove Presentation's AudioSettings and use Core's
2. Add conversion methods between the two types
3. Update services to accept Presentation's types

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"AudioSettings" /C:"CS1503"
```

**Rollback:**

- Revert the changes

---

### Phase 8: Fix Missing Methods in AudioOptimizationViewModel (2 errors)

**Problem:** `ApplyCurrentSettingsAsync` method doesn't exist.

**File:** `src/SaveState.Presentation/ViewModels/Settings/AudioOptimizationViewModel.cs`

**Errors:**

- Line 236: `ApplyCurrentSettingsAsync` not found
- Line 284: `ApplyCurrentSettingsAsync` not found

**Solution:** Implement the missing method or remove calls to it.

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"ApplyCurrentSettingsAsync" /C:"CS0103"
```

**Rollback:**

- Revert the changes

---

### Phase 9: Fix Other Type Mismatches (10+ errors)

**Problem:** Various other type conversion issues across multiple files.

**Files:**

- `src/SaveState.Presentation/Views/Dialogs/EmulatorEditorDialog.axaml.cs`
- `src/SaveState.Presentation/ViewModels/Automation/MacroMarketplaceViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Analytics/AnalyticsDashboardViewModel.cs`
- `src/SaveState.Presentation/Services/DialogService.cs`
- `src/SaveState.Presentation/ViewModels/Dialogs/EmulatorEditorDialogViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Overlays/MemoryMonitorOverlayViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MugenTournamentViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameOverviewTabViewModel.cs`
- `src/SaveState.Presentation/ViewModels/Shell/Mugen/MugenMiscViewModels.cs`
- `src/SaveState.Presentation/ViewModels/Shell/PerformanceDashboardViewModel.cs`

**Solution:** Address each error individually based on its specific context.

**Verification:**

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"error"
```

**Rollback:**

- Revert the changes

---

## Error Reduction Tracking Table

| Phase | Description | Errors Before | Errors After | Reduction |
|-------|-------------|---------------|--------------|-----------|
| 1 | Fix MugenMoveEntry Type Mismatch | 15+ | ~0 | 15+ |
| 2 | Fix MugenCharacterSummary Type Mismatch | 3+ | ~0 | 3+ |
| 3 | Fix Missing Repository Types | 5 | ~0 | 5 |
| 4 | Fix Missing Methods on IMugenRosterService | 4 | ~0 | 4 |
| 5 | Fix GameId to Game Conversion | 3 | ~0 | 3 |
| 6 | Fix Missing Properties on Compatibility Types | 8 | ~0 | 8 |
| 7 | Fix AudioSettings Type Mismatch | 5 | ~0 | 5 |
| 8 | Fix Missing Methods in AudioOptimizationViewModel | 2 | ~0 | 2 |
| 9 | Fix Other Type Mismatches | 10+ | ~0 | 10+ |
| **Total** | | **~70** | **~0** | **~70** |

---

## Implementation Order

The phases should be implemented in order as they have dependencies:

1. **Phase 1** - Fix MugenMoveEntry type mismatch (highest impact)
2. **Phase 2** - Fix MugenCharacterSummary type mismatch (high impact)
3. **Phase 3** - Fix missing repository types (medium impact)
4. **Phase 4** - Fix missing methods on IMugenRosterService (medium impact)
5. **Phase 5** - Fix GameId to Game conversion (medium impact)
6. **Phase 6** - Fix missing properties on compatibility types (medium impact)
7. **Phase 7** - Fix AudioSettings type mismatch (medium impact)
8. **Phase 8** - Fix missing methods in AudioOptimizationViewModel (low impact)
9. **Phase 9** - Fix other type mismatches (various impact)

---

## Verification After Each Phase

After completing each phase, run:

```bash
cd src/SaveState.Presentation && dotnet build 2>&1 | findstr /C:"error" /C:"Build FAILED"
```

Count the errors and verify the expected reduction.

---

## Final Verification

After completing all phases, run:

```bash
cd src/SaveState.Presentation && dotnet build
```

Expected result: Build succeeds with 0 errors.

---

## Notes

1. **Type System Inconsistency**: The main issue is inconsistency between Core ValueObjects, DTOs, and Presentation types. A long-term solution would be to standardize on one type system.

2. **Obsolete Types**: Several types in Application layer are marked as obsolete but are still being used. These should be migrated to Core types.

3. **Service Interface Mismatches**: Some service interfaces don't match what ViewModels expect. This suggests either the interfaces need updating or the ViewModels need updating.

4. **Clean Architecture Violation**: Presentation layer should not directly depend on Core types in some cases. Consider introducing Application layer DTOs for Presentation.

---

## Next Steps

1. Review this plan with the user
2. Confirm the approach for each phase
3. Implement Phase 1
4. Verify and proceed to Phase 2
5. Continue until all phases are complete
6. Run final build verification
