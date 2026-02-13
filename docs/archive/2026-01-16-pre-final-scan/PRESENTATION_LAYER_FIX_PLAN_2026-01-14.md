# Presentation Layer Build Error Fix Plan

**Date**: January 14, 2026
**Total Errors**: 94 (47 unique, each reported twice due to build parallelism)
**Estimated Effort**: 4-6 hours
**Priority**: HIGH - Blocking full solution build

---

## Executive Summary

The Presentation layer has 94 build errors falling into 5 categories. The root causes are:

1. **Duplicate type definitions** - Same classes defined in multiple files
2. **Ambiguous type references** - Same type name exists in multiple namespaces (Core.DTOs vs Core.ValueObjects)
3. **Missing type definitions** - Referenced types that don't exist
4. **Partial method issues** - Incomplete partial method implementations

---

## Error Breakdown

| Error Code | Count | Description | Root Cause |
|------------|-------|-------------|------------|
| **CS0104** | 36 (18 unique) | Ambiguous reference | Duplicate types in Core.Mugen.DTOs and Core.Mugen.ValueObjects |
| **CS0102** | 32 (16 unique) | Duplicate member definition | Same class defined in multiple files |
| **CS0111** | 20 (10 unique) | Duplicate method/constructor | Same class defined in multiple files |
| **CS0246** | 4 (2 unique) | Type not found | Missing `EmulatorInstallationOption` type |
| **CS0759** | 2 (1 unique) | Partial method issue | Missing partial method definition |
| **TOTAL** | **94** | | |

---

## Phase 1: Remove Duplicate Class Definitions (CS0102 + CS0111)

**Priority**: CRITICAL - Must fix first
**Estimated Time**: 1.5 hours
**Errors Fixed**: 52 (26 unique)

### 1.1 Duplicate Dialog ViewModels

The following ViewModels exist in BOTH dedicated files AND `MissingDialogViewModels.cs`:

| Class | Dedicated File | Also In |
|-------|---------------|---------|
| `EmulatorConfigDialogViewModel` | `EmulatorConfigDialogViewModel.cs` | `MissingDialogViewModels.cs` |
| `RomDetailsDialogViewModel` | `RomDetailsDialogViewModel.cs` | N/A (duplicate within file) |
| `RomScanProgressDialogViewModel` | `RomScanProgressDialogViewModel.cs` | N/A (duplicate within file) |

**Files to Fix**:

#### 1.1.1 `ViewModels/Dialogs/MissingDialogViewModels.cs`

**Action**: Remove duplicate `EmulatorConfigDialogViewModel` class (lines ~48-100)

```csharp
// REMOVE THIS ENTIRE CLASS - it's duplicated in EmulatorConfigDialogViewModel.cs
public class EmulatorConfigDialogViewModel : ViewModelBase
{
    private readonly IMediator _mediator;           // CS0102 duplicate
    private readonly IDialogService _dialogService; // CS0102 duplicate
    private readonly ILogger<EmulatorConfigDialogViewModel> _logger; // CS0102 duplicate
    private readonly Emulator? _existingEmulator;   // CS0102 duplicate
    public event Action<bool>? RequestClose;        // CS0102 duplicate

    public EmulatorConfigDialogViewModel(...) { }   // CS0111 duplicate constructor
}
```

#### 1.1.2 `ViewModels/Dialogs/RomDetailsDialogViewModel.cs`

**Action**: The file likely has duplicate field/property declarations. Check for:
- Duplicate `_romFile`, `_romFileRepository`, `_emulatorRepository` etc. fields
- Duplicate `RequestClose` event
- Duplicate constructor

**Root Cause Investigation**: File may have been partially merged or auto-generated twice.

#### 1.1.3 `ViewModels/Dialogs/RomScanProgressDialogViewModel.cs`

**Action**: Check for:
- Duplicate `_logger` field
- Duplicate `RequestClose` event
- Duplicate constructor
- Duplicate `UpdateElapsedTime` method

### 1.2 Duplicate Dialog Views

**Files to Fix**:

#### 1.2.1 `Views/Dialogs/MissingDialogViews.axaml.cs`

**Action**: Remove duplicate classes that exist in dedicated files:

