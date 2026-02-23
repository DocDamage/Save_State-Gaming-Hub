# Large Files Refactoring Plan

**Date:** February 22, 2026  
**Target:** Files >800 lines in `src/` (excluding migrations)  
**Goal:** Reduce complexity using Manager Pattern & Facade + Internal Classes  

---

## Executive Summary

| Priority | File | Lines | Pattern | Est. Effort |
|----------|------|-------|---------|-------------|
| **P1** | RomValidationService.cs | 1,032 | Manager Pattern | 4-6 hrs |
| **P1** | IStoryModeService.cs | 1,027 | Interface Segregation | 4-6 hrs |
| **P1** | IPerformanceProfilerService.cs | 1,009 | Interface Segregation | 4-6 hrs |
| **P2** | SaveStateCloudService.cs | 1,006 | Manager Pattern | 4-6 hrs |
| **P2** | InputRecordingService.cs | 983 | Manager Pattern | 3-4 hrs |
| **P2** | MacOSMemoryReader.cs | 979 | Platform Abstraction | 3-4 hrs |
| **P3** | CertificationSystem.cs | 976 | Manager Pattern | 3-4 hrs |
| **P3** | SignatureTestRunner.cs | 971 | Test Infrastructure | 2-3 hrs |
| **P3** | MemoryPatternDatabase.cs | 960 | Data Access Pattern | 3-4 hrs |
| **P3** | SignatureVerificationService.cs | 950 | Pipeline Pattern | 3-4 hrs |

**Total Estimated Effort:** 32-44 hours  
**Target Architecture Test Limit:** 800 lines (currently allows 1 file at 1000+)

---

## Progress Update (February 23, 2026)

- [x] **P1 - IPerformanceProfilerService.cs** interface segregation completed
- [x] Split monolithic contract into focused interfaces (`IProfilingSessionService`, `IPerformanceMetricsService`, `ICharacterProfilingService`, `IBattleProfilingService`, `IBottleneckAnalysisService`, `IOptimizationService`, `IBenchmarkService`, `IPerformanceReportingService`, `IPerformanceAlertService`)
- [x] Moved profiling DTOs/enums to `src/SaveState.Core/Mugen/Services/PerformanceProfilerModels.cs`
- [x] Reduced `IPerformanceProfilerService.cs` from ~878 lines to **175 lines** (current)
- [x] **P1 - IStoryModeService.cs** interface/model split completed
- [x] Extracted story contracts from monolithic file into focused contracts file + `StoryModeModels.cs`
- [x] Reduced `IStoryModeService.cs` from **1,027 lines** to **426 lines**
- [x] Updated Story Mode service registration to expose all focused interfaces through `StoryModeService`
- [x] **P1 - RomValidationService.cs** extracted helper responsibilities into `src/SaveState.Infrastructure/RomManagement/Validation/RomValidationManagers.cs`
- [x] **P1 - RomValidationService.cs** orchestration split completed using manager pattern
- [x] `RomValidationService` now acts as coordinator delegating to:
  `RomHashWorkflowManager`, `RomDatMatchingManager`, `RomValidationOrchestrationManager`, `RomValidationInsightsManager`
