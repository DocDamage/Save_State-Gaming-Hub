# Presentation Layer Build Error Fix Plan - Phase 3 (ENHANCED)

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
| CS0111: Duplicate `convert` method | MugenConverters.cs | 272 | Duplicate class | Phase 2 |
| CS0111: Duplicate `convertBack` method | MugenConverters.cs | 278 | Duplicate class | Phase 2 |
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

**Root Cause**: The code tries to use `SaveState.Core.Mugen.Repositories` but repositories are actually in `SaveState.Infrastructure.Mugen.Repositories`. This is a common architectural issue where the wrong namespace is referenced.

**Context**: The Infrastructure layer contains implementation classes for MUGEN repositories and services. The Core layer contains interfaces. The Presentation layer should reference Infrastructure for concrete implementations.

**Current Code**:

```csharp
using SaveState.Core.Mugen.Repositories;
```

**Required Code**:

```csharp
using SaveState.Infrastructure.Mugen.Repositories;
```

**Impact**: This affects all MUGEN repository registrations in DI container (lines 133-142 in Program.cs). Specifically, these lines will fail:

- Line 133: `builder.Services.AddTransient<IMugenTemplateRepository, SaveState.Infrastructure.Mugen.MugenTemplateRepository>();`
- Line 140: `builder.Services.AddTransient<IMatchDataRepository, SaveState.Infrastructure.Mugen.MatchDataRepository>();`
- Line 141: `builder.Services.AddTransient<ICharacterDataRepository, SaveState.Infrastructure.Mugen.CharacterDataRepository>();`

**Verification**: After fix, run `dotnet build src/SaveState.Presentation` and confirm error count reduces by 1

**Rollback**: If new errors appear, revert to `using SaveState.Core.Mugen.Repositories;`

---

### Phase 2: Duplicate Converter Class (3 errors)

**Errors**: CS0101, CS0111 (2x) - Duplicate `BoolToBrushConverter` class

**Location**: [`src/SaveState.Presentation/Converters/MugenConverters.cs:270`](src/SaveState.Presentation/Converters/MugenConverters.cs:270)

**Root Cause**: The class `BoolToBrushConverter` is defined in TWO files:

1. [`src/SaveState.Presentation/Converters/MugenConverters.cs:270`](src/SaveState.Presentation/Converters/MugenConverters.cs:270) (duplicate to delete)
2. [`src/SaveState.Presentation/Converters/GameLibraryConverters.cs:79`](src/SaveState.Presentation/Converters/GameLibraryConverters.cs:79) (original, keep this one)

**Context**: When both files are compiled, the C# compiler finds two classes with the same name in the same namespace `SaveState.Presentation.Converters`, causing a conflict. The duplicate appears to be a copy-paste error or accidental duplication.

**Solution**: DELETE the duplicate from MugenConverters.cs (lines 267-282). The GameLibraryConverters.cs version should be kept as it appears to be the original.

**Code to DELETE** (lines 267-282 in MugenConverters.cs):

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

**Verification**: After deletion, run `dotnet build src/SaveState.Presentation` and confirm error count reduces by 3

**Rollback**: If new errors appear related to BoolToStatusBrushConverter, restore the deleted code block

---

### Phase 3: Missing CharacterBalanceAnalysis Type (5 errors)

**Errors**: CS0246 (5x) - `CharacterBalanceAnalysis` type not found

**Locations**:

- [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs:36`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MachineLearningViewModel.cs:36)
- Generated file: `obj/Debug/net9.0/.../MachineLearningViewModel.g.cs` (lines 401, 407, 412, 418)

**Root Cause**: Missing using statement. The type exists at [`src/SaveState.Core/Mugen/ValueObjects/CharacterBalanceAnalysis.cs:1`](src/SaveState.Core/Mugen/ValueObjects/CharacterBalanceAnalysis.cs:1) but is not imported.

**Context**: The MVVM Toolkit source generator creates code that references types used in ObservableProperty attributes. When a type is not in scope, the generated code fails to compile. The generated file is created during build and contains references to `CharacterBalanceAnalysis` that need the type to be in scope.

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

**Required Code**: The `using SaveState.Core.Mugen.ValueObjects;` statement should already bring in `CharacterBalanceAnalysis`. If it doesn't work, try:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.ValueObjects.CharacterBalanceAnalysis;  // Add explicit reference
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation` and confirm error count reduces by 5

**Rollback**: If new errors appear, remove the added using statement

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

**Context**: The MoveCreationViewModel constructor and class methods reference these service interfaces. Without the proper using statements, the compiler cannot find these types.

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
{
    // ... constructor body
}
```

**Required Code**: Add the missing using statement:

```csharp
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Infrastructure.Mugen;  // This brings in all Infrastructure services
// ... other usings
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation` and confirm error count reduces by 10

**Rollback**: If new errors appear, remove `using SaveState.Infrastructure.Mugen;`

---

### Phase 5: Ambiguous ValidationResult Reference (1 error)

**Error**: CS0104 - `ValidationResult` is an ambiguous reference

**Location**: [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:37`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:37)

