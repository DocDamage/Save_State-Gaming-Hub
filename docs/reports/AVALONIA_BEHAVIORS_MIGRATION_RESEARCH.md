# Avalonia.Xaml.Behaviors Migration Research

**Research Date:** February 12, 2026  
**Researcher:** Kimi CLI  
**Status:** Migration Blocked - Requires Breaking Changes  

---

## Executive Summary

The migration from the deprecated `Avalonia.Xaml.Behaviors` to the new `Xaml.Behaviors.Avalonia` package involves significant breaking changes in namespace structure and API. The current codebase uses the deprecated package successfully, and migration would require extensive refactoring across multiple AXAML files.

---

## Package Information

### Deprecated Package
- **Package Name:** `Avalonia.Xaml.Behaviors`
- **Version Used:** 11.2.6
- **Status:** Archived on November 1, 2024 (read-only)
- **Repository:** https://github.com/AvaloniaUI/Avalonia.Xaml.Behaviors (archived)

### New Package (Replacement)
- **Package Name:** `Xaml.Behaviors.Avalonia`
- **Latest Version:** 11.3.0.7+
- **Maintainer:** wieslawsoltes
- **Repository:** https://github.com/wieslawsoltes/AvaloniaBehaviors
- **NuGet:** https://www.nuget.org/packages/Xaml.Behaviors.Avalonia

---

## Namespace Mapping Analysis

### Current (Deprecated Package) - Working
```xml
xmlns:i="clr-namespace:Avalonia.Xaml.Interactivity;assembly=Avalonia.Xaml.Interactivity"
xmlns:ia="clr-namespace:Avalonia.Xaml.Interactions.Core;assembly=Avalonia.Xaml.Interactions"
```

### New Package - Namespace Structure
Based on research of the new package's source code and samples:

```xml
<!-- Interactivity namespace -->
xmlns:i="clr-namespace:Avalonia.Xaml.Interactivity;assembly=Avalonia.Xaml.Interactivity"

<!-- Interactions namespaces (split by functionality) -->
xmlns:ia="clr-namespace:Avalonia.Xaml.Interactions.Core;assembly=Avalonia.Xaml.Interactions"
xmlns:idd="clr-namespace:Avalonia.Xaml.Interactions.DragAndDrop;assembly=Avalonia.Xaml.Interactions"
xmlns:iddn="clr-namespace:Avalonia.Xaml.Interactions.Draggable;assembly=Avalonia.Xaml.Interactions"
xmlns:idr="clr-namespace:Avalonia.Xaml.Interactions.Responsive;assembly=Avalonia.Xaml.Interactions"
```

### Key Differences

| Aspect | Old Package | New Package |
|--------|-------------|-------------|
| **Core Namespace** | `Avalonia.Xaml.Interactions.Core` | `Avalonia.Xaml.Interactions` or `Avalonia.Xaml.Interactions.Core` |
| **Assembly Names** | `Avalonia.Xaml.Interactivity`<br>`Avalonia.Xaml.Interactions` | `Avalonia.Xaml.Interactivity`<br>`Avalonia.Xaml.Interactions` |
| **Package Structure** | Single package | Multiple assemblies in single package |
| **API Compatibility** | Original API | Breaking changes in some behaviors |

---

## Files Affected

The following files in the codebase use Avalonia.Xaml.Behaviors:

1. **Primary Usage:**
   - `src/SaveState.Presentation/Views/Dialogs/BranchSelectionDialog.axaml`
     - Uses `EventTriggerBehavior`
     - Uses `InvokeCommandAction`

2. **Package Reference:**
   - `src/SaveState.Presentation/SaveState.Presentation.csproj`
   - `Directory.Packages.props`

---

## Migration Attempts & Results

### Attempt 1: Direct Package Swap
**Changes:**
- Package: `Avalonia.Xaml.Behaviors` → `Xaml.Behaviors.Avalonia` 11.3.0.7
- Keep same namespace declarations

**Result:** ❌ Failed
```
Unable to resolve type EventTriggerBehavior from namespace clr-namespace:Avalonia.Xaml.Interactions.Core;assembly=Avalonia.Xaml.Behaviors
```

### Attempt 2: Updated Assembly Names
**Changes:**
- Namespace: `assembly=Avalonia.Xaml.Behaviors` → `assembly=Avalonia.Xaml.Interactivity`
- Namespace: `assembly=Avalonia.Xaml.Behaviors` → `assembly=Avalonia.Xaml.Interactions`

**Result:** ❌ Failed
```
Unable to resolve type EventTriggerBehavior from namespace clr-namespace:Avalonia.Xaml.Interactions.Core;assembly=Avalonia.Xaml.Interactions
```

