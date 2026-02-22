# IStoryModeService Refactoring Plan

## Current State Analysis

### File Statistics
- **File**: `src/SaveState.Core/Mugen/Services/IStoryModeService.cs`
- **Lines**: 1,027
- **Status**: ✅ **Already properly architected** with Interface Segregation Principle applied

### Current Architecture (GOOD)

The file already follows the **Marker Interface + Focused Interfaces** pattern:

```
IStoryModeService (Marker - 0 methods)
├── IStoryProjectService (6 methods)
├── IStoryChapterService (5 methods)
├── IStorySceneService (6 methods)
├── IStoryDialogueService (6 methods)
├── IStoryCutsceneService (6 methods)
├── IStoryBranchingService (6 methods)
├── IStoryBattleIntegrationService (4 methods)
├── IStoryTestingService (5 methods)
└── IStoryAssetService (4 methods)
```

### Interface Method Counts (Within Budget ✅)

| Interface | Methods | Budget | Status |
|-----------|---------|--------|--------|
| IStoryModeService | 0 (marker) | N/A | ✅ |
| IStoryProjectService | 6 | ≤10 | ✅ |
| IStoryChapterService | 5 | ≤10 | ✅ |
| IStorySceneService | 6 | ≤10 | ✅ |
| IStoryDialogueService | 6 | ≤10 | ✅ |
| IStoryCutsceneService | 6 | ≤10 | ✅ |
| IStoryBranchingService | 6 | ≤10 | ✅ |
| IStoryBattleIntegrationService | 4 | ≤10 | ✅ |
| IStoryTestingService | 5 | ≤10 | ✅ |
| IStoryAssetService | 4 | ≤10 | ✅ |

### DTOs/Records in File (428 lines)

The file contains 48 record types, enums, and DTOs:
- Story models: `StoryProject`, `StoryChapter`, `StoryScene`, etc.
- Settings: `StorySettings`, `TextDisplaySettings`, `MusicSettings`, etc.
- Enums: `TextSpeed`, `SceneType`, `SpeakerPosition`, `AssetType`, etc.
- Results: `StoryProjectStats`, `AssetValidationResult`, etc.

---

## Refactoring Goals

The architecture is already correct, but the **file organization** needs improvement:

1. **Split monolithic file** into focused interface files
2. **Extract DTOs** to a dedicated models file
3. **Improve discoverability** and maintainability
4. **Maintain backward compatibility** during transition

---

## Recommended Refactoring Strategy

### Option A: Full Split (Recommended)

Split into 11 files following the pattern used by other Manager Pattern services:

```
src/SaveState.Core/Mugen/Services/StoryMode/
├── IStoryModeService.cs              # Marker interface only
├── IStoryProjectService.cs           # Project lifecycle
├── IStoryChapterService.cs           # Chapter management
├── IStorySceneService.cs             # Scene management
├── IStoryDialogueService.cs          # Dialogue system
├── IStoryCutsceneService.cs          # Cutscene editing
├── IStoryBranchingService.cs         # Branching and choices
├── IStoryBattleIntegrationService.cs # Battle integration
├── IStoryTestingService.cs           # Testing and preview
├── IStoryAssetService.cs             # Asset management
└── StoryModeModels.cs                # All DTOs/records/enums
```

### Option B: Partial Split (Minimal Change)

Keep interfaces together but separate models:

```
src/SaveState.Core/Mugen/Services/
├── IStoryModeService.cs              # All interfaces (399 lines)
└── StoryModeModels.cs                # All DTOs (600+ lines)
```

---

## Implementation Plan (Option A - Full Split)

### Phase 1: Create Directory Structure

```bash
mkdir -p src/SaveState.Core/Mugen/Services/StoryMode
```

### Phase 2: Create StoryModeModels.cs

Extract all records, enums, and DTOs to a single models file:

```csharp
namespace SaveState.Core.Mugen.Services.StoryMode;

// Enums
public enum TextSpeed { Slow, Normal, Fast, Instant }
public enum SceneType { Dialogue, Cutscene, Battle, Choice, Transition, Ending }
public enum SpeakerPosition { Left, Center, Right, Offscreen }
// ... etc

// Records
public record StoryProject(Guid Id, string Title, ...);
public record StoryChapter(Guid Id, string Title, ...);
// ... etc
```

