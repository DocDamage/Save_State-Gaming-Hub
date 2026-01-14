# Remaining Technical Debt - Prioritized Remediation Plan

## Overview

This document outlines the remaining technical debt after Phase 1 critical fixes. Items are prioritized by impact, severity, and effort.

---

## Phase 2: HIGH PRIORITY (Week 2-3) 🟠

### 1. TODO Comments in Production Code (15 instances)

**Severity**: 🟡 MEDIUM  
**Effort**: 20 hours  
**Impact**: Incomplete features, data loss risks

#### Critical TODOs

| File | Line | Issue | Priority |
|------|------|-------|----------|
| `NetworkQualityMonitor.cs` | 26 | Data loss on restart - no persistence | 🔴 HIGH |
| `RecommendationsViewModel.cs` | 50 | Hardcoded user ID "user123" | 🔴 HIGH |
| `MacroRecorderViewModel.cs` | 119 | Guid.Empty usage for missing IDs | 🔴 HIGH |
| `Program.cs` | 179 | Disabled feature flag | 🟡 MEDIUM |
| `GameRecommendationService.cs` | 13 | Feature incomplete | 🟡 MEDIUM |

#### Critical: NetworkQualityMonitor Persistence

**File**: `src/SaveState.Infrastructure/Sync/NetworkQualityMonitor.cs:26`

**Current Issue**:
```csharp
// TODO: Move to database for persistence
private readonly List<NetworkQualitySnapshot> _history = new();
```

**Problem**: All historical network quality data is lost on application restart. This is critical for:
- Cloud gaming QoS tracking
- Network performance analysis
- User experience diagnostics

**Solution**:
1. Create `NetworkQualityHistory` entity
2. Add EF Core mapping in `DbContext`
3. Implement repository with cleanup policy (e.g., keep last 30 days)
4. Add migration: `dotnet ef migrations add AddNetworkQualityHistory`

**Estimated Effort**: 6 hours

#### Critical: Hardcoded User IDs

**File**: `src/SaveState.Presentation/ViewModels/Recommendations/RecommendationsViewModel.cs:50`

**Current Issue**:
```csharp
// TODO: Get from CurrentUser service, not hardcoded
private const string _userId = "user123";
```

**Problem**: 
- Multi-user scenarios fail
- Testing is inaccurate
- User isolation not enforced

**Solution**:
1. Inject `ICurrentUserService`
2. Get user ID from `_currentUser.GetId()`
3. Update all data loads to use dynamic user ID

**Estimated Effort**: 3 hours

#### Critical: Guid.Empty Usage

**File**: `src/SaveState.Presentation/ViewModels/Shell/Mugen/MacroRecorderViewModel.cs:119`

**Current Issue**:
```csharp
// TODO: Replace Guid.Empty with proper macro ID from selection
var macroId = Guid.Empty;
```

**Problem**:
- Undefined behavior when macro ID is needed
- Silent failures in macro recording
- Database constraints violated

**Solution**:
1. Add macro selection UI step
2. Validate macro ID before recording
3. Add error handling for missing macro

**Estimated Effort**: 4 hours

---

### 2. Test Coverage for New Services (0% coverage)

**Severity**: 🟠 HIGH  
**Effort**: 40 hours  
**Impact**: Untested features, regression risk

#### Phase 7 Services Needing Tests (0% coverage)

```
✗ AzureSpeechService.cs
✗ GoogleCloudService.cs
✗ OpenAiService.cs
✗ TensorFlowMLService.cs
✗ MultiplayerService.cs
✗ StreamingService.cs
✗ TournamentService.cs
```

#### Phase 6 Services Needing Additional Tests (~20% coverage)

```
◐ CompletionPredictionService (partial)
✗ VoiceCommandService (none)
✗ AccessibilityService (none)
✗ AudioOptimizer (none)
```

#### Test Implementation Plan

**Unit Tests** (20 hours):
- Create test doubles for cloud services
- Test each service method independently
- Verify error handling paths

**Integration Tests** (15 hours):
- Test with real cloud services (using staging credentials)
- Test service composition
- Test fault tolerance and retries

**E2E Tests** (5 hours):
- Complete user workflows
- Multi-service interactions
- Real data scenarios

**Estimated Effort**: 40 hours

