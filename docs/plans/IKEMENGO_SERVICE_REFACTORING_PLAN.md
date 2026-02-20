# IKEMEN GO Service Refactoring Plan

**Document Version:** 1.0  
**Created:** February 20, 2026  
**Status:** Design Complete, Implementation Pending  
**Estimated Effort:** 4-6 hours  
**Priority:** Medium (Technical Debt)

---

## 📋 Executive Summary

The `IkemenGoService` has grown to **1,486 lines** and violates the Single Responsibility Principle. This plan outlines the extraction of 8 specialized manager classes to reduce complexity and improve maintainability.

### Current State
- **File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/IkemenGoService.cs`
- **Lines:** 1,486
- **Methods:** 40+ public methods
- **Responsibilities:** 8 distinct areas

### Target State
- **Coordinator Service:** ~150 lines (delegates to managers)
- **8 Manager Classes:** Each <350 lines
- **Single Responsibility:** Each manager handles one domain

---

## 🏗️ Architecture Overview

```
IkemenGoService (Coordinator)
├── IkemenGoInstallationManager
├── IkemenGoMigrationManager
├── IkemenGoConfigurationManager
├── IkemenGoNetworkManager
├── IkemenGoModuleManager
├── IkemenGoLaunchManager
├── IkemenGoReplayManager
└── IkemenGoAnalyticsManager
```

---

## 📦 Manager Specifications

### 1. IkemenGoInstallationManager
**Responsibility:** Engine detection, installation validation, version checking

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoInstallationManager.cs`

**Methods:**
```csharp
Task<Result<IkemenGoDetectionResult>> DetectInstallationAsync(CancellationToken ct)
List<string> GetDefaultSearchPaths()
IReadOnlyList<string> GetContentPaths(string installationPath)
Task<string?> DetectVersionAsync(string installationPath, CancellationToken ct)
```

**Estimated Lines:** ~180

**Dependencies:** ILogger, ITimeProvider, File System

---

### 2. IkemenGoMigrationManager
**Responsibility:** MUGEN to IKEMEN GO content migration

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoMigrationManager.cs`

**Methods:**
```csharp
Task<Result<CharacterMigrationResult>> MigrateCharacterAsync(string source, string dest, MigrationOptions options, CancellationToken ct)
Task<Result<StageMigrationResult>> MigrateStageAsync(string source, string dest, MigrationOptions options, CancellationToken ct)
Task<Result<BatchMigrationResult>> MigrateFullRosterAsync(string source, string dest, MigrationOptions options, IProgress<MigrationProgress>?, CancellationToken ct)
Task<Result<ScreenpackConversionResult>> ConvertScreenpackAsync(string source, string output, CancellationToken ct)
```

**Estimated Lines:** ~350

**Dependencies:** ILogger, ITimeProvider, File System, Regex

---

### 3. IkemenGoConfigurationManager
**Responsibility:** Config.json load/save/validation

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoConfigurationManager.cs`

**Methods:**
```csharp
Task<Result<IkemenGoConfig>> LoadConfigAsync(string configPath, CancellationToken ct)
Task<Result> SaveConfigAsync(string configPath, IkemenGoConfig config, CancellationToken ct)
Task<Result<ConfigUpdateResult>> UpdateConfigOptionsAsync(string configPath, Dictionary<string, object> options, CancellationToken ct)
Task<Result<IkemenGoConfigValidation>> ValidateConfigAsync(IkemenGoConfig config, CancellationToken ct)
IkemenGoConfig CreateDefaultConfig()
```

**Estimated Lines:** ~220

**Dependencies:** ILogger, ITimeProvider, System.Text.Json

---

### 4. IkemenGoNetworkManager
**Responsibility:** Online play, lobby, rollback netcode

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoNetworkManager.cs`

**Methods:**
```csharp
Task<Result> ConfigureOnlinePlayAsync(string configPath, OnlinePlaySettings settings, CancellationToken ct)
Task<Result<NetworkTestResult>> TestNetworkConnectionAsync(string host, int port, CancellationToken ct)
Task<Result<IReadOnlyList<IkemenGoServer>>> GetLobbyServersAsync(CancellationToken ct)
Task<Result> ConfigureRollbackNetcodeAsync(string configPath, RollbackSettings settings, CancellationToken ct)
Task<Result<PortValidationResult>> ValidatePortForwardingAsync(int port, CancellationToken ct)
```

**Estimated Lines:** ~280

**Dependencies:** ILogger, ITimeProvider, System.Net, HttpClient

---

### 5. IkemenGoModuleManager
**Responsibility:** Lua module lifecycle management

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoModuleManager.cs`

**Methods:**
```csharp
Task<Result<IReadOnlyList<IkemenGoModule>>> GetInstalledModulesAsync(string modulesPath, CancellationToken ct)
Task<Result<ModuleInstallResult>> InstallModuleAsync(string modulesPath, string source, ModuleInstallOptions options, CancellationToken ct)
Task<Result> UninstallModuleAsync(string modulesPath, string moduleId, CancellationToken ct)
Task<Result<ModuleValidationResult>> ValidateModuleAsync(string modulePath, CancellationToken ct)
Task<Result> ToggleModuleAsync(string modulesPath, string moduleId, bool enabled, CancellationToken ct)
```

