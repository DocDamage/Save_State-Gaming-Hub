# Avalonia XAML Build Errors Remediation Plan

## Overview

**Scope:** `src/SaveState.Presentation` XAML compile failures (`AVLN*`)  
**Date:** February 22, 2026  
**Status:** Completed + full solution validation pass (February 23, 2026)  
**Current UI package version:** Avalonia `11.3.12`  
**Baseline Repro Command:**

```bash
dotnet clean SaveStateReborn.sln -v minimal
dotnet build SaveStateReborn.sln -v minimal
```

**Baseline Result:** Fails on clean build with Avalonia XAML compile errors.

---

## Baseline Error Inventory

Source log: `clean_build_avalonia_xaml_errors_2026_02_22.log`

- Raw errors: `102` (duplicates included)
- Unique errors: `50`
- Unique by code:
  - `AVLN2000`: `45`
  - `AVLN3000`: `4`
  - `AVLN2200`: `1`

### Highest-Impact Files (Unique Error Count)

1. `src/SaveState.Presentation/Views/Automation/WorkflowEditorView.axaml` (`18`)
2. `src/SaveState.Presentation/Views/GameLibrary/RecommendationsView.axaml` (`8`)
3. `src/SaveState.Presentation/Styles/Animations.axaml` (`6`)
4. `src/SaveState.Presentation/Views/RetroArch/PlaylistView.axaml` (`4`)
5. `src/SaveState.Presentation/Views/Library/GameListView.axaml` (`2`)
6. `src/SaveState.Presentation/Views/Shell/Mugen/TournamentBracketView.axaml` (`2`)
7. `src/SaveState.Presentation/Views/Library/GameGridView.axaml` (`2`)
8. `src/SaveState.Presentation/Views/BigPicture/GameGridView.axaml` (`2`)
9. `src/SaveState.Presentation/Views/GameDeals/GameDealsView.axaml` (`2`)
10. `src/SaveState.Presentation/Views/Health/HealthMonitorView.axaml` (`2`)
11. `src/SaveState.Presentation/Views/Settings/AiAdministrationView.axaml` (`1`)
12. `src/SaveState.Presentation/Views/Settings/SystemHealthView.axaml` (`1`)

---

## Root Cause Clusters

1. **Invalid XAML types/resources**
   - `Styles/Animations.axaml`: `<TimeSpan x:Key=...>` cannot be resolved in Avalonia resource dictionary.

2. **Unsupported properties on control types**
   - `PathIcon` uses `Stroke`, `StrokeThickness`, `Fill` (unsupported on `PathIcon`).
   - `TextBlock` uses `TextTransform` and `CornerRadius` (unsupported).
   - `WrapPanel` uses `Spacing` (unsupported).

3. **Invalid/obsolete converter static members**
   - `ObjectConverters.Equals`, `ObjectConverters.Not`, `ObjectConverters.AreNotEqual` unresolved.

4. **Type resolution mismatch in `DataTemplate`**
   - `vm:GameRecommendation` referenced from `RecommendationsView.axaml`, but the type is in Core models namespace.

5. **Invalid binding to collection-typed property**
   - `PlaylistView.axaml`: `Grid ColumnDefinitions="{Binding ...}"` (binding string to `ColumnDefinitions` not supported, causing `AVLN3000`).

6. **Unsupported virtualization attached properties**
   - `VirtualizingPanel.IsVirtualizing` and `VirtualizingPanel.VirtualizationMode` unresolved across multiple views.

---

## Remediation Strategy

### Phase 1: Unblock Compile with Deterministic Syntax Fixes

- [x] `src/SaveState.Presentation/Styles/Animations.axaml`
  - Remove/replace `<TimeSpan>` keyed resources with Avalonia-valid values.
  - Keep transition definitions using inline `Duration` values.

- [x] `src/SaveState.Presentation/Views/GameLibrary/RecommendationsView.axaml`
  - Replace `TextBlock` `CornerRadius` style usage with `Border` wrapper where visual rounding is required.
  - Replace `ObjectConverters.Equals` with valid comparator pattern (`ObjectConverters.Equal` or binding expression).
  - Correct `DataTemplate` `DataType` to actual recommendation model namespace/type.

- [x] `src/SaveState.Presentation/Views/Settings/AiAdministrationView.axaml`
  - Replace `ObjectConverters.Not` with supported binding negation.

- [x] `src/SaveState.Presentation/Views/Settings/SystemHealthView.axaml`
  - Replace `ObjectConverters.AreNotEqual` with supported comparator (`NotEqual`/equivalent).

### Phase 2: Workflow Editor Compatibility Sweep

- [x] `src/SaveState.Presentation/Views/Automation/WorkflowEditorView.axaml`
  - Convert icon definitions that require stroke/fill customization from `PathIcon` to compatible pattern (`Path` inside `Viewbox` or `PathIcon` + `Foreground` only).
  - Replace unsupported `TextBlock TextTransform` with explicit transformed text source (converter or preformatted VM property).

### Phase 3: Layout/Panel Property Corrections

- [x] `src/SaveState.Presentation/Views/RetroArch/PlaylistView.axaml`
  - Remove invalid binding to `Grid.ColumnDefinitions`.
  - Replace with supported approach:
    - static column definitions + visibility-based layout, or
    - alternate templates for list vs grid mode.

