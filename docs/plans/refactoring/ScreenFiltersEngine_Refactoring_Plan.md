# ScreenFiltersEngine Refactoring Plan

## Overview

**File:** `src/SaveState.Application/Mugen/Services/ScreenFiltersEngine.cs`  
**Current Lines:** 1,062  
**Current Methods:** 9 public + 13 private + 3 nested classes  
**Target:** Split into Manager Pattern following project conventions

---

## Current Structure Analysis

### Service Breakdown

```
ScreenFiltersEngine (Coordinator - 555 lines)
├── CRTEmulator (nested class - 33 lines)
├── ScanlineGenerator (nested class - 30 lines)
├── PostProcessingPipeline (nested class - 17 lines - mostly empty)
├── Performance Calculation Methods (13 private methods)
├── Default Filter Initialization
├── Data Models (~410 lines of DTOs/Enums)
└── Interface Definition (~12 lines)
```

### Responsibility Areas Identified

1. **Filter Profile Management** - Creating, storing filter profiles
2. **CRT Emulation** - CRT monitor simulation, curvature, scanlines
3. **Scanline Generation** - Retro display scanline effects
4. **Custom Shader Management** - Shader creation, compilation, uniforms
5. **Filter Chain Management** - Multi-filter composition
6. **Preset Management** - Built-in and custom presets
7. **Performance Analysis** - FPS impact, memory usage, GPU utilization
8. **Effect Application** - Applying filters to render targets

---

## Proposed Manager Structure

### After Refactoring

```
ScreenFiltersEngine (Coordinator - ~120 lines)
├── IScreenFiltersEngine (interface - split to separate file)
├── FilterProfileManager (Filter profile CRUD, storage)
├── CrtFilterManager (CRTEmulator → Manager)
├── ScanlineManager (ScanlineGenerator → Manager)
├── ShaderManager (Custom shader management)
├── FilterChainManager (Filter composition, blending)
├── FilterPresetManager (Preset library, categories)
├── FilterPerformanceManager (Performance analysis)
└── EffectApplicationManager (Apply filters to targets)
```

### Manager Classes

#### 1. FilterProfileManager
**Responsibilities:**
- Create and manage filter profiles
- Store profiles in memory dictionary
- Profile validation
- Default profile initialization

**Methods:**
```csharp
public Task<ScreenFilterProfile> CreateProfileAsync(FilterProfileRequest request, CancellationToken ct)
public Task<ScreenFilterProfile?> GetProfileAsync(string profileId, CancellationToken ct)
public Task UpdateProfileAsync(ScreenFilterProfile profile, CancellationToken ct)
public Task DeleteProfileAsync(string profileId, CancellationToken ct)
public Task<IReadOnlyList<ScreenFilterProfile>> GetAllProfilesAsync(CancellationToken ct)
public Task InitializeDefaultProfilesAsync(CancellationToken ct)
```

**Estimated Lines:** ~140

---

#### 2. CrtFilterManager
**Responsibilities:**
- CRT monitor emulation
- Curvature, vignette, phosphor glow
- Color bleeding simulation
- Overscan and corner rounding
- Apply CRT effects to render targets

**Methods:**
```csharp
public Task<CRTSettings> CreateSettingsAsync(CRTSettingsRequest request, CancellationToken ct)
public Task ApplyCRTEffectAsync(CRTSettings settings, RenderTarget target, CancellationToken ct)
public Task<float> CalculateCRTPerformanceImpactAsync(CRTSettings settings, CancellationToken ct)
public CRTSettings GetDefaultSettings()
```

**Estimated Lines:** ~120

---

#### 3. ScanlineManager
**Responsibilities:**
- Scanline generation
- Intensity, thickness, spacing control
- Horizontal/vertical shift
- Color and animation
- Apply scanline effects

**Methods:**
```csharp
public Task<ScanlineSettings> CreateSettingsAsync(ScanlineSettingsRequest request, CancellationToken ct)
public Task ApplyScanlinesAsync(ScanlineSettings settings, RenderTarget target, CancellationToken ct)
public Task<float> CalculateScanlinePerformanceImpactAsync(ScanlineSettings settings, CancellationToken ct)
public ScanlineSettings GetDefaultSettings()
```

