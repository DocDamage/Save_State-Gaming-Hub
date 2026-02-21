# Comprehensive Technical Debt Audit Report

**Audit Date**: February 10, 2026  
**Project**: `SaveStateReborn`  
**Scope**: First-party product code and test infrastructure (`src`, `tests`, `tools`, `scripts`, `monitoring`)  
**Method**: Live build/test validation + static debt signal extraction

---

## Executive Summary

### Overall Health Score: 68/100 (Moderate Debt)

The codebase compiles cleanly and many core test suites pass, but there is significant structural debt in project topology, test execution reliability, and maintainability hotspots.

### Most Important Findings

1. **Project topology drift**: 61 projects in `src/tests/tools` are outside `SaveStateReborn.sln`, including **58 plugin projects**.
2. **Test execution reliability debt**:
   - `SaveState.IntegrationTests` discovers **0 tests**.
   - `SaveState.Tests.Infrastructure` test run aborts due runtime dependency resolution.
   - Full `EndToEnd` execution exceeds practical local timeout (10+ minutes in this audit run).
3. **Maintainability hotspots**:
   - 18 non-migration C# files exceed 1,000 LOC.
   - Broad `catch (Exception)` usage is very high (2,050 matches).
4. **Governance debt**:
   - Global and project-level analyzer suppressions are widespread (`NoWarn` entries: 36; `#pragma warning disable`: 5).
5. **Repository hygiene debt**:
   - 152 large artifact-style files in repo root (`.txt/.log/.bak/.db/.dat/.md`), including multi-GB-equivalent historical log footprint.

---

## Scope and Validation Snapshot

### Build / Runtime Baseline

- `dotnet build SaveStateReborn.sln -v minimal`: **0 warnings, 0 errors**.
- SDK detected: `.NET 10.0.100-rc.1` (preview SDK building `net9.0` targets).

### Targeted Test Validation

Validated test projects (targeted runs, `--no-build` unless noted):

- `SaveState.Core.Tests`: 152 passed
- `SaveState.Application.Tests`: 96 passed
- `SaveState.Infrastructure.Tests`: 172 passed, 29 skipped
- `SaveState.Presentation.Tests`: 46 passed
- `SaveState.CrossPlatform.Tests`: 31 passed
- `SaveState.Configuration.Tests`: 42 passed
- `SaveState.Monitoring.Tests`: 36 passed
- `SaveState.Accessibility.Tests`: 18 passed
- `SaveState.LoadTests`: 6 passed
- `SaveState.Presentation.UITests`: 1 passed
- `SaveState.EndToEndTests` smoke (`TestFramework_CanExecuteTests_Success`): 1 passed

Aggregated from validated runs: **601 passed, 0 failed, 29 skipped (630 discovered)**.

### Test Gaps Observed

- `SaveState.IntegrationTests`: **No tests discovered**.
- `SaveState.Tests.Infrastructure`: testhost abort (`Ardalis.GuardClauses` runtime dependency not resolved in test execution).
- Full `SaveState.EndToEndTests` run exceeded a 10-minute timeout in this audit attempt.

---

## Quantitative Debt Metrics

| Metric | Value | Notes |
| --- | ---: | --- |
| C# files scanned | 1,641 | `src/tests/tools/scripts/monitoring` |
| C# LOC scanned | 213,748 | Same scope |
| `src` LOC | 199,629 | Product code only |
| Mugen-path LOC | 68,141 | 34.13% of `src` LOC |
| Non-migration files >1000 LOC | 18 | Major maintainability hotspots |
| `TODO/FIXME/HACK/XXX` markers | 14 | Mostly scripts/tests, low in product code |
| `async void` occurrences | 5 | Event handlers/lifecycle points |
| `Thread.Sleep(...)` occurrences | 4 | Blocking calls in runtime code |
| `DateTime.Now` occurrences | 105 | Mixed business/UI usage |
| `catch (Exception ...)` occurrences | 2,050 | Broad catch footprint is high |
| `GetAwaiter().GetResult()` occurrences | 5 | Sync-over-async risk points |
| `NoWarn` entries in csproj files | 36 | Plus global suppression in props |
| `#pragma warning disable` | 5 | Includes non-generated code |
| Root artifact-like files | 152 | Logs, db backups, reports, dumps |

---

## Severity-Ranked Findings

## Critical

### C1. Solution Coverage Is Incomplete (Large Unvalidated Surface)

**Evidence**

- `SaveStateReborn.sln` includes 19 projects.
- `src/tests/tools` contain 80 `.csproj` files total.
- **61 projects are outside the solution**, including **58 plugin projects**.
- `src/SaveState.CLI/SaveState.CLI.csproj` is outside solution but referenced by `src/SaveState.Presentation/SaveState.Presentation.csproj:25`.

**Impact**

- CI/build confidence is overstated when validating only `SaveStateReborn.sln`.
- Out-of-solution projects can silently regress (compile/package/runtime), especially plugin ecosystem code.

