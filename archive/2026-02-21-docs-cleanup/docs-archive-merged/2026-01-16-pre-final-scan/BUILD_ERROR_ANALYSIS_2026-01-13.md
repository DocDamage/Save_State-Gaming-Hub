# Build Error Analysis - January 13, 2026

**Total Errors**: 24
**Project**: SaveState.Presentation
**Status**: Analysis Complete - Ready to Fix

---

## Error Summary by Category

| Category | Count | Priority |
|----------|-------|----------|
| Missing Type Definitions | 13 | 🔴 Critical |
| Duplicate Definitions | 3 | 🔴 Critical |
| Namespace Issues | 1 | 🔴 Critical |
| Type Ambiguity | 1 | 🔴 Critical |
| MVVM Toolkit | 1 | 🟠 High |
| Generated Code Issues | 6 | 🟡 Medium (Cascading) |

---

## Category 1: Missing Type Definitions (13 errors)

### Error Group 1.1: Missing MUGEN Service Interfaces (5 errors)

**File**: `MoveCreationViewModel.cs`
**Lines**: 18-22, 60-64

**Missing Types**:

1. `IMugenTemplateRepository` (2 occurrences)
2. `IMugenValidationService` (2 occurrences)
3. `IMugenBalancingService` (2 occurrences)
4. `IMugenExportService` (2 occurrences)
5. `IMugenTestService` (2 occurrences)

**Root Cause**: These interfaces don't exist in the Core layer

**Fix Strategy**:

- Option A: Create stub interfaces in Core layer
- Option B: Remove dependencies from ViewModel (if not needed yet)
- Option C: Comment out the ViewModel temporarily

**Recommendation**: Option A - Create interfaces with TODO implementations

---

### Error Group 1.2: Missing CharacterBalanceAnalysis Type (7 errors)

**File**: `MachineLearningViewModel.cs` + generated code
**Lines**: 36 (source), 96, 401, 407, 412, 418 (generated)

**Missing Type**: `CharacterBalanceAnalysis`

**Root Cause**: Type was removed or never created in Core/Application layer

**Fix Strategy**:

- Option A: Create `CharacterBalanceAnalysis` DTO/entity
- Option B: Replace with existing type
- Option C: Comment out property temporarily

**Recommendation**: Option A - Create DTO in Core.Mugen namespace

---

### Error Group 1.3: Missing Namespace (1 error)

**File**: `Program.cs`
**Line**: 20

**Missing**: `SaveState.Core.Mugen.Repositories` namespace

**Root Cause**: Namespace doesn't exist or using statement is wrong

**Fix Strategy**: Check if namespace exists, create if needed, or fix using statement

---

## Category 2: Duplicate Definitions (3 errors)

### Error Group 2.1: Duplicate BoolToBrushConverter (3 errors)

**File**: `MugenConverters.cs`
**Lines**: 270, 272, 278

**Issue**: `BoolToBrushConverter` defined multiple times in same namespace

**Root Cause**: Likely copy-paste error or merge conflict

**Fix Strategy**: Remove duplicate definition, keep only one

---

## Category 3: Type Ambiguity (1 error)

### Error Group 3.1: Ambiguous ValidationResult (1 error)

**File**: `MoveCreationViewModel.cs`
**Line**: 37

**Issue**: `ValidationResult` exists in both:

- `SaveState.Core.Mugen.Services.ValidationResult`
- `SaveState.Core.Mugen.ValueObjects.ValidationResult`

**Fix Strategy**: Use fully qualified name or add using alias

**Example**:

```csharp
using ValidationResult = SaveState.Core.Mugen.ValueObjects.ValidationResult;
```

---

## Category 4: MVVM Toolkit Error (1 error)

### Error Group 4.1: Invalid Command Signature (1 error)

**File**: `MoveCreationViewModel.cs`
**Line**: 124
**Method**: `CreateMoveFromTemplate(string, string)`

**Issue**: Method signature not compatible with RelayCommand

**Root Cause**: RelayCommand doesn't support methods with 2 string parameters

**Fix Strategy**:

- Option A: Change to single parameter (DTO)
- Option B: Remove [RelayCommand] and create manual command
- Option C: Change signature to `CreateMoveFromTemplate(object parameter)`

**Recommendation**: Option A - Create parameter DTO

---

## Category 5: Generated Code Issues (6 errors)

These are cascading errors from missing `CharacterBalanceAnalysis` type.
Will be fixed automatically when Category 1, Group 1.2 is resolved.

---

## Fix Order (Dependency-Based)

### Phase 1: Fix Duplicates (Immediate)

1. ✅ Remove duplicate `BoolToBrushConverter` in `MugenConverters.cs`

### Phase 2: Create Missing Types (30 min)

2. ✅ Create `CharacterBalanceAnalysis` DTO
2. ✅ Create missing MUGEN service interfaces:
   - `IMugenTemplateRepository`
   - `IMugenValidationService`
   - `IMugenBalancingService`
   - `IMugenExportService`
   - `IMugenTestService`

### Phase 3: Fix Ambiguity (5 min)

4. ✅ Add using alias for `ValidationResult` in `MoveCreationViewModel.cs`

### Phase 4: Fix Namespace (5 min)

5. ✅ Fix `SaveState.Core.Mugen.Repositories` namespace issue in `Program.cs`

### Phase 5: Fix MVVM Command (15 min)

6. ✅ Fix `CreateMoveFromTemplate` command signature

### Phase 6: Verify (5 min)

7. ✅ Run build and verify 0 errors

---

## Estimated Time to Fix

| Phase | Time | Complexity |
|-------|------|------------|
| Phase 1 | 5 min | Low |
| Phase 2 | 30 min | Medium |
| Phase 3 | 5 min | Low |
| Phase 4 | 5 min | Low |
| Phase 5 | 15 min | Medium |
| Phase 6 | 5 min | Low |
| **TOTAL** | **65 min** | **Medium** |

---

## Files to Modify

### Presentation Layer

1. `src/SaveState.Presentation/Converters/MugenConverters.cs` - Remove duplicate
2. `src/SaveState.Presentation/ViewModels/Shell/Mugen/MoveCreationViewModel.cs` - Add using alias, fix command
3. `src/SaveState.Presentation/Program.cs` - Fix namespace

### Core Layer (New Files)

4. `src/SaveState.Core/Mugen/DTOs/CharacterBalanceAnalysis.cs` - Create DTO
2. `src/SaveState.Core/Mugen/Repositories/IMugenTemplateRepository.cs` - Create interface
3. `src/SaveState.Core/Mugen/Services/IMugenValidationService.cs` - Create interface
4. `src/SaveState.Core/Mugen/Services/IMugenBalancingService.cs` - Create interface
5. `src/SaveState.Core/Mugen/Services/IMugenExportService.cs` - Create interface
6. `src/SaveState.Core/Mugen/Services/IMugenTestService.cs` - Create interface

---

## Success Criteria

- ✅ All 24 errors resolved
- ✅ Solution builds with 0 errors
- ✅ All 515 tests still passing
- ✅ No new warnings introduced
- ✅ Code follows existing patterns

---

**Next Action**: Start Phase 1 - Remove duplicate BoolToBrushConverter
