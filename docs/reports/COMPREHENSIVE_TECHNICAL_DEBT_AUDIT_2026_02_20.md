# Comprehensive Technical Debt Audit (Deep Sweep)

Audit date: February 20, 2026 (deep sweep updated February 21, 2026)  
Auditor: Codex (GPT-5)  
Scope: Full repo sweep (`src`, `tests`, `Plugins`, project files, build and architecture gates)

## Executive summary

The repository has real technical-debt regression compared to the prior "all resolved" narrative.  
The most important difference is that quality gates are currently red, not green.

Current headline status:

- Build: failing (`dotnet build SaveStateReborn.sln`)
- Architecture gate: failing (`Interfaces_Should_Not_Have_Too_Many_Members`)
- Framework consistency: split baseline (`net9.*` + `net10.0`)
- Structural complexity: 15 non-migration files at >=1000 lines
- Null-safety debt: 201 `null!` usages (`140` in `src`, `61` in `tests`)

Release recommendation right now: `NO-GO` until critical items are resolved.

## Reality check vs previous report

The previous report (`docs/reports/COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_20.md`) states:

- "0 errors, 0 warnings"
- "all high-priority debt resolved"
- "0 TODO/FIXME action items"

Current observed state (February 21, 2026):

- Build errors: 4
- Build warnings: 10
- Architecture test failures: 1
- TODO markers: 1 explicit TODO comment remains

## Methodology and reproducibility

Validation commands executed:

```powershell
dotnet build SaveStateReborn.sln --nologo -v minimal
dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj --filter "ArchitectureTests" --no-build
dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj --filter "CodeQuality" --no-build
```

Static scans run with `rg` for:

- `DateTime.Now|DateTime.UtcNow|DateTime.Today`
- `.Wait(`, `GetAwaiter().GetResult()`, and async `.Result` blocking patterns
- `null!`
- `Class1.cs` / `UnitTest1.cs` stubs
- TFM drift in `.csproj` files

## Current metrics snapshot

- C# files scanned: `2,747`
- Projects scanned: `82`
- Target frameworks:
  - `net9.*`: `42`
  - `net10.0`: `40`
- Large files:
  - `>=1000` lines (non-migration): `15`
  - `>=700` lines (non-migration): `59`
  - `>=500` lines (non-migration): `134`
- Time-coupling:
  - `DateTime.*` in `src`: `197` usages
  - Excluding `ITimeProvider` implementation file: `194`
- Sync-over-async:
  - `.Wait(`: `2`
  - `GetAwaiter().GetResult()`: `4`
  - async `.Result` patterns: `3`
- Null-forgiving:
  - `src`: `140`
  - `tests`: `61`
  - total: `201`
- Scaffolding residue:
  - `Class1.cs`: `36`
  - `UnitTest1.cs`: `12`

## Findings (detailed)

## Critical-1: Build is broken (compile-time regression)

### Evidence

- `tests/SaveState.Application.Tests/GameLibrary/Commands/MemoryCommandTests.cs:28`
- `tests/SaveState.Application.Tests/GameLibrary/Commands/MemoryCommandTests.cs:49`
- `tests/SaveState.Application.Tests/GameLibrary/Commands/MemoryCommandTests.cs:68`
- `tests/SaveState.Application.Tests/GameLibrary/Commands/MemoryCommandTests.cs:88`

### Real code example (test code still using old constructor shape)

```csharp
var handler = new AttachMemoryReaderCommandHandler(_memoryReaderMock.Object);
```

### Real code example (handler now requires logger)

`src/SaveState.Application/GameLibrary/Commands/AttachMemoryReaderCommand.cs:21`

```csharp
public AttachMemoryReaderCommandHandler(
    IGameMemoryReader memoryReader,
    ILogger<AttachMemoryReaderCommandHandler> logger)
```

### Why this fails

Unit tests instantiate handlers with 1 argument, while production handlers now require 2.  
This is a hard compiler error, not a flaky runtime issue.

### Edge cases

- If a future constructor adds `ITimeProvider` or telemetry deps, these tests will break again without shared factory helpers.
- Because this is compile-time, build fails before tests can run, masking additional downstream regressions.

### Remediation

- Use `Mock<ILogger<T>>` or `NullLogger<T>.Instance` in tests.
- Add a small test helper for handler instantiation to reduce constructor-coupling churn.

## Critical-2: Architecture gate fail (interface segregation drift)

### Evidence