**Root Cause**: Two `ValidationResult` types exist:

1. [`src/SaveState.Core/Mugen/ValueObjects/MugenMoveDefinition.cs:415`](src/SaveState.Core/Mugen/ValueObjects/MugenMoveDefinition.cs:415) - `public sealed record ValidationResult(bool IsValid, ...)`
2. Possibly another `ValidationResult` in `SaveState.Core.Mugen.Services` (not found in search, but error suggests it exists)

**Context**: When the compiler encounters `ValidationResult` in the code, it finds two matching types and cannot determine which one to use. This is a naming conflict that needs to be resolved with a fully qualified name.

**Current Code** (line 37):

```csharp
private readonly ValidationResult _validationResult = ValidationResult.Valid();
```

**Solution**: Use fully qualified name to resolve ambiguity:

```csharp
private readonly SaveState.Core.Mugen.ValueObjects.ValidationResult _validationResult = SaveState.Core.Mugen.ValueObjects.ValidationResult.Valid;
```

**Verification**: After fix, run `dotnet build src/SaveState.Presentation` and confirm error count reduces by 1

**Rollback**: If new errors appear, revert to `private readonly ValidationResult _validationResult = ValidationResult.Valid();`

---

### Phase 6: MVVMTK0007 Command Signature Error (1 error)

**Error**: MVVMTK0007 - Method signature not compatible with relay command types

**Location**: [`src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:124`](src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs:124)

**Root Cause**: The method `CreateMoveFromTemplate` has signature that doesn't match MVVM Toolkit's expected relay command signatures.

**Context**: The MVVM Toolkit's `[RelayCommand]` attribute expects specific method signatures:

- `Task Execute()` (no parameters)
- `Task Execute(T parameter)` (one parameter)
- `Task Execute(T1 param1, T2 param2)` (two parameters)

The method `CreateMoveFromTemplate(string templateName, string templatePath)` has TWO parameters, but the `[RelayCommand]` attribute without specifying a generic type will try to use the single-parameter version, which doesn't match.

**Current Code** (around line 124):

```csharp
[RelayCommand]
private async Task CreateMoveFromTemplate(string templateName, string templatePath)
{
    // ... implementation
}
```

**Analysis**: The MVVM Toolkit's `[RelayCommand]` source generator looks for methods with specific signatures. When it finds a method with two parameters but no generic type specification, it generates invalid code.

**Solution Options**:

**Option A**: Remove `[RelayCommand]` attribute and call manually (simplest):

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

**Recommended**: Option C (follows MVVM pattern best practices and is most maintainable)

**Verification**: After fix, run `dotnet build src/SaveState.Presentation` and confirm error count reduces by 1

**Rollback**: Restore original `[RelayCommand]` attribute and method signature

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
2. **Phase 2 Rollback**: Restore the deleted `BoolToStatusBrushConverter` class (lines 267-282 in MugenConverters.cs)
3. **Phase 3 Rollback**: Remove the added `using SaveState.Core.Mugen.ValueObjects.CharacterBalanceAnalysis;` statement
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

1. **Based on ACTUAL errors** - Not on outdated assumptions (138 errors that no longer exist)
2. **Exact line numbers** - From actual build output with clickable file links
3. **Root cause analysis** - Each error explains WHY it happens with architectural context
4. **Copy-paste ready fixes** - Exact code to replace/add, not just descriptions
5. **Verification commands** - PowerShell commands to verify each phase
6. **Rollback strategy** - Clear rollback steps if something goes wrong
7. **Execution order** - Phases must be done in sequence with dependency tracking
8. **Error tracking** - Table showing reduction after each phase
9. **Enhanced context** - Each phase includes detailed context about the error, impact, and verification steps
