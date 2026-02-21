# Visual Resources Integration Plan

**Date**: January 2, 2026  
**Status**: 🚧 In Progress  
**Priority**: High (Screenpack), Medium (FX/Shaders)

---

## 🎯 Overview

Integration plan for three visual resource repositories into SaveStateReborn:
1. **Elecbyte Screenpack** (High Priority) - Essential UI resources
2. **ikemenroundendfx** (Medium Priority) - Round transition effects
3. **ikgo-shaders** (Medium Priority) - Visual shader effects

---

## 📦 Repository Details

### 1. Elecbyte Screenpack
**Repository**: [ikemen-engine/Ikemen_GO-Elecbyte-Screenpack](https://github.com/ikemen-engine/Ikemen_GO-Elecbyte-Screenpack)  
**License**: Creative Commons 3.0 Non-commercial  
**Priority**: 🔴 **HIGH - Essential for functionality**

**Contents**:
- `chars/` - Character resources
- `data/` - Data files, system configurations
- `font/` - Font files
- `sound/` - Sound effects
- `stages/` - Stage resources
- `video/` - Video files

**Integration Location**: `engines/ikemen/data/` (standard Ikemen data directory)

---

### 2. ikemenroundendfx
**Repository**: [kamekaze-world/ikemenroundendfx](https://github.com/kamekaze-world/ikemenroundendfx)  
**Version**: 1.2  
**Priority**: 🟡 **MEDIUM - Visual enhancement**

**Contents**:
- `ik_roundtransition.air` - Animation file
- `ik_roundtransition.def` - Definition file
- `ik_roundtransition.sff` - Sprite file
- `ik_roundtransition.snd` - Sound file
- `roundtransition.zss` - ZSS script (main file)

**Integration Location**: 
- FX files: `engines/ikemen/data/commonFX/` or `data/commonFX/`
- ZSS script: Referenced in `save/config.json` under `CommonFiles`

**Requirements**:
- Ikemen GO .99 or later
- Must be added to `CommonFiles` in config.json
- Must be added to `CommonStates` area
- `ik_roundtransition.def` must be in `commonFX` area

---

### 3. ikgo-shaders
**Repository**: [wily-coyote/ikgo-shaders](https://github.com/wily-coyote/ikgo-shaders)  
**Priority**: 🟢 **MEDIUM - Advanced customization**

**Contents**:
- `ntsc/` - NTSC shader preset
- `kapuesu/` - Japanese CvS2 style preset
- `powervr2/` - PowerVR2 shader preset
- `level/` - Brightness/level adjustment shader
- `border/` - Border cropping shader
- `scale/` - Scaling shader
- Individual `.frag` and `.vert` files

**Integration Location**: `engines/ikemen/external/shaders/` (Ikemen GO standard location)

**Requirements**:
- Ikemen GO Nightly builds with shader support
- Configured via Ikemen config/system settings
- User-selectable in UI

---

## 🏗️ Implementation Structure

```
engines/ikemen/
├── Ikemen_GO.exe
├── config.json (updated)
├── data/                          # Elecbyte Screenpack
│   ├── chars/
│   ├── font/
│   ├── sound/
│   ├── stages/
│   ├── video/
│   ├── commonFX/                  # Round transition FX
│   │   ├── ik_roundtransition.def
│   │   ├── ik_roundtransition.air
│   │   ├── ik_roundtransition.sff
│   │   └── ik_roundtransition.snd
│   └── (other screenpack files)
├── external/
│   └── shaders/                   # Shader collection
│       ├── ntsc/
│       ├── kapuesu/
│       ├── powervr2/
│       ├── level/
│       ├── border/
│       ├── scale/
│       └── (individual shader files)
└── save/
    └── config.json                # User config (references CommonFiles)
```

---

## 📋 Implementation Tasks

### Phase 1: Elecbyte Screenpack (High Priority)

#### Task 1.1: Update Setup Script
- [x] Add screenpack download/extraction logic
- [x] Create directory structure
- [x] Handle license attribution
- [x] Verify file integrity

#### Task 1.2: Directory Structure
- [x] Ensure `engines/ikemen/data/` exists
- [x] Extract screenpack to correct location
- [x] Preserve directory structure

#### Task 1.3: Configuration
- [x] Verify Ikemen uses default data directory
- [x] No config changes needed (standard location)

---

### Phase 2: Round Transition Effects (Medium Priority)

#### Task 2.1: File Integration
- [ ] Copy FX files to `data/commonFX/` or appropriate location
- [ ] Copy ZSS script to appropriate location
- [ ] Verify file paths

#### Task 2.2: Configuration Updates
- [ ] Update `engines/ikemen/config.json` to reference CommonFiles
- [ ] Update user `save/config.json` template
- [ ] Add roundtransition.zss to CommonFiles list
- [ ] Add ik_roundtransition to commonFX area

#### Task 2.3: Documentation
- [ ] Document transition time configuration
- [ ] Explain customization options
- [ ] Provide usage examples

---

### Phase 3: Shader Collection (Medium Priority)

#### Task 3.1: File Integration
- [ ] Create `external/shaders/` directory
- [ ] Copy all shader presets
- [ ] Preserve directory structure

#### Task 3.2: Configuration Options
- [ ] Add shader selection to config.json
- [ ] Create UI options (if applicable)
- [ ] Document shader usage

#### Task 3.3: User Interface (Future)
- [ ] Shader selection dropdown
- [ ] Preview functionality
- [ ] Preset descriptions

---

## 🔧 Configuration Updates

### engines/ikemen/config.json

```json
{
  "name": "IKEMEN GO",
  "version": "0.99",
  "executable": "Ikemen_GO.exe",
  "dataDirectory": "../../../data",
  "screenpack": {
    "enabled": true,
    "directory": "data",
    "type": "Elecbyte"
  },
  "visualEffects": {
    "roundTransitions": {
      "enabled": true,
      "zssFile": "roundtransition.zss",
      "commonFX": "ik_roundtransition"
    },
    "shaders": {
      "enabled": true,
      "directory": "external/shaders",
      "default": "ntsc",
      "presets": ["ntsc", "kapuesu", "powervr2", "level", "border", "scale"]
    }
  },
  "arguments": {
    "versus": "-p1 {player1} -p2 {player2} -rounds 3",
    "training": "-p1 {player1} -p2 {dummy} -training",
    "watch": "-p1 {player1} -p2 {player2} -watch",
    "single": "-p1 {player1} -single"
  },
  "characterDirectories": [
    "../../../data/characters/streetfighter",
    "../../../data/characters/mvc2",
    "../../../data/characters/builtin"
  ],
  "features": {
    "luaScripting": true,
    "mugenCompatibility": true,
    "onlinePlay": false,
    "replayRecording": true,
    "trainingMode": true,
    "visualEffects": true,
    "shaderSupport": true
  }
}
```

### save/config.json (User Config Template)

```json
{
  "CommonFiles": [
    "roundtransition.zss"
  ],
  "CommonStates": [
    "...",
    "ik_roundtransition"
  ],
  "commonFX": [
    "...",
    "ik_roundtransition.def"
  ]
}
```

---

## 📝 Setup Script Updates

### New Functions Required

1. **Download-ElecbyteScreenpack**
   - Download from GitHub release/tag
   - Extract to `engines/ikemen/data/`
   - Verify file structure
   - Handle license file

2. **Download-RoundTransitionFX**
   - Download from GitHub
   - Extract FX files to `data/commonFX/`
   - Extract ZSS to appropriate location
   - Update config references

3. **Download-ShaderCollection**
   - Download from GitHub
   - Extract to `external/shaders/`
   - Verify shader files
   - Set default shader

---

## 🎨 User Experience

### Setup Flow

1. User runs `setup-ikemen.ps1`
2. Script downloads and extracts:
   - Elecbyte Screenpack (essential)
   - Round transition effects (optional, enabled by default)
   - Shader collection (optional, enabled by default)
3. Configuration files updated automatically
4. User can customize in UI or config files

### Configuration Options

- **Screenpack**: Enabled by default, no user action needed
- **Round Transitions**: Enabled by default, customizable transition time
- **Shaders**: User-selectable preset, default "ntsc"

---

## 🔍 Testing Checklist

### Screenpack
- [ ] Ikemen starts without errors
- [ ] UI displays correctly
- [ ] Fonts render properly
- [ ] Sound effects work
- [ ] Stages load correctly

### Round Transitions
- [ ] Round transitions display between rounds
- [ ] Custom transition time works
- [ ] ZSS script loads without errors
- [ ] FX files found by Ikemen

### Shaders
- [ ] Shader directory recognized
- [ ] Default shader applies
- [ ] Shader switching works (if implemented in UI)
- [ ] No performance degradation

---

## 📚 Documentation Updates

### Files to Update

1. `engines/ikemen/README.md`
   - Screenpack information
   - Visual effects documentation
   - Shader usage guide

2. `docs/features/ikemen_integration.md`
   - Visual resources section
   - Configuration options
   - Troubleshooting

3. `docs/planning/ikemen_repositories_analysis.md`
   - Mark implementations as complete

---

## 🚀 Next Steps

1. **Immediate**: Update setup script with screenpack download
2. **Short-term**: Integrate round transition effects
3. **Short-term**: Integrate shader collection
4. **Future**: UI for shader selection and preview
5. **Future**: Advanced transition customization UI

---

## 📄 License Considerations

- **Elecbyte Screenpack**: CC 3.0 Non-commercial - Include license file, provide attribution
- **ikemenroundendfx**: Check repository license
- **ikgo-shaders**: Check repository license

All licenses must be included in distribution and documented.
