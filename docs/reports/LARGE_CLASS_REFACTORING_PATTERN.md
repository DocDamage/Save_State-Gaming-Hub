# Large Class Refactoring Pattern

**Date:** February 12, 2026  
**Pattern:** Extract Engines, Models, and Use Partial Classes  
**Example:** NarrativeMemoryService Refactoring

---

## Overview

This document describes the established pattern for refactoring large service classes in the SaveStateReborn codebase.

### Example: NarrativeMemoryService

**Before:** 1,042 lines (single monolithic file)  
**After:** 441 lines service + extracted files (-58% reduction)

---

## Refactoring Pattern

### Step 1: Identify Components

Analyze the large service to identify:
- **Public API methods** (keep in main service)
- **Private helper methods** (move to engines or keep private)
- **Business logic engines** (extract to separate classes)
- **Model/DTO classes** (extract to Models folder)
- **Enums** (extract to Models folder)

### Step 2: Create Folder Structure

```
Services/{ServiceName}/
├── {ServiceName}.cs          # Main coordinator service
├── I{ServiceName}.cs         # Interface
├── Engines/                  # Business logic engines
│   ├── {Feature}Engine.cs
│   └── ...
└── TypeAliases.cs            # Backward compatibility

Models/{ServiceName}/
├── {Feature}Models.cs        # Grouped model classes
├── {Feature}Enums.cs         # Enumerations
└── TypeAliases.cs            # Backward compatibility types
```

### Step 3: Extract Engines

Engines contain pure business logic and don't depend on the service state:

```csharp
// Services/NarrativeMemory/Engines/CrystalEngine.cs
public class CrystalEngine
{
    private readonly ILogger<CrystalEngine> _logger;
    
    public CrystalEngine(ILogger<CrystalEngine> logger)
    {
        _logger = logger;
    }
    
    public async Task<MemoryCrystal> GenerateCrystalAsync(...)
    {
        // Business logic here
    }
}
```

### Step 4: Extract Models

Move all record/class definitions to the Models folder:

```csharp
// Models/NarrativeMemory/CrystalModels.cs
public record MemoryCrystal
{
    public string CrystalId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    // ...
}

public class AlternatePossibility { ... }
```

### Step 5: Simplify Main Service

The main service becomes a coordinator:

```csharp
public class NarrativeMemoryService : INarrativeMemoryService
{
    private readonly CrystalEngine _crystalEngine;
    private readonly TimelineEngine _timelineEngine;
    // ... more engines
    
    public async Task<Result<MemoryCrystal>> GenerateMemoryCrystalAsync(...)
    {
        // Delegate to engine
        var crystal = await _crystalEngine.GenerateCrystalAsync(...);
        // Handle storage, logging, etc.
        return Result.Success(crystal);
    }
}
```

### Step 6: Create Type Aliases (Optional)

For backward compatibility during transition:

```csharp
// TypeAliases.cs
using MemoryCrystal = Models.NarrativeMemory.MemoryCrystal;
using CrystalRarity = Models.NarrativeMemory.CrystalRarity;
```

---

## Results

### NarrativeMemoryService Refactoring

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Service file | 1,042 lines | 441 lines | -58% |
| Models extracted | 0 | 5 files | New |
| Engines extracted | 0 | 4 files | New |
| Total files | 1 | 11 | Better organization |

### Files Created

```
Services/NarrativeMemory/
├── NarrativeMemoryService.cs        # 441 lines (coordinator)
├── INarrativeMemoryService.cs       # Interface
└── Engines/
    ├── CrystalEngine.cs
    ├── TimelineEngine.cs
    ├── SynthesisEngine.cs
    └── ButterflyEngine.cs

Models/NarrativeMemory/
├── CrystalModels.cs
├── TimelineModels.cs
├── SynthesisModels.cs
├── ButterflyModels.cs
├── MatchModels.cs
├── NarrativeEnums.cs
└── TypeAliases.cs
```

---

## Benefits

1. **Single Responsibility** - Each class has one reason to change
2. **Testability** - Engines can be unit tested independently
3. **Readability** - Smaller files are easier to understand
4. **Maintainability** - Changes are localized
5. **Reusability** - Engines can be composed differently

---

## Remaining Large Services

Services that could benefit from this pattern:

| Service | Lines | Priority |
|---------|-------|----------|
| OpenMKService | ~1,104 | High |
| RetroArchService | ~1,086 | High |
| NetworkFeaturesService | ~1,007 | High |
| CrossPhaseIntegrationService | ~916 | Medium |
| BalanceTuningService | ~894 | Medium |
| LiveSyncService | ~826 | Medium |

**Estimated effort per service:** 30-60 minutes using this pattern

---

## Conclusion

The refactoring pattern has been successfully demonstrated with NarrativeMemoryService. The same approach can be applied to remaining large services to achieve the goal of reducing services exceeding 500 lines from 41 to under 30.