```csharp
// REMOVE - duplicated in EmulatorConfigDialog.axaml.cs
public partial class EmulatorConfigDialog : Window
{
    public EmulatorConfigDialog() { InitializeComponent(); }
}

// REMOVE - duplicated in EmulatorSetupWizard.axaml.cs
public partial class EmulatorSetupWizard : Window
{
    public EmulatorSetupWizard() { InitializeComponent(); }
    private void InitializeComponent() { ... }  // CS0111 duplicate
}
```

#### 1.2.2 `Views/Dialogs/RomDetailsDialog.axaml.cs`

**Action**: Check for duplicate constructor

#### 1.2.3 `Views/Dialogs/RomScanProgressDialog.axaml.cs`

**Action**: Check for duplicate constructor

### 1.3 Duplicate Service Method

#### 1.3.1 `Services/DialogService.cs` (line 1268)

**Action**: Remove duplicate `ShowFilePickerAsync` method

```csharp
// Find and remove duplicate method at line 1268
public async Task<string?> ShowFilePickerAsync(...) { }  // CS0111 duplicate
```

---

## Phase 2: Resolve Ambiguous Type References (CS0104)

**Priority**: HIGH
**Estimated Time**: 2 hours
**Errors Fixed**: 36 (18 unique)

### 2.1 Problem Analysis

The following types exist in BOTH `SaveState.Core.Mugen.DTOs` and `SaveState.Core.Mugen.ValueObjects`:

| Type | DTOs Location | ValueObjects Location |
|------|---------------|----------------------|
| `MugenNetplayLobby` | Core.Mugen.DTOs | Core.Mugen.ValueObjects |
| `MugenAssetEntry` | Core.Mugen.DTOs | Core.Mugen.ValueObjects |
| `MugenDiscoveryItem` | Core.Mugen.DTOs | Core.Mugen.ValueObjects |
| `MugenRosterEntry` | Core.Mugen.DTOs | Core.Mugen.ValueObjects |
| `MugenRosterEntryType` | Core.Mugen.DTOs | Core.Mugen.ValueObjects |
| `MugenMoveEntry` | Application.Mugen.DTOs | Core.Mugen.ValueObjects |
| `INaturalLanguageGameSearch` | Presentation.Services | Core.Ai.Services |

### 2.2 Recommended Solution: Type Consolidation

**Option A (Recommended)**: Remove duplicate types from DTOs, keep in ValueObjects

The ValueObjects namespace is the canonical location for domain types. DTOs should only contain data transfer types that differ from domain types.

**Steps**:

1. **Audit each duplicate type** - Compare definitions to understand differences
2. **Choose canonical location** - ValueObjects for domain types, DTOs only for transfer-specific types
3. **Remove duplicates** - Delete from non-canonical location
4. **Update references** - Fix all using statements in Presentation layer

### 2.3 Files to Fix (Presentation Layer)

After consolidation, add explicit using aliases or remove ambiguous imports:

#### 2.3.1 `ViewModels/Shell/MugenHubViewModel.cs`

**Errors at lines**: 209, 211, 410, 577

```csharp
// Add at top of file - choose the correct namespace
using MugenNetplayLobby = SaveState.Core.Mugen.ValueObjects.MugenNetplayLobby;
using MugenAssetEntry = SaveState.Core.Mugen.ValueObjects.MugenAssetEntry;
```

#### 2.3.2 `ViewModels/Shell/Mugen/MoveCreationViewModel.cs`

**Errors at lines**: 78, 81, 160

```csharp
// Add at top of file
using MugenMoveEntry = SaveState.Core.Mugen.ValueObjects.MugenMoveEntry;
```

#### 2.3.3 `ViewModels/Shell/Mugen/MugenDownloadsViewModel.cs`

**Errors at lines**: 26, 70

```csharp
// Add at top of file
using MugenDiscoveryItem = SaveState.Core.Mugen.ValueObjects.MugenDiscoveryItem;
```

#### 2.3.4 `ViewModels/Shell/Mugen/MugenMiscViewModels.cs`

**Errors at line**: 216

