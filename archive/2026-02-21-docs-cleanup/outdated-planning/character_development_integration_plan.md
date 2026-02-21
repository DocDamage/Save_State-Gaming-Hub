# Character Development Integration Plan

**Date**: January 3, 2026  
**Status**: 📋 Planning Phase  
**Goal**: Integrate character development tools and frameworks for full MUGEN/Ikemen potential

---

## 🎯 Executive Summary

This plan details the integration of identified character development repositories into SaveStateReborn to enable full character modification, creation, and development capabilities.

**Total Estimated Effort**: 190-255 hours (updated with NooksVsCrans)  
**Phases**: 3 phases over 12-16 weeks  
**Priority**: HIGH - Core feature for character modification

---

## 📦 Repositories to Integrate

### Confirmed High-Priority Repositories

1. **MugenHook** ⭐⭐⭐⭐⭐ (CRITICAL)
2. **LuaSupernull** ⭐⭐⭐⭐⭐ (CRITICAL)
3. **OpenMK** ⭐⭐⭐⭐⭐ (CRITICAL - from earlier analysis)
4. **ikemenarmor** ⭐⭐⭐⭐ (HIGH - from earlier analysis)
5. **iguana** ⭐⭐⭐⭐⭐ (HIGH - from earlier analysis)
6. **Sprite Editing Tools** ⭐⭐⭐⭐ (HIGH - reference/guide)
7. **NooksVsCrans** ⭐⭐⭐ (MEDIUM - Character Pack/Reference)

---

## Phase 1: Core Development Frameworks (Weeks 1-6)

### 1.1 MugenHook Integration ⭐⭐⭐⭐⭐

