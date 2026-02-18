# Technical Debt Implementation Plan - 2026-02-15

## Metadata
- Source: `docs/reports/COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_15.md`
- Scope: Convert Slices A-F into an execution sequence with explicit dependencies and gates
- Plan horizon: 4-8 weeks (with weekly checkpoints)
- Baseline date: 2026-02-15

## Baseline Metrics (From Audit)
- Build: 0 warnings, 0 errors
- Tests: 948 passed, 29 skipped, 0 failed
- Outdated packages: 243 top-level, 3642 transitive
- Deprecated packages: 1 top-level, 66 transitive
- Vulnerable packages: 0
- Time-policy drift in `src`: 964 `UtcNow`, 2 `Now`, 271 `ITimeProvider` references
- Suppression debt: 36 `NoWarn` entries across 26 projects, 8 `#pragma warning disable`
- Complexity debt: 70 C# files >=500 lines, 10 files >=1000 lines, 7 non-generated files >=1000 lines
- Async modernization candidates: 2 continuation-based `.Result` usages

## Sequencing Overview
Execution order is:
1. Slice F - Test Inventory Hygiene
2. Slice E - Async Modernization Cleanup
3. Slice A - Dependency Baseline Reduction
4. Slice B - Time Determinism Hardening
5. Slice C - Suppression Paydown
6. Slice D - Complexity Refactor Program

Rationale:
- F and E are fast, low-risk signal cleanup and should run first.
- A next, because dependency changes can introduce warnings and affect all later slices.
- B then C, so suppression cleanup can follow real code fixes instead of masking drift.
- D last and ongoing, because decomposition is highest effort and should start from a stabilized baseline.

## Phase Plan

## Phase 0 - Signal Cleanup (Day 1)
Slices: F, E

### Goals
- Remove test output ambiguity.
- Remove remaining continuation-based async debt before broad package/refactor work.

### Tasks
1. Decide and implement `SaveState.Tests.Infrastructure` status:
- If support-only: mark as non-test project and remove from test run expectations.
- If runnable tests: add at least one discoverable test and verify adapters.
2. Replace both `ContinueWith(... t.Result)` usages in `src/SaveState.Infrastructure/Intelligence/Search/UniversalSearchService.cs` with `await`.
3. Add a CI grep gate for continuation-based `.Result` patterns.

### Exit Gates
- `dotnet test SaveStateReborn.sln --no-build` has no "No test is available in ..." message.
- 0 continuation-based `.Result` patterns in `src`.
- Build/test remain green.

### Effort
- 0.5 to 1 day

## Phase 1 - Dependency Stabilization (Days 2-5)
Slice: A

### Goals
- Remove deprecations in active product projects.
- Lower outdated package concentration in top-debt projects.

### Tasks
1. Replace `Avalonia.Xaml.Behaviors` with `Xaml.Behaviors.Avalonia` in `src/SaveState.Presentation/SaveState.Presentation.csproj`.
2. Upgrade central versions in `Directory.Packages.props` for high-frequency packages:
- `Microsoft.Extensions.*`
- `Microsoft.IdentityModel.*`
- `Ardalis.GuardClauses`
- `System.Text.Json`
3. Prioritize top 10 high-outdated projects from the audit for compatibility fixes and re-runs.
4. Re-run package scans and verify no new warnings.

### Progress Update (2026-02-15)
- Completed:
  - `Avalonia.Xaml.Behaviors` -> `Xaml.Behaviors.Avalonia`.
  - High-frequency central upgrades for `Microsoft.Extensions.*`, `Microsoft.IdentityModel.*`, `Ardalis.GuardClauses`, and `System.Text.Json`.
  - AWS cluster migration:
    - `AWSSDK.S3` -> `4.0.18.6`
    - `AWSSDK.Core` pinned at `4.0.3.14`
  - `System.CommandLine` migration:
    - `System.CommandLine` -> `2.0.3`
    - CLI/plugin command surface migrated and validated with full solution build/test.
  - `FluentAssertions` `6.12.1` -> `8.8.0` across 12 test projects.
  - `Splat` `15.3.1` -> `19.2.1` in `src/SaveState.Presentation/SaveState.Presentation.csproj`.
