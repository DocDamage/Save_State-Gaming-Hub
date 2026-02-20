# Technical Debt Implementation Plan - 2026-02-20

## Metadata
- Supersedes: `docs/plans/TECHNICAL_DEBT_IMPLEMENTATION_PLAN_2026_02_15.md`
- Source audit: `docs/reports/COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_15.md`
- Horizon: 2 to 4 weeks (close-out wave)
- Baseline snapshot date: 2026-02-20

## Current Baseline (2026-02-20)
- Build: `dotnet build SaveStateReborn.sln --no-restore --verbosity minimal` -> passing (0 warnings, 0 errors)
- Tests: `dotnet test SaveStateReborn.sln --no-build --verbosity minimal` -> passing
- Time policy:
  - Direct `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in `src`: **354**
  - Direct `DateTime.Now` / `DateTimeOffset.Now` in `src`: **2** (exception-only target)
- Suppression footprint:
  - `<NoWarn>` entries: **23** (current scan)
- Dependency posture:
  - `SharpCompress` already migrated to `0.46.2`
  - `Tobii.Interaction` remains `0.7.3` (no update found at configured sources)
  - `Microsoft.EntityFrameworkCore.*` and `Microsoft.AspNetCore.TestHost` remain `9.0.13` pending coordinated `net10.0` wave
- Complexity guardrail:
  - `Non_Migration_Classes_Should_Not_Exceed_1000_Lines` budget at `<=1` (current floor policy: `SaveStateDbContext`)

## Scope To Close
1. Finish Slice B (`ITimeProvider`) for remaining Core/Infrastructure and selected Presentation hotspots.
2. Finish Slice C suppression paydown and add regression guardrails.
3. Finish Slice A deferred dependency queue with explicit decisions and tracked waivers.
4. Close governance gap on class-size metric policy (`DeclaredOnly` modernization vs floor policy).
5. Close remaining documentation drift and align all references to this plan.

## Execution Sequence
1. Workstream B1/B2/B3 (time determinism remaining hotspots)
2. Workstream C1/C2 (suppression cleanup + CI safeguards)
3. Workstream A1/A2 (dependency deferred queue decisions and execution)
4. Workstream G1 (class-size policy decision)
5. Workstream D1 (documentation and status alignment)

## Workstream B - Time Determinism Completion

### B1 - Highest Hotspot Tranches
Target top files first by direct `UtcNow` count:
- `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameSaveStatesTabViewModel.cs` (8)
- `src/SaveState.Presentation/ViewModels/Library/GameDetail/GameSessionsTabViewModel.cs` (5)
- `src/SaveState.Infrastructure/Performance/BatteryOptimizer.cs` (4)
- `src/SaveState.Infrastructure/AiCoOp/AiCoOpCompanionService.cs` (4)
- `src/SaveState.Infrastructure/Streaming/StreamingService.cs` (4)
- `src/SaveState.Infrastructure/Repositories/CommunityRepository.cs` (4)

### B2 - Core/Infrastructure 4-Usage Cluster
- `src/SaveState.Infrastructure/Ai/ChaosTester.cs`
- `src/SaveState.Infrastructure/Ai/ML/DifficultyMlModel.cs`
- `src/SaveState.Infrastructure/Input/TouchController.cs`
- `src/SaveState.Infrastructure/Mugen/Repositories/MugenPlayerDataRepository.cs`
- `src/SaveState.Core/AutoSave/AutoSaveModels.cs`
- `src/SaveState.Core/GameLibrary/Entities/GameNote.cs`

### B3 - Remaining 3-Usage Cluster and Cleanup
Run repeated ranked scans and drain the remaining list in small deterministic batches:
- 4 files per tranche maximum
- full build + no-build test validation after each tranche
- update plan and audit with metric delta each tranche

### B Exit Gates
- `DateTime.Now` / `DateTimeOffset.Now` in `src` remains **2** known exception-only matches (no regressions).
- Direct `UtcNow` in `src` reduced from **354** to **<=100** in this close-out wave.
- No direct `DateTime.Now/DateTimeOffset.Now` introduced in migrated files.
- Tranche evidence appended to audit + plan after each batch.

## Workstream C - Suppression Paydown Completion

### C1 - Project NoWarn Tightening
1. Inventory each remaining `<NoWarn>` entry and classify:
- required and justified
- replaceable with code fix
- replaceable with local pragma scope
2. Remove broad project-level suppressions where fix cost is low and risk is controlled.
3. Keep `CS1591` only where public API surface is intentionally undocumented (plugins/internal tools).

### C2 - Guardrails
1. Add CI step that fails on net increase of `<NoWarn>` count.
2. Add CI step that reports `#pragma warning disable` count delta.
3. Add PR checklist item: any new suppression requires rationale and scope.