- Failing assertion: `tests/SaveState.Infrastructure.Tests/Architecture/ArchitectureTests.cs:250`
- Current value: `97` interfaces with >10 methods
- Budget: `<=95`

### Real code example (gate logic)

```csharp
largeInterfaces.Count.Should().BeLessThanOrEqualTo(95,
    $"{largeInterfaces.Count} interfaces have more than 10 methods...");
```

### Reported top offenders

- `IStoryModeService`: 52 methods
- `ISpriteAnimationService`: 41 methods
- `ISoundDesignService`: 40 methods
- `IPerformanceProfilerService`: 39 methods
- `IIkemenGoService`: 34 methods

### Real code example (oversized interface surface)

`src/SaveState.Core/Mugen/Services/IStoryModeService.cs`

```csharp
Task<Result<StoryProject>> CreateProjectAsync(...);
Task<Result<StoryProject>> OpenProjectAsync(...);
Task<Result> SaveProjectAsync(...);
Task<Result<StoryProjectStats>> GetProjectStatsAsync(...);
Task<Result<string>> ExportForMugenAsync(...);
// ... many additional regions/methods ...
```

### Edge cases

- A single cancellation/validation policy change requires touching many methods, increasing contract-breaking risk.
- Mocking these interfaces in tests creates brittle setups and low signal tests.
- Any API evolution has high ripple effect across handlers/managers and plugins.

### Remediation

- Split by capability (`IStoryProjectService`, `IStorySceneService`, `IStoryBattleService`, etc.).
- Keep compatibility adapter interfaces temporarily to avoid big-bang refactor.

## High-1: Framework baseline drift (`net9` vs `net10`)

### Evidence

- `42` projects target `net9.*`
- `40` projects target `net10.0`
- Build emits NETSDK1057 preview-SDK notices.

### Real code examples

`src/SaveState.Core/SaveState.Core.csproj`

```xml
<TargetFramework>net9.0</TargetFramework>
```

`src/SaveState.Plugins.GameBackupManager/SaveState.Plugins.GameBackupManager.csproj`

```xml
<TargetFramework>net10.0</TargetFramework>
```

### Edge cases

- Contributors with only .NET 9 SDK cannot build full solution.
- CI agents pinned to stable SDKs may fail intermittently depending on workload image.
- Package behavior can diverge between net9 and net10 (especially analyzers/source generators).

### Remediation

- Decide one baseline now.
- Pin SDK via `global.json`.
- Align all plugin TFMs to the chosen baseline (or multi-target intentionally with explicit rationale).

## High-2: Monolithic file debt still large

### Evidence

- 15 non-migration files >=1000 lines.
- Largest: `src/SaveState.Infrastructure/GameLibrary/Heuristics/ValueHeuristics.cs` (3,288 lines).
- Contains 24 heuristic classes in one file.

### Real code example

`src/SaveState.Infrastructure/GameLibrary/Heuristics/ValueHeuristics.cs`

```csharp
public sealed class HealthHeuristic : IValueHeuristic
public sealed class CurrencyHeuristic : IValueHeuristic
public sealed class PositionHeuristic : IValueHeuristic
// ... 21 more heuristic classes in same file ...
```

### Edge cases

- Single-file merge conflict hotspot for unrelated heuristic edits.
- Harder targeted unit testing and review isolation.
- Hidden duplication risk (e.g., repeated conversion logic and scoring patterns).

### Remediation

- Move each heuristic into dedicated file + shared scoring/conversion utility.
- Add focused tests per heuristic category.

## High-3: Dependency Injection composition root is oversized

### Evidence

- `src/SaveState.Infrastructure/DependencyInjection.cs`: 997 lines
- 76 `using` directives
- 279 registration calls (`AddScoped/AddSingleton/AddTransient/...`)

### Real code example

```csharp
services.AddScoped<ISpriteAnimationService, SaveState.Infrastructure.Mugen.SpriteAnimation.SpriteAnimationService>();
services.AddScoped<MatchPredictionManager>();
services.AddScoped<PlayerSkillManager>();
services.AddScoped<MachineLearningManager>();
// ... hundreds more registrations ...
```

### Edge cases

- Misconfigured service lifetime hidden among many lines.
- Conflict-prone during parallel feature branches.
- Hard to enforce bounded-context ownership of registrations.

### Remediation

- Split into `AddInfrastructureGameLibrary`, `AddInfrastructureMugen`, `AddInfrastructureSocial`, etc.
- Keep root file as orchestration only.