**Estimated lines**: ~600 lines

### Phase 3: Create Individual Interface Files

Each interface file follows this template:

```csharp
using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services.StoryMode;

/// <summary>
/// Service for story [specific concern].
/// </summary>
public interface I[Name]Service
{
    // Methods...
}
```

| File | Lines | Content |
|------|-------|---------|
| IStoryModeService.cs | ~15 | Marker interface + XML docs |
| IStoryProjectService.cs | ~40 | 6 method signatures |
| IStoryChapterService.cs | ~35 | 5 method signatures |
| IStorySceneService.cs | ~45 | 6 method signatures + settings |
| IStoryDialogueService.cs | ~45 | 6 method signatures + settings |
| IStoryCutsceneService.cs | ~55 | 6 method signatures + camera/animation |
| IStoryBranchingService.cs | ~50 | 6 method signatures + conditions |
| IStoryBattleIntegrationService.cs | ~35 | 4 method signatures + settings |
| IStoryTestingService.cs | ~40 | 5 method signatures + results |
| IStoryAssetService.cs | ~35 | 4 method signatures + types |

### Phase 4: Update Namespace

Change namespace from:
```csharp
namespace SaveState.Core.Mugen.Services;
```

To:
```csharp
namespace SaveState.Core.Mugen.Services.StoryMode;
```

### Phase 5: Update Usings in Dependent Files

Files that need updating:
- `src/SaveState.Infrastructure/Mugen/StoryMode/StoryModeService.cs`
- `src/SaveState.Infrastructure/Mugen/StoryMode/Managers/*.cs`
- Any ViewModels using these interfaces
- Any tests

**Using update pattern:**
```csharp
// OLD
using SaveState.Core.Mugen.Services;

// NEW
using SaveState.Core.Mugen.Services.StoryMode;
```

### Phase 6: Delete Original File

After confirming all imports work:
```bash
rm src/SaveState.Core/Mugen/Services/IStoryModeService.cs
```

---

## Backward Compatibility

### Option 1: Type Forwarding (Recommended)

Add to old namespace for temporary backward compatibility:

```csharp
// In SaveState.Core/Mugen/Services/IStoryModeService.cs (temporary)
namespace SaveState.Core.Mugen.Services;

[Obsolete("Use SaveState.Core.Mugen.Services.StoryMode namespace instead")]
public interface IStoryModeService : StoryMode.IStoryModeService { }
// ... etc
```

### Option 2: Global Using (Quick Fix)

Add to `GlobalUsings.cs`:
```csharp
global using SaveState.Core.Mugen.Services.StoryMode;
```

---

## Benefits After Refactoring

| Benefit | Before | After |
|---------|--------|-------|
| **File Size** | 1,027 lines | ~130 lines avg |
| **Discoverability** | Poor - one giant file | Excellent - focused files |
| **Compilation** | Full recompile on change | Incremental per interface |
| **Code Reviews** | Difficult | Easy - focused changes |
| **Interface Count** | 10 in 1 file | 10 in 10 files |
| **Namespace Clarity** | Generic | Specific (`StoryMode`) |

---

## Effort Estimate

- **Phase 1-3** (File creation): 1 hour
- **Phase 4-5** (Namespace updates): 2-3 hours (depends on dependent file count)
- **Phase 6** (Cleanup): 15 minutes
- **Testing**: 1 hour

**Total**: ~4-5 hours

---

## Verification Checklist

- [ ] All 9 focused interfaces created in new namespace
- [ ] All 48 DTOs/records/enums moved to StoryModeModels.cs
- [ ] Marker interface preserved
- [ ] All dependent files updated with new usings
- [ ] Build succeeds with 0 errors
- [ ] All tests pass
- [ ] Original file deleted
- [ ] AGENTS.md updated if needed

---

## Related Patterns in Codebase

Similar successful refactorings:
- `SpriteAnimationService` → 6 managers in `SpriteAnimation/Managers/`
- `ComboDatabaseService` → 8 managers in `ComboDatabase/Managers/`
- `PredictiveAnalyticsEngine` → 5 managers
- `BlockchainService` → 4 managers

This refactoring aligns with the established **Manager Pattern** in the codebase.

---

*Plan created: February 21, 2026*
*Status: Ready for implementation*