- [x] Reduced `RomValidationService.cs` from ~918 lines to **136 lines** (current)
- [x] Validation: `dotnet build src/SaveState.Core/SaveState.Core.csproj` and `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj` both pass
- [x] Targeted regression tests: `RomValidationServiceTests` passing (11/11) and ROM validation integration tests passing (10/10)
- [x] Cross-plan dependency update: Avalonia XAML remediation completed; Presentation build and targeted UI smoke checks are passing.
- [x] **P2 - SaveStateCloudService.cs** manager extraction completed
- [x] Extracted provider/payload/path responsibilities into `src/SaveState.Infrastructure/SaveStates/SaveStateCloudManagers.cs`
- [x] `SaveStateCloudService.cs` reduced from **892 lines** to **701 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed
- [x] Targeted regression tests: `SaveStateCloudServiceTests` passing (8/8)
- [x] **P2 - MacOSMemoryReader.cs** structural split completed
- [x] Extracted Mach interop/constants/error translation into `src/SaveState.Infrastructure/GameLibrary/Services/MacOSMemoryReader.Interop.cs`
- [x] `MacOSMemoryReader.cs` reduced from **881 lines** to **771 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed
- [x] **P3 - CertificationSystem.cs** manager/contract/model split completed
- [x] Extracted interface to `src/SaveState.Application/Mugen/Services/CertificationSystem.Contracts.cs`
- [x] Extracted models/enums to `src/SaveState.Application/Mugen/Services/CertificationSystem.Models.cs`
- [x] Extracted helper classes to `src/SaveState.Application/Mugen/Services/CertificationSystem.Managers.cs`
- [x] `CertificationSystem.cs` reduced from **976 lines** to **615 lines**
- [x] Validation: `dotnet build src/SaveState.Application/SaveState.Application.csproj -c Release` passed
- [x] Targeted test run: `dotnet test tests/SaveState.Application.Tests/SaveState.Application.Tests.csproj -c Release --filter "FullyQualifiedName~Mugen"` completed (no matching tests found)
- [x] **P3 - SignatureTestRunner.cs** model/type extraction completed
- [x] Extracted test runner DTOs/enums into `src/SaveState.Infrastructure/GameLibrary/Testing/SignatureTestRunner.Models.cs`
- [x] `SignatureTestRunner.cs` reduced from **971 lines** to **475 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors)
- [x] **P3 - MemoryPatternDatabase.cs** seed-data split completed
- [x] Extracted signature seed catalog into `src/SaveState.Infrastructure/GameLibrary/Services/MemoryPatternDatabase.SeedData.cs`
- [x] `MemoryPatternDatabase.cs` reduced from **960 lines** to **471 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors)
- [x] Targeted regression tests: `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~MemoryPatternDatabase"` passed (5/5)
- [x] **P3 - SignatureVerificationService.cs** private-method pipeline split completed
- [x] Extracted verification pipeline/private helper methods into `src/SaveState.Infrastructure/GameLibrary/Services/SignatureVerificationService.VerificationMethods.cs`
- [x] `SignatureVerificationService.cs` reduced from **950 lines** to **318 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors)
- [x] Regression safety run: `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~Memory"` passed (20/20)
- [x] **P2 - InputRecordingService.cs** maintenance/storage split completed
- [x] Extracted maintenance, validation, and storage/export helpers into `src/SaveState.Infrastructure/InputRecording/InputRecordingService.MaintenanceAndStorage.cs`
- [x] `InputRecordingService.cs` reduced from **983 lines** to **702 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors)
- [x] Targeted regression tests: `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~InputRecordingServiceFacadeTests"` passed (2/2)
- [x] **P3 - MugenEsportsService.cs** manager/contract/model split completed
- [x] Extracted contracts to `src/SaveState.Application/Mugen/Services/MugenEsportsService.Contracts.cs`
- [x] Extracted models/enums to `src/SaveState.Application/Mugen/Services/MugenEsportsService.Models.cs`
- [x] Extracted ranking/sponsorship helper classes to `src/SaveState.Application/Mugen/Services/MugenEsportsService.Managers.cs`
- [x] `MugenEsportsService.cs` reduced from **943 lines** to **376 lines**
- [x] Validation: `dotnet build src/SaveState.Application/SaveState.Application.csproj -c Release` passed
- [x] **Continuation - NetworkQualityMonitor.cs** measurement/diagnostics split completed
- [x] Extracted measurement/diagnostics/helper methods to `src/SaveState.Infrastructure/Sync/NetworkQualityMonitor.Measurements.cs`
- [x] `NetworkQualityMonitor.cs` reduced from **815 lines** to **386 lines**
- [x] Validation: `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors)
- [x] **Continuation - MugenPrizePoolService.cs** manager/contract/model split completed
- [x] Extracted contracts to `src/SaveState.Application/Mugen/Services/MugenPrizePoolService.Contracts.cs`
- [x] Extracted models/enums to `src/SaveState.Application/Mugen/Services/MugenPrizePoolService.Models.cs`
- [x] Extracted payment/distribution helper classes to `src/SaveState.Application/Mugen/Services/MugenPrizePoolService.Managers.cs`
- [x] `MugenPrizePoolService.cs` reduced from **809 lines** to **381 lines**
- [x] Validation: `dotnet build src/SaveState.Application/SaveState.Application.csproj -c Release` passed
- [x] **Continuation - QuantumSuperpositionService.cs** manager/contract/model split completed
- [x] Extracted contracts to `src/SaveState.Application/Mugen/Services/QuantumSuperpositionService.Contracts.cs`
- [x] Extracted models/enums to `src/SaveState.Application/Mugen/Services/QuantumSuperpositionService.Models.cs`
- [x] Extracted quantum engine helper classes to `src/SaveState.Application/Mugen/Services/QuantumSuperpositionService.Managers.cs`
- [x] `QuantumSuperpositionService.cs` reduced from **801 lines** to **349 lines**
- [x] Validation: `dotnet build src/SaveState.Application/SaveState.Application.csproj -c Release` passed
- [x] `>800` remediation target complete: no non-migration source files remain above 800 lines
- [x] Oversized file count reduced in this session from **11** to **0** (`>800` lines, excluding migrations)

---

## Detailed Refactoring Plans

### P1: RomValidationService.cs (PARTIALLY COMPLETED - February 22, 2026)

**Location:** `src/SaveState.Infrastructure/RomManagement/RomValidationService.cs`

**Current Responsibilities:**
- ROM file hash calculation (CRC32, MD5, SHA1)
- File format validation
- ROM matching against databases
- Report generation
- Background validation coordination
- Validation history tracking

**Proposed Structure - Manager Pattern:**

```
RomValidationService (Coordinator, ~200 lines)
├── HashCalculationManager (~200 lines)
│   ├── Crc32Calculator
│   ├── Md5Calculator  
│   └── Sha1Calculator
├── RomMatcherManager (~200 lines)
│   ├── DatabaseMatcher
│   ├── HashMatcher
│   └── MetadataMatcher
├── ValidationReportManager (~200 lines)
│   ├── ReportGenerator
│   ├── ReportFormatter
│   └── ReportExporter
└── BackgroundValidationManager (~150 lines)
    ├── QueueProcessor
    ├── ProgressTracker
    └── CancellationHandler
