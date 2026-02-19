# 🔧 Technical Debt Remediation Plan

**Project:** SaveStateReborn  
**Date:** February 18, 2026  
**Status:** Post-Audit Remediation Plan  
**Priority:** Critical → High → Medium → Low  
**Estimated Total Effort:** 80-120 hours

---

## 🎯 Executive Summary

This plan addresses **88 failing tests** and **1,862 code quality violations** identified in the February 18, 2026 technical debt audit. The remediation is organized into four phases over **4-6 weeks**.

| Phase | Focus | Duration | Effort | Outcome |
|-------|-------|----------|--------|---------|
| **Phase 1** | Test Infrastructure | Week 1 | 16-24h | All tests passing |
| **Phase 2** | Performance & E2E | Week 2 | 20-32h | Stable E2E suite |
| **Phase 3** | Code Quality | Weeks 3-4 | 32-48h | Architecture compliance |
| **Phase 4** | Refactoring | Weeks 5-6 | 12-16h | Maintainability |

---

## 🔴 Phase 1: Critical Test Infrastructure (Week 1)

### 1.1 Integration Test Database Configuration
**Priority:** 🔴 Critical  
**Effort:** 4-6 hours  
**Status:** Blocking 34 tests

#### Problem
Integration tests fail with:
```
OptionsValidationException: DataAnnotation validation failed for 'DatabaseOptions' 
members: 'ConnectionString' with the error: 'Connection string is required'.
```

#### Solution
Create test-specific configuration:

**Files to Modify:**
- `tests/SaveState.IntegrationTests/TestDbContextFactory.cs` (create if missing)
- `tests/SaveState.IntegrationTests/IntegrationTestFixture.cs` (create if missing)
- `tests/SaveState.IntegrationTests/appsettings.Test.json` (create)

**Implementation Steps:**
1. Create in-memory SQLite configuration for tests
2. Add `WebApplicationFactory` customization
3. Configure test service provider with mocked options
4. Add database seeding for test data

**Code Template:**
```csharp
// IntegrationTestFixture.cs
public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    
    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();
        
        // Configure in-memory database
        services.AddDbContext<SaveStateDbContext>(options =>
            options.UseSqlite("DataSource=:memory:"));
        
        // Configure minimal options
        services.Configure<DatabaseOptions>(opts =>
            opts.ConnectionString = "DataSource=:memory:");
        
        ServiceProvider = services.BuildServiceProvider();
        
        // Initialize database
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
    }
    
    public void Dispose()
    {
        // Cleanup
    }
}

// Use with: public class MyTests : IClassFixture<IntegrationTestFixture>
```

#### Success Criteria
- [ ] `RomValidationIntegrationTests` - 10 tests passing
- [ ] `MigrationTests` - 4 tests passing  
- [ ] `SmartLauncherIntegrationTests` - 5 tests passing
- [ ] `GameLibraryIntegrationTests` - 2 tests passing
- [ ] `MetadataEnrichmentIntegrationTests` - 2 tests passing

---

### 1.2 Concurrency Test Stabilization
**Priority:** 🔴 Critical  
**Effort:** 6-8 hours  
**Status:** Blocking 7 tests

#### Problem
Concurrency tests fail intermittently:
- `ConcurrentGameImports_HandlesRaceConditions`
- `ConcurrentReadOperations_DatabaseConsistency`
- `ConcurrentMediatorOperations_HandlesCommandProcessing`
- `ConcurrentAiOperations_MemoryManagement`

#### Solution

**Root Causes:**
1. Shared test state between parallel tests
2. Missing synchronization primitives
3. Test isolation issues

**Files to Modify:**
- `tests/SaveState.Application.Tests/Concurrency/ConcurrencyTests.cs`

**Implementation Steps:**
1. Add `CollectionDefinition` for test isolation:
```csharp
[CollectionDefinition("ConcurrencyTests", DisableParallelization = true)]
public class ConcurrencyTestCollection { }
```

2. Use unique database names per test:
```csharp
private string GetUniqueDbName() => $"test_{Guid.NewGuid():N}.db";
```

3. Add proper cleanup in `Dispose()`:
```csharp
public void Dispose()
{
    _context.Database.EnsureDeleted();
    _context.Dispose();
}
```

4. Increase timeout thresholds for CI:
```csharp
[Fact(Timeout = 60000)] // 60 seconds
public async Task ConcurrentGameImports_HandlesRaceConditions()
```

#### Success Criteria
- [ ] All 7 concurrency tests passing
- [ ] Tests run reliably on CI (10 consecutive passes)

