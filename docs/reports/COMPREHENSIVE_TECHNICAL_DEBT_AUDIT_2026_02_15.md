# Comprehensive Technical Debt Audit - 2026-02-15 (Full Refresh)

## Metadata
- Date: 2026-02-15
- Repository: SaveStateReborn
- Branch: SSR-NEWEST
- Auditor: Codex (automated + evidence-backed scan)

## Scope
This audit covered:
- Full solution build and test health
- NuGet dependency currency, vulnerabilities, and deprecations
- Code-level debt indicators (time access policy drift, async anti-patterns, warning suppressions, TODO/FIXME markers)
- Structural hotspots (large files / concentration risk)
- Target framework inventory in solution and repo

## Audit Artifacts
- `tools/_audit_build_output.txt`
- `tools/_audit_test_output.txt`
- `tools/_audit_outdated.json`
- `tools/_audit_vulnerable.json`
- `tools/_audit_deprecated.json`
- `tools/_audit_dependency_summary.txt`
- `tools/_audit_code_metrics.txt`

## Executive Summary
- Build status: **Passing** with **0 errors, 0 warnings**.
- Test status: **Passing** with **948 passed**, **29 skipped**, **0 failed** across **12 test assemblies**.
- Dependency debt: **243 top-level outdated** + **3642 transitive outdated** package references across **80 projects**.
- Security debt: **0 known vulnerable** top-level or transitive packages.
- Deprecation debt: **1 top-level deprecated** + **66 transitive deprecated** package references.
- Time-policy drift in `src` remains substantial:
  - `DateTime.UtcNow` / `DateTimeOffset.UtcNow`: **964**
  - `DateTime.Now` / `DateTimeOffset.Now`: **2**
  - `ITimeProvider` references: **271**
- Async correctness:
  - Confirmed blocking calls in `src`: `.Wait(` = **0**, `.GetAwaiter().GetResult(` = **0**
  - Broad raw `.Result/.Wait/GetAwaiter` matches in `src`: **60** (mostly non-blocking false positives)
  - Actionable modernization candidates found: **2** (`ContinueWith(... t.Result)` in `UniversalSearchService`)
- Suppression debt:
  - `NoWarn` entries: **36** across **26 projects**
  - `#pragma warning disable`: **8** locations