### C Exit Gates
- `<NoWarn>` count reduced from **23** to **<=15**.
- No net increase in `#pragma warning disable`.
- Suppression policy documented and enforced in CI.

## Workstream A - Deferred Dependency Queue Closure

### A1 - Tobii Strategy Closure
1. Reconfirm package source availability for `Tobii.Interaction`.
2. If still unavailable:
- keep pinned at `0.7.3`
- document explicit waiver with owner/date/review cadence
- isolate usage behind optional provider boundaries where practical

### A2 - EF/TestHost net10 Migration Gate Plan
1. Produce migration checklist and compatibility matrix for `net10.0` adoption:
- app projects
- tests
- tooling/runtime impacts
2. Schedule controlled migration window and branch strategy.
3. Upgrade `Microsoft.EntityFrameworkCore.*` and `Microsoft.AspNetCore.TestHost` in one coordinated wave after TFM switch.

### A Exit Gates
- `Tobii.Interaction` has explicit disposition (upgraded or waived with review date).
- EF/TestHost migration gate documented with clear go/no-go criteria.
- No stale "SharpCompress remaining" references in docs.

## Workstream G - Class-Size Policy Closure
1. Decide one path and codify it:
- Option A: modernize estimator using declared-only member counting.
- Option B: keep current estimator and formalize `SaveStateDbContext` floor exception policy.
2. Update architecture test comment and debt docs to reflect final policy.

### G Exit Gates
- Architecture guardrail policy documented and consistent across tests + audit + plan.
- No ambiguous "temporary floor" language left in active docs.

## Workstream D - Documentation and Status Alignment
1. Keep a single active technical debt plan (this file).
2. Archive superseded plan files under dated archive folder.
3. Update references in:
- audit report
- development status
- documentation index
- technical debt completion summary
4. Refresh "Remaining Work" section dates and remove stale completed items.

### D Exit Gates
- No active docs point to archived plan path.
- Remaining-work section reflects current baseline and completed tranches.

## Milestones

### Milestone M1 (Week 1)
- B1 complete
- first C1 suppression pass complete
- docs references switched to this plan

### Milestone M2 (Week 2)
- B2 complete
- B3 started with ranked drain
- Tobii disposition finalized

### Milestone M3 (Week 3)
- B close-out target (`UtcNow <=100`) reached or explicitly re-baselined with evidence
- C2 CI safeguards merged
- class-size policy decision merged

### Milestone M4 (Week 4 buffer)
- EF/TestHost migration gate plan approved
- final debt report refresh published

## Weekly Cadence
1. Re-run build/test and debt scans.
2. Record metric deltas in this plan and the comprehensive audit.
3. Do not open new tranches until previous tranche validation is green.

## Required Validation Commands
- `dotnet build SaveStateReborn.sln --no-restore --verbosity minimal`
- `dotnet test SaveStateReborn.sln --no-build --verbosity minimal`
- `rg -n "DateTime\\.UtcNow|DateTimeOffset\\.UtcNow" src`
- `rg -n "DateTime\\.Now|DateTimeOffset\\.Now" src`
- `rg -n "<NoWarn>" src tests`
- `rg -n "ContinueWith\\s*\\(.*\\.Result" src`

## Program Definition Of Done
1. Time determinism, suppression, dependency, and policy workstreams meet exit gates.
2. Build/test remain green at each milestone.
3. Active docs are internally consistent and point to current plan only.