```

**Refactoring Steps:**
1. Extract `HashCalculationManager` with individual calculator classes
2. Extract `RomMatcherManager` with matching strategies
3. Extract `ValidationReportManager` for report operations
4. Extract `BackgroundValidationManager` for async operations
5. Update `IRomValidationService` interface if needed
6. Update all usages in tests and background services

**Files to Create:**
- `RomManagement/Validation/HashCalculationManager.cs`
- `RomManagement/Validation/RomMatcherManager.cs`
- `RomManagement/Validation/ValidationReportManager.cs`
- `RomManagement/Validation/BackgroundValidationManager.cs`
- `RomManagement/Validation/Calculators/*.cs`

**Current Progress:**
- Extracted hash/matching helpers, DAT parsing helpers, export helpers, and header analysis into `RomManagement/Validation/RomValidationManagers.cs`
- Split orchestration into dedicated managers:
  `RomHashWorkflowManager`, `RomDatMatchingManager`, `RomValidationOrchestrationManager`, `RomValidationInsightsManager`
- `RomValidationService.cs` is now a thin coordinator (136 lines)
- Remaining work: optional renaming/alignment of manager filenames to the original target naming scheme (`HashCalculationManager`, `ValidationReportManager`, etc.)

---

### P1: IStoryModeService.cs (COMPLETED - February 22, 2026)

**Location:** `src/SaveState.Core/Mugen/Services/IStoryModeService.cs`

**Current State (Before):**
- Interface + model definitions in one file
- 1,027 lines total with 9 focused interfaces and 50+ story models/enums
- Mix of contracts and DTO/value types in the same compilation unit

**Proposed Structure - Interface Segregation (already started):**

```
IStoryModeService (Marker interface)
├── IStoryProjectService (Project lifecycle)
├── IStoryChapterService (Chapter management)
├── IStorySceneService (Scene editing)
├── IStoryDialogueService (Dialogue system)
├── IStoryBranchingService (Choices & branches)
├── IStoryBattleIntegrationService (Battle integration)
├── IStoryCutsceneService (Cutscene editing)
├── IStoryTestingService (Testing & preview)
└── IStoryAssetService (Asset management)
```

**Completed Changes:**
1. Kept `IStoryModeService` and all focused interfaces in `IStoryModeService.cs`
2. Moved all story request/response models and enums to `StoryModeModels.cs`
3. Updated `StoryModeService` to implement all focused interfaces explicitly
4. Updated DI to register all focused interfaces via the same scoped `StoryModeService` instance
5. Fixed pre-existing signature mismatch for asset optimization (`StoryAssetOptimizationOptions`)

**Validation Notes:**
- `dotnet build src/SaveState.Core/SaveState.Core.csproj` ✅
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj` ✅
- `IStoryModeService.cs` reduced to **426 lines** (from 1,027)

---

### P1: IPerformanceProfilerService.cs (COMPLETED - February 22, 2026)

**Location:** `src/SaveState.Core/Mugen/Services/IPerformanceProfilerService.cs`

**Current Responsibilities:**
- CPU profiling
- GPU profiling
- Memory profiling
- Frame time analysis
- Bottleneck detection
- Optimization recommendations

**Proposed Structure - Interface Segregation:**

```
IPerformanceProfilerService (Marker/Coordinator)
├── ICpuProfiler
├── IGpuProfiler
├── IMemoryProfiler
├── IFrameTimeAnalyzer
├── IBottleneckDetector
└── IOptimizationRecommender
```

**Completed Changes:**
1. Extracted focused service interfaces by responsibility area
2. Kept `IPerformanceProfilerService` as composite marker interface for backward compatibility
3. Moved all profiling request/response models into `PerformanceProfilerModels.cs`
4. Verified existing implementation (`PerformanceProfilerService`) remains compatible without DI changes

**Validation Notes:**
- `dotnet build src/SaveState.Core/SaveState.Core.csproj` ✅
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj` ✅
- `dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj -v minimal` âœ…
- Targeted interactive smoke tests for touched views are passing (`WorkflowEditor`, `Recommendations`, `Playlist`, `HealthMonitor`)

---

### P2: SaveStateCloudService.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Infrastructure/SaveStates/SaveStateCloudService.cs`

**Current Responsibilities:**
- Cloud save upload/download
- Sync conflict resolution
- Multi-provider support (OneDrive, Google Drive, etc.)
- Encryption/decryption
- Sync scheduling
- Quota management

**Completed Structure - Manager Pattern:**

```text
SaveStateCloudService (Coordinator, 701 lines)
- SaveStateCloudProviderResolver
- SaveStateCloudPayloadManager
- SaveStateCloudHelpers (static path/name/cleanup utilities)
```

**Completed Changes:**
1. Extracted provider selection/authentication logic into `SaveStateCloudProviderResolver`.
2. Extracted payload encryption/decryption and upload/download preparation into `SaveStateCloudPayloadManager`.
3. Extracted path and cleanup helpers into `SaveStateCloudHelpers`.
4. Preserved public behavior while reducing the service file below the >800-line threshold.

**Validation Notes:**
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed.
- `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~SaveStateCloudServiceTests"` passed (8/8).

---

### P2: InputRecordingService.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Infrastructure/InputRecording/InputRecordingService.cs`

**Current Responsibilities:**
- Input capture
- Recording storage
- Playback
- Replay analysis
- Macro generation

**Completed Structure - Partial-Class Maintenance Split:**

```text
InputRecordingService.cs (core recording/playback/query flows, 702 lines)
InputRecordingService.MaintenanceAndStorage.cs (statistics/validation/repair + frame persistence/export/import helpers)
```

**Completed Changes:**
1. Converted `InputRecordingServiceOperations` to a partial class.
2. Moved maintenance/statistics/validation/repair responsibilities and storage/export/import helper methods into `InputRecordingService.MaintenanceAndStorage.cs`.
3. Preserved facade wiring and public service behavior while reducing the main file below the >800 threshold.

**Validation Notes:**
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors).
- `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~InputRecordingServiceFacadeTests"` passed (2/2).

---

### P2: MacOSMemoryReader.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/MacOSMemoryReader.cs`

**Current Responsibilities:**
- macOS-specific memory reading
- Mach kernel API calls
- Permission handling
- Error handling for SIP

**Completed Structure - Partial-Class Split:**

```text
MacOSMemoryReader.cs (771 lines - behavior/orchestration)
MacOSMemoryReader.Interop.cs (114 lines - Mach interop/constants/error map)
```

**Completed Changes:**
1. Converted `MacOSMemoryReader` to a partial class to separate interop concerns from runtime behavior.
2. Moved Mach constants, P/Invoke declarations, region struct, and Mach error translation into `MacOSMemoryReader.Interop.cs`.
3. Preserved existing method signatures and runtime behavior while reducing main file complexity below the >800 threshold.

**Validation Notes:**
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed.

---

### P3: CertificationSystem.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Application/Mugen/Services/CertificationSystem.cs`

**Current Responsibilities:**
- Character certification
- Stage certification
- Content validation
- Compliance checking
- Certificate generation

**Completed Structure - Manager Pattern + Type Split:**

```
CertificationSystem.cs (coordinator/orchestration, 615 lines)
CertificationSystem.Contracts.cs (service contract)
CertificationSystem.Models.cs (DTOs/enums)
CertificationSystem.Managers.cs (assessment/badge/progress helper classes)
```

**Completed Changes:**
1. Reduced the main coordinator file by moving contracts, models, and helper classes into dedicated files.
2. Preserved service behavior and signatures for all public certification APIs.
3. Kept manager/helper classes available without altering existing call paths.

**Validation Notes:**
- `dotnet build src/SaveState.Application/SaveState.Application.csproj -c Release` passed.
- `dotnet test tests/SaveState.Application.Tests/SaveState.Application.Tests.csproj -c Release --filter "FullyQualifiedName~Mugen"` completed (no matching tests in current suite).

---

### P3: SignatureTestRunner.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Testing/SignatureTestRunner.cs`

**Current Responsibilities:**
- Test execution
- Result collection
- Report generation
- Test case management
- Performance measurement

**Completed Structure - Type Extraction:**

```
SignatureTestRunner.cs (orchestration/export helpers, 475 lines)
SignatureTestRunner.Models.cs (test suite options/results/progress/report DTOs + enums)
```

**Completed Changes:**
1. Moved embedded test suite models/enums to `SignatureTestRunner.Models.cs`.
2. Kept runner public APIs and behavior unchanged while reducing file size below the >800 threshold.
3. Left verification/execution logic in the coordinator file for minimal risk.

**Validation Notes:**
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors).

---

### P3: MemoryPatternDatabase.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/MemoryPatternDatabase.cs`

**Current Responsibilities:**
- Pattern storage
- Pattern retrieval
- Game signature management
- Pattern validation
- Database operations

**Completed Structure - Seed Data Split:**

```
MemoryPatternDatabase.cs (runtime behavior/query/persistence, 471 lines)
MemoryPatternDatabase.SeedData.cs (built-in signature catalog initialization)
```

**Completed Changes:**
1. Converted `MemoryPatternDatabase` to a partial class and moved `InitializeKnownPatterns()` into `MemoryPatternDatabase.SeedData.cs`.
2. Preserved all public APIs and database behavior while reducing the main file below the >800 threshold.
3. Kept all seed signatures unchanged to avoid behavior drift.

**Validation Notes:**
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors).
- `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~MemoryPatternDatabase"` passed (5/5).

---

### P3: SignatureVerificationService.cs (COMPLETED - February 23, 2026)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/SignatureVerificationService.cs`

**Current Responsibilities:**
- Signature validation
- Multi-pass verification
- Health scoring
- Result reporting

**Completed Structure - Private Pipeline Split:**

```
SignatureVerificationService.cs (public orchestration APIs, 318 lines)
SignatureVerificationService.VerificationMethods.cs (private verification/validation pipeline helpers, 642 lines)
```

**Completed Changes:**
1. Converted `SignatureVerificationService` to a partial class.
2. Moved all private verification and validation helper methods (static/dynamic/stability checks, scan/read helpers, scoring/validation suggestions) into `SignatureVerificationService.VerificationMethods.cs`.
3. Preserved public API behavior and signatures while reducing the main service file below the >800 threshold.

**Validation Notes:**
- `dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj -c Release` passed (warnings only; no new errors).
- `dotnet test tests/SaveState.Infrastructure.Tests/SaveState.Infrastructure.Tests.csproj -c Release --filter "FullyQualifiedName~Memory"` passed (20/20).

---

## Implementation Strategy

### Phase 1: High-Priority (P1) - Week 1
1. **RomValidationService.cs** - Manager pattern extraction ✅ Completed (February 22, 2026)
2. **IPerformanceProfilerService.cs** - Interface segregation ✅ Completed (February 22, 2026)
3. **IStoryModeService.cs** - Interface/model split and focused interface registration âœ… Completed (February 22, 2026)

### Phase 2: Medium-Priority (P2) - Week 2
1. **SaveStateCloudService.cs** - Completed (February 23, 2026)
2. **MacOSMemoryReader.cs** - Completed (February 23, 2026)
3. **InputRecordingService.cs** - Completed (February 23, 2026)

### Phase 3: Lower-Priority (P3) - Week 3
1. **CertificationSystem.cs** - Completed (February 23, 2026)
2. **SignatureTestRunner.cs** - Completed (February 23, 2026)
3. **MemoryPatternDatabase.cs** - Completed (February 23, 2026)
4. **SignatureVerificationService.cs** - Completed (February 23, 2026)

### Per-File Refactoring Checklist

- [ ] Analyze current responsibilities
- [ ] Identify natural boundaries for extraction
- [ ] Create Manager/Interface classes
- [ ] Move methods to appropriate classes
- [ ] Update original service to coordinate (thin facade)
- [ ] Update DI registration
- [ ] Update unit tests
- [ ] Update integration tests
- [ ] Verify build (0 errors)
- [ ] Run architecture tests
- [ ] Update documentation

---

## Success Metrics

| Metric | Before | Target |
|--------|--------|--------|
| Files >1000 lines | 3 | 1 (SaveStateDbContext allowed) |
| Files >800 lines | 10+ | 3-5 |
| Average file size | ~900 lines | ~500 lines |
| Build errors | 0 | 0 |
| Test failures | 0 | 0 |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Breaking changes | Refactor one file at a time, extensive testing |
| Test updates | Use IDE refactoring tools, update test mocks |
| DI registration | Document all new services, verify in startup |
| Interface changes | Keep old interfaces during transition period |
| Performance | Benchmark before/after to ensure no regression |

---

## Pattern Reference

### Manager Pattern
```csharp
// Coordinator Service (~200 lines)
public class MainService : IMainService
{
    private readonly SubManager1 _manager1;
    private readonly SubManager2 _manager2;
    
    public MainService(SubManager1 m1, SubManager2 m2)
    {
        _manager1 = m1;
        _manager2 = m2;
    }
    
    public Result DoWork() => _manager1.DoWork();
}

// Individual Manager (~200 lines each)
public class SubManager1
{
    public Result DoWork() { /* implementation */ }
}
```

### Interface Segregation
```csharp
// Instead of one large interface
public interface IMainService : ISubService1, ISubService2 { }

public interface ISubService1 { void Method1(); }
public interface ISubService2 { void Method2(); }

// Implementation implements all
public class MainService : IMainService { }
```

---

*Document created based on COMPREHENSIVE_TECHNICAL_DEBT_AUDIT_2026_02_21_FRESH.md*





