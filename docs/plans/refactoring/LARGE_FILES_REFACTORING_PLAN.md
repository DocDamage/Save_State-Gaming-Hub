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

## Progress Update (February 22, 2026)

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
- [ ] Next target: `SaveStateCloudService.cs` (currently 1006 lines)
- [x] Oversized file count reduced in this session from **14** to **11** (`>800` lines)

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

### P2: SaveStateCloudService.cs (1,006 lines)

**Location:** `src/SaveState.Infrastructure/SaveStates/SaveStateCloudService.cs`

**Current Responsibilities:**
- Cloud save upload/download
- Sync conflict resolution
- Multi-provider support (OneDrive, Google Drive, etc.)
- Encryption/decryption
- Sync scheduling
- Quota management

**Proposed Structure - Manager Pattern:**

```
SaveStateCloudService (Coordinator, ~200 lines)
├── CloudUploadManager (~180 lines)
├── CloudDownloadManager (~180 lines)
├── SyncConflictManager (~180 lines)
├── CloudEncryptionManager (~150 lines)
├── SyncScheduleManager (~150 lines)
└── QuotaManager (~100 lines)
```

---

### P2: InputRecordingService.cs (983 lines)

**Location:** `src/SaveState.Infrastructure/InputRecording/InputRecordingService.cs`

**Current Responsibilities:**
- Input capture
- Recording storage
- Playback
- Replay analysis
- Macro generation

**Proposed Structure - Manager Pattern:**

```
InputRecordingService (Coordinator, ~180 lines)
├── InputCaptureManager (~200 lines)
├── RecordingStorageManager (~200 lines)
├── PlaybackManager (~200 lines)
├── ReplayAnalysisManager (~180 lines)
└── MacroGenerationManager (~150 lines)
```

---

### P2: MacOSMemoryReader.cs (979 lines)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/MacOSMemoryReader.cs`

**Current Responsibilities:**
- macOS-specific memory reading
- Mach kernel API calls
- Permission handling
- Error handling for SIP

**Proposed Structure - Platform Abstraction:**

```
MacOSMemoryReader (~300 lines - thin wrapper)
├── MachKernelApi (~250 lines)
├── MacOSPermissionHandler (~200 lines)
├── MacOSErrorTranslator (~150 lines)
└── SipDetector (~100 lines)
```

**Note:** This is platform-specific code. Consider extracting into `CrossPlatform/MacOS/` namespace.

---

### P3: CertificationSystem.cs (976 lines)

**Location:** `src/SaveState.Application/Mugen/Services/CertificationSystem.cs`

**Current Responsibilities:**
- Character certification
- Stage certification
- Content validation
- Compliance checking
- Certificate generation

**Proposed Structure - Manager Pattern:**

```
CertificationSystem (Coordinator, ~180 lines)
├── CharacterCertificationManager (~200 lines)
├── StageCertificationManager (~200 lines)
├── ContentValidationManager (~200 lines)
├── ComplianceChecker (~150 lines)
└── CertificateGenerator (~150 lines)
```

---

### P3: SignatureTestRunner.cs (971 lines)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Testing/SignatureTestRunner.cs`

**Current Responsibilities:**
- Test execution
- Result collection
- Report generation
- Test case management
- Performance measurement

**Proposed Structure - Test Infrastructure:**

```
SignatureTestRunner (~300 lines)
├── TestExecutionEngine (~250 lines)
├── TestResultCollector (~200 lines)
├── TestReportGenerator (~150 lines)
└── TestCaseRepository (~150 lines)
```

---

### P3: MemoryPatternDatabase.cs (960 lines)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/MemoryPatternDatabase.cs`

**Current Responsibilities:**
- Pattern storage
- Pattern retrieval
- Game signature management
- Pattern validation
- Database operations

**Proposed Structure - Data Access Pattern:**

```
MemoryPatternDatabase (~250 lines)
├── PatternStorageService (~200 lines)
├── PatternRetrievalService (~200 lines)
├── SignatureManager (~200 lines)
└── PatternValidator (~150 lines)
```

---

### P3: SignatureVerificationService.cs (950 lines)

**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/SignatureVerificationService.cs`

**Current Responsibilities:**
- Signature validation
- Multi-pass verification
- Health scoring
- Result reporting

**Proposed Structure - Pipeline Pattern:**

```
SignatureVerificationService (~200 lines)
├── VerificationPipeline (~200 lines)
├── StaticAnalysisStage (~150 lines)
├── DynamicAnalysisStage (~150 lines)
├── StabilityTestStage (~150 lines)
└── HealthScoreCalculator (~150 lines)
```

---

## Implementation Strategy

### Phase 1: High-Priority (P1) - Week 1
1. **RomValidationService.cs** - Manager pattern extraction ✅ Completed (February 22, 2026)
2. **IPerformanceProfilerService.cs** - Interface segregation ✅ Completed (February 22, 2026)
3. **IStoryModeService.cs** - Interface/model split and focused interface registration âœ… Completed (February 22, 2026)

### Phase 2: Medium-Priority (P2) - Week 2
1. **SaveStateCloudService.cs**
2. **InputRecordingService.cs**
3. **MacOSMemoryReader.cs** (consider platform extraction)

### Phase 3: Lower-Priority (P3) - Week 3
1. **CertificationSystem.cs**
2. **SignatureTestRunner.cs**
3. **MemoryPatternDatabase.cs**
4. **SignatureVerificationService.cs**

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