**Estimated Lines:** ~100

---

#### 4. ShaderManager
**Responsibilities:**
- Custom shader creation
- Shader compilation
- Uniform parsing and management
- Performance rating calculation
- Shader storage

**Methods:**
```csharp
public Task<CustomShader> CreateShaderAsync(CustomShaderRequest request, CancellationToken ct)
public Task<CustomShader?> GetShaderAsync(string shaderId, CancellationToken ct)
public Task<IReadOnlyList<ShaderUniform>> ParseUniformsAsync(string fragmentShader, CancellationToken ct)
public ShaderPerformanceRating CalculatePerformanceRating(string vertexShader, string fragmentShader)
public Task<ShaderCompilationResult> CompileShaderAsync(string vertexShader, string fragmentShader, CancellationToken ct)
```

**Estimated Lines:** ~130

---

#### 5. FilterChainManager
**Responsibilities:**
- Create filter chains
- Validate chain compatibility
- Manage blend modes
- Chain execution order

**Methods:**
```csharp
public Task<FilterChain> CreateChainAsync(FilterChainRequest request, CancellationToken ct)
public Task<Result> ValidateChainAsync(FilterChain chain, CancellationToken ct)
public Task<FilterChain?> GetChainAsync(string chainId, CancellationToken ct)
public Task ExecuteChainAsync(FilterChain chain, RenderTarget target, CancellationToken ct)
public Task<IReadOnlyList<FilterChain>> GetCompatibleChainsAsync(string profileId, CancellationToken ct)
```

**Estimated Lines:** ~110

---

#### 6. FilterPresetManager
**Responsibilities:**
- Built-in preset library
- Custom preset management
- Preset categorization (CRT, Arcade, Handheld, etc.)
- Preset discovery and search

**Methods:**
```csharp
public Task<FilterPreset> CreatePresetAsync(FilterPresetRequest request, CancellationToken ct)
public Task<IReadOnlyList<FilterPreset>> GetPresetsAsync(FilterCategory? category, CancellationToken ct)
public Task<FilterPreset?> GetPresetByIdAsync(string presetId, CancellationToken ct)
public Task<IReadOnlyList<FilterPreset>> SearchPresetsAsync(string query, CancellationToken ct)
public Task InitializeBuiltInPresetsAsync(CancellationToken ct)
```

**Estimated Lines:** ~120

---

#### 7. FilterPerformanceManager
**Responsibilities:**
- Performance impact analysis
- Frame rate impact calculation
- Memory usage estimation
- GPU utilization calculation
- Draw call counting
- Shader switch counting
- Performance recommendations

**Methods:**
```csharp
public Task<FilterPerformanceReport> AnalyzePerformanceAsync(ScreenFilterProfile profile, CancellationToken ct)
public float CalculateFrameRateImpact(ScreenFilterProfile profile)
public long CalculateMemoryUsage(ScreenFilterProfile profile)
public float CalculateGPUUtilization(ScreenFilterProfile profile)
public int CalculateDrawCallsAdded(ScreenFilterProfile profile)
public int CalculateShaderSwitches(ScreenFilterProfile profile)
public IReadOnlyList<string> GenerateRecommendations(ScreenFilterProfile profile)
```

**Estimated Lines:** ~150

---

#### 8. EffectApplicationManager
**Responsibilities:**
- Apply complete filter profiles to render targets
- Orchestrate effect application order
- Color correction
- Noise/grain effects
- Bloom effects
- Custom effect application

**Methods:**
```csharp
public Task ApplyProfileAsync(string profileId, RenderTarget target, CancellationToken ct)
public Task ApplyColorCorrectionAsync(ColorSettings settings, RenderTarget target, CancellationToken ct)
public Task ApplyNoiseAsync(NoiseSettings settings, RenderTarget target, CancellationToken ct)
public Task ApplyBloomAsync(BloomSettings settings, RenderTarget target, CancellationToken ct)
public Task ApplyCustomEffectAsync(CustomEffect effect, RenderTarget target, CancellationToken ct)
```