**Repository**: [ermaccer/mugenhook](https://github.com/ermaccer/mugenhook)  
**Effort**: 40-50 hours  
**Priority**: CRITICAL

#### Objectives
- Integrate MugenHook engine enhancements into Ikemen setup
- Support multiple characters per slot
- Enable animated portraits
- Provide UI for enhanced character features

#### Technical Implementation

**Step 1: Compatibility Evaluation** (4-6 hours)
```csharp
// Create compatibility checker service
public interface IMugenHookCompatibilityService
{
    Task<CompatibilityResult> CheckIkemenCompatibilityAsync(CancellationToken ct = default);
    Task<bool> IsMugenHookAvailableAsync(CancellationToken ct = default);
    Task<Version> GetMugenHookVersionAsync(CancellationToken ct = default);
}
```

**Step 2: Service Interface Design** (6-8 hours)
```csharp
// Character slot enhancement service
public interface IEnhancedCharacterSlotService
{
    Task<Result<IReadOnlyList<CharacterSlot>>> GetCharacterSlotsAsync(CancellationToken ct = default);
    Task<Result<CharacterSlot>> CreateMultiCharacterSlotAsync(
        IEnumerable<Guid> characterIds, 
        string slotName, 
        CancellationToken ct = default);
    Task<Result> SetAnimatedPortraitAsync(Guid characterId, string portraitPath, CancellationToken ct = default);
}

public class CharacterSlot : EntityBase
{
    public string Name { get; private set; }
    public IReadOnlyList<Guid> CharacterIds { get; private set; }
    public bool SupportsMultipleCharacters { get; private set; }
    public AnimatedPortrait? Portrait { get; private set; }
}
```

**Step 3: Infrastructure Implementation** (12-16 hours)
- Create `MugenHookIntegrationService`
- Implement slot management
- Handle animated portrait storage
- Configure MugenHook in Ikemen setup

**Step 4: Configuration Updates** (4-6 hours)
```json
// engines/ikemen/config.json
{
  "mugenHook": {
    "enabled": true,
    "version": "latest",
    "features": {
      "multipleCharactersPerSlot": true,
      "animatedPortraits": true,
      "enhancedIcons": true
    }
  }
}
```

**Step 5: UI Integration** (8-10 hours)
- Character slot management UI
- Multi-character slot creation
- Animated portrait upload/management
- Feature toggle controls

**Step 6: Testing & Documentation** (6-8 hours)
- Unit tests for slot management
- Integration tests with Ikemen
- Documentation updates
- User guide creation

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/IEnhancedCharacterSlotService.cs`
- `src/SaveState.Core/Mugen/Entities/CharacterSlot.cs`
- `src/SaveState.Infrastructure/Mugen/MugenHookIntegrationService.cs`
- `src/SaveState.Infrastructure/Mugen/MugenHookCompatibilityService.cs`
- `tests/SaveState.Infrastructure.Tests/Mugen/MugenHookIntegrationServiceTests.cs`

**Modified Files**:
- `engines/ikemen/config.json`
- `engines/setup-ikemen.ps1`
- `src/SaveState.Core/Configuration/MugenOptions.cs`
- `src/SaveState.Infrastructure/Mugen/MugenLauncher.cs`

---

### 1.2 LuaSupernull Framework Integration ⭐⭐⭐⭐⭐

**Repository**: [ZiddiaMUGEN/LuaSupernull](https://github.com/ZiddiaMUGEN/LuaSupernull)  
**Effort**: 35-45 hours  
**Priority**: CRITICAL

#### Objectives
- Bundle LuaSupernull as character development framework
- Create character templates based on LuaSupernull
- Integrate into character modification workflows
- Provide Lua supernull documentation and examples

#### Technical Implementation

**Step 1: Framework Integration** (8-10 hours)
```csharp
// LuaSupernull framework service
public interface ILuaSupernullFrameworkService
{
    Task<Result> InstallFrameworkAsync(string targetDirectory, CancellationToken ct = default);
    Task<Result<CharacterTemplate>> CreateTemplateFromFrameworkAsync(
        string templateName, 
        CancellationToken ct = default);
    Task<Result> ValidateSupernullImplementationAsync(Guid characterId, CancellationToken ct = default);
}

public class CharacterTemplate : EntityBase
{
    public string Name { get; private set; }
    public string Framework { get; private set; } // "LuaSupernull", "Standard", etc.
    public string TemplateDirectory { get; private set; }
    public TemplateMetadata Metadata { get; private set; }
}
```

**Step 2: Template System** (10-12 hours)
- Create template directory structure
- Template metadata system
- Template customization system
- Template validation

**Step 3: Character Creation Workflow** (8-10 hours)
```csharp
// Enhanced character creation service
public interface ICharacterTemplateService
{
    Task<Result<IReadOnlyList<CharacterTemplate>>> GetAvailableTemplatesAsync(CancellationToken ct = default);
    Task<Result<MugenCharacter>> CreateCharacterFromTemplateAsync(
        Guid templateId, 
        string characterName, 
        CharacterCustomizationOptions options, 
        CancellationToken ct = default);
    Task<Result> ApplyTemplateToCharacterAsync(
        Guid characterId, 
        Guid templateId, 
        CancellationToken ct = default);
}
```

**Step 4: Documentation & Examples** (6-8 hours)
- LuaSupernull integration guide
- Template creation tutorial
- Example templates
- Best practices documentation

**Step 5: Testing** (3-5 hours)
- Template creation tests
- Framework validation tests
- Character creation from template tests

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/ILuaSupernullFrameworkService.cs`
- `src/SaveState.Core/Mugen/Services/ICharacterTemplateService.cs`
- `src/SaveState.Core/Mugen/Entities/CharacterTemplate.cs`
- `src/SaveState.Infrastructure/Mugen/LuaSupernullFrameworkService.cs`
- `src/SaveState.Infrastructure/Mugen/CharacterTemplateService.cs`
- `data/templates/luasupernull/` (template directory)
- `docs/features/character-templates.md`
- `docs/features/luasupernull-integration.md`

**Modified Files**:
- `engines/setup-ikemen.ps1` (add LuaSupernull setup)
- `src/SaveState.Core/Configuration/MugenOptions.cs`
- `src/SaveState.Infrastructure/Mugen/MugenCharacterLoader.cs`

---

### 1.3 OpenMK Integration ⭐⭐⭐⭐⭐

**Repository**: [Lazin3ss/OpenMK](https://github.com/Lazin3ss/OpenMK)  
**Effort**: 50-60 hours  
**Priority**: CRITICAL (Mortal Kombat-style character development)

#### Objectives
- Bundle OpenMK as character development toolkit
- Support MK-specific game modes
- Provide MK character templates
- Integrate MK mechanics into character modification

#### Technical Implementation

**Step 1: OpenMK Bundle Integration** (10-12 hours)
```csharp
// OpenMK toolkit service
public interface IOpenMkToolkitService
{
    Task<Result> InstallOpenMkAsync(string targetDirectory, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MkGameMode>>> GetAvailableGameModesAsync(CancellationToken ct = default);
    Task<Result> ApplyMkMechanicsToCharacterAsync(
        Guid characterId, 
        MkMechanicsOptions options, 
        CancellationToken ct = default);
}

public enum MkGameMode
{
    Tower,
    Story,
    TagBattle,
    Fatalities,
    Brutalities,
    FatalBlow
}
```

**Step 2: Game Mode Support** (12-15 hours)
- Tower mode configuration
- Story mode setup
- Tag battle mechanics
- Fatalities/Brutalities system

**Step 3: Character Templates** (10-12 hours)
- MK-style character templates
- Fatalities template
- Brutalities template
- Tag battle character template

**Step 4: Mechanics Integration** (10-12 hours)
```csharp
// MK mechanics service
public interface IMortalKombatMechanicsService
{
    Task<Result> AddFatalityToCharacterAsync(Guid characterId, FatalityData fatality, CancellationToken ct = default);
    Task<Result> AddBrutalityToCharacterAsync(Guid characterId, BrutalityData brutality, CancellationToken ct = default);
    Task<Result> ConfigureFatalBlowAsync(Guid characterId, FatalBlowConfig config, CancellationToken ct = default);
    Task<Result> SetupTagMechanicsAsync(Guid characterId, TagMechanicsConfig config, CancellationToken ct = default);
}
```

**Step 5: UI Integration** (8-10 hours)
- MK game mode selection
- MK mechanics configuration UI
- Template selection for MK characters
- Mechanics editor

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/IOpenMkToolkitService.cs`
- `src/SaveState.Core/Mugen/Services/IMortalKombatMechanicsService.cs`
- `src/SaveState.Core/Mugen/Entities/MkGameMode.cs`
- `src/SaveState.Core/Mugen/ValueObjects/FatalityData.cs`
- `src/SaveState.Infrastructure/Mugen/OpenMkToolkitService.cs`
- `src/SaveState.Infrastructure/Mugen/MortalKombatMechanicsService.cs`
- `data/templates/mortal-kombat/` (MK templates)
- `data/development/openmk/` (OpenMK resources)
- `docs/features/openmk-integration.md`
- `docs/features/mortal-kombat-mechanics.md`

**Modified Files**:
- `engines/ikemen/config.json` (MK game modes)
- `engines/setup-ikemen.ps1`
- `src/SaveState.Application/Mugen/Commands/LaunchIkemenVersusCommand.cs`
- `src/SaveState.Core/Configuration/MugenOptions.cs`

---

## Phase 2: Character Modification Tools & Resources (Weeks 7-10)

### 2.1 ikemenarmor Integration ⭐⭐⭐⭐

**Repository**: [kamekaze-world/ikemenarmor](https://github.com/kamekaze-world/ikemenarmor)  
**Effort**: 20-25 hours  
**Priority**: HIGH

#### Objectives
- Integrate armor system into character modification toolkit
- Provide UI for armor mechanics configuration
- Create character templates with armor systems
- Document armor integration

#### Technical Implementation

**Step 1: Armor System Integration** (6-8 hours)
```csharp
// Armor mechanics service
public interface ICharacterArmorService
{
    Task<Result> EnableArmorAsync(Guid characterId, ArmorType armorType, CancellationToken ct = default);
    Task<Result> ConfigureArmorAsync(Guid characterId, ArmorConfiguration config, CancellationToken ct = default);
    Task<Result<bool>> IsArmorEnabledAsync(Guid characterId, CancellationToken ct = default);
    Task<Result<ArmorConfiguration>> GetArmorConfigurationAsync(Guid characterId, CancellationToken ct = default);
}

public enum ArmorType
{
    SuperArmor,
    HyperArmor,
    Custom
}

public class ArmorConfiguration
{
    public ArmorType Type { get; set; }
    public bool ThrowProtection { get; set; }
    public int Duration { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; }
}
```

**Step 2: ZSS Script Management** (4-5 hours)
- Copy armor.zss to appropriate location
- Manage script references in character config
- Handle script dependencies

**Step 3: Character Templates** (4-5 hours)
- Create armor-enabled character templates
- Template customization options
- Template validation

**Step 4: UI Integration** (4-5 hours)
- Armor configuration UI
- Template selection with armor options
- Armor status indicators

**Step 5: Documentation** (2-4 hours)
- Armor integration guide
- Usage examples
- Configuration reference

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/ICharacterArmorService.cs`
- `src/SaveState.Core/Mugen/ValueObjects/ArmorConfiguration.cs`
- `src/SaveState.Infrastructure/Mugen/CharacterArmorService.cs`
- `data/development/armor/armor.zss`
- `data/templates/armor-enabled/` (templates)
- `docs/features/armor-mechanics.md`

**Modified Files**:
- `engines/ikemen/config.json`
- `src/SaveState.Core/Configuration/MugenOptions.cs`

---

### 2.2 iguana Movelist Generator Integration ⭐⭐⭐⭐⭐

**Repository**: [SuperFromND/iguana](https://github.com/SuperFromND/iguana)  
**Effort**: 30-40 hours  
**Priority**: HIGH (metadata extraction)

#### Objectives
- Integrate movelist extraction from character files
- Populate character metadata automatically
- Enhance character database with move information
- Provide movelist viewing/editing

#### Technical Implementation

**Step 1: Evaluation & Porting Decision** (4-6 hours)
- Evaluate Go code structure
- Decide: Port to C# vs. External tool integration
- Recommendation: Port to C# for better integration

**Step 2: C# Implementation** (12-16 hours)
```csharp
// Movelist extraction service
public interface IMovelistExtractionService
{
    Task<Result<MovelistData>> ExtractMovelistAsync(string characterDefPath, CancellationToken ct = default);
    Task<Result<MovelistData>> ExtractMovelistFromCharacterAsync(Guid characterId, CancellationToken ct = default);
    Task<Result> UpdateCharacterMovelistAsync(Guid characterId, CancellationToken ct = default);
}

public class MovelistData
{
    public IReadOnlyList<Move> Moves { get; set; }
    public IReadOnlyList<Combo> Combos { get; set; }
    public IReadOnlyList<SpecialMove> SpecialMoves { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class Move
{
    public string Name { get; set; }
    public string Input { get; set; }
    public string Description { get; set; }
    public int Damage { get; set; }
    public FrameData FrameData { get; set; }
}
```

**Step 3: Database Integration** (6-8 hours)
- Extend MugenCharacter entity with movelist
- Create Move, Combo entities
- Migration scripts
- Repository updates

**Step 4: Character Scanning Enhancement** (4-6 hours)
- Integrate movelist extraction into character scanning
- Batch processing support
- Error handling

**Step 5: UI Integration** (4-6 hours)
- Movelist display in character details
- Move editing capabilities
- Movelist export/import

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/IMovelistExtractionService.cs`
- `src/SaveState.Core/Mugen/Entities/Move.cs`
- `src/SaveState.Core/Mugen/Entities/Combo.cs`
- `src/SaveState.Infrastructure/Mugen/MovelistExtractionService.cs`
- `src/SaveState.Infrastructure/Mugen/Parsers/DefFileMovelistParser.cs`
- `tests/SaveState.Infrastructure.Tests/Mugen/MovelistExtractionServiceTests.cs`

**Modified Files**:
- `src/SaveState.Core/Mugen/Entities/MugenCharacter.cs` (add Movelist property)
- `src/SaveState.Infrastructure/Mugen/MugenCharacterLoader.cs`
- `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs`

---

### 2.3 Sprite Editing Tools Reference Integration ⭐⭐⭐⭐

**Repositories**: LibreSprite, Aseprite, SpriteFactory  
**Effort**: 15-20 hours  
**Priority**: MEDIUM (reference/guide)

#### Objectives
- Document sprite tool recommendations
- Integrate sprite viewing in character editor
- Provide sprite extraction capabilities
- Create sprite editing workflows

#### Technical Implementation

**Step 1: Sprite Viewing Service** (8-10 hours)
```csharp
// Sprite viewing service
public interface ISpriteViewingService
{
    Task<Result<Stream>> ExtractSpriteSheetAsync(Guid characterId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SpriteFrame>>> GetSpriteFramesAsync(Guid characterId, CancellationToken ct = default);
    Task<Result<Stream>> GetSpriteFrameAsync(Guid characterId, int frameNumber, CancellationToken ct = default);
}

public class SpriteFrame
{
    public int FrameNumber { get; set; }
    public int GroupNumber { get; set; }
    public int ImageNumber { get; set; }
    public byte[] ImageData { get; set; }
}
```

**Step 2: Documentation** (4-5 hours)
- Sprite tool recommendations guide
- Sprite extraction tutorial
- Sprite editing workflow
- Tool comparison

**Step 3: UI Integration** (3-5 hours)
- Sprite viewer in character editor
- Sprite extraction UI
- Tool recommendation links

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/ISpriteViewingService.cs`
- `src/SaveState.Infrastructure/Mugen/SpriteViewingService.cs`
- `docs/features/sprite-editing-tools.md`
- `docs/guides/sprite-extraction-guide.md`

**Modified Files**:
- Character editor UI (sprite viewing panel)

---

### 2.4 NooksVsCrans Character Pack Integration ⭐⭐⭐

**Repository**: [Carlmundo/NooksVsCrans](https://github.com/Carlmundo/NooksVsCrans)  
**Effort**: 10-15 hours  
**Priority**: MEDIUM (Character Pack/Reference)

#### Objectives
- Evaluate NooksVsCrans as character pack resource
- Integrate as optional character pack
- Use as reference implementation for MUGEN setup
- Extract characters for character library expansion

#### Technical Implementation

**Step 1: Repository Evaluation** (2-3 hours)
- Review repository structure
- Evaluate character quality and compatibility
- Check license and usage terms
- Assess Ikemen GO compatibility

**Step 2: Character Pack Management Service** (4-6 hours)
```csharp
// Character pack management service
public interface ICharacterPackService
{
    Task<Result<IReadOnlyList<CharacterPackInfo>>> GetAvailablePacksAsync(CancellationToken ct = default);
    Task<Result> InstallCharacterPackAsync(string packSource, string targetDirectory, CancellationToken ct = default);
    Task<Result> ExtractCharactersFromPackAsync(string packPath, CancellationToken ct = default);
    Task<Result<CharacterPackInfo>> GetPackInfoAsync(string packSource, CancellationToken ct = default);
}

public class CharacterPackInfo
{
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public string Source { get; set; } // GitHub URL, file path, etc.
    public int CharacterCount { get; set; }
    public string License { get; set; }
    public bool IsInstalled { get; set; }
    public string InstallPath { get; set; }
}
```

**Step 3: Pack Installation & Extraction** (3-4 hours)
- Download/clone NooksVsCrans repository
- Extract characters to character directory
- Scan and catalog extracted characters
- Handle pack metadata

**Step 4: Documentation & Integration** (1-2 hours)
- Document NooksVsCrans as character pack option
- Add to character pack catalog
- Create installation guide
- Reference as example MUGEN setup

#### Evaluation Notes

**Value Assessment**:
- ✅ Complete MUGEN package with characters
- ✅ Reference implementation of MUGEN setup
- ✅ Character pack example
- ⚠️ Not a development tool (lower priority for modification workflows)
- ⚠️ May require compatibility testing with Ikemen GO

**Integration Approach**:
- Optional character pack (not critical path)
- Use as character library expansion
- Reference for MUGEN package structure
- Example for character pack management system

#### Files to Create/Modify

**New Files**:
- `src/SaveState.Core/Mugen/Services/ICharacterPackService.cs`
- `src/SaveState.Core/Mugen/Entities/CharacterPackInfo.cs`
- `src/SaveState.Infrastructure/Mugen/CharacterPackService.cs`
- `docs/features/character-packs.md` (if not exists)

**Modified Files**:
- `engines/setup-ikemen.ps1` (optional pack installation)
- Character scanning system (pack extraction support)

---

## Phase 3: Advanced Features & Polish (Weeks 11-12)

### 3.1 Character Editor UI Development

**Effort**: 25-30 hours  
**Priority**: HIGH

#### Objectives
- Create comprehensive character editor UI
- Integrate all character modification services
- Provide real-time preview
- Support all character file editing

#### Implementation

**Components to Create**:
1. Character Editor Main Window
2. File Editor (def, cns, cmd editing)
3. Sprite Viewer/Editor Panel
4. Movelist Editor
5. Mechanics Configuration Panel (Armor, MK mechanics)
6. Template Selector
7. Real-time Preview Panel

**Files to Create**:
- `src/SaveState.Presentation/Views/CharacterEditor/CharacterEditorView.axaml`
- `src/SaveState.Presentation/ViewModels/CharacterEditor/CharacterEditorViewModel.cs`
- `src/SaveState.Presentation/ViewModels/CharacterEditor/FileEditorViewModel.cs`
- `src/SaveState.Presentation/ViewModels/CharacterEditor/SpriteViewerViewModel.cs`
- `src/SaveState.Presentation/ViewModels/CharacterEditor/MovelistEditorViewModel.cs`

---

### 3.2 Integration Testing & Documentation

**Effort**: 15-20 hours  
**Priority**: HIGH

#### Objectives
- Comprehensive integration tests
- End-to-end workflow tests
- User documentation
- Developer documentation

#### Deliverables
- Integration test suite
- User guides for each feature
- API documentation
- Troubleshooting guides

---

## 📊 Implementation Timeline

| Phase | Duration | Key Deliverables | Dependencies |
|-------|----------|------------------|--------------|
| **Phase 1** | Weeks 1-6 | MugenHook, LuaSupernull, OpenMK | None |
| **Phase 2** | Weeks 7-10 | Armor, Movelist, Sprite Tools, Character Packs | Phase 1 |
| **Phase 3** | Weeks 11-12 | UI, Testing, Documentation | Phase 1, 2 |

**Total Duration**: 12 weeks  
**Total Effort**: 180-240 hours

---

## 🎯 Success Criteria

### Phase 1 Success
- ✅ MugenHook integrated and functional
- ✅ LuaSupernull templates available
- ✅ OpenMK toolkit operational
- ✅ MK game modes supported

### Phase 2 Success
- ✅ Armor system integrated
- ✅ Movelist extraction working
- ✅ Sprite viewing functional
- ✅ Character modification workflows complete
- ✅ Character pack management system functional (optional)

### Phase 3 Success
- ✅ Character editor UI complete
- ✅ All services integrated in UI
- ✅ Comprehensive documentation
- ✅ Integration tests passing

---

## 🔗 Dependencies

### External Dependencies
- MugenHook binaries/libraries
- LuaSupernull framework files
- OpenMK toolkit resources
- ikemenarmor ZSS script

### Internal Dependencies
- Existing `MugenCharacterLoader`
- Existing `MugenLauncher`
- Character database schema
- Configuration system

---

## 📝 Next Steps

1. **Review and Approve Plan** (1-2 days)
2. **Set Up Development Environment** (2-3 days)
   - Download and evaluate repositories
   - Set up test environments
   - Create feature branches

3. **Begin Phase 1 Implementation** (Week 1)
   - Start with MugenHook compatibility evaluation
   - Begin LuaSupernull framework integration

4. **Regular Progress Reviews** (Weekly)
   - Track implementation progress
   - Adjust timeline as needed
   - Address blockers

---

**This plan provides a comprehensive roadmap for integrating character development tools and frameworks into SaveStateReborn, enabling full MUGEN/Ikemen character modification and creation capabilities.**
