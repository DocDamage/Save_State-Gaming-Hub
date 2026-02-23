# Master Refactoring Roadmap

**Project:** SaveStateReborn Architecture Hardening  
**Scope:** Baseline roadmap for 14 large files (>800 lines) using Manager Pattern  
**Generated:** February 21, 2026  
**Total Estimated Effort:** 180-220 hours

---

## February 23, 2026 Completion Update

- The `>800` non-migration source file remediation objective is complete.
- Current count of non-migration `src/` files above 800 lines: `0`.
- Recently completed service splits:
  - `MugenEsportsService`
  - `NetworkQualityMonitor`
  - `MugenPrizePoolService`
  - `QuantumSuperpositionService`
- This roadmap remains as the baseline planning artifact; execution details and validation logs are tracked in `docs/plans/refactoring/LARGE_FILES_REFACTORING_PLAN.md`.

---

## Executive Summary

This document provides a comprehensive roadmap for refactoring 14 large service files that exceed the architecture guardrail of 800 lines. Each file will be decomposed using the **Manager Pattern** already established in the codebase.

### Current State
| Metric | Value |
|--------|-------|
| Files Over 1000 Lines | 14 |
| Total Lines | 14,653 |
| Average Lines/File | 1,047 |
| Architecture Guardrail | 800 lines |

### Target State
| Metric | Value |
|--------|-------|
| Coordinator Lines (avg) | ~150 |
| Manager Classes | 75-85 |
| Line Reduction | 75-85% |

---

## Files to Refactor

### Tier 1: Infrastructure Services (Priority: High)

#### 1. AutoDiscoveryEngine.cs (1,079 lines) â†’ 6 Managers
**Location:** `src/SaveState.Infrastructure/GameLibrary/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| DiscoverySessionManager | Process attachment, session lifecycle | 180 |
| MemoryScanManager | Initial memory scanning | 200 |
| ChangeMonitorManager | Value change tracking | 150 |
| HeuristicAnalysisManager | 91 heuristics, confidence scoring | 140 |
| MemoryReaderManager | Win32 API memory reading | 120 |
| DiscoveryFeedbackManager | User feedback, result ranking | 100 |

**Coordinator Target:** ~180 lines  
**Effort:** 12-14 hours  
**Plan:** [AutoDiscoveryEngine_Refactoring_Plan.md](./AutoDiscoveryEngine_Refactoring_Plan.md)

---

#### 2. DependencyInjection.cs (1,051 lines) â†’ Extension Methods
**Location:** `src/SaveState.Infrastructure/`

**Note:** This file was partially decomposed earlier. The plan focuses on completing the decomposition.

| Extension Method | Content | Est. Lines |
|-----------------|---------|------------|
| AddDatabaseServices() | DbContext, factories | 20 |
| AddRepositoryServices() | 20+ repositories | 30 |
| AddCoreInfrastructureServices() | Session, detection, social | 50 |
| AddMugenServices() | 8 manager-pattern services | 120 |
| AddAiServices() | AI providers, orchestrator | 40 |
| AddExternalApiClients() | 15+ HTTP clients | 80 |
| ... (8 more) | Various domains | 280 |

**Coordinator Target:** ~50 lines  
**Effort:** 6-7 hours  
**Plan:** [DependencyInjection_Refactoring_Plan.md](./DependencyInjection_Refactoring_Plan.md)

---

#### 3. RomValidationService.cs (1,032 lines) â†’ 5 Managers
**Location:** `src/SaveState.Infrastructure/RomManagement/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| HashCalculationManager | MD5, SHA1, CRC32 hashing | 200 |
| IntegrityCheckManager | File integrity verification | 180 |
| DatFileManager | DAT file parsing | 150 |
| BatchProcessorManager | Batch validation | 140 |
| ValidationReportManager | Report generation | 120 |

**Coordinator Target:** ~160 lines  
**Effort:** 10-12 hours  
**Plan:** [RomValidationService_Refactoring_Plan.md](./RomValidationService_Refactoring_Plan.md)

