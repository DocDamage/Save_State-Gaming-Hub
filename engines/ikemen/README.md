# IKEMEN Engine

This directory contains the IKEMEN fighting game engine, bundled with SaveState Reborn.

## What's Included

- **IKEMEN executable** (`Ikemen_GO.exe` or `Ikemen_GO`)
- **Engine configuration** files
- **Default data** and resources

## Character Packs

The following character packs are included:

- **Street Fighter**: Classic Street Fighter series characters
- **MVC2**: Marvel vs Capcom 2 characters
- **Builtin**: Additional characters and stages

## Usage

IKEMEN is automatically integrated with SaveState's character management system. Characters are loaded from:

- `data/characters/streetfighter/`
- `data/characters/mvc2/`
- `data/characters/builtin/`

## Launch Integration

SaveState can launch IKEMEN with specific character selections for training, versus mode, etc.

## Version

- IKEMEN GO v0.99 (latest stable)
- Includes Lua scripting support
- Full MUGEN compatibility mode

## Visual Resources

SaveState Reborn includes comprehensive visual resources for IKEMEN:

### Elecbyte Screenpack
- **Location**: `engines/ikemen/data/`
- **Purpose**: Default UI, fonts, stages, and system resources
- **Status**: ✅ Included and configured
- **License**: Creative Commons 3.0 Non-commercial

### Round Transition Effects
- **Location**: `engines/ikemen/data/commonFX/` (FX files)
- **ZSS Script**: Referenced in `save/config.json` CommonFiles
- **Purpose**: Custom round start/end transition animations
- **Status**: ✅ Included and configured
- **Customization**: Transition time configurable (default: 80 frames)

### Shader Collection
- **Location**: `engines/ikemen/external/shaders/`
- **Presets**: NTSC, Kapuesu, PowerVR2, Level, Border, Scale
- **Purpose**: Visual shader effects for retro/arcade aesthetics
- **Status**: ✅ Included and configured
- **Default**: NTSC shader preset

All visual resources are automatically configured during setup.