---

### 1.3 Performance Test Threshold Adjustment
**Priority:** 🟠 High  
**Effort:** 2-4 hours  
**Status:** 2 tests flaky

#### Problem
Performance tests fail on development hardware:
- `ProfileComparison_Performance` (204ms > threshold)
- `CalculateSessionDuration_Performance` (104ms > threshold)

#### Solution

**Files to Modify:**
- `tests/SaveState.Application.Tests/SmartLauncher/SmartLauncherPerformanceTests.cs`

**Implementation:**
1. Use statistical approach instead of fixed thresholds:
```csharp
[Fact]
public async Task ProfileComparison_Performance()
{
    const int iterations = 10000;
    var stopwatch = Stopwatch.StartNew();
    
    // ... test code ...
    
    stopwatch.Stop();
    
    // Allow variance: median should be < 100ms
    var medianTime = GetMedianTime(results);
    medianTime.Should().BeLessThan(100);
    
    // Or use relative baseline
    var baseline = GetBaselineTime();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(baseline * 2);
}
```

2. Or skip on non-CI environments:
```csharp
[SkippableFact]
public async Task ProfileComparison_Performance()
{
    Skip.IfNot(Environment.GetEnvironmentVariable("CI") == "true", 
        "Performance tests only run on CI");
    // ...
}
```

#### Success Criteria
- [ ] Performance tests pass on CI
- [ ] Tests don't block local development

---

## 🟠 Phase 2: E2E & Load Test Stabilization (Week 2)

### 2.1 E2E Performance Test Fixes
**Priority:** 🟠 High  
**Effort:** 12-16 hours  
**Status:** 21 of 33 tests failing

#### Problem
All E2E performance tests failing:
- `AdvancedFeatures_MemoryUsage_Acceptable`
- `NetworkQualityMonitoring_Performance_Acceptable`
- `CloudGamingOperations_Performance_Acceptable`
- `ConcurrentAdvancedFeatures_Performance_Acceptable`
- `SaveStateOperations_Performance_Acceptable`
- `ServiceInitialization_Performance_Acceptable`
- `LaunchExperienceGeneration_Performance_Acceptable`
- `ResourceCleanup_Performance_Acceptable`
- `VoiceCommandProcessing_Performance_Acceptable`

#### Solution

**Files to Modify:**
- `tests/SaveState.EndToEndTests/PerformanceIntegrationTests.cs`

**Implementation Steps:**

1. **Diagnose Root Causes** (4h):
   - Add detailed logging to each test
   - Identify bottlenecks with dotTrace/dotMemory
   - Document actual vs expected performance

2. **Optimize Hot Paths** (6-8h):
   - Cache repeated operations
   - Optimize database queries
   - Reduce memory allocations

3. **Adjust Thresholds** (2-4h):
   - Set realistic baselines based on CI hardware
   - Use percentile-based thresholds (p95, p99)

**Example Optimization:**
```csharp
// Before: Loads all games from database
var games = await _gameRepository.GetAllAsync();

// After: Use projection for memory efficiency
var gameCount = await _gameRepository.CountAsync();
```

#### Success Criteria
- [ ] 25+ of 33 E2E tests passing
- [ ] Performance tests run < 5 minutes total

---

### 2.2 Load Test Fixes
**Priority:** 🟠 High  
**Effort:** 8-12 hours  
**Status:** 5 of 6 tests failing

#### Problem
Load tests failing due to:
- Database connection pool exhaustion
- EF Core tracking issues
- Missing retry policies

#### Solution

**Files to Modify:**
- `tests/SaveState.LoadTests/DatabaseLoadTests.cs`
- `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs`

**Implementation:**

1. Add connection resiliency:
```csharp
// In SaveStateDbContext configuration
options.UseSqlite(connectionString, sqliteOptions =>
{
    sqliteOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(1),
        errorNumbersToAdd: null);
});
```

2. Use AsNoTracking for read-heavy operations:
```csharp
var games = await _context.Games
    .AsNoTracking()
    .ToListAsync();
```

3. Implement proper disposal patterns:
```csharp
public async Task RapidSequentialOperations_MaintainsDataIntegrity()
{
    await using var context = CreateDbContext();
    // ... test code ...
}
```

#### Success Criteria
- [ ] 4+ of 6 load tests passing
- [ ] Tests complete without connection errors

---

## 🟡 Phase 3: Code Quality Compliance (Weeks 3-4)

### 3.1 ITimeProvider Migration
**Priority:** 🟡 Medium  
**Effort:** 32-40 hours  
**Status:** 739 violations

