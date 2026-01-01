# IKEMEN Bundle - Complete Fighting Game Platform

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

## 📚 Resources

- [IKEMEN GO GitHub](https://github.com/K4thos/Ikemen_GO)
- [MUGEN Documentation](https://mugenarchive.com/docs/)
- [Character Creation Tutorials](https://mugenarchive.com/forums/)

---

**SaveState Reborn** makes IKEMEN accessible to everyone - no technical setup required! 🎮✨
