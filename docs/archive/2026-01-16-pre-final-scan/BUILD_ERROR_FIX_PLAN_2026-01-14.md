# Build Error Fix Plan - January 14, 2026

## Overview

This document provides a detailed plan to fix the **17 remaining build errors** identified in the SaveState solution. All errors are in the Infrastructure layer's MUGEN services.

**Build Command**: `dotnet build`
**Current Status**: 17 Errors, 4 Warnings
**Target Status**: 0 Errors, 0 Warnings (or existing warning count)

---

## Error Summary by File

| File | Errors | Category |
|------|--------|----------|
| `MugenPlayerDataRepository.cs` | 4 | Constructor parameter name mismatch |
| `MugenTemplateRepository.cs` | 2 | Missing type + constructor parameter mismatch |
| `MoveCreationService.cs` | 3 | Missing enum value |
| `MugenExportService.cs` | 8 | Record constructor mismatch + missing properties |
| **TOTAL** | **17** | |

---

## Phase 1: MugenPlayerDataRepository (4 Errors)

### Error Details

```
CS1739: The best overload for 'PlayerSkill' does not have a parameter named 'PlayerId'
Lines: 36, 98, 110, 122
```

### Root Cause Analysis

The `PlayerSkill` class uses a **positional constructor** with lowercase parameter names:

```csharp
// Current Core Definition (PlayerSkill.cs)
public sealed class PlayerSkill
{
    public PlayerSkill(string playerId, double rating, double volatility,
                       IReadOnlyDictionary<string, double> characterRatings, DateTime lastUpdated)
    {
        PlayerId = playerId;
        Rating = rating;
        Volatility = volatility;
        CharacterRatings = characterRatings;
        LastUpdated = lastUpdated;
    }
    // Properties...
}
```

The Infrastructure code uses **named parameters with PascalCase**:

```csharp
// Current Infrastructure Code (WRONG)
var defaultSkill = new PlayerSkill(
    PlayerId: playerId,      // ❌ Should be lowercase: playerId:
    Rating: 1500.0,          // ❌ Should be lowercase: rating:
    Volatility: 0.06,        // ❌ Should be lowercase: volatility:
    CharacterRatings: new Dictionary<string, double>(),
    LastUpdated: DateTime.UtcNow);
```

### Fix Strategy

**Option A (Recommended)**: Change named parameters to match the class constructor (lowercase)

```csharp
var defaultSkill = new PlayerSkill(
    playerId: playerId,
    rating: 1500.0,
    volatility: 0.06,
    characterRatings: new Dictionary<string, double>(),
    lastUpdated: DateTime.UtcNow);
```

**Option B**: Use positional parameters (no names)

```csharp
var defaultSkill = new PlayerSkill(
    playerId,
    1500.0,
    0.06,
    new Dictionary<string, double>(),
    DateTime.UtcNow);
```

### Affected Lines

| Line | Current Code | Fixed Code |
|------|--------------|------------|
| 35-40 | `PlayerId:`, `Rating:`, `Volatility:`, `CharacterRatings:`, `LastUpdated:` | `playerId:`, `rating:`, `volatility:`, `characterRatings:`, `lastUpdated:` |
| 97-107 | Same pattern | Same fix |
| 109-118 | Same pattern | Same fix |
| 121-131 | Same pattern | Same fix |

### Estimated Effort: 15 minutes

---

## Phase 2: MugenTemplateRepository (2 Errors)

### Error Details

```
CS0246: The type or namespace name 'MoveTemplateData' could not be found
Line: 160

CS1739: The best overload for 'MoveTemplate' does not have a parameter named 'Id'
Line: 167
```

### Root Cause Analysis

1. **MoveTemplateData Missing**: The code references a `MoveTemplateData` type that doesn't exist in Core
2. **MoveTemplate Constructor Mismatch**: The `MoveTemplate` class uses property initializers, not a constructor with named parameters