**Recommended action**

- Define canonical build topology:
  - `SaveStateReborn.sln` (core app)
  - `SaveStateReborn.All.sln` or solution filters for plugin matrix validation
- Add CI lanes to compile/test plugin projects at least nightly.

### C2. Test Infrastructure Has Real Blind Spots

**Evidence**

- `tests/SaveState.IntegrationTests/SaveState.IntegrationTests.csproj` has xUnit + SDK but no runner package; test discovery returns zero tests in this environment.
- `tests/SaveState.Tests.Infrastructure/SaveState.Tests.Infrastructure.csproj` lacks `Microsoft.NET.Test.Sdk`; `dotnet test` aborts with missing dependency runtime resolution.
- `tests/SaveState.EndToEndTests/SaveState.EndToEndTests.csproj:11` includes a TODO noting tests need updating.
- Full E2E execution exceeded timeout in audit run.

**Impact**

- Integration and infrastructure regression signals are unreliable.
- Long-running E2E pipelines slow feedback and reduce developer confidence.

**Recommended action**

- Standardize test project template (SDK + runner + deterministic settings).
- Fix discovery for `SaveState.IntegrationTests`.
- Convert E2E suite to tiered execution (`smoke`, `full`, `perf`) with explicit CI budgets.

## High

### H1. Monolithic Service/ViewModel Files Increase Change Risk

**Evidence**

18 non-migration files exceed 1,000 LOC, for example:

- `src/SaveState.Application/Mugen/Services/UiUxEnhancementService.cs`
- `src/SaveState.Application/Mugen/Services/VrArIntegrationService.cs`
- `src/SaveState.CLI/Commands/MugenCommands.cs`
- `src/SaveState.Presentation/Services/DialogService.cs`
- `src/SaveState.Presentation/ViewModels/Shell/MugenHubViewModel.cs`

**Impact**

- Higher regression probability per change.
- Harder onboarding and test isolation.
- Lower ability to parallelize work safely.

**Recommended action**

- Split by capability boundaries (query handlers, orchestration services, policy modules, serializers, UI interaction coordinators).
- Enforce a soft limit (for example 500 LOC/file for non-generated application code).

### H2. Broad Exception Catching Is Excessive

**Evidence**

- `catch (Exception ...)` matches: 2,050.
- High-density examples:
  - `src/SaveState.Presentation/Services/DialogService.cs` (many broad catches)
  - `src/SaveState.Infrastructure/RetroArch/RetroArchService.cs`
  - `src/SaveState.Infrastructure/OpenMK/OpenMKService.cs`

**Impact**

- Error semantics are blurred.
- Recoverability policy is unclear.
- Failures can be unintentionally masked.

**Recommended action**

- Replace broad catches with typed exception handling plus centralized fallback policy.
- Require explicit rationale where `catch (Exception)` remains.

### H3. Analyzer Governance Is Suppression-Heavy

**Evidence**

- Global suppression baseline in `Directory.Build.props:14`.
- Global package baseline in `Directory.Build.props:22`.
- `NoWarn` entries across csproj files: 36.
- Additional local suppressions: `#pragma warning disable` in:
  - `src/SaveState.Infrastructure/Common/MemoryCacheService.cs:54`
  - `src/SaveState.Infrastructure/Persistence/Migrations/20260116010730_OpenMKProgressAndMatchState.cs:4`
  - `tests/SaveState.LoadTests/DatabaseLoadTests.cs:1`
  - plus generated migration files.

**Impact**

- Quality gates are weakened and drift-prone.
- It becomes harder to distinguish intentional exceptions from legacy residue.

**Recommended action**

- Move from blanket suppression to scoped suppressions with expiration comments.
- Track suppression count as a KPI and ratchet down monthly.

## Medium

### M1. Time and Blocking Patterns Need Consolidation

**Evidence**

- `DateTime.Now` occurrences: 105 (`src`, `tests`, `tools`, `scripts`, `monitoring`).
- Blocking sleeps:
  - `src/SaveState.Infrastructure/GameLibrary/Services/PerformanceMetricsCollector.cs:115`
  - `src/SaveState.Infrastructure/GameLibrary/Services/PerformanceMetricsCollector.cs:126`
  - `src/SaveState.Infrastructure/GameLibrary/Services/PerformanceMetricsCollector.cs:168`
  - `src/SaveState.Infrastructure/Performance/PerformanceMonitor.cs:355`
- Sync-over-async:
  - `tests/SaveState.Tests.Infrastructure/BaseTests.cs:122`
  - `tests/SaveState.Application.Tests/RomManagement/LiveSyncServiceTests.cs:37`
  - `tests/SaveState.EndToEndTests/IntegrationTestFixture.cs:70`
  - `src/SaveState.Infrastructure/GameLibrary/Services/GameMemoryReader.cs:321`
  - `src/SaveState.Presentation/Services/Performance/VirtualizedCollection.cs:43`

