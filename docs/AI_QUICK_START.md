# AI Quick Start - 30 Second Briefing

**Read Time**: 30 seconds
**Last Verified**: January 2, 2026

---

## 🚀 Immediate Commands

```powershell
# Build (must pass before any PR)
dotnet build src/SaveState.sln

# Test (must pass before any PR)
dotnet test --no-build

# Run the app
dotnet run --project src/SaveState.Presentation
```

---

## ✅ Current Status

| Check | Status | Notes |
|-------|--------|-------|
| **Build** | ✅ PASSING | 0 errors |
| **Tests** | ✅ 529/529 | 100% pass rate |
| **Warnings** | ⚠️ 1,220 | All CS1591 (ignore) |
| **Health** | 91/100 | 3 critical issues |

---

## 🔴 Fix These First (Critical)

| File | Line | Problem | Fix |
|------|------|---------|-----|
| `JwtTokenService.cs` | 29, 98, 118 | `.Result` blocks async | Use `await` |

---

## 📁 Where Things Live

| What | Location |
|------|----------|
| **Domain Entities** | `src/SaveState.Core/` |
| **Commands/Queries** | `src/SaveState.Application/` |
| **Database/External** | `src/SaveState.Infrastructure/` |
| **UI (Avalonia)** | `src/SaveState.Presentation/` |
| **CLI** | `src/SaveState.CLI/` |
| **Tests** | `tests/` |

---

## 🎯 Key Patterns (Use These)

```csharp
// ✅ Result pattern for errors
return Result.Failure<Game>(GameErrors.NotFound(id));

// ✅ MediatR for commands
await _mediator.Send(new CreateGameCommand { ... });

// ✅ Value objects for IDs
public record GameId(Guid Value);
```

---

## ❌ Never Do This

```csharp
// ❌ Don't return null
return null;  // Use Result.Failure instead

// ❌ Don't use .Result
var result = asyncMethod().Result;  // Use await

// ❌ Don't use async void
async void OnClick() { }  // Use async Task
```

---

## 📚 Read Next

1. **[PATTERNS_COOKBOOK.md](PATTERNS_COOKBOOK.md)** - All code patterns
2. **[ACTIVE_ISSUES.md](ACTIVE_ISSUES.md)** - Current bugs/debt
3. **[DECISIONS_LOG.md](DECISIONS_LOG.md)** - Why we made choices
4. **[AI_MASTER_CONTEXT.md](AI_MASTER_CONTEXT.md)** - Full context

---

## 🔍 Quick Find

```powershell
# Find all commands
Get-ChildItem -Recurse -Filter "*Command.cs" src/

# Find all handlers
Get-ChildItem -Recurse -Filter "*Handler.cs" src/

# Find all services
Select-String -Path "src/**/*.cs" -Pattern "public class.*Service"
```