**Estimated Lines:** ~320

**Dependencies:** ILogger, ITimeProvider, System.Text.Json, HttpClient

---

### 6. IkemenGoLaunchManager
**Responsibility:** Process launch, monitoring, termination

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoLaunchManager.cs`

**Methods:**
```csharp
Task<Result<IkemenGoProcess>> LaunchAsync(string executablePath, LaunchOptions options, CancellationToken ct)
Task<Result<IkemenGoProcess>> LaunchTrainingModeAsync(string executablePath, TrainingModeOptions options, CancellationToken ct)
Task<Result<IkemenGoProcess>> LaunchOnlineVersusAsync(string executablePath, OnlineVersusOptions options, CancellationToken ct)
Task<Result<IkemenGoProcessStatus>> GetProcessStatusAsync(int processId, CancellationToken ct)
Task<Result> TerminateAsync(int processId, bool force, CancellationToken ct)
Task<Result<IReadOnlyList<IkemenGoProcess>>> GetRunningProcessesAsync(CancellationToken ct)
```

**Estimated Lines:** ~240

**Dependencies:** ILogger, ITimeProvider, System.Diagnostics.Process

---

### 7. IkemenGoReplayManager
**Responsibility:** Replay handling, export, conversion

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoReplayManager.cs`

**Methods:**
```csharp
Task<Result<IReadOnlyList<IkemenGoReplay>>> GetReplaysAsync(string replaysPath, CancellationToken ct)
Task<Result<ReplayExportResult>> ExportReplayToVideoAsync(string replayPath, string outputPath, VideoExportOptions options, CancellationToken ct)
Task<Result> ConvertMugenReplayAsync(string mugenReplayPath, string outputPath, CancellationToken ct)
Task<Result<IkemenGoReplayAnalysis>> AnalyzeReplayAsync(string replayPath, CancellationToken ct)
Task<Result> DeleteReplayAsync(string replayPath, CancellationToken ct)
```

**Estimated Lines:** ~280

**Dependencies:** ILogger, ITimeProvider, System.Text.Json

---

### 8. IkemenGoAnalyticsManager
**Responsibility:** Player stats, match history, library analysis