---

#### 4. SaveStateCloudService.cs (1,006 lines) â†’ 6 Managers
**Location:** `src/SaveState.Infrastructure/SaveStates/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| CloudProviderManager | Multi-provider abstraction | 180 |
| ConflictResolutionManager | Sync conflict resolution | 160 |
| EncryptionManager | Data encryption/decryption | 140 |
| VersionManager | Save state versioning | 120 |
| UploadDownloadManager | Transfer orchestration | 140 |
| CloudStatusManager | Status monitoring | 100 |

**Coordinator Target:** ~180 lines  
**Effort:** 11-13 hours  
**Plan:** [SaveStateCloudService_Refactoring_Plan.md](./SaveStateCloudService_Refactoring_Plan.md)

---

### Tier 2: Application Services - Mugen (Priority: High)

#### 5. EmotionalResonanceService.cs (1,073 lines) â†’ 6 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| EmotionStateManager | Character emotional state | 220 |
| ResonanceFieldManager | Field creation, transfer | 80 |
| SpectatorInfluenceManager | Crowd psychology | 90 |
| PsychologicalStateManager | Breaking points | 60 |
| EffectApplicationManager | Cross-cutting effects | 50 |
| EmotionalAnalyticsManager | Analytics, reporting | 100 |

**Coordinator Target:** ~120 lines  
**Effort:** 13-15 hours  
**Plan:** [EmotionalResonanceService_Refactoring_Plan.md](./EmotionalResonanceService_Refactoring_Plan.md)

---

#### 6. AdvancedReportingService.cs (1,063 lines) â†’ 6 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| ReportGenerationManager | Report creation, export | 200 |
| DashboardManager | Dashboard, widgets | 160 |
| VisualizationManager | Charts, data points | 140 |
| ReportTemplateManager | Template CRUD | 120 |
| ReportSchedulingManager | Automated scheduling | 100 |
| ReportSharingManager | Sharing, permissions | 80 |

**Coordinator Target:** ~150 lines  
**Effort:** 11-13 hours  
**Plan:** [AdvancedReportingService_Refactoring_Plan.md](./AdvancedReportingService_Refactoring_Plan.md)

---

#### 7. ScreenFiltersEngine.cs (1,062 lines) â†’ 8 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| FilterProfileManager | Profile CRUD | 140 |
| CrtFilterManager | CRT emulation | 150 |
| ScanlineManager | Scanline generation | 120 |
| ShaderManager | Custom shader compilation | 130 |
| FilterChainManager | Multi-filter composition | 100 |
| FilterPresetManager | Built-in presets | 80 |
| FilterPerformanceManager | FPS/memory analysis | 90 |
| EffectApplicationManager | Apply to render targets | 80 |

**Coordinator Target:** ~140 lines  
**Effort:** 14-16 hours  
**Plan:** [ScreenFiltersEngine_Refactoring_Plan.md](./ScreenFiltersEngine_Refactoring_Plan.md)

---

#### 8. CinematicCameraSystem.cs (1,057 lines) â†’ 9 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| CameraSequenceManager | Sequence CRUD | 130 |
| CameraPresetManager | Preset management | 110 |
| CameraPathManager | Path creation | 100 |
| CinematicEventManager | Trigger-based events | 90 |
| CameraTransitionManager | Transitions, easing | 80 |
| CameraRigManager | Physical rig setup | 90 |
| SequenceExecutionManager | Playback orchestration | 100 |
| CameraControllerManager | Low-level camera state | 80 |
| SequenceAnalyticsManager | Performance analysis | 70 |

**Coordinator Target:** ~130 lines  
**Effort:** 15-17 hours  
**Plan:** [CinematicCameraSystem_Refactoring_Plan.md](./CinematicCameraSystem_Refactoring_Plan.md)

---

#### 9. AdvancedPhysicsCombatService.cs (1,056 lines) â†’ 6 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| HitDetectionManager | Collision, cross-up detection | 200 |
| JuggleDecayManager | Combo scaling, gravity | 180 |
| CharacterGravityManager | Per-character physics | 140 |
| WallSplatManager | Wall collision, bounce | 120 |
| EnvironmentDestructionManager | Stage breaking | 100 |
| PhysicsReportingManager | Statistics aggregation | 80 |

**Coordinator Target:** ~150 lines  
**Effort:** 12-14 hours  
**Plan:** [AdvancedPhysicsCombatService_Refactoring_Plan.md](./AdvancedPhysicsCombatService_Refactoring_Plan.md)

---

#### 10. DynamicDifficultyAdjustment.cs (1,052 lines) â†’ 6 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| PerformanceMonitorManager | Player performance analysis | 200 |
| DifficultyAdaptationManager | Adjustment calculations | 180 |
| OpponentBehaviorManager | AI behavior generation | 160 |
| DifficultyProfileManager | Profile CRUD | 120 |
| ChallengeCalibrationManager | Challenge calibration | 100 |
| DifficultyReportingManager | Trend analysis | 80 |

**Coordinator Target:** ~140 lines  
**Effort:** 13-15 hours  
**Plan:** [DynamicDifficultyAdjustment_Refactoring_Plan.md](./DynamicDifficultyAdjustment_Refactoring_Plan.md)

---

#### 11. EnterpriseSecurityService.cs (1,044 lines) â†’ 6 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| AccessControlManager | Permission evaluation | 200 |
| EncryptionManager | Data encryption/decryption | 180 |
| ComplianceManager | Regulatory compliance | 160 |
| ThreatDetectionManager | Threat detection | 140 |
| AuditTrailManager | Security event logging | 120 |
| SecurityPolicyManager | Policy management | 100 |

**Coordinator Target:** ~140 lines  
**Effort:** 12-14 hours  
**Plan:** [EnterpriseSecurityService_Refactoring_Plan.md](./EnterpriseSecurityService_Refactoring_Plan.md)

---

### Tier 3: ML and Learning Services (Priority: Medium)

#### 12. PatternPredictionModel.cs (1,062 lines) â†’ 5 Managers
**Location:** `src/SaveState.Infrastructure/GameLibrary/ML/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| DiscoveryPatternManager | Pattern discovery | 220 |
| GenreProfileManager | Genre profiling | 180 |
| EngineProfileManager | Engine profiling | 160 |
| PredictionEngineManager | ML predictions | 140 |
| AddressCalculatorManager | Address calculations | 120 |

