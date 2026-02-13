# Presentation Layer Build Error Fix Plan - Phase 3 (ACTUAL ERRORS)

**Date**: 2026-01-15
**Current Errors**: 18 errors (from actual build output)
**Target**: 0 errors
**Status**: READY TO EXECUTE

---

## 📊 ACTUAL ERROR SUMMARY (from build_errors_presentation.txt)

| Error | File | Line | Root Cause | Fix Phase |
|-------|------|------|-------------|------------|
| CS0234: `Repositories` namespace doesn't exist | Program.cs | 20 | Wrong namespace reference | Phase 1 |
| CS0101: Duplicate `BoolToBrushConverter` | MugenConverters.cs | 270 | Class defined twice | Phase 2 |
| CS0111: Duplicate `Convert` method | MugenConverters.cs | 272 | Duplicate class | Phase 2 |
| CS0111: Duplicate `ConvertBack` method | MugenConverters.cs | 278 | Duplicate class | Phase 2 |
| CS0246: `CharacterBalanceAnalysis` not found | MachineLearningViewModel.cs | 36 | Missing using | Phase 3 |
| CS0246: `CharacterBalanceAnalysis` not found (generated) | MachineLearningViewModel.g.cs | 401,407,412,418 | Missing using | Phase 3 |
| CS0246: `IMugenTemplateRepository` not found | MoveCreationViewModel.cs | 18 | Missing using | Phase 4 |
| CS0246: `IMugenValidationService` not found | MoveCreationViewModel.cs | 19 | Missing using | Phase 4 |
| CS0246: `IMugenBalancingService` not found | MoveCreationViewModel.cs | 20 | Missing using | Phase 4 |
| CS0246: `IMugenExportService` not found | MoveCreationViewModel.cs | 21 | Missing using | Phase 4 |
| CS0246: `IMugenTestService` not found | MoveCreationViewModel.cs | 22 | Missing using | Phase 4 |
| CS0104: Ambiguous `ValidationResult` | MoveCreationViewModel.cs | 37 | Two types exist | Phase 5 |
| CS0246: `IMugenTemplateRepository` not found | MoveCreationViewModel.cs | 60 | Missing using | Phase 4 |
| CS0246: `IMugenValidationService` not found | MoveCreationViewModel.cs | 61 | Missing using | Phase 4 |
| CS0246: `IMugenBalancingService` not found | MoveCreationViewModel.cs | 62 | Missing using | Phase 4 |
| CS0246: `IMugenExportService` not found | MoveCreationViewModel.cs | 63 | Missing using | Phase 4 |
| CS0246: `IMugenTestService` not found | MoveCreationViewModel.cs | 64 | Missing using | Phase 4 |
| MVVMTK0007: `CreateMoveFromTemplate` signature | MoveCreationViewModel.cs | 124 | Wrong method signature | Phase 6 |

---

## 🔍 ERROR ANALYSIS

### Phase 1: Namespace Reference Error (1 error)

**Error**: `CS0234: The type or namespace name 'Repositories' does not exist in the namespace 'SaveState.Core.Mugen'`

**Location**: [`src/SaveState.Presentation/Program.cs:20`](src/SaveState.Presentation/Program.cs:20)

**Root Cause**: The code tries to use `SaveState.Core.Mugen.Repositories` but repositories are actually in `SaveState.Infrastructure.Mugen.Repositories`

**Current Code**:

```csharp
using SaveState.Core.Mugen.Repositories;
```

**Required Code**:

```csharp
using SaveState.Infrastructure.Mugen.Repositories;
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation`

---

### Phase 2: Duplicate Converter Class (3 errors)

**Errors**: CS0101, CS0111 (2x) - Duplicate `BoolToBrushConverter` class

**Location**: [`src/SaveState.Presentation/Converters/MugenConverters.cs:270-282`](src/SaveState.Presentation/Converters/MugenConverters.cs:270)

**Root Cause**: The class `BoolToBrushConverter` is defined in TWO files:

1. [`src/SaveState.Presentation/Converters/MugenConverters.cs:270`](src/SaveState.Presentation/Converters/MugenConverters.cs:270)
2. [`src/SaveState.Presentation/Converters/GameLibraryConverters.cs:79`](src/SaveState.Presentation/Converters/GameLibraryConverters.cs:79)

**Solution**: DELETE the duplicate from MugenConverters.cs (lines 270-282)

**Code to DELETE**:

```csharp
/// <summary>
/// Converts boolean values to status brush colors (Green for true, Red for false).
/// </summary>
public class BoolToStatusBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        return boolValue ? Brushes.Green : Brushes.Red;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
```