**File:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/IkemenGoAnalyticsManager.cs`

**Methods:**
```csharp
Task<Result<IkemenGoPlayerStats>> GetPlayerStatsAsync(string playerName, string dataPath, CancellationToken ct)
Task<Result<IReadOnlyList<IkemenGoMatchRecord>>> GetMatchHistoryAsync(string playerName, string dataPath, int limit, CancellationToken ct)
Task<Result<IkemenGoLibraryCompatibilityReport>> AnalyzeLibraryCompatibilityAsync(string charsPath, string stagesPath, CancellationToken ct)
Task<Result> RecordMatchAsync(string dataPath, IkemenGoMatchRecord match, CancellationToken ct)
Task<Result> UpdatePlayerStatsAsync(string dataPath, string playerName, MatchOutcome outcome, string characterUsed, CancellationToken ct)
```

**Estimated Lines:** ~320

**Dependencies:** ILogger, ITimeProvider, System.Text.Json

---

## 🚧 Implementation Challenges

### 1. Interface Signature Complexity
**Problem:** `IIkemenGoService` defines 40+ methods with specific parameter types.

**Impact:** Each manager must match the exact signature from the interface.

**Solution:** 
- Extract interface methods by region
- Create one manager at a time
- Use compiler errors to guide implementation

### 2. Result Type Dependencies
**Problem:** Many custom result/option types defined in `IIkemenGoService.cs`.

**Types Affected:**
- `MigrationOptions` vs `IkemenGoMigrationOptions`
- `RollbackSettings` vs `RollbackNetcodeSettings`
- `OnlinePlaySettings` vs `IkemenGoNetworkSettings`
- `ModuleInstallOptions` (not defined in interface)
- `MigrationProgress` (not defined in interface)

**Solution:**
- Verify type names in `IIkemenGoService.cs` before implementation
- Create missing types in Core project if needed
- Use `global using` aliases if names conflict

### 3. Service Registration
**Problem:** Managers need to be registered in DI container.

**Solution:**
```csharp
// In ServiceRegistration.cs or similar
services.AddScoped<IkemenGoInstallationManager>();
services.AddScoped<IkemenGoMigrationManager>();
// ... etc
```

---

## 📋 Pre-Implementation Checklist

Before starting implementation, complete these tasks:

- [ ] **Verify Interface Types:** Check `IIkemenGoService.cs` for exact type names
- [ ] **Extract Shared Types:** Move common types to `SaveState.Core.Mugen.Models`
- [ ] **Create Manager Directory:** `src/SaveState.Infrastructure/Mugen/IkemenGo/Managers/`
- [ ] **Update DI Registration:** Add managers to service collection
- [ ] **Create Interface Facade:** Consider `IIkemenGoInstallationService` etc. for testability

---

## 📝 Step-by-Step Implementation Plan

### Phase 1: Foundation (1 hour)
1. **Create Manager Directory Structure**
   ```
   src/SaveState.Infrastructure/Mugen/IkemenGo/
   ├── Managers/
   │   ├── IkemenGoInstallationManager.cs
   │   ├── IkemenGoConfigurationManager.cs
   │   └── (others)
   └── (existing files)
   ```

2. **Verify Type Definitions**
   - Read `IIkemenGoService.cs` completely
   - List all parameter types needed
   - Create missing types in Core project

3. **Update DI Registration**
   - Add all 8 managers to service collection
   - Ensure proper lifetime scope (Scoped)

### Phase 2: Manager Implementation (3-4 hours)

Implement managers in this order (simplest to most complex):

1. **IkemenGoInstallationManager** (30 min)
   - Extract detection logic
   - Test: Detection works on your system

2. **IkemenGoConfigurationManager** (30 min)
   - Extract config load/save
   - Test: Config round-trip works

3. **IkemenGoLaunchManager** (30 min)
   - Extract process management
   - Test: Launch/terminate works

4. **IkemenGoNetworkManager** (45 min)
   - Extract online features
   - Test: Lobby servers retrieved

5. **IkemenGoReplayManager** (45 min)
   - Extract replay handling
   - Test: Replay list loads

6. **IkemenGoModuleManager** (45 min)
   - Extract module lifecycle
   - Test: Module list loads

7. **IkemenGoAnalyticsManager** (45 min)
   - Extract stats/history
   - Test: Stats load

8. **IkemenGoMigrationManager** (60 min)
   - Extract migration logic
   - Test: Character migration works

### Phase 3: Service Refactoring (1 hour)

1. **Update IkemenGoService**
   - Add manager fields
   - Update constructor injection
   - Convert methods to delegates

2. **Before/After Comparison**
   - Original: 1,486 lines
   - Target: ~150 lines (coordinator only)

### Phase 4: Testing (1 hour)

1. **Build Verification**
   ```bash
   dotnet build src/SaveState.Infrastructure
   ```

2. **Unit Tests**
   ```bash
   dotnet test tests/SaveState.Infrastructure.Tests --filter "FullyQualifiedName~IkemenGo"
   ```

3. **Integration Tests**
   - Test detection on clean system
   - Test migration with sample content
   - Test launch/monitor/terminate cycle

---

## 🧪 Testing Strategy

### Unit Tests (New)
Create test file: `tests/SaveState.Infrastructure.Tests/Mugen/IkemenGo/Managers/`

```csharp
public class IkemenGoInstallationManagerTests
{
    [Fact]
    public async Task DetectInstallationAsync_WhenInstalled_ReturnsPath()
    {
        // Arrange
        var manager = CreateManager();
        
        // Act
        var result = await manager.DetectInstallationAsync();
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Integration Tests (Existing)
Update existing tests in:
- `tests/SaveState.Infrastructure.Tests/Mugen/IkemenGoServiceTests.cs`

Ensure they still pass with manager delegation.

---

## 📊 Success Metrics

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Lines in Service | 1,486 | ~150 | <200 |
| Max Lines per Manager | - | ~350 | <400 |
| Test Coverage | ? | ? | Maintain |
| Build Time | - | - | Faster |

---

## 🎯 Benefits

1. **Single Responsibility:** Each manager has one reason to change
2. **Testability:** Managers can be unit tested independently
3. **Maintainability:** Smaller files are easier to understand
4. **Reusability:** Managers can be composed in new ways
5. **Collaboration:** Multiple devs can work on different managers

---

## ⚠️ Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Type name mismatches | High | Medium | Verify interface first |
| DI registration errors | Medium | High | Test each manager individually |
| Breaking changes | Low | High | Maintain interface compatibility |
| Test failures | Medium | Medium | Run full test suite after each manager |

---

## 🔄 Rollback Plan

If issues arise:

1. **Git Revert:** `git revert HEAD` to restore original service
2. **Partial Implementation:** Comment out manager delegation, keep original logic
3. **Feature Flags:** Use `#if MANAGER_REFACTORING` to toggle between implementations

---

## 📚 References

- **Original Service:** `src/SaveState.Infrastructure/Mugen/IkemenGo/IkemenGoService.cs`
- **Interface Definition:** `src/SaveState.Core/Mugen/Services/IIkemenGoService.cs`
- **Related Services:** `IkemenGoServiceFacade.cs` for high-level operations

---

## ✅ Sign-off

**Created by:** Kimi CLI  
**Date:** February 20, 2026  
**Status:** Ready for Implementation  
**Next Review:** When Phase 1 complete

---

*This plan is a living document. Update as implementation progresses.*
