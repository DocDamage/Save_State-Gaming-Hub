# SaveState Reborn: User Manual
## Getting Started Guide

**Document ID:** SS-UM-001  
**Revision:** 1.0  
**Date:** 2024-12-20

---

## 1. Installation

### Windows
1. Download `SaveState-win-x64.zip` from [Releases](https://github.com/)
2. Extract to desired location
3. Run `SaveState.exe`

### Linux
```bash
flatpak install flathub com.savestate.app
```

### macOS
```bash
brew install --cask savestate
```

---

## 2. First Launch

1. **Welcome Screen** - Select your game stores
2. **Library Scan** - SaveState detects installed games
3. **Metadata Fetch** - Artwork and info downloaded

---

## 3. Managing Your Library

### Adding Games

| Source | Method |
|--------|--------|
| Steam, GOG, Epic, etc. | Automatic (if enabled) |
| ROMs | File → Add ROM Folder |
| Manual | Right-click → Add Game |

### Launching Games

- **Double-click** any game to launch
- **Right-click** for options (uninstall, properties)

### Organizing

- **Filters** - By platform, store, genre
- **Sort** - Name, last played, playtime
- **Collections** - Create custom groups

---

## 4. ROM Management

### Supported Platforms
- NES, SNES, N64, GameCube, Wii
- PlayStation 1/2/3/P, PSP, Vita
- Sega Genesis, Saturn, Dreamcast
- Game Boy, GBA, DS, 3DS
- And more...

### Adding ROMs
1. Go to **Settings → ROMs**
2. Add folder path
3. SaveState scans and matches games

### BIOS Files
Some emulators require BIOS files:
1. **Settings → Emulation → BIOS**
2. Place files in indicated folder
3. SaveState validates checksums

---

## 5. Customization

### Themes
- **Dark** (default)
- **Light**
- **System** (follows OS)

### Grid Size
- Small, Medium, Large
- List view available

### Metadata Sources
- IGDB (default)
- SteamGridDB (artwork)
- Manual entry

---

## 6. Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Search | `Ctrl+F` |
| Settings | `Ctrl+,` |
| Add Game | `Ctrl+N` |
| Refresh Library | `F5` |
| Fullscreen | `F11` |
| Quit | `Alt+F4` |

---

## 7. FAQ

**Q: Why aren't my Steam games showing?**  
A: Ensure Steam is running and you're logged in.

**Q: How do I change emulator settings?**  
A: Settings → Emulation → Select platform → Configure

**Q: Where is my data stored?**  
A: `%APPDATA%\SaveState` (Windows) or `~/.config/SaveState` (Linux)

**Q: Can I sync across computers?**  
A: Database can be backed up/restored manually. Cloud sync planned.