## Medium-1: Sync-over-async in production paths

### Evidence and code examples

`src/SaveState.Infrastructure/GameLibrary/Services/MacOSMemoryReader.cs:473`

```csharp
UnfreezeValueAsync(address, ct).Wait(ct);
```

`src/SaveState.Infrastructure/GameLibrary/Services/MacOSMemoryReader.cs:822`

```csharp
var result = ReadMemoryBytesAsync(region.Address, sampleSize, default).Result;
```

`src/SaveState.Application/Mugen/Services/Graphics/AdvancedGraphicsEngine.cs:68`

```csharp
_lightingManager.CreateLightingSetupAsync(request, ct).Result
```

`src/SaveState.Infrastructure/GameLibrary/Services/GameMemoryDatabaseLoader.cs:260`

```csharp
return LoadAsync(forceReload).GetAwaiter().GetResult();
```

### Edge cases

- UI/main-thread deadlocks if context capture occurs.
- Cancellation token behavior becomes inconsistent (`Wait(ct)` vs awaited flow).
- Thread-pool starvation under concurrent memory scan operations.

### Remediation

- Convert call chains to fully async.
- If sync compatibility is required, isolate in boundary adapters and document blocking constraints.

## Medium-2: Time abstraction policy drift

### Evidence

- `DateTime.*` in `src`: 197
- Excluding `ITimeProvider` implementation file: 194
- `DateTime.UtcNow` alone: 187

### Real code examples

`src/SaveState.Infrastructure/GameLibrary/Services/TemplateBasedPatternDetector.cs:199`

```csharp
DetectedAt = DateTime.UtcNow
```

`src/SaveState.Presentation/ViewModels/Shell/StatusBarViewModel.cs:248`

```csharp
var todayKey = DateOnly.FromDateTime(DateTime.Today);
```

`src/SaveState.Core/GameLibrary/Entities/GameSession.cs:33`

```csharp
public TimeSpan GetDuration() => GetDuration(DateTime.UtcNow);
```

### Edge cases

- Tests become non-deterministic around midnight/day boundaries.
- Inconsistent timezone assumptions between UI and core models.
- Replay/session metrics can drift in distributed or emulated-time scenarios.

### Remediation

- Prioritize `Infrastructure` and `Presentation` usage migration to `ITimeProvider`.
- For domain entities, inject time via factory/services and keep entity methods deterministic where possible.

## Medium-3: Null-forgiving (`null!`) regression

### Evidence

- total `null!`: 201
- `src`: 140
- `tests`: 61

### Real code examples

`src/SaveState.Infrastructure/Mugen/TournamentBracket/TournamentBracketService.cs:778`

```csharp
participants.Add(null!);
```

`src/SaveState.Infrastructure/Performance/WindowsPointerPathFinder.cs:30`

```csharp
var baseAddressResult = await _memoryReader.GetModuleBaseAddressAsync(processId, null!, ct);
```

`src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs:175`

```csharp
_eventPublisher = null!;
```

### Edge cases

- `null!` used as data sentinel can leak into runtime flow and force repeated null checks downstream.
- EF/snapshot constructors may mask accidental production-path null initialization.
- Contract mismatch hidden from compiler until runtime.

### Remediation

- Replace `null!` sentinels with explicit optional types/overloads.
- Add project-level budget checks in CI to prevent re-growth.

## Medium-4: Duplicate/stale handler implementation

### Evidence

- New handler path: `src/SaveState.Application/GameLibrary/Commands/DetachMemoryReaderCommand.cs:15`
- Stale duplicate path: `src/SaveState.Application/GameLibrary/Commands/Handlers/DetachMemoryReaderCommandHandler.cs:7`

### Real code examples

New handler (logger + scopes):

```csharp
public class DetachMemoryReaderCommandHandler : IRequestHandler<DetachMemoryReaderCommand, Result>
{
    private readonly ILogger<DetachMemoryReaderCommandHandler> _logger;
    // ...
}
```

Old duplicate handler:

```csharp
public class DetachMemoryReaderCommandHandler : IRequestHandler<DetachMemoryReaderCommand, Result>
{
    private readonly IGameMemoryReader _memoryReader;
    // ...
}
```

### Edge cases

- Team members update one handler and assume behavior applies globally.
- Test implementations accidentally follow outdated constructor/behavior shape.

### Remediation

- Remove stale handler file or mark obsolete and consolidate into one authoritative implementation.

## Medium-5: Scaffolding residue in production and tests

