# IKEMEN Bundle - Complete Fighting Game Platform

**Status**: ✅ Implemented
**Last Updated**: January 3, 2026
**Layer**: Infrastructure + Presentation
**Related**: [Character API](character-api.md), [MUGEN Plugins](mugen-plugins.md), [Character Development Integration Plan](../planning/character_development_integration_plan.md)

---

SaveState Reborn includes a complete IKEMEN fighting game bundle with pre-configured character packs and launch integration.

## 🎮 What's Included

### Engine

- **IKEMEN GO v0.99** - Latest stable version with Lua scripting support
- **MUGEN Compatibility Mode** - Plays traditional MUGEN characters
- **Training Mode** - Practice with customizable dummies
- **Versus Mode** - Player vs Player matches
- **Watch Mode** - Observe AI matches

### Character Packs

#### Street Fighter Series

- Ryu, Ken, Chun-Li, Guile, Zangief
- Blanka, Dhalsim, Honda, Sagat, Balrog
- Vega, M. Bison, Akuma, Cammy, Fei Long
- And many more classic characters

#### Marvel vs Capcom 2

- Ryu, Ken, Mega Man, Roll, Tron Bonne
- Morrigan, Felicia, Hsien-Ko, Anakaris
- Captain Commando, Jin Saotome, Sonson
- All MVC2 roster with accurate movesets

#### Built-in Characters

- KFM (Kung Fu Man) - Training dummy
- Common MUGEN characters
- Tutorial characters

## 🚀 Quick Start

1. **Setup IKEMEN**:

   ```powershell
   .\engines\setup-ikemen.ps1
   ```

2. **Scan Characters** (via SaveState UI or API):
   - Launches automatically scan all bundled characters
   - Characters appear in SaveState library

3. **Launch Games**:
   - Select characters in SaveState
   - Choose game mode (Versus, Training, etc.)
   - SaveState launches IKEMEN with your selection

## 📁 Directory Structure

```
SaveStateReborn/
├── engines/
│   └── ikemen/           # IKEMEN executable and config
├── data/
│   ├── characters/       # Character packs
│   │   ├── streetfighter/
│   │   ├── mvc2/
│   │   └── builtin/
│   ├── stages/          # Fighting arenas
│   └── music/           # Background music
└── src/                 # SaveState application
```

## 🎯 Game Modes

### Versus Mode

- Player vs Player matches
- Configurable round counts
- All standard fighting game rules

### Training Mode

- Practice individual moves/techs
- Customizable dummy AI
- Combo trials and tutorials

### Single Player

- Story mode (character-specific)
- Survival mode
- Time attack challenges

## 🛠️ Configuration

IKEMEN settings are in `engines/ikemen/config.json`:

```json
{
  "executable": "Ikemen_GO.exe",
  "arguments": {
    "versus": "-p1 {player1} -p2 {player2} -rounds 3",
    "training": "-p1 {player1} -p2 {dummy} -training"
  },
  "characterDirectories": [
    "../../../data/characters/streetfighter",
    "../../../data/characters/mvc2",
    "../../../data/characters/builtin"
  ]
}
```

## 🔧 Technical Details

- **Platform**: Windows (64-bit)
- **Requirements**: DirectX 9.0c or later
- **File Format**: MUGEN .def files with SFF sprites
- **Scripting**: Lua integration for advanced features
- **Save States**: Full save/load functionality

## 🎨 Customization

### Adding Characters

1. Place character folder in `data/characters/`
2. Run character scan in SaveState
3. Character appears in library

### Modding

- Edit character .def files
- Modify Lua scripts
- Create custom stages
- Add background music

## 🐛 Troubleshooting

### IKEMEN Won't Start

- Ensure `Ikemen_GO.exe` is in `engines/ikemen/`
- Check antivirus isn't blocking the executable
- Verify character files are not corrupted

### Characters Not Loading

- Run character scan in SaveState
- Check .def file paths in character folders
- Ensure required sprite/sound files exist

### Performance Issues

- Lower resolution in IKEMEN settings
- Close other applications
- Update graphics drivers

## 🎨 Visual Resources

SaveState Reborn includes comprehensive visual resources:

### Elecbyte Screenpack
- Default UI, fonts, stages, and system resources
- Located in `engines/ikemen/data/`
- Automatically configured during setup
- License: Creative Commons 3.0 Non-commercial

### Round Transition Effects
- Custom round start/end transition animations
- Based on [ikemenroundendfx](https://github.com/kamekaze-world/ikemenroundendfx)
- Configurable transition time (default: 80 frames)
- Located in `engines/ikemen/data/commonFX/`

### Shader Collection
- Visual shader effects for retro/arcade aesthetics
- Based on [ikgo-shaders](https://github.com/wily-coyote/ikgo-shaders)
- Presets: NTSC, Kapuesu, PowerVR2, Level, Border, Scale
- Located in `engines/ikemen/external/shaders/`
- Default preset: NTSC

All visual resources are automatically configured and ready to use.

## 📚 Resources

- [IKEMEN GO GitHub](https://github.com/ikemen-engine/Ikemen_GO)
- [Elecbyte Screenpack](https://github.com/ikemen-engine/Ikemen_GO-Elecbyte-Screenpack)
- [Round Transition FX](https://github.com/kamekaze-world/ikemenroundendfx)
- [Shader Collection](https://github.com/wily-coyote/ikgo-shaders)
- [MUGEN Documentation](https://mugenarchive.com/docs/)
- [Character Creation Tutorials](https://mugenarchive.com/forums/)

---

**Related Documentation**:

- [Character API](character-api.md) - Character management
- [MUGEN Plugins](mugen-plugins.md) - Advanced features
- [Character Development Integration Plan](../planning/character_development_integration_plan.md) - Character development tools (MugenHook, LuaSupernull, OpenMK, etc.)
- [IKEMEN Repository Analysis](../planning/ikemen_repositories_analysis.md) - Repository evaluation and integration strategies
- [AI_MASTER_CONTEXT](../AI_MASTER_CONTEXT.md) - Architecture overview

**SaveState Reborn** makes IKEMEN accessible to everyone - no technical setup required! 🎮✨
