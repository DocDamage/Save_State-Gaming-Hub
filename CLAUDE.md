# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in this repository.

## Scope and Current Baseline

- Project: SaveState Reborn
- Architecture: Clean Architecture + CQRS + MediatR
- Primary stack: C# 13, .NET 9 (solution also contains net10.0 projects)
- UI: Avalonia 11
- Database: EF Core + SQLite
- Last documentation refresh: 2026-02-20

Use this file as an execution guide, not as immutable historical record. For current status, always check the active docs listed below.

## Active Source of Truth Documents

Read these first before making major changes:

1. `docs/plans/TECHNICAL_DEBT_IMPLEMENTATION_PLAN_2026_02_20.md`
   - Active technical debt close-out plan.
2. `docs/reports/COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_15.md`
   - Evidence-backed audit and remediation slices.
3. `docs/plans/FEATURE_ROADMAP_2026.md`
   - Active feature roadmap.
4. `docs/status/DEVELOPMENT_STATUS.md`
   - Current development and delivery status.
5. `docs/CURRENT_DOCUMENTATION_INDEX.md`
   - Active documentation index.
6. `docs/architecture/ENGINEERING_RULES.md`
   - Architecture and engineering policy reference.
7. `docs/architecture/adrs/007-result-pattern.md`
   - Result pattern decision record.
8. `docs/architecture/adrs/011-time-provider-abstraction.md`
   - Time abstraction decision record.

Archive references:

- `docs/archive/2026-02-20-documentation-refresh/README.md`
- `docs/archive/2026-02-20-technical-debt-plan-refresh/README.md`

Do not treat archived docs as active plans.

## Repository Layout

- `src/SaveState.Core/` - Domain entities, value objects, interfaces
- `src/SaveState.Application/` - CQRS commands/queries and orchestration
- `src/SaveState.Infrastructure/` - Persistence, external integrations, service implementations
- `src/SaveState.Presentation/` - Avalonia MVVM UI
- `src/SaveState.CLI/` - Command-line interface
- `tests/` - Unit, integration, UI, e2e, performance, and specialty test projects
- `docs/` - Active docs and archive manifests

Dependency direction must remain:

- `Presentation -> Application -> Core`
- `Infrastructure -> Core`
- No circular references.

## Essential Commands

```powershell
# Restore + build full solution
dotnet build SaveStateReborn.sln

# Run full test suite
dotnet test SaveStateReborn.sln

# Fast validation during debt slices
dotnet build SaveStateReborn.sln --no-restore --verbosity minimal
dotnet test SaveStateReborn.sln --no-build --verbosity minimal

# Run desktop app
dotnet run --project src/SaveState.Presentation

# Run CLI
dotnet run --project src/SaveState.CLI -- --help
```

EF Core migrations:

```powershell
cd src/SaveState.Infrastructure
dotnet ef database update --startup-project ../SaveState.Presentation
dotnet ef migrations add <MigrationName> --startup-project ../SaveState.Presentation
```

## Non-Negotiable Engineering Rules

### 1. Result Pattern for Public Failure Paths

- Do not return `null` from public APIs/services/repositories to represent failure.
- Use `Result` / `Result<T>` and propagate explicit error context.

```csharp
public async Task<Result<Game>> GetGameAsync(int id)
{
    var game = await _repository.GetByIdAsync(id).ConfigureAwait(false);
    if (game is null)
        return Result<Game>.Failure($"Game {id} not found", ErrorType.NotFound);

    return Result<Game>.Success(game);
}
```

### 2. Time Abstraction

- Use `ITimeProvider` in application and infrastructure logic.
- Do not introduce new direct `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, or `DateTimeOffset.UtcNow` in `src`.

```csharp
public sealed class ExampleService
{
    private readonly ITimeProvider _timeProvider;

    public ExampleService(ITimeProvider timeProvider) => _timeProvider = timeProvider;

    public DateTime GetNow() => _timeProvider.UtcNow;
}
```

### 3. Async Correctness

- No `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` in normal flow.
- Prefer `async`/`await`; use `ConfigureAwait(false)` in library code.
- `async void` only for top-level UI event handlers, wrapped in robust exception handling.

### 4. DI and Configuration

- Register services in DI, not with manual static/service-locator patterns.
- Validate options on startup (`ValidateOnStart`) for external provider configuration.

### 5. Logging

- Use structured logging with named parameters.
- No silent catches.

### 6. Security

- No hardcoded secrets.
- Use configuration + environment variables + user secrets for sensitive values.

## Current Technical Debt Close-Out (Active Plan)

Use `docs/plans/TECHNICAL_DEBT_IMPLEMENTATION_PLAN_2026_02_20.md` as the execution order.

### Workstream B: Time Determinism

Baseline snapshot (2026-02-20):

- Direct `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in `src`: 354
- Direct `DateTime.Now` / `DateTimeOffset.Now` in `src`: 2 (exception-only target)

Goal: reduce direct UTC-now usages to <=100 in close-out wave and prevent regressions.

### Workstream C: Suppression Paydown

- Baseline `<NoWarn>` entries: 23
- Goal: reduce to <=15 and block suppression growth in CI.

### Workstream A: Deferred Dependencies

- `Tobii.Interaction` remains at `0.7.3` unless feed/source strategy changes.
- EF Core/TestHost remain `9.0.13` pending coordinated net10 migration gate.

### Workstream G: Class Size Policy

- Finalize class-size enforcement policy and align tests/docs to one rule.

## Required Debt Validation Commands

```powershell
rg -n "DateTime\\.UtcNow|DateTimeOffset\\.UtcNow" src
rg -n "DateTime\\.Now|DateTimeOffset\\.Now" src
rg -n "<NoWarn>" src tests
rg -n "ContinueWith\\s*\\(.*\\.Result" src
```

Run build/tests after each debt tranche:

```powershell
dotnet build SaveStateReborn.sln --no-restore --verbosity minimal
dotnet test SaveStateReborn.sln --no-build --verbosity minimal
```

## Coding Workflow Expectations

1. Confirm relevant active doc(s) before coding.
2. Keep edits scoped to the tranche/hotspot being worked.
3. Add or update tests for behavioral changes.
4. Validate with build + tests.
5. Update the active plan/audit when debt metrics materially change.
6. If docs are superseded, move them to dated archive folders and leave a manifest.

## Notes

This file supersedes older references to archived paths such as:

- `docs/archive/2026-01-16-pre-final-scan/FEATURE_SURFACING_PLAN.md`
- historical status/reports moved under `docs/archive/2026-02-20-documentation-refresh/`

If any instruction in this file conflicts with `AGENTS.md`, follow `AGENTS.md`.

Last updated: 2026-02-20.