**Current Core Definition (MoveCreationValueObjects.cs)**:
```csharp
public sealed class MoveTemplate
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public MoveCategory Category { get; init; }
    public DifficultyLevel Difficulty { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = string.Empty;
    public MoveType Type { get; init; }
    public MoveType MoveType => Type;
}
```

**Infrastructure Code (WRONG)**:
```csharp
var templateData = new MoveTemplateData(  // ❌ Type doesn't exist
    BaseProperties: properties,
    States: new List<MoveState> { state },
    DefaultParameters: new Dictionary<string, string>(),
    CustomizationPoints: new List<string>());

return new MoveTemplate(                  // ❌ No constructor, use object initializer
    Id: id,
    Name: name,
    Description: description,
    Category: category,
    Type: type,
    Difficulty: difficulty,
    Tags: new[] { "template", "basic" },
    Data: templateData);
```

### Fix Strategy

**Option A (Recommended)**: Simplify to use object initializer syntax and remove `MoveTemplateData` usage

```csharp
return new MoveTemplate
{
    Id = id.ToString(),
    Name = name,
    Description = description,
    Category = category,
    Type = type,
    Difficulty = difficulty,
    Tags = new[] { "template", "basic" }
};
```

**Note**: The `Data` property doesn't exist on `MoveTemplate`, so it should be removed. If extended data is needed, consider adding it to the Core class.

### Estimated Effort: 20 minutes

---

## Phase 3: MoveCreationService (3 Errors)

### Error Details

```
CS0117: 'MoveCategory' does not contain a definition for 'Attack'
Lines: 30, 159, 173
```

### Root Cause Analysis

The `MoveCategory` enum doesn't have an `Attack` value:

```csharp
// Current Core Definition (MugenMoveDefinition.cs)
public enum MoveCategory
{
    Normal,
    CommandNormal,
    Special,
    Super,
    Hyper,
    Throw,
    Counter,
    Parry,
    Taunt,
    Movement
}
```

Infrastructure code references `MoveCategory.Attack` which doesn't exist.

### Fix Strategy

**Option A (Recommended)**: Map `Attack` to the most appropriate existing value - `Normal`

```csharp
// BEFORE
Category = category ?? MoveCategory.Attack,

// AFTER
Category = category ?? MoveCategory.Normal,
```

**Option B**: Add `Attack` to the enum in Core (if semantically correct)

```csharp
public enum MoveCategory
{
    Normal,
    CommandNormal,
    Special,
    Super,
    Hyper,
    Throw,
    Counter,
    Parry,
    Taunt,
    Movement,
    Attack  // New value
}
```

**Recommendation**: Use Option A since `Normal` moves are typically attack moves in fighting games. Adding a new enum value would be appropriate if `Attack` has a distinct meaning from `Normal`.

### Affected Lines

| Line | Current Code | Fixed Code |
|------|--------------|------------|
| 30 | `MoveCategory.Attack` | `MoveCategory.Normal` |
| 159 | `MoveCategory.Attack` | `MoveCategory.Normal` |
| 173 | `MoveCategory.Attack` | `MoveCategory.Normal` |

### Estimated Effort: 10 minutes

---

## Phase 4: MugenExportService (8 Errors)

### Error Details

```
CS7036: There is no argument given that corresponds to the required parameter 'IsValid' of 'ValidationResult.ValidationResult(...)'
Line: 536

CS0117: 'ValidationResult' does not contain a definition for 'Summary' (Line: 539)
CS0117: 'ValidationResult' does not contain a definition for 'MoveAnalyses' (Line: 540)
CS0117: 'ValidationResult' does not contain a definition for 'Recommendations' (Line: 541)
CS0117: 'ValidationResult' does not contain a definition for 'ActionableTips' (Line: 542)
CS0117: 'ValidationResult' does not contain a definition for 'CharacterName' (Line: 543)
CS0117: 'ValidationResult' does not contain a definition for 'BalanceScore' (Line: 544)
CS0117: 'ValidationResult' does not contain a definition for 'PredictedWinRate' (Line: 545)
```

