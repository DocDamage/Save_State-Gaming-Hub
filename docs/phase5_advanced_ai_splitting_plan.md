# Phase 5: AdvancedAiService Splitting Plan

## Current State Analysis

**File**: `AdvancedAiService.cs`
**Lines**: 506
**Dependencies**: 18 injected services
**Public Methods**: 16
**Complexity**: **VERY HIGH** (God Object pattern)

## Identified Responsibilities

The `AdvancedAiService` is currently handling:

1. **Request Processing** - Orchestrating the full AI request pipeline
2. **Memory Management** - Coordinating memory layers (short-term, episodic, canonical)
3. **Intent Classification** - Routing based on user intent
4. **Agent Orchestration** - Selecting and routing to specialist agents
5. **State Management** - Managing world and player state
6. **Validation** - Critiquing and validating AI outputs
7. **Confidence Scoring** - Evaluating response confidence
8. **Player Modeling** - Tracking player behavior and preferences
9. **Timeline Management** - Creating savepoints and what-if simulations
10. **Event Management** - Publishing and subscribing to AI events
11. **Narrative Generation** - Specialized narrative and commentary generation

## Proposed Extraction

### Component 1: `AiRequestProcessor` (IAiRequestProcessor)

**Responsibility**: Core request processing orchestration
**Lines**: ~120
**Methods**:

- `ProcessAsync()`
- Internal pipeline coordination

### Component 2: `AiMemoryCoordinator` (IAiMemoryCoordinator)

**Responsibility**: Memory layer coordination
**Lines**: ~40
**Methods**:

- `RecordInteractionAsync()`
- `GetContextualMemoryAsync()`
- `BuildMemoryContext()`

### Component 3: `AiWorldStateCoordinator` (IAiWorldStateCoordinator)

**Responsibility**: World and player state management
**Lines**: ~50
**Methods**:

- `UpdateWorldState()`
- `GetCurrentWorldState()`
- `UpdatePlayerModelAsync()`
- `GetPlayerProfileAsync()`

### Component 4: `AiValidationCoordinator` (IAiValidationCoordinator)

**Responsibility**: Output validation and confidence scoring
**Lines**: ~60
**Methods**:

- `ValidateResponse()`
- `ScoreConfidence()`
- `WrapWithUncertainty()`
- `ValidateActionAsync()`

### Component 5: `AiNarrativeGenerator` (IAiNarrativeGenerator)

**Responsibility**: Specialized content generation
**Lines**: ~60
**Methods**:

- `GenerateNarrativeAsync()`
- `GenerateCommentaryAsync()`

### Component 6: `AiTimelineCoordinator` (IAiTimelineCoordinator)

**Responsibility**: Timeline and savepoint management
**Lines**: ~30
**Methods**:

- `CreateSavePoint()`
- `SimulateWhatIfAsync()`

### Component 7: `AiEventCoordinator` (IAiEventCoordinator)

**Responsibility**: Event publishing and subscription
**Lines**: ~30
**Methods**:

- `SubscribeToEvent()`
- `PublishEventAsync()`

### Refactored Facade: `AdvancedAiService`

**Responsibility**: Unified interface, delegates to components
**Lines**: ~80-100
**Methods**: All public interface methods (delegates)

## Implementation Steps

1. ✅ Create all 7 component interfaces in a new file
2. ✅ Extract `AiRequestProcessor` with full implementation
3. ✅ Extract `AiMemoryCoordinator` with implementation
4. ✅ Extract `AiWorldStateCoordinator` with implementation
5. ✅ Extract `AiValidationCoordinator` with implementation
6. ✅ Extract `AiNarrativeGenerator` with implementation
7. ✅ Extract `AiTimelineCoordinator` with implementation
8. ✅ Extract `AiEventCoordinator` with implementation
9. ✅ Refactor `AdvancedAiService` to be a facade
10. ✅ Update DI registrations in `ServiceCollectionExtensions.cs`
11. ✅ Run build and tests
12. ✅ Update documentation

## Expected Outcomes

- **Reduced Complexity**: 506 lines → ~100 lines facade + 7 focused components (~50-120 lines each)
- **Improved Testability**: Each component can be tested in isolation
- **Clear Separation**: Each component has a single, well-defined responsibility
- **Maintainability**: Easier to understand, modify, and extend individual components
- **Dependency Clarity**: Each component only depends on what it actually needs

## Risk Mitigation

- **Backward Compatibility**: Maintain the existing `IAdvancedAiService` interface
- **Gradual Migration**: Internal restructuring without breaking external contracts
- **Comprehensive Testing**: Existing tests should pass with zero modifications