### Evidence

- `Class1.cs` files: 36
- `UnitTest1.cs` files: 12

### Real code examples

`src/SaveState.Plugins.GameTimer/Class1.cs`

```csharp
public class Class1
{
}
```

`tests/SaveState.Core.Tests/UnitTest1.cs`

```csharp
public class UnitTest1
{
    [Fact]
    public void Test1()
    {
    }
}
```

### Edge cases

- New contributors infer these stubs are required patterns.
- Noise dilutes code search results and review focus.

### Remediation

- Remove all unused scaffolding files.
- Add lint rule to block `Class1.cs`/`UnitTest1.cs` additions.

## Low: Warnings and docs cleanliness

### Build warnings (10 total) include:

- CA1863 (`string.Format` allocation pattern):
  - `src/SaveState.Plugins.GamingAnalytics/GamingAnalyticsPlugin.cs:277`
- CA1827 (`Count() > 0` vs `Any()`):
  - `src/SaveState.Infrastructure/GameLibrary/ML/PatternPredictionModel.cs:242`
- CS1572 (stale XML param docs):
  - `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/CollectionsManager.cs:35`
  - `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/UserInteractionManager.cs:230`

### TODO residue

- `tests/SaveState.Presentation.Tests/ViewModels/ViewModelTests.cs:67`

```csharp
// TODO: Add full LibraryViewModel tests once DI factory is available
```

## Edge-case risk matrix

| Debt Area | Typical Failure | Edge Case | User-visible impact |
|---|---|---|---|
| Build break | compile fails | additional hidden regressions masked by early failure | CI red / no release |
| Interface bloat | difficult mocking | broad refactor causes accidental API break | slower feature work, fragile tests |
| TFM drift | SDK mismatch | contributor machine only has .NET 9 | "works on my machine" failures |
| Sync-over-async | thread blocking | deadlock under UI synchronization context | frozen UI or hung operations |
| DateTime coupling | non-deterministic tests | DST/midnight boundary behavior shifts | incorrect "today"/duration metrics |
| `null!` usage | hidden null contract violations | production path takes ctor meant for tooling/test | runtime exceptions |
| DI monolith | registration mistake hard to review | lifetime mismatch hidden in large diff | startup/runtime defects |

## Prioritized remediation plan (with acceptance criteria)

1. Build recovery (same day)
- Fix constructor mismatch in `MemoryCommandTests`.
- Consolidate duplicate `DetachMemoryReaderCommandHandler`.
- Acceptance: `dotnet build SaveStateReborn.sln` succeeds with 0 errors.

2. Architecture gate recovery (1-2 days)
- Split enough interfaces to reduce large-interface count from 97 to <=95.
- Acceptance: `Interfaces_Should_Not_Have_Too_Many_Members` passes.

3. Framework baseline decision (2-4 days)
- Unify on one TFM baseline or intentionally multi-target with documented policy.
- Acceptance: no NETSDK1057 preview requirement for standard dev build (unless explicitly chosen).

4. Async and time policy hardening (1 week)
- Remove production sync-over-async hotspots.
- Migrate highest-churn `DateTime.*` usages to `ITimeProvider`.
- Acceptance: no `.Result/.Wait` in production services outside approved adapters.

5. Structural cleanup (2-3 weeks, incremental)
- Split `DependencyInjection.cs`.
- Split `ValueHeuristics.cs` by class/category.
- Remove scaffolding residue files.
- Acceptance: measurable reduction in 1000+ file count and merge-conflict hotspots.

6. Guardrail automation (1 week)
- Add CI checks:
  - `null!` budget thresholds
  - `DateTime.*` budget thresholds
  - banned filenames (`Class1.cs`, `UnitTest1.cs`)
- Acceptance: PR fails when debt budgets regress.

## What is healthy

- Code-quality tests currently pass: 3/3 (`CodeQualityTests` subset).
- Architecture tests mostly pass: 12/13.
- Existing architecture tests already encode debt budgets, which is good governance; thresholds now need active burn-down to avoid silent drift.

## Appendix: command outcomes

- `dotnet build SaveStateReborn.sln --nologo -v minimal`
  - Result: failed
  - Errors: 4
  - Warnings: 10
- `dotnet test ... --filter "ArchitectureTests" --no-build`
  - Result: 13 total, 12 passed, 1 failed
- `dotnet test ... --filter "CodeQuality" --no-build`
  - Result: 3 passed, 0 failed