**Coordinator Target:** ~180 lines  
**Effort:** 11-13 hours  
**Plan:** [PatternPredictionModel_Refactoring_Plan.md](./PatternPredictionModel_Refactoring_Plan.md)

---

#### 13. BeginnerPathwaysService.cs (1,000 lines) â†’ 5 Managers
**Location:** `src/SaveState.Application/Mugen/Services/`

| Manager | Responsibility | Est. Lines |
|---------|----------------|------------|
| LearningPathManager | Path creation | 200 |
| ProgressTrackingManager | Progress monitoring | 180 |
| LessonContentManager | Content delivery | 160 |
| AdaptiveLearningManager | Adaptive difficulty | 140 |
| MilestoneManager | Achievement tracking | 120 |

**Coordinator Target:** ~200 lines  
**Effort:** 10-12 hours  
**Plan:** [BeginnerPathwaysService_Refactoring_Plan.md](./BeginnerPathwaysService_Refactoring_Plan.md)

---

### Tier 4: Core Interfaces (Priority: Low)

#### 14. IStoryModeService.cs (1,027 lines) â†’ File Organization
**Location:** `src/SaveState.Core/Mugen/Services/`

**Note:** This file is already properly architected (marker + focused interfaces). Only file organization is needed.

**Action:** Split into separate files:
- `IStoryModeService.cs` (marker interface only)
- `IStoryProjectService.cs`
- `IStoryChapterService.cs`
- `IStorySceneService.cs`
- `IStoryDialogueService.cs`
- `IStoryCutsceneService.cs`
- `IStoryBranchingService.cs`
- `IStoryBattleIntegrationService.cs`
- `IStoryTestingService.cs`
- `IStoryAssetService.cs`
- `StoryModeModels.cs` (all DTOs)

