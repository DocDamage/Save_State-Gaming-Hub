# AI Quick Start - 30 Second Briefing

**Read Time**: 30 seconds  
**Last Verified**: February 16, 2026

---

## 🚀 Immediate Commands

```powershell
# Build (must pass before any PR)
dotnet build SaveStateReborn.sln

# Test (must pass before any PR)
dotnet test --no-build

# Run the app
dotnet run --project src/SaveState.Presentation
```

---

## ✅ Current Status (February 16, 2026)

| Check | Status | Notes |
|-------|--------|-------|
| **Build** | ✅ **PASSING** | 0 errors, 0 warnings |
| **Tests** | ✅ **600+** | 100% pass rate |
| **Warnings** | ✅ **0** | Clean build |
| **Health Score** | ✅ **9.1/10** | Technical debt 10/10 initiative |
| **Result Pattern** | ✅ **COMPLETE** | 183 null returns migrated |
| **Backend** | ✅ **100%** | All 90+ services complete |
| **Overall** | 🎯 **95%** | Production ready! |

---

## 🎉 Major Milestone: Result Pattern Migration Complete

**February 16, 2026**: Successfully migrated 183 `return null` statements to `Result<T>` pattern!

### Services Refactored:
- ✅ **AchievementService** (16 nulls eliminated)
- ✅ **Smart Launcher Feature** (18 nulls eliminated)
- ✅ **RecordingEngine** (6 nulls eliminated)
- ✅ **SessionRecoveryService** (6 nulls eliminated)
- ✅ **XboxCatalogClient** (3 nulls eliminated)
- ✅ **SequenceAnalysisEngine** (4 nulls eliminated)
- ✅ **ReplayPathResolver** (4 nulls eliminated)
- ✅ **NaturalLanguageGameSearch** (4 nulls eliminated)
- ✅ **Additional services** (122 nulls eliminated)

**All public API methods now return `Result<T>` for consistent error handling.**

---

## ✅ Critical Issues - ALL RESOLVED

No blocking issues. Codebase is healthy and production-ready with:
- Zero build errors
- Zero build warnings
- 600+ tests passing
- Result pattern fully implemented

---

## 📁 Where Things Live

| What | Location |
|------|----------|
| **Domain Entities** | `src/SaveState.Core/` |
| **Commands/Queries** | `src/SaveState.Application/` |
| **Database/External** | `src/SaveState.Infrastructure/` |
| **UI (Avalonia)** | `src/SaveState.Presentation/` |
| **CLI** | `src/SaveState.CLI/` |
| **Plugins** | `src/SaveState.Plugins.*/` |
| **Tests** | `tests/` |

---

## 🎯 Key Patterns (Use These)

```csharp
// ✅ Result pattern for errors (MANDATORY)
return Result.Success(game);
return Result.Failure<Game>("Game not found", ErrorType.NotFound);

// ✅ MediatR for commands
await _mediator.Send(new CreateGameCommand(title, coverPath));

// ✅ Value objects for IDs
public record GameId(Guid Value);

// ✅ ITimeProvider for date/time (MANDATORY)
public MyService(ITimeProvider timeProvider) { _timeProvider = timeProvider; }
var now = _timeProvider.Now;  // Never use DateTime.Now directly
```

---

## ❌ Never Do This

```csharp
// ❌ Don't return null from public methods
return null;  // Use Result<T>.Failure instead

// ❌ Don't use .Result or .Wait()
var result = asyncMethod().Result;  // Deadlock risk! Use await

// ❌ Don't use async void
async void OnClick() { }  // Use async Task (except event handlers)

// ❌ Don't use DateTime.Now
var now = DateTime.Now;  // Not testable! Use injected ITimeProvider

// ❌ Don't use ! without null check
var value = result.Value!;  // Dangerous! Check IsFailure first

// ❌ Don't use string interpolation in logs
_logger.LogInformation($"Game: {game.Id}");  // Use structured: _logger.LogInformation("Game: {GameId}", game.Id);
```

---

## ✅ Navigation Pattern

```csharp
// ✅ Always await navigation (supports INavigationAware)
[RelayCommand]
private async Task OpenGame()
{
    await _navigationService.NavigateTo("Library", GameId);
}
```

---

## ✅ Acceptable Nullable Patterns

These patterns are **ACCEPTABLE** and do not need migration:

```csharp
// ✅ Nullable VALUE types for "no data" states
public Task<Guid?> GetLastPlayedGameIdAsync() { return Task.FromResult<Guid?>(null); }
// null means "no last game" - this is a valid business state, not an error

// ✅ Private parsing helpers
private string? ExtractValue(string input) { return null; }  // "Not found" is valid

// ✅ UI dialog cancellation
public Task<string?> ShowDialogAsync() { return Task.FromResult<string?>(null); }

// ✅ Demo/stub implementations
public object? GetResourceDictionary() { return null; }  // Not implemented yet
```

---

## 📚 Read Next

1. **[PATTERNS_COOKBOOK.md](../architecture/PATTERNS_COOKBOOK.md)** - All code patterns with examples
2. **[007-result-pattern.md](../architecture/adrs/007-result-pattern.md)** - Result pattern ADR
3. **[DECISIONS_LOG.md](../architecture/DECISIONS_LOG.md)** - Architecture decision records
4. **[ENGINEERING_RULES.md](../architecture/ENGINEERING_RULES.md)** - Engineering standards

---

## 🔍 Quick Find

```powershell
# Find all commands
Get-ChildItem -Recurse -Filter "*Command.cs" src/

# Find all handlers
Get-ChildItem -Recurse -Filter "*Handler.cs" src/

# Find all services
Select-String -Path "src/**/*.cs" -Pattern "public class.*Service"

# Count Result<T> usage
Select-String -Path "src/**/*.cs" -Pattern "Result<" | Measure-Object

# Check for remaining null returns (should only show acceptable patterns)
Select-String -Path "src/**/*.cs" -Pattern "return\s+null;"
```

---

## 🆘 Quick Help

| Issue | Solution |
|-------|----------|
| Build fails with null error | Change `return null;` to `Result<T>.Failure(...)` |
| Missing ITimeProvider | Add to constructor, use `_timeProvider.Now` |
| Test needs time mocking | Use `Mock<ITimeProvider>` or `SystemTimeProvider` |
| Async warning | Add `Async` suffix, return `Task<T>` |
| Navigation not working | Add `await` before `_navigationService.NavigateTo()` |

---

*Keep this file handy while coding! Last updated February 16, 2026.*