---

### 3. Exception Handling Patterns (6 instances)

**Severity**: 🟠 HIGH  
**Effort**: 16 hours  
**Impact**: Inconsistent error recovery

#### Issue: Empty Catch Blocks with Logging

**Files**:
- `src/SaveState.Infrastructure/Performance/PerformanceMonitor.cs` (fixed ✅)
- `src/SaveState.Application/RomManagement/Services/LiveSyncService.cs:307-318`
- `src/SaveState.Presentation/Services/Performance/VirtualizedCollection.cs:95-115`

#### Example: LiveSyncService

**Current**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error handling FileSystemWatcher event");
    // ❌ Then what? Just swallow and continue?
}
```

**Recommended Pattern**:
```csharp
private async Task<Result> SafeHandleAsync(Func<Task> action, string operationName)
{
    try
    {
        await action().ConfigureAwait(false);
        return Result.Success();
    }
    catch (OperationCanceledException)
    {
        return Result.Failure($"{operationName} was cancelled", ErrorType.Cancelled);
    }
    catch (IOException ioEx)
    {
        _logger.LogWarning(ioEx, "IO error in {Operation}", operationName);
        return Result.Failure($"File system error: {ioEx.Message}", ErrorType.External);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in {Operation}", operationName);
        _telemetry.TrackException(ex);
        return Result.Failure($"{operationName} failed: {ex.Message}", ErrorType.Internal);
    }
}
```

**Benefits**:
- Different handling for different exception types
- Caller can decide on retry/escalation
- Telemetry tracking for monitoring
- Clear error categorization

**Estimated Effort**: 8 hours

---

## Phase 3: MEDIUM PRIORITY (Week 4-5) 🟡

### 1. Large God Classes (3 instances)

**Severity**: 🟡 MEDIUM  
**Effort**: 32 hours  
**Impact**: Hard to test, maintain, and understand

#### EmergingTechnologiesService (987 lines!)

**File**: `src/SaveState.Application/Mugen/Services/EmergingTechnologiesService.cs`

**Current Responsibilities**:
- Quantum computing simulation
- Voice assistance
- Augmented reality
- Virtual reality
- Blockchain integration
- Emerging AI

**Refactoring Plan**:
```csharp
// AFTER refactoring
Services/
├── Quantum/QuantumComputingService.cs (250 lines)
├── Voice/VoiceAssistantService.cs (150 lines)
├── Augmented/AugmentedRealityService.cs (180 lines)
├── Virtual/VirtualRealityService.cs (180 lines)
├── Blockchain/BlockchainService.cs (120 lines)
└── Ai/EmergingAIService.cs (120 lines)
```

**Estimated Effort**: 16 hours

#### DreamLogicArenaService (493 lines)

**Responsibilities**: AI + Arena + Character management

**Suggested split**:
- `DreamLogicArenaService` (250 lines) - Arena logic
- `CharacterBalanceAnalyzer` (150 lines) - Balance analysis
- `MatchmakingEngine` (93 lines) - Matchmaking

**Estimated Effort**: 12 hours

#### MugenExportService (476 lines)

**Responsibilities**: Export + Validation + File operations

**Suggested split**:
- `MugenExportService` (200 lines) - Orchestration
- `MugenExportValidator` (150 lines) - Validation
- `MugenFileExporter` (126 lines) - File operations

**Estimated Effort**: 8 hours

---

### 2. Magic Numbers & Hardcoded Configuration (8 instances)

**Severity**: 🟡 MEDIUM  
**Effort**: 12 hours  
**Impact**: Maintainability, configurability

#### Example: DreamLogicArenaService

**Current**:
```csharp
if (score > 85) // ❌ What does 85 mean?
{
    // Excellent performance path
}
```

**After**:
```csharp
public class DreamLogicOptions
{
    public int ExcellentScoreThreshold { get; set; } = 85;
    public int GoodScoreThreshold { get; set; } = 70;
    public int PoorScoreThreshold { get; set; } = 50;
}

