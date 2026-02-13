# Technical Debt Remediation - Quick Reference Card

## 🚨 When You Find Issues

### Finding `return null;` in New Code
**❌ Don't:**
```csharp
return null;
```

**✅ Do:**
```csharp
return Result.Failure<T>("Descriptive error message", ErrorType.NotFound);
```

### Finding `!` (Null-Forgiving) Operator
**❌ Don't:**
```csharp
var name = obj!.Property;
```

**✅ Do:**
```csharp
if (obj is null)
    return Result.Failure<string>("Object not found", ErrorType.NotFound);
var name = obj.Property;
```

### Finding Empty Catch Block
**❌ Don't:**
```csharp
catch (Exception)
{
}
```

**✅ Do:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed: {Operation}", operationName);
    return Result.Failure<T>($"Operation failed: {ex.Message}", ErrorType.Internal);
}
```

---

## 🔧 Common Refactoring Patterns

### Service Method Pattern
```csharp
public async Task<Result<SomeDto>> GetSomethingAsync(Guid id, CancellationToken ct = default)
{
    try
    {
        // Validation
        if (id == Guid.Empty)
            return Result.Failure<SomeDto>("Invalid ID", ErrorType.Validation);
        
        // Repository call
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity.IsFailure)
            return Result.Failure<SomeDto>("Entity not found", ErrorType.NotFound);
        
        // Mapping
        var dto = MapToDto(entity.Value);
        
        return Result.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get something: {Id}", id);
        return Result.Failure<SomeDto>($"Internal error: {ex.Message}", ErrorType.Internal);
    }
}
```

### Repository Pattern
```csharp
public async Task<Result<Entity>> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    var entity = await _dbContext.Entities
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Id == id, ct);
    
    if (entity is null)
        return Result.Failure<Entity>($"Entity with ID {id} not found", ErrorType.NotFound);
    
    return Result.Success(entity);
}
```

---

## 📊 Weekly Metrics to Track

Run these commands weekly and record results:

```powershell
# Count return null statements
(Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
    Select-String -Pattern "return\s+null;" | 
    Measure-Object).Count

# Count null-forgiving operators
(Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
    Select-String -Pattern "!\.|!\[" | 
    Measure-Object).Count

# Count TODO comments
(Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
    Select-String -Pattern "TODO|FIXME|HACK" | 
    Measure-Object).Count

# Run tests
dotnet test --verbosity minimal
```

---

## 🎯 Priority Cheat Sheet

| Issue | Priority | Effort | Impact |
|-------|----------|--------|--------|
| EndToEnd test failures | 🔴 P0 | 8-12h | Release blocking |
| Result pattern violations | 🟠 P1 | 40-60h | Runtime safety |
| Null-forgiving operators | 🟠 P1 | 60-80h | Compile-time safety |
| Dependency versions | 🟡 P2 | 4-8h | Build stability |
| Debug logging | 🟡 P2 | 4h | Performance |
| TODO comments | 🟢 P3 | 12-16h | Maintainability |
| Large classes | 🟢 P3 | 80h+ | Architecture |

---

## 🔍 Code Review Checklist

- [ ] No `return null;` without justification comment
- [ ] No `!` operator without justification comment  
- [ ] All catch blocks have logging or explicit handling
- [ ] All service methods return `Result<T>` or `Task<Result<T>>`
- [ ] All public methods have XML documentation
- [ ] No new TODO comments without issue number

---

## 📞 When to Ask for Help

**Ask immediately if:**
- Changing an interface affects >5 files
- Test failures seem unrelated to your changes
- You're unsure which ErrorType to use
- The refactoring is getting complex (>2 hours)

**ErrorType Guidelines:**
- `Validation` - Input validation failed
- `NotFound` - Resource doesn't exist
- `Unauthorized` - User not authenticated
- `Forbidden` - User lacks permission
- `Conflict` - Resource already exists
- `Internal` - Unexpected error
- `External` - Third-party service failed

---

## 📚 Key Files to Know

| File | Purpose |
|------|---------|
| `src/SaveState.Core/Common/Result.cs` | Result pattern implementation |
| `src/SaveState.Core/Common/ErrorType.cs` | Error categorization |
| `TECHNICAL_DEBT_REMEDIATION_PLAN_EXTENSIVE.md` | Full remediation plan |
| `.editorconfig` | Code style rules |
| `Directory.Build.props` | Shared MSBuild properties |

---

*Keep this card handy while coding!*