```csharp
// Add at top of file
using MugenMoveEntry = SaveState.Core.Mugen.ValueObjects.MugenMoveEntry;
```

#### 2.3.5 `ViewModels/Shell/Mugen/MugenRosterViewModel.cs`

**Errors at lines**: 353, 367, 383, 393

```csharp
// Add at top of file
using MugenRosterEntry = SaveState.Core.Mugen.ValueObjects.MugenRosterEntry;
using MugenRosterEntryType = SaveState.Core.Mugen.ValueObjects.MugenRosterEntryType;
```

#### 2.3.6 `ViewModels/GameLibraryViewModel.cs`

**Errors at lines**: 33, 57

```csharp
// Add at top of file - use Core version
using INaturalLanguageGameSearch = SaveState.Core.Ai.Services.INaturalLanguageGameSearch;
```

#### 2.3.7 `ViewModels/Library/LibraryViewModel.cs`

**Errors at lines**: 30, 118

```csharp
// Add at top of file - use Core version
using INaturalLanguageGameSearch = SaveState.Core.Ai.Services.INaturalLanguageGameSearch;
```

### 2.4 Alternative: Remove Presentation Duplicate Interface

The `INaturalLanguageGameSearch` interface exists in:
- `SaveState.Core.Ai.Services` (canonical)
- `SaveState.Presentation.Services` (duplicate)

**Action**: Delete `SaveState.Presentation.Services.INaturalLanguageGameSearch` if it's identical to the Core version.

---

## Phase 3: Add Missing Type Definitions (CS0246)

**Priority**: MEDIUM
**Estimated Time**: 30 minutes
**Errors Fixed**: 4 (2 unique)

### 3.1 Missing `EmulatorInstallationOption`

**File**: `ViewModels/Emulators/EmulatorSetupWizardViewModel.cs` (lines 124, 129)

**Action**: Create the missing type

```csharp
// Add to SaveState.Presentation/ViewModels/Emulators/ or Models/

/// <summary>
/// Represents an emulator installation option for the setup wizard.
/// </summary>
public class EmulatorInstallationOption
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
    public bool RequiresManualInstall { get; set; }
}
```

---

## Phase 4: Fix Partial Method Issues (CS0759)

**Priority**: MEDIUM
**Estimated Time**: 30 minutes
**Errors Fixed**: 2 (1 unique)

### 4.1 Missing Partial Method Definition

**File**: `ViewModels/Shell/Mugen/MoveCreationViewModel.cs` (line 160)

**Error**: `No defining declaration found for implementing declaration of partial method 'MoveCreationViewModel.OnSelectedMoveChanged(MugenMoveEntry?)'`

**Root Cause**: The ViewModel uses `[ObservableProperty]` attribute which generates a partial method `OnSelectedMoveChanged`, but there's a mismatch due to the ambiguous `MugenMoveEntry` type.

**Fix**: After resolving the CS0104 error with `MugenMoveEntry`, this error should auto-resolve. If not:

```csharp
// Ensure the partial method signature matches the generated one exactly
partial void OnSelectedMoveChanged(SaveState.Core.Mugen.ValueObjects.MugenMoveEntry? value);
```

---

## Execution Order

### Step 1: Phase 1 - Remove Duplicates (CRITICAL)
Must be done first as duplicates cause cascading errors.

1. [ ] Fix `MissingDialogViewModels.cs` - Remove `EmulatorConfigDialogViewModel`
2. [ ] Fix `MissingDialogViews.axaml.cs` - Remove `EmulatorConfigDialog`, `EmulatorSetupWizard`
3. [ ] Fix `RomDetailsDialogViewModel.cs` - Remove duplicate members
4. [ ] Fix `RomScanProgressDialogViewModel.cs` - Remove duplicate members
5. [ ] Fix `RomDetailsDialog.axaml.cs` - Remove duplicate constructor
6. [ ] Fix `RomScanProgressDialog.axaml.cs` - Remove duplicate constructor
7. [ ] Fix `DialogService.cs` - Remove duplicate `ShowFilePickerAsync`

### Step 2: Phase 3 - Add Missing Types
Add types before fixing references.

1. [ ] Create `EmulatorInstallationOption` class

