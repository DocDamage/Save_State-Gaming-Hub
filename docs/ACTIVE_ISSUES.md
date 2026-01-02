# Active Issues - What's Broken Right Now

**Last Updated**: January 2, 2026 12:42 PM
**Next Review**: January 9, 2026

> [!IMPORTANT]
> Fix items in order of severity. Critical issues block deployments.

---

## 🔴 CRITICAL (Fix Immediately)

These cause runtime failures or deadlocks.

| # | File | Line(s) | Issue | How to Fix | Est. Time |
|---|------|---------|-------|------------|-----------|
| 1 | `JwtTokenService.cs` | 29 | `.Result` blocks async | Replace with `await` | 10 min |
| 2 | `JwtTokenService.cs` | 98 | `.Result` blocks async | Replace with `await` | 10 min |
| 3 | `JwtTokenService.cs` | 118 | `.Result` blocks async | Replace with `await` | 10 min |

### Fix Template for .Result Issues

```csharp
// ❌ BEFORE (Causes deadlock)
var token = GenerateTokenAsync(claims).Result;

// ✅ AFTER (Proper async)
var token = await GenerateTokenAsync(claims);
```

**After fixing**: Update this file and `PROJECT_METRICS.md` critical_issues count.

---

## 🟠 HIGH (Fix This Sprint)

These cause exceptions or hide failures.

| # | File | Line(s) | Issue | How to Fix | Est. Time |
|---|------|---------|-------|------------|-----------|
| 4 | `MainWindowViewModel.cs` | 45 | `async void` method | Change to `async Task` | 15 min |
| 5 | `GameLibraryViewModel.cs` | 112 | `async void` method | Change to `async Task` | 15 min |
| 6 | `MugenViewModel.cs` | 89 | `async void` method | Change to `async Task` | 15 min |
| 7 | `MugenCharacterLoader.cs` | ~50 | Silent catch block | Add logging | 10 min |
| 8 | `GameDetectionService.cs` | ~120 | Silent catch block | Add logging | 10 min |
| 9 | `CoverArtService.cs` | ~80 | Silent catch block | Add logging | 10 min |
| 10 | `SteamGridDbService.cs` | ~95 | Silent catch block | Add logging | 10 min |

### Fix Template for async void

```csharp
// ❌ BEFORE (Exceptions lost)
private async void LoadDataAsync()
{
    var data = await _service.GetAsync();
}

// ✅ AFTER (Exceptions caught)
private async Task LoadDataAsync()
{
    var data = await _service.GetAsync();
}

// In constructor or init, call like this:
_ = LoadDataAsync(); // Fire and forget with awareness
```

### Fix Template for Silent Catch

```csharp
// ❌ BEFORE (Failures hidden)
catch (Exception)
{
    // Silent - debugging nightmare
}

// ✅ AFTER (Failures logged)
catch (Exception ex)
{
    _logger.LogWarning(ex, "Operation failed: {Message}", ex.Message);
}
```

---

## 🟡 MEDIUM (Backlog)

Technical debt to address during code review.

| Category | Count | Action |
|----------|-------|--------|
| `return null` statements | 45+ | Convert to `Result.Failure` gradually |
| `TODO` comments | 68+ | Address or remove during reviews |
| Missing XML docs (CS1591) | 1,220 | Low priority, cosmetic |

---

## ✅ Recently Fixed

| Date | Issue | Resolution |
|------|-------|------------|
| Jan 2 | AI Knowledge Base integration | Added web search fallback |
| Jan 2 | DI cyclomatic complexity | Refactored to partial classes |
| Jan 1 | Library tab crash | Fixed XAML bindings |

---

## 🚫 Known Warnings to Ignore

These are intentional or harmless:

| Warning | Count | Why Ignore |
|---------|-------|------------|
| CS1591 | 1,220 | XML docs not required for internal code |
| XAML Designer | ~50 | Work at runtime, designer limitation |
| Nullable reference | ~30 | Handled by Result pattern |

---

## 📝 How to Update This File

When you fix an issue:

1. Move from CRITICAL/HIGH to "Recently Fixed"
2. Update `PROJECT_METRICS.md` critical_issues count
3. Run sync tool: `dotnet run --project tools/SaveState.Docs.Sync`
4. Commit with message: `fix: resolve [issue description]`