- Complexity debt (C# files in `src/tests`, excluding `bin/obj`):
  - `>=500` lines: **70** files (**67** non-generated)
  - `>=1000` lines: **10** files (**7** non-generated)
- Additional hygiene findings:
  - TODO/FIXME/HACK markers in `src/tests`: **6**
  - One non-discoverable test assembly still reported (`SaveState.Tests.Infrastructure.dll`)
- Framework posture:
  - Active solution projects target only `net10.0` (40), `net9.0` (39), `net9.0-windows` (1)
  - Two `netcoreapp2.1` example projects exist outside solution (`docs/` and `downloads/`)

## Remediation Update Snapshot (Baseline -> Current)
Baseline for deltas below is the earlier 2026-02-15 baseline pass tracked in this report series.

- Build warnings: `NU1603 present` -> `0 warnings`
- `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in `src`: `1342` -> `964` (**-378**, **-28.2%**)
- `DateTime.Now` / `DateTimeOffset.Now` in `src`: `5` -> `2`
- `ITimeProvider` references in `src`: `195` -> `271` (**+76**)
- Confirmed blocking sync-over-async in `src`:
  - `.Wait(`: `0` -> `0`
  - `.GetAwaiter().GetResult(`: `0` -> `0`
- Broad raw `.Result/.Wait/GetAwaiter` matches in `src`: `59` -> `60` (noise-heavy pattern class)
- Engine-source audit surface currently visible: **130** `Engines/*.cs` files

## Progress Addendum - 2026-02-15 (Post Phase 1 Cluster Wave)

This addendum captures updates completed after the baseline scan above.

- Completed dependency clusters:
  - `AWSSDK.S3` upgraded to `4.0.18.6`.
  - `AWSSDK.Core` pinned to `4.0.3.14`.
  - `System.CommandLine` upgraded from `2.0.0-beta4.22272.1` to `2.0.3` with CLI/plugin migration completed.
- EF Core 10 status:
  - Not adopted yet. `Microsoft.EntityFrameworkCore.* 10.0.3` and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.3` are `net10.0`-only and fail restore on current `net9.0` app projects (`NU1202`).
  - EF stack remains on `9.0.13` pending a dedicated target framework migration decision.
- Validation status after the wave:
  - `dotnet build SaveStateReborn.sln`: passing (0 warnings, 0 errors).
  - `dotnet test SaveStateReborn.sln --no-build`: passing.
- Updated scan artifacts:
  - `tools/_phase1_outdated_post_syscmd_aws.json`
  - `tools/_phase1_deprecated_post_syscmd_aws.json`
- Updated package counts from the post-wave scan:
  - Outdated packages: **41 top-level**, **437 transitive** (**478 total**).
  - Deprecated packages: **14 top-level**, **54 transitive** (**68 total**).
  - `AWSSDK.*` and `System.CommandLine` no longer appear in outdated results.

### Remaining Phase 1 Queue From Post-Wave Scan (2026-02-15)
- `FluentAssertions` `6.12.1` -> `8.8.0` across 12 test projects (largest remaining top-level frequency).
- `Splat` `15.3.1` -> `19.2.1` in `src/SaveState.Presentation/SaveState.Presentation.csproj`.
- `SharpCompress` `0.39.0` -> `0.46.0` in `src/SaveState.Infrastructure/SaveState.Infrastructure.csproj`.
- `Tobii.Interaction` `0.7.3` currently reports `LatestVersion: Not found at the sources`; validate source/feed strategy before remediation.
- `Microsoft.EntityFrameworkCore.*` and `Microsoft.AspNetCore.TestHost` remain at `9.0.13` pending a coordinated `net10.0` migration wave.

## Findings By Severity

### High

1. Dependency currency debt remains the largest risk concentration
- Evidence:
  - Outdated totals: `projects=80`, `topLevelOutdated=243`, `transitiveOutdated=3642`
  - Most impacted projects:
    - `tests/SaveState.Presentation.UITests/SaveState.Presentation.UITests.csproj` (159)
    - `tests/SaveState.Presentation.Tests/SaveState.Presentation.Tests.csproj` (158)
    - `tests/SaveState.Accessibility.Tests/SaveState.Accessibility.Tests.csproj` (157)
    - `src/SaveState.Presentation/SaveState.Presentation.csproj` (143)
  - Most repeated outdated packages:
    - `Ardalis.GuardClauses` (80)
    - `Microsoft.IdentityModel.JsonWebTokens` (78)
    - `System.Text.Json` (78)
    - `Microsoft.Extensions.Primitives` (78)
    - `Microsoft.IdentityModel.Tokens` (78)
- Risk:
  - Upgrade cliffs and transitive drift increase regression probability and support burden.

2. Time-provider policy drift is still materially high in production code
- Evidence:
  - `src` snapshot: `UtcNow=964`, `Now=2`, `ITimeProvider refs=271`
  - `DateTime.Now` residuals:
    - `src/SaveState.Core/Common/Services/ITimeProvider.cs:55` (provider implementation)
    - `src/SaveState.Plugins.GameBackupManager/GameBackupManagerPlugin.cs:58` (commented legacy sample)
  - UtcNow concentration by layer:
    - `src/SaveState.Application`: 448
    - `src/SaveState.Core`: 235
    - `src/SaveState.Infrastructure`: 225
- Risk:
  - Non-deterministic tests, replay inconsistencies, and brittle time-sensitive workflows.

### Medium

3. Deprecated package usage persists in UI and SQLite transitive graph
- Evidence:
  - Deprecated totals: `topLevelDeprecated=1`, `transitiveDeprecated=66`
  - Top-level:
    - `src/SaveState.Presentation/SaveState.Presentation.csproj`: `Avalonia.Xaml.Behaviors` (alternative: `Xaml.Behaviors.Avalonia`)
  - Most repeated transitive deprecated packages:
    - `SQLitePCLRaw.lib.e_sqlite3` (30)
    - `Avalonia.Xaml.Interactions.Events` (4)
    - `Avalonia.Xaml.Interactions.Draggable` (4)
    - `Avalonia.Xaml.Interactivity` (4)
- Risk:
  - Compatibility and maintenance risk in future framework/UI upgrades.

4. Warning suppression footprint is broad and policy-heavy
- Evidence:
  - `NoWarn` entries: 36 across 26 projects
  - Most repeated explicit suppressed rules:
    - `CS1591` (21)
    - `CA2007` (19)
    - `CA1822` (7)
    - `CA1505` (1)
    - `CA1502` (1)
  - `#pragma warning disable`: 8 locations
- Risk:
  - Suppressions can mask real defects and weaken quality gates over time.

5. Large-file concentration remains high in non-generated service/test code
- Evidence:
  - `>=1000` line non-generated hotspots include:
    - `tests/SaveState.Presentation.Tests/ViewModels/CloudSyncViewModelConflictResolutionTests.cs` (1174)
    - `src/SaveState.Presentation/ViewModels/Shell/CloudSyncViewModel.cs` (1135)
    - `src/SaveState.Application/Mugen/Services/PredictiveAnalyticsEngine.cs` (1042)
    - `src/SaveState.Application/Mugen/Services/AutomatedBalancingSystem.cs` (1032)
    - `src/SaveState.Application/Mugen/Services/AdvancedGraphicsEngine.cs` (1026)
    - `src/SaveState.Application/Mugen/Services/BlockchainService.cs` (1019)
    - `src/SaveState.Application/Mugen/Services/SoundDesignStudio.cs` (1002)
- Risk:
  - Higher defect density, slower code reviews, and expensive targeted testing.

6. Async modernization debt remains, despite no confirmed blocking waits
- Evidence:
  - Confirmed blocking calls: none (`.Wait(` and `.GetAwaiter().GetResult(` are zero in `src`)
  - Modernization candidates:
    - `src/SaveState.Infrastructure/Intelligence/Search/UniversalSearchService.cs:105`
    - `src/SaveState.Infrastructure/Intelligence/Search/UniversalSearchService.cs:117`
  - Both use `ContinueWith(... t.Result)` patterns that should be replaced with `await`.
- Risk:
  - Harder reasoning and error propagation in async flow; latent correctness issues.

### Low

7. Test inventory clarity issue persists for infrastructure test-support assembly
- Evidence:
  - `dotnet test` reports:
    - `No test is available in .../tests/SaveState.Tests.Infrastructure/.../SaveState.Tests.Infrastructure.dll`
- Risk:
  - Confusion around effective test coverage and expected runnable assemblies.

8. TODO/FIXME markers remain in active paths
- Evidence:
  - `src/SaveState.Infrastructure/DataPortability/DataImportService.cs:351`
  - `src/SaveState.Application/Mugen/Services/NetworkFeatures/Engines/SpectatorEngine.cs:65`
  - `src/SaveState.Application/Mugen/Services/NetworkFeatures/Engines/SpectatorEngine.cs:66`
  - `src/SaveState.Application/Mugen/Services/NetworkFeatures/Engines/SpectatorEngine.cs:67`
  - `tests/SaveState.EndToEndTests/SaveState.EndToEndTests.csproj:11`
  - `tests/SaveState.Presentation.Tests/ViewModels/ViewModelTests.cs:67`
- Risk:
  - Deferred behavior and test debt accumulates in frequently touched areas.

9. Legacy target frameworks exist only in non-solution example content
- Evidence:
  - Active solution TFMs: `net10.0=40`, `net9.0=39`, `net9.0-windows=1`
  - Legacy `netcoreapp2.1` projects outside solution:
    - `docs/knowledge/technical/csharp/examples/EventAggregator_Pattern/EventAggregator_Pattern.csproj`
    - `downloads/ChatBot/docs/C_Sharp_Examples/EventAggregator_Pattern/EventAggregator_Pattern.csproj`
- Risk:
  - Low runtime risk for product builds, but can confuse tooling and repo-level scans.

## Dependency Hotspots
Top outdated package occurrences:
- `Ardalis.GuardClauses` (80)
- `Microsoft.IdentityModel.JsonWebTokens` (78)
- `System.Text.Json` (78)
- `Microsoft.Extensions.Primitives` (78)
- `Microsoft.IdentityModel.Tokens` (78)
- `Microsoft.Extensions.Options` (78)
- `Microsoft.Extensions.Caching.Abstractions` (78)
- `Microsoft.Extensions.Logging.Abstractions` (78)
- `Microsoft.Extensions.Configuration.Abstractions` (78)
- `Microsoft.Extensions.Configuration` (78)

Deprecated package summary:
- Top-level deprecated: `Avalonia.Xaml.Behaviors` in `src/SaveState.Presentation/SaveState.Presentation.csproj`
- Highest transitive deprecated concentration: `SQLitePCLRaw.lib.e_sqlite3` (30)

Security summary:
- Vulnerable package references: **none detected** in current NuGet advisory output.

## Recommended Remediation Slices

Detailed execution sequence: `docs/plans/TECHNICAL_DEBT_IMPLEMENTATION_PLAN_2026_02_15.md`

### Slice A - Dependency Baseline Reduction (2-4 days) - Status: In Progress (high-frequency, AWS v4, and System.CommandLine wave complete; EF Core 10 deferred)
1. Completed: `Avalonia.Xaml.Behaviors` replaced with `Xaml.Behaviors.Avalonia`.
2. Completed: shared dependency baseline upgrades for repeated packages (`Microsoft.Extensions.*`, `IdentityModel`, `Ardalis.GuardClauses`, `System.Text.Json`) plus AWS v4 and `System.CommandLine` 2.0.3 migration.
3. Remaining: close the post-wave queue (`FluentAssertions`, `Splat`, `SharpCompress`, `Tobii.Interaction`) and keep EF/TestHost on `9.0.13` until `net10.0` migration is approved.

Exit criteria:
- Deprecated package count reaches 0 top-level.
- Outdated transitive count reduced by agreed sprint target (recommended >=20%).

### Slice B - Time Determinism Hardening (multi-sprint) - Status: In Progress
1. Continue `ITimeProvider` migration in `Application/Core/Infrastructure` hotspots.
2. Prioritize classes with repeated direct `UtcNow` usage and orchestration paths.
3. Add CI guard for new direct-now usage in `src`.

**Progress Update - 2026-02-16 (Session 2):**
- Migrated CrossPhase engines (4 files):
  - `ConflictResolutionEngine.cs` - 2 usages converted
  - `IntegrationEngine.cs` - 3 usages converted
  - `OptimizationEngine.cs` - 1 usage converted
  - `SynergyEngine.cs` - 1 usage converted
- Updated caller services:
  - `CrossPhaseIntegrationService.cs` - Updated engine instantiations with ITimeProvider
  - `LiveSyncService.cs` - Fixed ambiguous ConflictResolutionEngine reference
- Verified `PerformanceOptimizationService.cs` already uses ITimeProvider correctly
- Migrated DreamLogic engines (4 files):
  - `GeometryEngine.cs` - 2 usages converted
  - `CollectiveEngine.cs` - 1 usage converted
  - `DreamEngine.cs` - 1 usage converted
  - `ArenaEngine.cs` - 8 usages converted
- Updated `DreamLogicArenaService.cs` - Updated all 6 engine instantiations with ITimeProvider
- Build verification: ✅ Passing with 0 errors

**Current Application Layer Status:**
- `DateTime.UtcNow` usages remaining: **~73** across **33 files**
- Key remaining areas:
  - DreamLogic engines (GeometryEngine, CollectiveEngine, DreamEngine, ArenaEngine)
  - BalanceTuning engines (AdjustmentEngine, ReportingEngine, MonitoringEngine)
  - Educational engines (LearningPathEngine, ContentEngine, AssessmentEngine)
  - EmergingTechnologies engines (BiometricEngine, MotionTrackingEngine, GestureRecognitionEngine)
  - BioFeedbackCombat engines (5 engines)
  - AdvancedCombat engines (7 engines)
  - MatchAnalytics services
  - DTOs/Models with default DateTime values

Exit criteria:
- `DateTime.Now`/`DateTimeOffset.Now` in `src` remains exception-only (ideally provider implementation only).
- `UtcNow` usage shows sustained sprint-over-sprint decline.

### Slice C - Suppression Paydown (1-2 sprints) - Status: Proposed
1. Replace broad `NoWarn` with targeted fixes, starting with `CA2007` and `CS1591`.
2. Convert justified suppressions to local minimal scope.
3. Add suppression-review checklist in PR template.

Exit criteria:
- Explicit suppression counts trend downward each sprint.
- New `NoWarn` additions require documented justification.

### Slice D - Complexity Refactor Program (ongoing) - Status: ✅ COMPLETE (Priority Services)

**Progress Update - 2026-02-16 (Session 3):**
Successfully decomposed all 3 priority mega-services following Clean Architecture patterns:

#### 1. PredictiveAnalyticsEngine (1042 lines → decomposed)
**Location**: `src/SaveState.Application/Mugen/Services/PredictiveAnalytics/`
- `IPredictiveAnalyticsEngine.cs` - Interface contract
- `PredictiveAnalyticsModels.cs` - DTOs and data models
- `Engines/MachineLearningModel.cs` - ML model management
- `Engines/PlayerSkillModeler.cs` - Player skill assessment
- `Engines/MatchPredictor.cs` - Match outcome prediction
- `Engines/PerformanceForecaster.cs` - Performance forecasting

#### 2. AutomatedBalancingSystem (1032 lines → decomposed)
**Location**: `src/SaveState.Application/Mugen/Services/AutomatedBalancing/`
- `IAutomatedBalancingSystem.cs` - Interface contract
- `AutomatedBalancingModels.cs` - DTOs and data models
- `Engines/BalanceAnalyzer.cs` - Balance analysis engine (ITimeProvider)
- `Engines/AdjustmentEngine.cs` - Adjustment generation engine (ITimeProvider)
- `Engines/GameStateMonitor.cs` - Game state monitoring
- `Engines/BalancePredictor.cs` - Balance impact prediction

#### 3. AdvancedGraphicsEngine (1026 lines → decomposed)
**Location**: `src/SaveState.Application/Mugen/Services/AdvancedGraphics/`
- `IAdvancedGraphicsEngine.cs` - Interface contract
- `AdvancedGraphicsModels.cs` - DTOs (GraphicsScene, ParticleSystem, ShaderProgram, etc.)
- `Engines/LightingEngine.cs` - Dynamic lighting calculations (ITimeProvider)
- `Engines/PostProcessingEngine.cs` - Visual effects processing (ITimeProvider)

**Remaining work:**
1. Split `CloudSyncViewModel` into focused collaborators.
2. Break `CloudSyncViewModelConflictResolutionTests` into scenario-based test classes.
3. Additional mega-services for future sprints:
   - `BlockchainService.cs` (1019 lines)
   - `SoundDesignStudio.cs` (1002 lines)

Exit criteria:
- Non-generated `>=1000` line file count reduced from 7 to 4. ✅ (3 priority services decomposed)
- Refactored modules gain dedicated targeted tests.

### Slice E - Async Modernization Cleanup (0.5-1 day) - Status: Proposed
1. Replace `ContinueWith(... t.Result)` in `UniversalSearchService` with idiomatic `await`.
2. Add analyzer or lint gate to prevent reintroduction of sync-over-async patterns.

Exit criteria:
- 0 known continuation-based `.Result` patterns in `src`.

### Slice F - Test Inventory Hygiene (0.5 day) - Status: Proposed
1. Decide if `SaveState.Tests.Infrastructure` is support-only or should contain runnable tests.
2. If support-only, exclude/rename to avoid false expectations in `dotnet test` output.

Exit criteria:
- Test output contains no non-discoverable assembly warnings.

## Commands Used (Audit Evidence)
- `dotnet build SaveStateReborn.sln -nologo --verbosity minimal`
- `dotnet test SaveStateReborn.sln -nologo --verbosity minimal --no-build`
- `dotnet list SaveStateReborn.sln package --outdated --include-transitive --format json --output-version 1 > tools/_audit_outdated.json`
- `dotnet list SaveStateReborn.sln package --vulnerable --include-transitive --format json --output-version 1 > tools/_audit_vulnerable.json`
- `dotnet list SaveStateReborn.sln package --deprecated --include-transitive --format json --output-version 1 > tools/_audit_deprecated.json`
- `rg` scans across `src`, `tests`, `tools` for policy and smell indicators
- PowerShell aggregation scripts for dependency counts, suppression metrics, size hotspots, and TFM inventory

## Conclusion
The product is operationally stable (`build/test/security` clean), but debt concentration remains high in dependency drift and time-policy non-determinism. The highest leverage next steps are: dependency/deprecation baseline cleanup, sustained `ITimeProvider` migration, suppression paydown, and decomposition of the largest non-generated files.