**Verification**: After deletion, run `dotnet build src/SaveState.Presentation`

---

### Phase 3: Missing CharacterBalanceAnalysis Type (5 errors)

**Errors**: CS0246 (5x) - `CharacterBalanceAnalysis` type not found

**Locations**:

- [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs:36`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs:36)
- Generated file: `obj/Debug/net9.0/.../MachineLearningViewModel.g.cs` (lines 401, 407, 412, 418)

**Root Cause**: Missing using statement. The type exists at [`src/SaveState.Core/Mugen/ValueObjects/CharacterBalanceAnalysis.cs`](src/SaveState.Core/Mugen/ValueObjects/CharacterBalanceAnalysis.cs:1)

**Current Code** (line 1-9):

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;
```

**Required Code**: Add the missing using statement:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation`

---

### Phase 4: Missing Service Interfaces (10 errors)

**Errors**: CS0246 (10x) - Missing MUGEN service interfaces

**Locations**: [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:18-22,60-64)

**Root Cause**: Missing using statements for Infrastructure services. These interfaces exist in `SaveState.Infrastructure.Mugen`:

- `IMugenTemplateRepository` → [`SaveState.Infrastructure.Mugen.MugenTemplateRepository`](src/SaveState.Infrastructure/Mugen/MugenTemplateRepository.cs:11)
- `IMugenValidationService` → [`SaveState.Infrastructure.Mugen.MugenValidationService`](src/SaveState.Infrastructure/Mugen/MugenValidationService.cs:15)
- `IMugenBalancingService` → [`SaveState.Infrastructure.Mugen.MugenBalancingService`](src/SaveState.Infrastructure/Mugen/MugenBalancingService.cs:13)
- `IMugenExportService` → [`SaveState.Infrastructure.Mugen.MugenExportService`](src/SaveState.Infrastructure/Mugen/MugenExportService.cs:15)
- `IMugenTestService` → [`SaveState.Infrastructure.Mugen.MugenTestService`](src/SaveState.Infrastructure/Mugen/MugenTestService.cs:14)

**Current Code** (lines 18-22, 60-64):

```csharp
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
// ... other usings

// Constructor parameters (lines 60-64)
public MoveCreationViewModel(
    IMediator mediator,
    IMoveCreationService moveCreationService,
    INotificationService notificationService,
    IMugenTemplateRepository templateRepository,      // ERROR: not found
    IMugenValidationService validationService,          // ERROR: not found
    IMugenBalancingService balancingService,          // ERROR: not found
    IMugenExportService exportService,              // ERROR: not found
    IMugenTestService testService)                  // ERROR: not found
```

**Required Code**: Add the missing using statement:

```csharp
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Infrastructure.Mugen;
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation`

---

### Phase 5: Ambiguous ValidationResult Reference (1 error)

**Error**: CS0104 - `ValidationResult` is an ambiguous reference

**Location**: [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:37`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:37)

**Root Cause**: Two `ValidationResult` types exist:

1. [`src/SaveState.Core/Mugen/ValueObjects/MugenMoveDefinition.cs:415`](src/SaveState.Core/Mugen/ValueObjects/MugenMoveDefinition.cs:415) - `public sealed record ValidationResult(bool IsValid, ...)`
2. Possibly another `ValidationResult` in `SaveState.Core.Mugen.Services` (not found in search, but error suggests it exists)

**Current Code** (line 37):

```csharp
private readonly ValidationResult _validationResult = ValidationResult.Valid();
```

**Solution**: Use fully qualified name to resolve ambiguity:

```csharp
private readonly SaveState.Core.Mugen.ValueObjects.ValidationResult _validationResult = SaveState.Core.Mugen.ValueObjects.ValidationResult.Valid;
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation`

---

### Phase 6: MVVMTK0007 Command Signature Error (1 error)

**Error**: MVVMTK0007 - Method signature not compatible with relay command types