### Root Cause Analysis

The `ValidationResult` is a **positional record** with 4 parameters:

```csharp
// Current Core Definition (MugenMoveDefinition.cs)
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings,
    IReadOnlyList<string> Suggestions);
```

The Infrastructure code tries to use **object initializer syntax** with non-existent properties:

```csharp
// WRONG: Object initializer on positional record
return Result.Success(new ValidationResult
{
    IsValid = true,
    Summary = "Ready",                    // ❌ Doesn't exist
    MoveAnalyses = Array.Empty<string>(), // ❌ Doesn't exist
    Recommendations = Array.Empty<string>(), // ❌ Doesn't exist
    ActionableTips = Array.Empty<string>(), // ❌ Doesn't exist
    CharacterName = "",                    // ❌ Doesn't exist
    BalanceScore = 0,                      // ❌ Doesn't exist
    PredictedWinRate = 0                   // ❌ Doesn't exist
});
```

### Fix Strategy

**Option A (Quick Fix)**: Use the positional constructor with required parameters only

```csharp
return Result.Success(new ValidationResult(
    IsValid: true,
    Errors: Array.Empty<ValidationError>(),
    Warnings: Array.Empty<ValidationWarning>(),
    Suggestions: Array.Empty<string>()));
```

**Option B (Enhanced)**: Extend the `ValidationResult` record in Core to include the additional properties if they're needed

```csharp
// In Core - Extended ValidationResult
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings,
    IReadOnlyList<string> Suggestions,
    string Summary = "",
    IReadOnlyList<string> MoveAnalyses = null,
    IReadOnlyList<string> Recommendations = null,
    IReadOnlyList<string> ActionableTips = null,
    string CharacterName = "",
    double BalanceScore = 0,
    double PredictedWinRate = 0)
{
    public IReadOnlyList<string> MoveAnalyses { get; init; } = MoveAnalyses ?? Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Recommendations ?? Array.Empty<string>();
    public IReadOnlyList<string> ActionableTips { get; init; } = ActionableTips ?? Array.Empty<string>();
}
```

**Recommendation**: Use Option A for quick fix. The additional properties seem like presentation/analysis concerns that may not belong in the core validation result.

### Estimated Effort: 15 minutes

---

## Execution Order

1. **Phase 3: MoveCreationService** (3 errors) - Simple enum value replacement
2. **Phase 1: MugenPlayerDataRepository** (4 errors) - Parameter name case fix
3. **Phase 2: MugenTemplateRepository** (2 errors) - Remove missing type, use initializer
4. **Phase 4: MugenExportService** (8 errors) - Record constructor fix

**Total Estimated Effort**: ~60-90 minutes

---

## Verification Steps

After each phase:

```powershell
dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj
```

After all phases:

```powershell
dotnet build
dotnet test
```

---

## Risk Assessment

| Phase | Risk | Mitigation |
|-------|------|------------|
| Phase 1 | Low | Parameter name changes are safe refactoring |
| Phase 2 | Medium | Removing `MoveTemplateData` may lose functionality - review usage first |
| Phase 3 | Low | Enum mapping is semantically appropriate |
| Phase 4 | Low | Using correct constructor syntax is safe |

---

## Post-Fix Actions

1. **Run full test suite** to ensure no regressions
2. **Update audit document** to mark errors as resolved
3. **Create PR/commit** with descriptive message
4. **Update DEVELOPMENT_STATUS.md** with fix summary

---

## Files to Modify

1. `src/SaveState.Infrastructure/Mugen/Repositories/MugenPlayerDataRepository.cs`
2. `src/SaveState.Infrastructure/Mugen/MugenTemplateRepository.cs`
3. `src/SaveState.Infrastructure/Mugen/MoveCreationService.cs`
4. `src/SaveState.Infrastructure/Mugen/MugenExportService.cs`

---

**Document Created**: January 14, 2026
**Author**: Claude Code
**Status**: Ready for Implementation