**Estimated Lines:** ~130

---

## Before/After Code Structure

### Before (Current)

```csharp
// ScreenFiltersEngine.cs - 1,062 lines
public class ScreenFiltersEngine : IScreenFiltersEngine
{
    private readonly CRTEmulator _crtEmulator;
    private readonly ScanlineGenerator _scanlineGenerator;
    private readonly PostProcessingPipeline _postProcessingPipeline;
    private readonly Dictionary<string, ScreenFilterProfile> _filterProfiles = new();
    private readonly Dictionary<string, CustomShader> _customShaders = new();
    
    public async Task<Result<ScreenFilterProfile>> CreateFilterProfileAsync(FilterProfileRequest request, CancellationToken ct = default)
    {
        // Profile creation logic
    }
    
    public async Task<Result> ApplyScreenFiltersAsync(string profileId, RenderTarget target, CancellationToken ct = default)
    {
        // Complex orchestration - 50+ lines
        // Calls CRT, scanlines, color, noise, bloom, custom effects
    }
    
    // 7 more public methods...
    // 13 private performance calculation methods...
    // 3 nested classes (CRTEmulator, ScanlineGenerator, PostProcessingPipeline)...
    // 35+ data model classes and enums...
}
```

### After (Target)

```csharp
// ScreenFiltersEngine.cs - ~120 lines
public class ScreenFiltersEngine : IScreenFiltersEngine
{
    private readonly FilterProfileManager _profileManager;
    private readonly CrtFilterManager _crtManager;
    private readonly ScanlineManager _scanlineManager;
    private readonly ShaderManager _shaderManager;
    private readonly FilterChainManager _chainManager;
    private readonly FilterPresetManager _presetManager;
    private readonly FilterPerformanceManager _performanceManager;
    private readonly EffectApplicationManager _effectManager;
    
    public ScreenFiltersEngine(
        FilterProfileManager profileManager,
        CrtFilterManager crtManager,
        ScanlineManager scanlineManager,
        ShaderManager shaderManager,
        FilterChainManager chainManager,
        FilterPresetManager presetManager,
        FilterPerformanceManager performanceManager,
        EffectApplicationManager effectManager)
    {
        _profileManager = profileManager;
        _crtManager = crtManager;
        _scanlineManager = scanlineManager;
        _shaderManager = shaderManager;
        _chainManager = chainManager;
        _presetManager = presetManager;
        _performanceManager = performanceManager;
        _effectManager = effectManager;
    }
    
    public Task<Result<ScreenFilterProfile>> CreateFilterProfileAsync(FilterProfileRequest request, CancellationToken ct = default)
        => _profileManager.CreateProfileAsync(request, ct);
    
    public Task<Result> ApplyScreenFiltersAsync(string profileId, RenderTarget target, CancellationToken ct = default)
        => _effectManager.ApplyProfileAsync(profileId, target, ct);
    
    // Other methods delegate to appropriate managers...
}

// Managers/CrtFilterManager.cs - ~120 lines
public class CrtFilterManager
{
    private readonly ILogger<CrtFilterManager> _logger;
    
    public async Task<CRTSettings> CreateSettingsAsync(CRTSettingsRequest request, CancellationToken ct)
    {
        // Full implementation
    }
    
    public async Task ApplyCRTEffectAsync(CRTSettings settings, RenderTarget target, CancellationToken ct)
    {
        // CRT-specific effect logic
    }
}

// Similar for other managers...
```

---

## Data Model Restructuring

### New File Structure