**Location**: [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:124`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:124)

**Root Cause**: The method `CreateMoveFromTemplate` has signature that doesn't match MVVM Toolkit's expected relay command signatures

**Current Code** (around line 124):

```csharp
[RelayCommand]
private async Task CreateMoveFromTemplate(string templateName, string templatePath)
{
    // ... implementation
}
```

**Analysis**: The MVVM Toolkit's `[RelayCommand]` attribute expects specific method signatures:

- `Task Execute()` (no parameters)
- `Task Execute(T parameter)` (one parameter)
- `Task Execute(T1 param1, T2 param2)` (two parameters)

The method `CreateMoveFromTemplate(string templateName, string templatePath)` has TWO parameters, but the `[RelayCommand]` attribute without specifying a generic type will try to use the single-parameter version.

**Solution Options**:

**Option A**: Remove the `[RelayCommand]` attribute and call manually (simplest):

```csharp
// Remove [RelayCommand] attribute
private async Task CreateMoveFromTemplate(string templateName, string templatePath)
{
    // ... implementation
}

// Call it manually where needed
```

**Option B**: Change to single parameter with a DTO:

```csharp
public record TemplateSelection(string TemplateName, string TemplatePath);

[RelayCommand]
private async Task CreateMoveFromTemplate(TemplateSelection selection)
{
    // ... implementation
}
```

**Option C**: Create a separate command method with no parameters that reads from properties:

```csharp
[ObservableProperty]
private string _selectedTemplateName = string.Empty;

[ObservableProperty]
private string _selectedTemplatePath = string.Empty;

[RelayCommand]
private async Task CreateMoveFromTemplate()
{
    var result = await _moveCreationService.CreateMoveFromTemplateAsync(
        SelectedTemplateName,
        SelectedTemplatePath);
    // ... rest of implementation
}
```

**Recommended**: Option C (follows MVVM pattern best practices)

**Verification**: After fix, run `dotnet build src/SaveState.Presentation`

---

## ✅ VERIFICATION CHECKLIST

After completing all phases:

```powershell
# 1. Clean and rebuild
dotnet clean SaveStateReborn.sln
dotnet build SaveStateReborn.sln

# 2. Check error count (should be 0)
dotnet build SaveStateReborn.sln 2>&1 | Select-String "error CS" | Measure-Object

# 3. Run tests
dotnet test SaveStateReborn.sln --no-build
```

---

## 📋 EXECUTION ORDER

**CRITICAL**: Execute phases in exact order. Each phase fixes specific errors and may depend on previous phases.

1. **Phase 1** - Fix namespace reference (1 error)
2. **Phase 2** - Remove duplicate converter (3 errors)
3. **Phase 3** - Add missing using for CharacterBalanceAnalysis (5 errors)
4. **Phase 4** - Add missing using for Infrastructure services (10 errors)
5. **Phase 5** - Fix ambiguous ValidationResult reference (1 error)
6. **Phase 6** - Fix MVVMTK0007 command signature (1 error)

**Total errors to fix**: 18

---

## 🔄 ROLLBACK STRATEGY

If a fix causes new errors:

1. **Phase 1 Rollback**: Revert namespace change to `using SaveState.Core.Mugen.Repositories;`
2. **Phase 2 Rollback**: Restore the deleted `BoolToStatusBrushConverter` class
3. **Phase 3 Rollback**: Remove the added `using SaveState.Core.Mugen.ValueObjects;`
4. **Phase 4 Rollback**: Remove `using SaveState.Infrastructure.Mugen;`
5. **Phase 5 Rollback**: Revert to `private readonly ValidationResult _validationResult = ValidationResult.Valid();`
6. **Phase 6 Rollback**: Restore original `[RelayCommand]` attribute and method signature

---

## 📁 FILES TO MODIFY (Summary)

### Presentation Layer (4 files)

1. [`src/SaveState.Presentation/Program.cs`](src/SaveState.Presentation/Program.cs) - Phase 1: Fix namespace
2. [`src/SaveState.Presentation/Converters/MugenConverters.cs`](src/SaveState.Presentation/Converters/MugenConverters.cs) - Phase 2: Delete duplicate converter
3. [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs) - Phase 3: Add using statement
4. [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs) - Phase 4,5,6: Add usings, fix ambiguity, fix command signature

---

## 📊 ERROR REDUCTION TRACKING

| Phase | Errors Before | Errors After | Reduction |
|--------|---------------|--------------|------------|
| Phase 1 | 18 | 17 | -1 |
| Phase 2 | 17 | 14 | -3 |
| Phase 3 | 14 | 9 | -5 |
| Phase 4 | 9 | 0 | -9 |
| Phase 5 | 0 | 0 | 0 |
| Phase 6 | 0 | 0 | 0 |
| **TOTAL** | **18** | **0** | **-18** |

---

## 🎯 KEY IMPROVEMENTS OVER PREVIOUS PLAN

1. **Based on ACTUAL errors** - Not on outdated assumptions
2. **Exact line numbers** - From actual build output
3. **Root cause analysis** - Each error explains WHY it happens
4. **Copy-paste ready fixes** - Exact code to replace/add
5. **Verification commands** - PowerShell commands to verify each phase
6. **Rollback strategy** - Clear rollback steps if something goes wrong
7. **Execution order** - Phases must be done in sequence
8. **Error tracking** - Table showing reduction after each phase
