# UI/UX Enhancement Service Refactoring Plan

## Current State
- **File**: `UiUxEnhancementService.cs` (1,543 lines)
- **Issue**: Large class handling 24+ experimental features
- **Namespace Prefix**: `UiUxEnhancementService` (causes naming confusion)

## Planned Extraction

### Phase 2: Extract Engines

#### HudEngine.cs
Responsible for HUD element generation and management.

**Methods to extract:**
- `GenerateHudElements()`
- `CalculateOptimalLayout()`
- `UpdateHudPositions()`
- `CreateFrameAdvantageMeter()`
- `CreateParryTimingIndicator()`
- `CreateJustDefendFeedback()`

#### MenuEngine.cs
Responsible for menu system generation.

**Methods to extract:**
- `GenerateMenus()`
- `BuildNavigationGraph()`
- `CreateMechanicsGlossaryMenu()`
- `CreateComboTrialsMenu()`

#### VisualFeedbackEngine.cs
Responsible for visual feedback systems.

**Methods to extract:**
- `CreateVisualFeedbackSystem()`
- `CreateDamageVisualization()`
- `CreateStatusEffectVisualization()`
- `CreateComboScalingIndicator()`

### Phase 3: Namespace Cleanup

**Current problematic names:**
- `UiUxEnhancementServiceHudConfiguration` → `HudConfiguration`
- `UiUxEnhancementServiceMenuSystem` → `MenuSystem`
- `UiUxEnhancementServiceIUiUxEnhancementService` → `IUiUxEnhancementService`

Use type aliases to maintain backward compatibility during transition.

### Directory Structure

```
UiUx/
├── UiUxEnhancementService.cs      (refactored, <300 lines)
├── IUiUxEnhancementService.cs     (extracted interface)
├── README.md                      (this file)
├── Engines/
│   ├── HudEngine.cs               (~300 lines)
│   ├── MenuEngine.cs              (~250 lines)
│   └── VisualFeedbackEngine.cs    (~350 lines)
└── Models/
    ├── HudConfiguration.cs        (extracted)
    ├── MenuSystem.cs              (extracted)
    └── VisualFeedbackSystem.cs    (extracted)
```