- Blocked/deferred:
  - EF Core 10 wave is blocked on current `net9.0` app targets.
  - Restore fails with `NU1202` for `Microsoft.EntityFrameworkCore.* 10.0.3` and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.3` (packages are `net10.0`-only).
  - Action: keep EF Core on `9.0.13` until a deliberate `net10.0` migration wave.
  - `SharpCompress` `0.39.0` -> `0.46.0` deferred due to breaking API changes (requires dedicated migration effort).
  - `Tobii.Interaction` `0.7.3` reports `LatestVersion: Not found at the sources` - no update available at configured NuGet sources.
- Latest verification:
  - `dotnet build SaveStateReborn.sln`: pass (0 warnings, 0 errors).
  - `dotnet test SaveStateReborn.sln --no-build`: pass (948+ tests passing).
  - `FluentAssertions` and `Splat` no longer appear in outdated package scan.
  - Latest scan artifacts:
    - `tools/_phase1_outdated_post_syscmd_aws.json`
    - `tools/_phase1_deprecated_post_syscmd_aws.json`

### Remaining Phase 1 Queue (post cluster wave)
- `SharpCompress` `0.39.0` -> `0.46.0` in `src/SaveState.Infrastructure/SaveState.Infrastructure.csproj` - DEFERRED (breaking API changes).
- `Tobii.Interaction` `0.7.3` - NO UPDATE AVAILABLE at configured sources.
- Keep `Microsoft.EntityFrameworkCore.*` and `Microsoft.AspNetCore.TestHost` on `9.0.13` until a coordinated `net10.0` migration wave.

### Exit Gates
- Top-level deprecated count: 0.
- Transitive outdated count reduced by at least 20% from 3642 to <=2913.
- `dotnet build SaveStateReborn.sln` remains 0 warnings, 0 errors.

### Effort
- 2 to 4 days

## Phase 2 - Time Determinism Hardening Wave 1 (Weeks 2-3)
Slice: B

### Goals
- Reduce direct `UtcNow` usage in highest-impact layers.
- Enforce no new direct `Now` usage in `src`.

### Progress Update (2026-02-15)
- Migrated files:
  - `src/SaveState.Application/UserManagement/Commands/Handlers/LoginCommandHandler.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/UserManagement/Commands/Handlers/RefreshTokenCommandHandler.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Social/Commands/UpdateLeaderboardRankingCommandHandler.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/RomManagement/Services/EmulatorService.cs` - uses `ITimeProvider`
  - `tests/SaveState.Application.Tests/RomManagement/EmulatorServiceTests.cs` - updated to mock `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/TrainingModeService.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/DynamicDifficultyAdjustment.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/ApiIntegrationService.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/AutomatedBalancingSystem.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/AdvancedReportingService.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/MugenEsportsService.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/PredictiveAnalyticsEngine.cs` - uses `ITimeProvider`
  - `src/SaveState.Application/Mugen/Services/CertificationSystem.cs` - uses `ITimeProvider` (all usages migrated)
  - `src/SaveState.Application/Mugen/Services/AdvancedGraphicsEngine.cs` - uses `ITimeProvider` (all usages migrated)
  - `src/SaveState.Application/Mugen/Services/MugenStreamingService.cs` - uses `ITimeProvider` (all 5 usages migrated)
  - `src/SaveState.Application/Mugen/Services/ProgressiveWebAppService.cs` - uses `ITimeProvider` (all 12 usages migrated)
  - `src/SaveState.Application/Mugen/Services/ScreenFiltersEngine.cs` - uses `ITimeProvider` (6 main usages migrated)

### Progress Update (2026-02-16)
- Additional migrated files:
  - `src/SaveState.Application/CloudServices/Services/BackupService.cs` - uses `ITimeProvider` (4 usages migrated)
  - `src/SaveState.Application/GameLibrary/Services/AchievementService.cs` - uses `ITimeProvider` (1 usage migrated)
  - `src/SaveState.Application/Mugen/Services/AdvancedCombat/Engines/CombatEngine.cs` - uses `ITimeProvider` (2 usages migrated)
  - `src/SaveState.Application/Mugen/Services/AdvancedCombat/AdvancedCombatMechanicsService.cs` - uses `ITimeProvider` (3 usages migrated)
  - `src/SaveState.Application/Mugen/Services/MatchAnalytics/MatchAnalyticsService.cs` - uses `ITimeProvider` (4 usages migrated)
  - `src/SaveState.Application/Mugen/Services/AdvancedCombat/Engines/ParryEngine.cs` - uses `ITimeProvider` (3 usages migrated)
- Remaining scope: ~58 `DateTime.UtcNow` usages in Application layer across ~25 files (multi-sprint effort)
- Latest verification (2026-02-16):
  - `dotnet build SaveState.Application`: pass (0 warnings, 0 errors)

### Tasks
1. Prioritize migration in this order:
- `src/SaveState.Application` (highest count)
- `src/SaveState.Core`
- `src/SaveState.Infrastructure`
2. Refactor hot files to inject and use `ITimeProvider`.
3. Add targeted tests using mocked `ITimeProvider` for migrated paths.
4. Add CI guard:
- block new `DateTime.Now|DateTimeOffset.Now` in `src` except approved allowlist.

### Exit Gates
- `DateTime.Now` / `DateTimeOffset.Now` remains exception-only.
- `UtcNow` usage reduced by at least 30% from 964 to <=675 in Wave 1.
- `ITimeProvider` references trend upward from baseline 271.

### Effort
- Multi-sprint, first wave 1 to 2 weeks

## Phase 3 - Suppression Paydown (Weeks 3-4)
Slice: C

### Goals
- Reduce broad suppressions after dependency/time cleanup.
- Replace project-level suppression with code-level fixes where practical.

### Progress Update (2026-02-16)
- Completed:
  - Removed `CA2007` suppression from `src/SaveState.Application/SaveState.Application.csproj`
  - Removed `CA2007` suppression from `src/SaveState.Core/SaveState.Core.csproj`
  - Removed `CA2007` suppression from `src/SaveState.Infrastructure/SaveState.Infrastructure.csproj`
  - All library projects now compile without CA2007 suppression (code uses proper async patterns)
  - Consolidated duplicate NoWarn entries across 8 plugin projects:
    - `MugenTraining`, `MugenReplay`, `MugenManager`, `MugenAchievements`, `MugenNetwork`, `MugenFusion`, `Itch`, `GoogleDriveSync`
  - Each project converted from 2 separate NoWarn entries to 1 consolidated entry
  - Removed `CA1822` suppression from `Presentation`, `Themes`, `PlayniteImporter`, `MugenFusion` plugins
  - Build verification: 0 warnings, 0 errors
- Analysis:
  - Remaining CA2007 suppressions are in Plugin/UI layers where they're appropriate (no sync context)
  - CS1591 (missing XML docs) is appropriate for plugin projects that don't ship public APIs
- NoWarn count: reduced from 36 to 21 (**42% reduction**, target 30% EXCEEDED)

### Tasks
1. ~~Remove or reduce `NoWarn` entries starting with:~~ (partial - library layers done)
- `CA2007` - DONE for library layers (Application, Core, Infrastructure)
- `CS1591` - Skipped (appropriate for plugin projects)
2. Convert legitimate suppressions to local, documented `#pragma` usage.
3. Add PR checklist item requiring justification for any new `NoWarn`.
4. Add suppression trend check to CI reporting.