```
ScreenFilters/
├── Services/
│   ├── ScreenFiltersEngine.cs (coordinator)
│   ├── FilterProfileManager.cs
│   ├── CrtFilterManager.cs
│   ├── ScanlineManager.cs
│   ├── ShaderManager.cs
│   ├── FilterChainManager.cs
│   ├── FilterPresetManager.cs
│   ├── FilterPerformanceManager.cs
│   └── EffectApplicationManager.cs
├── Models/
│   ├── ScreenFilterProfile.cs
│   ├── CRTSettings.cs
│   ├── ScanlineSettings.cs
│   ├── ColorSettings.cs
│   ├── NoiseSettings.cs
│   ├── BloomSettings.cs
│   ├── CustomShader.cs
│   ├── ShaderUniform.cs
│   ├── FilterChain.cs
│   ├── FilterPreset.cs
│   ├── FilterPerformanceReport.cs
│   ├── RenderTarget.cs
│   ├── CustomEffect.cs
│   └── [Other models...]
├── Enums/
│   ├── FilterPresetType.cs
│   ├── FilterCategory.cs
│   ├── NoiseType.cs
│   ├── BlendMode.cs
│   ├── ShaderCompilationStatus.cs
│   ├── ShaderPerformanceRating.cs
│   ├── UniformType.cs
│   └── PixelFormat.cs
└── Interfaces/
    └── IScreenFiltersEngine.cs
```

---

## Edge Cases and Challenges

### 1. Effect Application Order
**Challenge:** Current `ApplyScreenFiltersAsync` applies effects in specific order: CRT → Scanlines → Color → Noise → Bloom → Custom.

**Solution:**
- `EffectApplicationManager` owns the orchestration logic
- Extract order as configuration
- Allow custom order via `FilterChain`

### 2. Performance Calculation Dependencies
**Challenge:** Performance calculations depend on all filter settings (CRT, Scanlines, Bloom, etc.).

**Solution:**
- `FilterPerformanceManager` accepts `ScreenFilterProfile` (complete object)
- Calculations remain centralized
- Other managers provide impact estimates for their specific effects

### 3. Shader Uniform Parsing
**Challenge:** `ParseShaderUniforms` is currently a simple string search.

**Solution:**
- Move to `ShaderManager`
- Can be enhanced later with proper GLSL parser
- Keep simple implementation for now

### 4. PostProcessingPipeline Stub
**Challenge:** `PostProcessingPipeline` is mostly empty (only 17 lines).

**Solution:**
- Can be merged into `EffectApplicationManager`
- Or kept as separate manager if pipeline complexity grows
- Decision: Merge into `EffectApplicationManager` for now

### 5. Shared Settings Objects
**Challenge:** `ColorSettings`, `NoiseSettings`, `BloomSettings` created but not fully implemented.

**Solution:**
- Keep in Models
- Implement full effect logic in `EffectApplicationManager`
- Add corresponding settings creation methods to coordinator

---

## Implementation Steps

### Phase 1: Extract Data Models
1. Create `Models/` directory structure
2. Move all DTO classes to appropriate files
3. Move all enums to `Enums/` directory
4. Update using statements

### Phase 2: Create Managers
1. Create `FilterProfileManager`
2. Create `CrtFilterManager` from `CRTEmulator`
3. Create `ScanlineManager` from `ScanlineGenerator`
4. Create `ShaderManager`
5. Create `FilterChainManager`
6. Create `FilterPresetManager`
7. Create `FilterPerformanceManager` (from private methods)
8. Create `EffectApplicationManager`

### Phase 3: Refactor Coordinator
1. Update `ScreenFiltersEngine` to use managers
2. Remove nested classes
3. Update constructor to accept managers via DI
4. Simplify methods to delegate calls

### Phase 4: Update DI Registration
1. Register all managers in DI container
2. Update service registration
3. Ensure proper lifetime scopes

### Phase 5: Testing
1. Update unit tests for filter application
2. Test each manager independently
3. Verify filter chain behavior
4. Test performance calculations

---

## Statistics Summary

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Files | 1 | 10 | +9 |
| Lines per file | 1,062 | ~120 avg | -89% |
| Classes per file | 4 | 1 | Clean separation |
| Public methods per class | 9 | ~2-4 | Focused |
| Private methods | 13 | 0 (moved to managers) | Organized |
| Testability | Low | High | Isolated units |

---

## References

- [AGENTS.md](../../../AGENTS.md) - Manager Pattern guidelines
- [Interface Segregation ADR](../../../docs/architecture/adrs/) - Interface splitting patterns
- Existing Manager implementations: `IkemenGoService`, `CharacterDiscoveryService`