**Impact**

- Harder deterministic testing and time abstraction.
- Potential deadlocks or responsiveness degradation in mixed sync/async paths.

**Recommended action**

- Introduce/standardize a time provider in business/infrastructure services.
- Replace `Thread.Sleep` with awaitable delay or event-driven synchronization.

### M2. One Deprecated Dependency in Active UI Layer

**Evidence**

- `dotnet list ... package --deprecated` flags:
  - `Avalonia.Xaml.Behaviors` in `src/SaveState.Presentation/SaveState.Presentation.csproj:37`
  - recommended alternative: `Xaml.Behaviors.Avalonia`.

**Impact**

- Increased maintenance risk and upgrade friction.

**Recommended action**

- Migrate to the replacement package and add an upgrade regression checklist for UI interactions.

### M3. Repository Artifact Sprawl

**Evidence**

- 152 artifact-like files in repo root, including:
  - `savestate.db.bak` (~75 MB)
  - `msbuild.log` (~13 MB)
  - multiple 7-11 MB build logs/reports.

**Impact**

- Larger repo surface, slower tooling operations, and noise in audit/navigation workflows.

**Recommended action**

- Move ephemeral outputs to ignored directories (`artifacts/`, `logs/`).
- Tighten `.gitignore` and introduce cleanup script in CI/local workflows.

## Low

### L1. Inline Debt Markers Are Low but Not Zero

**Evidence**

- Marker count is relatively low in first-party scope; notable TODOs:
  - `tests/SaveState.EndToEndTests/SaveState.EndToEndTests.csproj:11`
  - `tests/SaveState.Presentation.Tests/ViewModels/ViewModelTests.cs:64`

**Impact**

- Manageable, but these indicate acknowledged test debt not yet closed.

---

## Positive Signals

- Main solution build is currently clean (0 errors, 0 warnings).
- No vulnerable packages reported in solution scope.
- Broad set of core/unit/infra/ui test suites execute successfully and quickly.

---

## 30/60/90 Day Remediation Plan

### Next 30 Days (Stabilize Validation Surface)

1. Define and commit canonical solution topology for core + plugins.
2. Fix test project consistency:
   - make integration test discovery deterministic
   - align infrastructure test project packaging/execution.
3. Split full E2E into smoke/full/perf lanes with explicit timeout budgets.
4. Add CI metric reporting for:
   - projects built vs total projects
   - discovered tests vs expected tests.

### Days 31-60 (Reduce Structural Risk)

1. Refactor top 5 largest non-generated files into bounded components.
2. Reduce broad catch usage in top offender files by at least 30%.
3. Replace deprecated Avalonia behavior package.
4. Remove or scope high-impact global suppressions where feasible.

### Days 61-90 (Institutionalize Debt Control)

1. Add debt quality gates:
   - max new files > 700 LOC
   - no new blanket `NoWarn` without justification.
2. Add monthly debt trend reporting from scripted metrics.
3. Enforce repository artifact policy (no root build dumps/backups).

---

## Suggested Tracking KPIs

- `% projects validated in CI` (target: 100% of intended shippable projects)
- `integration tests discovered` (target: non-zero and stable)
- `E2E runtime budget` (target: smoke < 5 min, full < 20 min)
- `files >1000 LOC` (target: reduce from 18 to <= 8)
- `broad catch count` (target: month-over-month reduction)
- `NoWarn + pragma suppression count` (target: month-over-month reduction)

---

## Evidence References

- `Directory.Build.props:14`
- `Directory.Build.props:22`
- `src/SaveState.Presentation/SaveState.Presentation.csproj:37`
- `src/SaveState.Presentation/SaveState.Presentation.csproj:25`
- `tests/SaveState.EndToEndTests/SaveState.EndToEndTests.csproj:8`
- `tests/SaveState.EndToEndTests/SaveState.EndToEndTests.csproj:11`
- `tests/SaveState.IntegrationTests/SaveState.IntegrationTests.csproj:11`
- `tests/SaveState.Tests.Infrastructure/SaveState.Tests.Infrastructure.csproj:1`
- `tests/SaveState.Tests.Infrastructure/SaveState.Tests.Infrastructure.csproj:11`
- `src/SaveState.Infrastructure/GameLibrary/Services/PerformanceMetricsCollector.cs:115`
- `src/SaveState.Infrastructure/Performance/PerformanceMonitor.cs:355`
- `src/SaveState.Infrastructure/GameLibrary/Services/GameMemoryReader.cs:321`
- `src/SaveState.Presentation/Services/Performance/VirtualizedCollection.cs:43`
- `src/SaveState.Presentation/Services/DialogService.cs:83`
- `src/SaveState.Infrastructure/RetroArch/RetroArchService.cs:72`
- `src/SaveState.Infrastructure/OpenMK/OpenMKService.cs:42`