if (score > _options.ExcellentScoreThreshold)
{
    // Excellent performance path
}
```

**Files to Update**:
- `DreamLogicArenaService.cs` (3 magic numbers)
- `MugenExportService.cs` (3 magic numbers)
- `OpenMKSampleData.cs` (2 magic numbers)

**Estimated Effort**: 6-8 hours

---

### 3. Database Optimization

**Severity**: 🟡 MEDIUM  
**Effort**: 16 hours  
**Impact**: Query performance, scalability

#### Missing Indexes

**High-Priority Tables**:

```csharp
// Games table
builder.HasIndex(g => g.Name);
builder.HasIndex(g => g.Platform);
builder.HasIndex(g => new { g.Platform, g.ReleaseDate });

// SaveStates table
builder.HasIndex(s => s.GameId);
builder.HasIndex(s => s.CreatedAt);
builder.HasIndex(s => new { s.GameId, s.CreatedAt });

// PlayerData table
builder.HasIndex(p => p.CharacterId);
builder.HasIndex(p => p.Timestamp);

// Achievements table
builder.HasIndex(a => a.GameId);
builder.HasIndex(a => a.UnlockedAt);
```

**Effort**: 4 hours

#### Phase 7 Database Migration

**Missing Migration**: Phase 7 added entities but no migration created

```bash
dotnet ef migrations add Phase7Entities -p src/SaveState.Infrastructure
```

**New Entities to Map**:
- Streaming entities
- Tournament entities
- Multiplayer entities
- Social entities
- Community marketplace entities

**Effort**: 8 hours

#### N+1 Query Problems

**Risk Areas** (needs profiling):
- Game library loading with achievements
- Character loading with move sets
- Tournament bracket with participant details

**Solution**: Add `.Include()` statements and use EF Core query projections

**Effort**: 4 hours

---

### 4. Security Improvements

**Severity**: 🟡 MEDIUM  
**Effort**: 24 hours  
**Impact**: Secrets management, data protection

#### API Key Management

**Current Problem**: API keys stored in `appsettings.json`

**Files Affected**:
- `CloudGamingOptions.cs`
- `MugenNetworkOptions.cs`
- Various service classes

**Solution**: Implement Azure Key Vault integration

```csharp
public class SecureConfigurationService
{
    private readonly ISecretManager _secretManager; // Azure Key Vault
    private readonly MemoryCache _cache;
    
    public async Task<string> GetSecretAsync(string key)
    {
        if (_cache.TryGetValue(key, out string? value))
            return value;
            
        value = await _secretManager.GetSecretAsync(key);
        _cache.Set(key, value, TimeSpan.FromMinutes(15)); // Cache with expiration
        return value;
    }
}
```

**Implementation**:
1. Add Azure Identity NuGet package
2. Create SecureConfigurationService
3. Update DI registration
4. Migrate secrets to Key Vault
5. Add rotation policies

**Effort**: 12 hours

#### JWT Token Validation

**Current Issue**: Token validation configuration not visible

**Missing**:
- Token lifetime validation
- Issuer/Audience validation
- Signature verification
- Refresh token handling

**Effort**: 8 hours

#### Input Sanitization

**Current**: `InputSanitizer` exists but not consistently used

**Solution**: Create validation attributes and automatic sanitization

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class SanitizeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext context)
    {
        if (value is string str)
        {
            var sanitized = InputSanitizer.Sanitize(str);
            if (sanitized != str)
                return new ValidationResult("Input contains invalid characters");
        }
        return ValidationResult.Success;
    }
}

// Usage
public class CreateMoveCommand
{
    [Required, MaxLength(100), Sanitize]
    public string Name { get; set; } = string.Empty;
}
```

**Effort**: 4 hours

---

## Phase 4: LOW PRIORITY (Week 6+) 🔵

### 1. Code Quality Improvements

**Severity**: 🔵 LOW  
**Effort**: 32 hours

#### Null Handling Standardization

**Current**: Mixed patterns (is null, == null, string.IsNullOrEmpty)

**Target**: C# 10+ patterns
- `is null` / `is not null`
- Null-coalescing: `value ?? defaultValue`
- Null-conditional: `obj?.Property`

**Effort**: 8 hours

#### XML Documentation

**Current Coverage**: ~65% of public APIs

**Target**: 90% coverage

**Tool**: Enable warnings and fix
```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsAsErrors>$(WarningsAsErrors);CS1591</WarningsAsErrors>
</PropertyGroup>
```

**Effort**: 16 hours