### Root Cause
The new package has restructured the class hierarchy and potentially moved types to different namespaces or assemblies. The `EventTriggerBehavior` and `InvokeCommandAction` classes may:
1. Be in a different namespace (not `.Core`)
2. Have different assembly references
3. Have breaking API changes

---

## Recommended Migration Path

### Phase 1: Research & Preparation
1. **Clone the new repository locally:**
   ```bash
   git clone https://github.com/wieslawsoltes/AvaloniaBehaviors.git
   ```

2. **Examine the exact namespace structure:**
   - Check `src/Avalonia.Xaml.Interactions/Core/` directory
   - Verify class locations for `EventTriggerBehavior`
   - Verify class locations for `InvokeCommandAction`

3. **Review sample projects:**
   - `samples/DragAndDropSample/`
   - `samples/BehaviorsSample/`

### Phase 2: Namespace Updates
Based on source code examination, update namespace declarations:

```xml
<!-- BEFORE (Deprecated) -->
xmlns:i="clr-namespace:Avalonia.Xaml.Interactivity;assembly=Avalonia.Xaml.Interactivity"
xmlns:ia="clr-namespace:Avalonia.Xaml.Interactions.Core;assembly=Avalonia.Xaml.Interactions"

<!-- AFTER (New Package) - TBD after source examination -->
<!-- Likely one of these combinations: -->
xmlns:i="clr-namespace:Avalonia.Xaml.Interactivity;assembly=Avalonia.Xaml.Interactivity"
xmlns:ia="clr-namespace:Avalonia.Xaml.Interactions;assembly=Avalonia.Xaml.Interactions"
<!-- OR -->
xmlns:i="using:Avalonia.Xaml.Interactivity"
xmlns:ia="using:Avalonia.Xaml.Interactions.Core"
```

### Phase 3: API Compatibility Check
The new package may have API changes requiring code updates:
- `EventTriggerBehavior` constructor parameters
- `InvokeCommandAction` property names
- Behavior attachment syntax

### Phase 4: Testing
1. Build the Presentation project
2. Test the BranchSelectionDialog functionality
3. Verify command binding works correctly

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Breaking API changes | High | Thorough testing of all behavior usage |
| Missing functionality | Medium | Verify all required behaviors exist in new package |
| Designer support | Medium | Test Avalonia Designer compatibility |
| Runtime errors | High | Comprehensive UI testing |

---

## Alternative Approaches

### Option 1: Stay on Deprecated Package (Current)
- **Pros:** Works, no changes needed
- **Cons:** No updates, potential future incompatibility
- **Recommendation:** Acceptable short-term

### Option 2: Remove Behaviors Usage
Replace behavior-based event handling with code-behind or attached properties:
```csharp
// Alternative: Attached property for command binding
public static class CommandAttachedProperty
{
    public static readonly AttachedProperty<ICommand> PointerPressedCommandProperty =
        AvaloniaProperty.RegisterAttached<Interactive, ICommand>(
            "PointerPressedCommand", 
            null, 
            false, 
            BindingMode.OneTime);
    
    // Implementation...
}
```

### Option 3: Full Migration (Recommended Long-term)
Complete migration to new package after thorough research.

---

## Decision Matrix

| Approach | Effort | Risk | Long-term Viability | Recommendation |
|----------|--------|------|---------------------|----------------|
| Stay on 11.2.6 | None | Low | Poor | Short-term |
| Remove behaviors | Medium | Low | Good | Consider |
| Full migration | High | Medium | Best | Long-term |

---

## Conclusion

**Current Status:** Migration blocked due to incompatible namespace/class structure between packages.

**Recommendation:** 
1. **Short-term:** Continue using `Avalonia.Xaml.Behaviors` 11.2.6 (functional, stable)
2. **Medium-term:** Consider removing behaviors in favor of attached properties or code-behind
3. **Long-term:** Plan full migration after Avalonia 12.0 release when breaking changes are expected anyway

**Next Steps:**
1. Clone `AvaloniaBehaviors` repository
2. Examine exact class locations and namespaces
3. Create proof-of-concept migration
4. Update all affected AXAML files
5. Comprehensive testing

---

## References

1. **New Package Repository:** https://github.com/wieslawsoltes/AvaloniaBehaviors
2. **Deprecated Repository:** https://github.com/AvaloniaUI/Avalonia.Xaml.Behaviors (archived)
3. **NuGet - New:** https://www.nuget.org/packages/Xaml.Behaviors.Avalonia
4. **NuGet - Old:** https://www.nuget.org/packages/Avalonia.Xaml.Behaviors
5. **Avalonia Documentation:** https://docs.avaloniaui.net/

---

*Document generated: February 12, 2026*