### Step 3: Phase 2 - Resolve Ambiguous References
Either consolidate types in Core or add using aliases.

**Option A - Core Layer Cleanup (Recommended)**:
1. [ ] Audit duplicate types in Core.Mugen.DTOs vs Core.Mugen.ValueObjects
2. [ ] Delete duplicate DTOs (keep ValueObjects as canonical)
3. [ ] Update all references

**Option B - Quick Fix with Using Aliases**:
1. [ ] Add using aliases to each affected Presentation file
2. [ ] Remove Presentation.Services.INaturalLanguageGameSearch if duplicate

### Step 4: Phase 4 - Fix Partial Methods
Should auto-resolve after Phase 2.

1. [ ] Verify `OnSelectedMoveChanged` error is resolved
2. [ ] If not, update partial method signature

---

## Verification Steps

After each phase:

```powershell
dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj
```

After all phases:

```powershell
dotnet build
dotnet test
```

---

## Risk Assessment

| Phase | Risk | Mitigation |
|-------|------|------------|
| Phase 1 | LOW | Simply removing duplicate code |
| Phase 2 Option A | MEDIUM | May break other projects if they reference deleted DTOs |
| Phase 2 Option B | LOW | Using aliases are non-breaking |
| Phase 3 | LOW | Adding new type, no existing code affected |
| Phase 4 | LOW | Should auto-resolve |

---

## Files Summary

### Files to Modify

| File | Changes |
|------|---------|
| `ViewModels/Dialogs/MissingDialogViewModels.cs` | Remove `EmulatorConfigDialogViewModel` class |
| `Views/Dialogs/MissingDialogViews.axaml.cs` | Remove `EmulatorConfigDialog`, `EmulatorSetupWizard` classes |
| `ViewModels/Dialogs/RomDetailsDialogViewModel.cs` | Remove duplicate members |
| `ViewModels/Dialogs/RomScanProgressDialogViewModel.cs` | Remove duplicate members |
| `Views/Dialogs/RomDetailsDialog.axaml.cs` | Remove duplicate constructor |
| `Views/Dialogs/RomScanProgressDialog.axaml.cs` | Remove duplicate constructor |
| `Services/DialogService.cs` | Remove duplicate `ShowFilePickerAsync` |
| `ViewModels/Shell/MugenHubViewModel.cs` | Add using aliases |
| `ViewModels/Shell/Mugen/MoveCreationViewModel.cs` | Add using alias |
| `ViewModels/Shell/Mugen/MugenDownloadsViewModel.cs` | Add using alias |
| `ViewModels/Shell/Mugen/MugenMiscViewModels.cs` | Add using alias |
| `ViewModels/Shell/Mugen/MugenRosterViewModel.cs` | Add using aliases |
| `ViewModels/GameLibraryViewModel.cs` | Add using alias |
| `ViewModels/Library/LibraryViewModel.cs` | Add using alias |

### Files to Create

| File | Purpose |
|------|---------|
| `Models/EmulatorInstallationOption.cs` | Missing type for EmulatorSetupWizard |

### Files to Potentially Delete (Core Layer - Phase 2 Option A)

| File | Reason |
|------|--------|
| `Core/Mugen/DTOs/MugenNetplayLobby.cs` | Duplicate of ValueObjects version |
| `Core/Mugen/DTOs/MugenAssetEntry.cs` | Duplicate of ValueObjects version |
| `Core/Mugen/DTOs/MugenDiscoveryItem.cs` | Duplicate of ValueObjects version |
| `Core/Mugen/DTOs/MugenRosterEntry.cs` | Duplicate of ValueObjects version |
| `Core/Mugen/DTOs/MugenRosterEntryType.cs` | Duplicate of ValueObjects version |
| `Presentation/Services/INaturalLanguageGameSearch.cs` | Duplicate of Core version |

---

## Post-Fix Actions

1. **Run full test suite** to ensure no regressions
2. **Update CLAUDE.md** with new health score
3. **Update technical debt audit** document
4. **Create commit** with descriptive message
5. **Consider architectural review** to prevent future duplicates

---

**Document Created**: January 14, 2026
**Author**: Claude Code
**Status**: Ready for Implementation