- [x] `src/SaveState.Presentation/Views/Health/HealthMonitorView.axaml`
  - Replace `TextTransform` usage.
  - Replace `WrapPanel Spacing` with item-level margin or supported panel.

- [x] Virtualization property cleanup in:
  - `src/SaveState.Presentation/Views/Library/GameListView.axaml`
  - `src/SaveState.Presentation/Views/Shell/Mugen/TournamentBracketView.axaml`
  - `src/SaveState.Presentation/Views/Library/GameGridView.axaml`
  - `src/SaveState.Presentation/Views/BigPicture/GameGridView.axaml`
  - `src/SaveState.Presentation/Views/GameDeals/GameDealsView.axaml`
  - Remove unsupported attached properties and keep supported virtualization behavior for current control/panel combinations.
  - Update nearby comments that currently claim unsupported properties are active.

### Phase 4: Regression Guard Sweep

- [x] Run targeted static scan for known-invalid patterns in XAML:

```bash
rg -n "ObjectConverters\\.(Equals|Not|AreNotEqual)|VirtualizingPanel\\.(IsVirtualizing|VirtualizationMode)|TextTransform=|<TimeSpan x:Key|PathIcon[^\\n]*(Stroke|StrokeThickness|Fill)|ColumnDefinitions=\"\\{Binding" src/SaveState.Presentation --glob "*.axaml"
```

- [x] Resolve all remaining hits that are true compile blockers.

### Phase 5: Validation Gates

- [x] Clean build gate:

```bash
dotnet clean SaveStateReborn.sln -v minimal
dotnet build SaveStateReborn.sln -v minimal
```

- [x] Confirm `0` Avalonia compile errors.
- [x] Confirm no regression in solution compile status.

---

## Post-Remediation Progress (February 22, 2026)

### Runtime Stabilization Follow-Through

- [x] Fixed `SelectPlaylistCommand` gap in `RetroArchPlaylistViewModel` to match `PlaylistView.axaml` command binding.
- [x] Added startup guard around MUGEN seed/save DB failure path (`DbUpdateException`) so UI startup continues for smoke testing.
- [x] Added guard around `DatabaseInitializer.InitializeAsync(...)` to prevent hard crash during startup initialization failures.
- [x] Added guard around `host.StartAsync()` and conditional shutdown path to allow interactive UI smoke scenarios even when background services fail.
- [x] Fixed `NavigationService` constructor initialization order bug (logger used before assignment).
- [x] Removed duplicate Avalonia resource key in `Styles/Brushes.axaml` that caused startup failure.

### Interactive Smoke Validation (Touched Views)

- [x] Added focused headless interactive smoke tests for:
  - Workflow Editor
  - Recommendations
  - Playlist
  - Health Monitor
- [x] Added missing converter implementations and resource registrations required by these views at runtime.
- [x] Validation command:

```bash
dotnet test tests/SaveState.Presentation.UITests/SaveState.Presentation.UITests.csproj --filter "FullyQualifiedName~TouchedViewsSmokeTests" -v minimal
```

- [x] Result: `Passed: 4, Failed: 0` for touched-view smoke tests.
- [x] Full UI test project verification: `Passed: 16, Failed: 0`.
- [x] Presentation build verification:

```bash
dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj -v minimal
```

- [x] Result: build succeeded, `0` errors.

---

## Final Verification Pass (February 23, 2026)

- [x] Full solution build verification:

```bash
dotnet build SaveStateReborn.sln -c Release
```

- [x] Result: build succeeded, `0` errors.
- [x] Full solution test verification:

```bash
dotnet test SaveStateReborn.sln -c Release --no-build
```

- [x] Result: full solution test run passed.
- [x] Targeted touched-view smoke suite re-run passed (`WorkflowEditor`, `Recommendations`, `Playlist`, `HealthMonitor`).

---

## Execution Order (Recommended)

1. `Styles/Animations.axaml` and converter/member fixes (fast compile wins).
2. `RecommendationsView.axaml` type and style corrections.
3. `WorkflowEditorView.axaml` icon/property migration.
4. `PlaylistView.axaml` `ColumnDefinitions` refactor.
5. Virtualization attached-property cleanup and comment corrections.
6. Final scan + clean-build validation.

---

## Risks and Mitigations

1. **Visual drift after icon/text adjustments**
   - Mitigation: quick UI smoke pass on edited views after successful build.

2. **Behavior drift in list/grid mode (`PlaylistView`)**
   - Mitigation: verify both `IsGridView=true` and `false` states.

3. **Performance perception changes after virtualization-property cleanup**
   - Mitigation: preserve supported virtualization mechanisms; avoid unsupported attributes and document actual behavior.

---

## Definition of Done

1. Clean solution build passes with no Avalonia XAML compile errors.
2. All files in the baseline inventory are fixed or intentionally rewritten with equivalent behavior.
3. Static scan has no remaining instances of known-invalid patterns above (or each remaining instance is proven valid).
4. Interactive smoke checks pass for touched views: Workflow Editor, Recommendations, Playlist, Health Monitor.
5. Full solution build and test verification pass after remediation changes.
6. Plan file remains as the execution checklist for this remediation effort.