#### Duplicated Error Handling

**Current**: ~20 similar try-catch patterns

**Solution**: Create reusable extension methods

```csharp
public static class ResultExtensions
{
    public static async Task<Result<T>> ExecuteAsync<T>(
        this Task<T> task,
        ILogger logger,
        string operationName)
    {
        try
        {
            var result = await task;
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to {Operation}", operationName);
            return Result<T>.Failure(
                $"{operationName} failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }
}
```

**Effort**: 8 hours

---

### 2. Performance Optimization

**Severity**: 🔵 LOW  
**Effort**: 32 hours

#### QueryOptimizer Activation

**File**: `src/SaveState.Infrastructure/Performance/QueryOptimizer.cs`

**Issue**: Created but not registered or used

**Solution**:
1. Register in DI
2. Update repositories to use it
3. Add profiling to verify improvements

**Effort**: 8 hours

#### Memory Leak Prevention

**Risk Areas**:
- Event handler subscriptions in ViewModels
- FileSystemWatcher disposal
- Timer references

**Pattern to Enforce**:
```csharp
public class SomeViewModel : IDisposable
{
    private readonly IDisposable _subscription;
    
    public SomeViewModel(IEventAggregator events)
    {
        _subscription = events.Subscribe(OnEvent);
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

**Effort**: 12 hours

#### Async/Await Anti-patterns

**Issue**: Unnecessary Task.Run in hot paths

**Pattern to Fix**:
```csharp
// WRONG
await Task.Run(async () =>
{
    while (!ct.IsCancellationRequested)
    {
        // work
    }
});

// CORRECT
while (!ct.IsCancellationRequested)
{
    try
    {
        var task = await _queue.DequeueAsync(ct);
        await ExecuteTaskAsync(task);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}
```

**Effort**: 8 hours

---

### 3. CI/CD & Build Infrastructure

**Severity**: 🔵 LOW  
**Effort**: 24 hours

#### GitHub Actions Setup

**Create**: `.github/workflows/build.yml`

```yaml
name: Build & Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Test
        run: dotnet test --no-build -c Release
```

**Effort**: 4 hours

#### Code Quality Checks

**Setup SonarQube/SonarCloud**:
```xml
<PropertyGroup>
  <AnalysisMode>All</AnalysisMode>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**Effort**: 8 hours

#### Code Coverage Reporting

**Tool**: Coverlet + ReportGenerator

**Effort**: 6 hours

#### Automated Deployments

**Effort**: 6 hours

---

### 4. Documentation

**Severity**: 🔵 LOW  
**Effort**: 40 hours

#### Missing Documentation

- [ ] **Deployment Guide** - How to deploy to production
- [ ] **Operations Manual** - How to run and maintain
- [ ] **Troubleshooting Guide** - Common issues and solutions
- [ ] **Architecture Decision Records** - Why key decisions were made
- [ ] **API Reference** - Auto-generated from XML docs
- [ ] **Contributing Guide** - How to contribute to the project

#### Update Existing

- [ ] `README.md` - Add current status, architecture diagram
- [ ] `CHANGELOG.md` - Track all changes
- [ ] API documentation with Swagger/OpenAPI

**Effort**: 40 hours

---

## Summary Matrix

| Phase | Category | Severity | Count | Effort (hrs) | Impact |
|-------|----------|----------|-------|-------------|--------|
| 1 | 🔴 CRITICAL | Critical | 5 | ✅ DONE | Build works |
| 2 | 🟠 HIGH | High | 3 | 76 | Core features complete |
| 3 | 🟡 MEDIUM | Medium | 4 | 84 | Production-ready |
| 4 | 🔵 LOW | Low | 4 | 128 | World-class quality |
| **TOTAL** | | | **16** | **288** | **Fully Remediated** |

---

## Execution Timeline

**Phase 1**: ✅ Complete (Critical fixes)  
**Phase 2**: 🟠 Next 2 weeks (High priority)  
**Phase 3**: 🟡 Following 2 weeks (Medium priority)  
**Phase 4**: 🔵 Following 4 weeks (Low priority - nice to have)

**Total Timeline**: 8 weeks to full remediation

---

*Technical Debt Analysis Complete*  
*All issues documented with solutions and effort estimates*
