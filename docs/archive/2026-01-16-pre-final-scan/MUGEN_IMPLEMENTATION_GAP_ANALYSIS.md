# MUGEN/IKEMEN Implementation Gap Analysis

**Date**: January 11, 2026
**Version**: 1.0
**Status**: 🚧 Partial Implementation Detected

## Executive Summary

A deep code scan revealed a significant discrepancy between the `Backend Infrastructure` (which is ~90% complete) and the `User Interface/Logic Integration` (which is only ~40% complete). While the domain models, services, and repositories exist, many UI features are currently placeholders or "dead" controls that do not trigger the underlying logic.

This document serves as the remediation plan to bridge the gap between the backend capabilities and the user experience.

---

## 🔴 Priority 1: Missing Logic & "Dead" Controls

These features have UI controls that currently do nothing or simulate fake behavior.

### 1. AI Coach Chat Integration

* **Current State**: **Fake / Simulation**
* **Location**: `MugenCoachViewModel.cs` (Line 252)
* **The Issue**: The `SendMessageAsync` method simulates a network delay and returns a hardcoded string ("*Based on my analysis, keep practicing your spacing.*"). It does **not** call the actual `IMugenCoachService`.
* **Backend Readiness**: ✅ **Ready**. `MugenCoachService.cs` is fully implemented with `IAiOrchestrator` integration.
* **Required Action**:
    1. Update `MugenCoachViewModel` to call `_coachService.GetCoachingAdviceAsync` or a new chat method.
    2. Ensure the service can handle conversational context if needed.

### 2. Replay Theater Functionality

* **Current State**: **Incomplete Wire-up / Missing Launcher Support**
* **Location**: `MugenReplayViewModel.cs` & `MugenLauncher.cs`
* **The Issue**:
  * `PlayReplayAsync` in ViewModel is empty: `// Logic to launch MUGEN with replay file`.
  * `AnalyzeReplayAsync` in ViewModel is empty.
  * `MugenLauncher.cs` lacks a specific method to launch IKEMEN with a replay file argument (usually `-r`).
* **Backend Readiness**: ⚠️ **Partial**. `MugenCoachService` has `AnalyzeReplayAsync`, but `MugenLauncher` needs an update.
* **Required Action**:
    1. Add `LaunchReplayAsync(string replayPath)` to `IMugenLauncher` and `MugenLauncher`.
    2. Wire up `PlayReplayCommand` in ViewModel.
    3. Wire up `AnalyzeReplayCommand` to call the Coach service.

### 3. Engine Modifications (Config Editor)

* **Current State**: **UI Shell Only**
* **Location**: `MugenEngineModsViewModel.cs`
* **The Issue**: The `ApplyModsAsync` method is empty: `// Logic to update IKEMEN config.json would go here`. The toggles in the UI change memory state but do not persist to the game engine's configuration files.
* **Backend Readiness**: ❌ **Missing**. No service currently exists to parse/write `config.json` or `system.def`.
* **Required Action**:
    1. Create `IMugenConfigService` to parse JSON/DEF files safely.
    2. Implement mapping between ViewModel booleans (e.g., `ActiveTagEnabled`) and actual config keys.

---

## 🟡 Priority 2: UI Visualizers Required

These features function logic-wise but lack the necessary visual components to be usable.

### 4. Tournament Bracket Visualizer

* **Current State**: **Text Placeholder**
* **Location**: `TournamentSection.axaml` (Line 139)
* **The Issue**: The bracket is represented by a text block: `[Interactive Bracket Visualization]`.
* **Backend Readiness**: ✅ **Ready**. `MugenTournamentService` handles tournament logic, round progression, and match generation.
* **Required Action**:
    1. Create a `TournamentBracketControl` (Custom Avalonia Control or complex ItemsControl).
    2. Visualize the tree structure (Quarters -> Semis -> Finals).
    3. Bind match states (Winner/Loser) to visual lines/connectors.

---

## 🟢 Priority 3: Minor Gaps

* **Training Mode Launching**: `MugenTrainingViewModel` constructs a `TrainingConfig` but doesn't fully utilize all options (hitboxes, input display) when calling the launcher.
* **Fusion Loading State**: The visual feedback during character fusion is basic; could be enhanced with a real progress bar if the backend reported progress.

---

## Action Plan

1. **Immedate Fix**: Wire up the **AI Coach** to the real service (Low effort, high value).
2. **Core Feature**: Implement **Replay Launcher** support in infrastructure (Critical for "Replay Theater").
3. **UI Heavy**: Build the **Tournament Bracket** visualizer (Time consuming, but essential for "Meta-Game").
4. **Utility**: Implement **Config/Engine Mod** persistence (Can be delayed).