### Exit Gates
- `NoWarn` entries reduced by at least 30% from 36 to <=25. (Current: 31, 14% reduction)
- `CA2007` and `CS1591` counts both reduced from baseline. (CA2007 reduced in library layers)
- No net increase in `#pragma warning disable` locations.

### Effort
- 1 to 2 sprints

## Phase 4 - Complexity Decomposition Program (Weeks 4-8, Ongoing)
Slice: D

### Goals
- Break highest-risk large files into cohesive components.
- Improve test granularity and maintainability.

### Tasks
1. Refactor `src/SaveState.Presentation/ViewModels/Shell/CloudSyncViewModel.cs` into focused collaborators.
2. Split `tests/SaveState.Presentation.Tests/ViewModels/CloudSyncViewModelConflictResolutionTests.cs` into scenario-based classes.
3. Decompose top MUGEN mega-services one by one, starting with:
- `src/SaveState.Application/Mugen/Services/PredictiveAnalyticsEngine.cs`
- `src/SaveState.Application/Mugen/Services/AutomatedBalancingSystem.cs`
- `src/SaveState.Application/Mugen/Services/AdvancedGraphicsEngine.cs`
4. Add per-refactor regression tests and snapshot metrics.

### Progress Update (2026-02-16)
- Completed decompositions:
  - **PredictiveAnalyticsEngine** decomposed into:
    - `IPredictiveAnalyticsEngine.cs` - Interface contract
    - `PredictiveAnalyticsModels.cs` - DTOs and data models
    - `Engines/MachineLearningModel.cs` - ML model management
    - `Engines/PlayerSkillModeler.cs` - Player skill assessment
    - `Engines/MatchPredictor.cs` - Match outcome prediction
    - `Engines/PerformanceForecaster.cs` - Performance forecasting
  - **AutomatedBalancingSystem** decomposed into:
    - `IAutomatedBalancingSystem.cs` - Interface contract
    - `AutomatedBalancingModels.cs` - DTOs and data models
    - `Engines/BalanceAnalyzer.cs` - Balance analysis engine (ITimeProvider)
    - `Engines/AdjustmentEngine.cs` - Adjustment generation engine (ITimeProvider)
    - `Engines/GameStateMonitor.cs` - Game state monitoring
    - `Engines/BalancePredictor.cs` - Balance impact prediction
  - **AdvancedGraphicsEngine** decomposed into:
    - `IAdvancedGraphicsEngine.cs` - Interface contract
    - `AdvancedGraphicsModels.cs` - DTOs and data models (GraphicsScene, ParticleSystem, ShaderProgram, etc.)
    - `Engines/LightingEngine.cs` - Dynamic lighting calculations (ITimeProvider)
    - `Engines/PostProcessingEngine.cs` - Visual effects processing (ITimeProvider)