**Effort:** 4-5 hours  
**Plan:** [IStoryModeService_Refactoring_Plan.md](./IStoryModeService_Refactoring_Plan.md)

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2)
1. **DependencyInjection.cs** - Completes DI decomposition
2. **IStoryModeService.cs** - File organization (low risk)

### Phase 2: Infrastructure Services (Week 3-6)
3. **AutoDiscoveryEngine.cs**
4. **RomValidationService.cs**
5. **SaveStateCloudService.cs**

### Phase 3: Mugen Services - Batch 1 (Week 7-10)
6. **EmotionalResonanceService.cs**
7. **AdvancedReportingService.cs**
8. **ScreenFiltersEngine.cs**

### Phase 4: Mugen Services - Batch 2 (Week 11-14)
9. **CinematicCameraSystem.cs**
10. **AdvancedPhysicsCombatService.cs**
11. **DynamicDifficultyAdjustment.cs**

### Phase 5: Mugen Services - Batch 3 (Week 15-17)
12. **EnterpriseSecurityService.cs**
13. **PatternPredictionModel.cs**
14. **BeginnerPathwaysService.cs**

---

## Common Patterns

### Manager Pattern Structure
```csharp
// Coordinator Service (~150 lines)
public class MyService : IMyService
{
    private readonly Manager1 _manager1;
    private readonly Manager2 _manager2;
    // ... more managers

    public MyService(Manager1 manager1, Manager2 manager2, ...)
    {
        _manager1 = manager1;
        _manager2 = manager2;
    }

    public Task<Result<T>> DoSomethingAsync(params)
    {
        // Delegate to managers
        var result = await _manager1.DoPartAsync(params);
        if (result.IsFailure) return Result<T>.Failure(result.Error!);
        
        return await _manager2.DoAnotherPartAsync(result.Value);
    }
}

// Individual Manager (~150 lines each)
public class Manager1
{
    // Single responsibility implementation
}
```

### Key Principles
1. **Single Responsibility** - Each manager has one job
2. **Dependency Injection** - All dependencies injected via constructor
3. **Result Pattern** - Use `Result<T>` for all operations
4. **Interface Segregation** - Each manager has focused interface
5. **No Circular Dependencies** - Careful orchestration in coordinator

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking changes | Maintain obsolete overloads, gradual migration |
| Test coverage | Each manager gets unit tests before coordinator refactor |
| Performance | Benchmark before/after, lazy loading where appropriate |
| Complexity | One service at a time, full regression testing |
| Merge conflicts | Short-lived branches, daily rebasing |

---

## Success Metrics

| Metric | Before | Target | Actual (Feb 23, 2026) | Measurement |
|--------|--------|--------|------------------------|-------------|
| Lines > 800 | 14 | 0 | 0 | Architecture test |
| Avg service lines | 1,047 | <200 | ~550 (refactored set average) | Line count |
| Test coverage | Baseline | +10% | Targeted regressions passed | Coverage report |
| Build time | Baseline | No regression | No regression observed | CI metrics |
| Cyclomatic complexity | High | Reduced | Reduced in split services | Static analysis |

---

## Next Steps

1. **Archive** this roadmap as completed for the `>800` threshold objective.
2. **Choose** next threshold for continued hardening (`>750` or `>700`).
3. **Create** focused follow-up issues only for remaining medium-sized hotspots.
4. **Add** CI architecture guardrails for the new threshold.
5. **Schedule** the next refactor wave based on team capacity.

---

*For detailed implementation guidance, see individual plan files linked above.*