#### Problem
Direct `DateTime.Now/UtcNow` usage violates architecture rules and makes code untestable.

#### Solution

**Top Priority Files** (highest usage):
1. `CharacterDiscoveryService.cs` - 23 usages
2. `SoundDesignService.cs` - 23 usages
3. `PerformanceProfilerService.cs` - 12 usages
4. `MugenNetplayService.cs` - 11 usages
5. `ComboDatabaseService.cs` - 11 usages

**Implementation Pattern:**
```csharp
// Before
public class MyService
{
    public void DoWork()
    {
        var now = DateTime.UtcNow; // ❌ Wrong
    }
}

// After
public class MyService
{
    private readonly ITimeProvider _timeProvider;
    
    public MyService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void DoWork()
    {
        var now = _timeProvider.UtcNow; // ✅ Correct
    }
}
```

**Migration Strategy:**
1. **Week 3:** Migrate MUGEN services (5 files, ~80 usages)
2. **Week 4:** Migrate remaining high-usage services (20 files, ~150 usages)
3. **Backlog:** Remaining 500+ usages (gradual migration)

**Tracking Progress:**
```bash
# Check remaining violations
Get-ChildItem -Path "src" -Recurse -Filter "*.cs" | 
    Select-String -Pattern "DateTime\.(Now|UtcNow)" | 
    Measure-Object
```

#### Success Criteria
- [ ] MUGEN services migrated (0 violations)
- [ ] Architecture test updated to enforce ITimeProvider
- [ ] < 600 total violations remaining

---

### 3.2 Result Pattern Completion
**Priority:** 🟡 Medium  
**Effort:** 24-32 hours  
**Status:** 730 methods need migration

#### Problem
Public service methods return raw types instead of `Result<T>`, making error handling inconsistent.

**Migration Priority:**
1. **Core Services** (Public API surface)
2. **Application Services** (CQRS handlers)
3. **Infrastructure Services** (External integrations)

**Example Migration:**
```csharp
// Before
public async Task<UserProfile> GetUserProfileAsync(int userId)
{
    var user = await _repository.GetByIdAsync(userId);
    if (user == null) return null; // ❌ Silent failure
    return MapToProfile(user);
}

// After
public async Task<Result<UserProfile>> GetUserProfileAsync(int userId)
{
    var user = await _repository.GetByIdAsync(userId);
    if (user == null)
        return Result<UserProfile>.Failure("User not found", ErrorType.NotFound);
    return Result<UserProfile>.Success(MapToProfile(user));
}
```

**Exclusions** (Acceptable null returns):
- Private parsing helpers (`TryParse`, `Extract*`)
- UI cancellation patterns (`DialogResult?`)
- Value converters

#### Success Criteria
- [ ] All public service methods use `Result<T>`
- [ ] Architecture test threshold reduced to < 400 violations
- [ ] No breaking changes to existing working code

---

### 3.3 Async Naming Convention
**Priority:** 🟡 Medium  
**Effort:** 16-20 hours  
**Status:** 393 methods missing `Async` suffix

#### Problem
Inconsistent async method naming violates .NET conventions.

**Common Violations:**
```csharp
// Wrong
public Task<User> GetUser(int id);          // Should be GetUserAsync
public Task Handle(Command command);        // Should be HandleAsync
public Task SaveChanges(CancellationToken ct); // Should be SaveChangesAsync
```

**Migration Strategy:**
1. **Batch rename** with IDE refactoring tools
2. **Update all call sites** simultaneously
3. **Run full test suite** after each batch

**Files by Priority:**
1. `SaveState.Application/Queries/Handlers/*` - 150+ methods
2. `SaveState.Application/Commands/Handlers/*` - 150+ methods
3. `SaveState.Core/Services/*` - 93 methods

#### Success Criteria
- [ ] < 200 naming violations remaining
- [ ] No compilation errors
- [ ] All tests passing

---

## 🟢 Phase 4: Large Class Refactoring (Weeks 5-6)

### 4.1 SaveStateDbContext Split
**Priority:** 🟢 Low  
**Effort:** 8-12 hours  
**Status:** 2,847 lines (target: <1,000)

#### Problem
`SaveStateDbContext` violates single responsibility principle.

#### Solution

**Refactoring Strategy:**

1. **Extract configurations** (already done - separate files)
2. **Split into bounded contexts:**
```
SaveStateDbContext (core)
├── GameLibraryDbContext : SaveStateDbContext
├── MugenDbContext : SaveStateDbContext  
├── RomManagementDbContext : SaveStateDbContext
└── UserManagementDbContext : SaveStateDbContext
```

