# Large Class Refactoring Plan

**Date:** February 11, 2026  
**Scope:** 104 files exceeding 500 lines of code

---

## 📊 Current State

### Top 15 Largest Files

| Rank | File | Lines | Folder | Types |
|------|------|-------|--------|-------|
| 1 | UiUxEnhancementService.cs | 1,376 | Services | ~50 |
| 2 | VrArIntegrationService.cs | 1,321 | Services | ~45 |
| 3 | MugenCommands.cs | 1,219 | Commands | ~15 |
| 4 | EmergingTechnologiesService.cs | 1,207 | Services | ~40 |
| 5 | BalanceTuningService.cs | 1,174 | Services | ~42 |
| 6 | BioFeedbackCombatService.cs | 1,120 | Services | 45+ |
| 7 | DialogService.cs | 1,115 | Services | ~30 |
| 8 | MugenHubViewModel.cs | 1,105 | Shell | ~25 |
| 9 | AiOpponentsService.cs | 1,080 | Services | ~40 |
| 10 | EducationalContentService.cs | 1,079 | Services | ~38 |
| 11 | DreamLogicArenaService.cs | 1,066 | Services | ~35 |
| 12 | AdvancedAnalyticsService.cs | 1,047 | Services | ~30 |
| 13 | PredictiveAnalyticsEngine.cs | 1,042 | Services | ~32 |
| 14 | AutomatedBalancingSystem.cs | 1,032 | Services | ~30 |
| 15 | RetroArchService.cs | 1,028 | RetroArch | ~20 |

---

## 🎯 Refactoring Strategy

### Category A: Data Model Heavy Files
**Files:** BioFeedbackCombatService.cs, BalanceTuningService.cs, AiOpponentsService.cs

**Pattern:** Extract data models to separate files

**Structure:**
```
Services/
├── BioFeedbackCombat/
│   ├── BioFeedbackCombatService.cs (main service only)
│   ├── Models/
│   │   ├── BioProfile.cs
│   │   ├── CombatSession.cs
│   │   ├── BioFeedback.cs
│   │   └── ...
│   ├── Engines/
│   │   ├── HeartRateEngine.cs
│   │   ├── BreathingEngine.cs
│   │   └── ...
│   └── Enums/
│       ├── BioProfileStatus.cs
│       ├── CombatStatus.cs
│       └── ...
```

**Effort:** High (40-50 types per file)  
**Risk:** Medium (requires namespace updates)  
**Impact:** High (improved maintainability)

---

### Category B: Feature-Rich Service Files
**Files:** UiUxEnhancementService.cs, VrArIntegrationService.cs, EmergingTechnologiesService.cs

**Pattern:** Split by feature/technology area

**Example for VrArIntegrationService:**
```
Services/
├── VrAr/
│   ├── VrArIntegrationService.cs (orchestrator)
│   ├── VrHeadsetManager.cs
│   ├── ArOverlayService.cs
│   ├── MotionControllerService.cs
│   └── Models/
│       └── ...
```

**Effort:** High (complex logic dependencies)  
**Risk:** High (feature integration)  
**Impact:** High (better separation of concerns)

---

### Category C: Command Group Files
**Files:** MugenCommands.cs

**Pattern:** Split by command group

**Structure:**
```
Commands/
├── Mugen/
│   ├── CharacterCommands.cs
│   ├── TournamentCommands.cs
│   ├── TrainingCommands.cs
│   └── ...
```

**Effort:** Medium  
**Risk:** Low (commands are mostly independent)  
**Impact:** Medium

---

### Category D: ViewModel Files
**Files:** MugenHubViewModel.cs, DialogService.cs

**Pattern:** Extract helpers, commands, and models

**Example for DialogService:**
```
Services/
├── Dialog/
│   ├── DialogService.cs
│   ├── DialogResult.cs
│   ├── DialogOptions.cs
│   └── DialogTypes.cs
```

**Effort:** Medium  
**Risk:** Medium (UI dependencies)  
**Impact:** Medium

---

## 📅 Phased Implementation

### Phase 1: Quick Wins (Week 1)
Target files with high type count but simple structure:
- MugenCommands.cs → Split into command groups
- RetroArchService.cs → Extract helper methods

### Phase 2: Model Extraction (Weeks 2-3)
Target data model heavy files:
- BioFeedbackCombatService.cs → Extract 40+ data models
- BalanceTuningService.cs → Extract data models

### Phase 3: Service Decomposition (Weeks 4-6)
Target complex service files:
- UiUxEnhancementService.cs
- VrArIntegrationService.cs
- EmergingTechnologiesService.cs

### Phase 4: ViewModel Cleanup (Week 7)
- MugenHubViewModel.cs
- DialogService.cs

---

## 🛠️ Implementation Template

### Step 1: Create Directory Structure
```bash
mkdir -p Services/{ServiceName}/{Models,Engines,Enums}
```

### Step 2: Extract Enums
Move all enums to separate files in Enums/ folder

### Step 3: Extract Data Models
Move all POCO/data classes to Models/ folder

### Step 4: Extract Engines/Helpers
Move complex logic to separate engine classes

### Step 5: Update Main Service
- Update using statements
- Keep only orchestration logic
- Inject extracted engines

### Step 6: Update References
- Find all usages
- Update namespace references
- Fix dependency injection

---

## ⚠️ Risk Mitigation

1. **Build After Each Step**
   - Never break the build
   - Run tests after each file move

2. **Namespace Preservation**
   - Keep original namespace for backward compatibility
   - Use partial classes if needed

3. **Interface Stability**
   - Don't change public method signatures
   - Use adapter pattern if needed

4. **Incremental Commits**
   - One file type per commit
   - Easy rollback if issues

---

## 📈 Success Metrics

| Metric | Target |
|--------|--------|
| Files >1000 LOC | Reduce from 15 to <5 |
| Files >500 LOC | Reduce from 104 to <50 |
| Average LOC/file | Reduce from ~200 to <150 |
| Build status | Always passing |
| Test coverage | Maintain or improve |