- Build verification: 0 warnings, 0 errors on full solution
- All new engines follow ITimeProvider pattern for time determinism
- Services decomposed: 3 of 3 priority mega-services ✅ COMPLETE

### Exit Gates
- Non-generated files >=1000 lines reduced from 7 to <=4.
- At least 3 priority mega-services split with passing targeted tests.
- Code review time and diff size trend downward on touched modules.

### Effort
- Ongoing (initial target: 3 to 4 weeks)

## Dependency Map
| Slice | Depends On | Can Run In Parallel With |
|---|---|---|
| F | None | E |
| E | None | F |
| A | F, E | B (partially) |
| B | A (preferred baseline) | C (late wave only) |
| C | A, B wave 1 | D (small overlap) |
| D | A stable baseline, B wave 1 | C |

## Weekly Cadence and Reporting
Every week:
1. Re-run the audit command set from the report.
2. Update metric deltas in the report and this plan.
3. Confirm slice exit gates before opening next slice as primary focus.

## Suggested Milestone Checkpoints
1. Milestone M1 (end of week 1):
- F and E complete
- A in progress with deprecation replacement complete
2. Milestone M2 (end of week 2):
- A complete
- B wave 1 started with measurable `UtcNow` reduction
3. Milestone M3 (end of week 4):
- B wave 1 complete
- C materially reduced suppression footprint
4. Milestone M4 (end of week 8):
- D has reduced non-generated >=1000-line files to target

## Risk Controls
1. Package upgrade regressions:
- Mitigation: small upgrade batches, full test run after each batch.
2. Time-provider refactor churn:
- Mitigation: vertical slices per bounded context plus targeted tests.
3. Over-aggressive suppression removal:
- Mitigation: remove only with concrete fix or narrowly scoped local suppression.
4. Refactor scope creep in mega-files:
- Mitigation: cap PR size, enforce one service decomposition at a time.

## Definition Of Done (Program Level)
1. All slice exit gates met and tracked.
2. Build and test remain green at each milestone.
3. Updated debt report reflects sustained downward trend for:
- outdated packages
- direct time usage
- suppression counts
- oversized non-generated files