3. **Use partial classes** for organization:
```csharp
// SaveStateDbContext.Core.cs
public partial class SaveStateDbContext : DbContext
{
    // Core entities only
}

// SaveStateDbContext.Mugen.cs  
public partial class SaveStateDbContext
{
    public DbSet<MugenCharacter> MugenCharacters { get; set; }
    // ...
}
```

#### Success Criteria
- [ ] Main DbContext < 1,000 lines
- [ ] All existing tests passing
- [ ] No breaking changes to DI registration

---

### 4.2 MUGEN Service Refactoring
**Priority:** 🟢 Low  
**Effort:** 4-8 hours  
**Status:** 5 services >1,000 lines

#### Services to Refactor:
| Service | Lines | Strategy |
|---------|-------|----------|
| StoryModeService | 1,719 | Extract repositories |
| ComboDatabaseService | 1,662 | Extract search/query logic |
| SpriteAnimationService | 1,586 | Extract format handlers |
| SoundDesignService | 1,426 | Extract effect pipeline |
| PerformanceProfilerService | 1,379 | Extract analytics |

**Example Refactoring:**
```csharp
// Before: StoryModeService has 1,719 lines with all logic

// After: Split responsibilities
public class StoryModeService : IStoryModeService
{
    private readonly IStoryRepository _repository;
    private readonly IStoryExportService _exportService;
    private readonly IStoryAssetManager _assetManager;
    // Each < 500 lines
}
```

#### Success Criteria
- [ ] All services < 1,500 lines
- [ ] Architecture test passing
- [ ] No regression in functionality

---

## 📊 Progress Tracking

### Weekly Checkpoints

| Week | Deliverable | KPI | Owner |
|------|-------------|-----|-------|
| 1.1 | Integration tests | 34 → 0 failures | TBD |
| 1.2 | Concurrency tests | 7 → 0 failures | TBD |
| 2.1 | E2E tests | 21 → 5 failures | TBD |
| 2.2 | Load tests | 5 → 1 failures | TBD |
| 3.1 | ITimeProvider | 739 → 500 violations | TBD |
| 3.2 | Result pattern | 730 → 500 violations | TBD |
| 4.1 | Async naming | 393 → 200 violations | TBD |
| 5.1 | DbContext split | 2,847 → 1,000 lines | TBD |
| 6.1 | Service refactoring | 11 → 5 large classes | TBD |

### Success Metrics

```
Target State (4-6 weeks):
├── Build: 0 errors, 0 warnings ✅
├── Test Pass Rate: >95% (1,100+ passing)
├── Code Coverage: >80%
├── ITimeProvider: <200 violations
├── Result Pattern: <400 violations  
├── Async Naming: <100 violations
├── Large Classes: <5 violations
└── Debt Score: 7.8 → 9.0+/10
```

---

## 🛠️ Tools & Scripts

### Automated Analysis

```bash
# Count remaining DateTime violations
Get-ChildItem -Path "src" -Recurse -Filter "*.cs" | 
    Select-String -Pattern "DateTime\.(Now|UtcNow)" | 
    Group-Object Filename | 
    Sort-Object Count -Descending

# Count Result<T> violations
Get-ChildItem -Path "src" -Recurse -Filter "*.cs" | 
    Select-String -Pattern "public.*Task<[^R].*>\s+\w+Async\s*\(" | 
    Measure-Object

# Find large classes
Get-ChildItem -Path "src" -Recurse -Filter "*.cs" | 
    Where-Object { $_.FullName -notmatch "(obj|bin|Migration)" } |
    ForEach-Object { 
        $lines = (Get-Content $_.FullName | Measure-Object).Count
        if ($lines -gt 1000) { [PSCustomObject]@{ File = $_.Name; Lines = $lines } }
    } | Sort-Object Lines -Descending
```

---

## 📝 Notes

### Technical Constraints
- Maintain backward compatibility for public APIs
- All changes must include tests
- No changes to working Core tests (312 passing)
- Code review required for architectural changes

### Risk Mitigation
- **Rollback plan:** All changes in feature branches
- **CI/CD:** Validate each phase before proceeding
- **Monitoring:** Track test metrics daily

### Resources Required
- 1 Senior Developer (architecture decisions)
- 1-2 Developers (implementation)
- QA Support (E2E test validation)

---

*Plan created: February 18, 2026*  
*Last updated: February 18, 2026*  
*Next review: Weekly during execution*
