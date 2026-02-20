# Steam Deck Setup Guide

## Overview
SaveStateReborn works great on Steam Deck! This guide covers installation, controls, and Memory Intelligence setup.

## Installation Methods

### Method 1: Flatpak (Recommended)

```bash
# Add Flathub if not already added
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo

# Install SaveStateReborn (when available)
flatpak install flathub com.savestatereborn.SaveStateReborn
```

### Method 2: Native Build

```bash
# Switch to Desktop Mode (Steam button → Power → Switch to Desktop)

# Open Konsole (terminal)
# Install dotnet
sudo steamos-readonly disable
sudo pacman -S dotnet-sdk

# Build SaveStateReborn
git clone https://github.com/DocDamage/Save_State-Gaming-Hub.git
cd Save_State-Gaming-Hub
dotnet build src/SaveState.Presentation -c Release

# Create desktop shortcut
cp assets/savestate.desktop ~/Desktop/
```

### Method 3: AppImage (When Available)

Download the AppImage and run:
```bash
chmod +x SaveStateReborn-x86_64.AppImage
./SaveStateReborn-x86_64.AppImage
```

## Controls

### Gamepad Support
SaveStateReborn supports Steam Deck controls:
- **Left Stick**: Navigate UI
- **A Button**: Select/Confirm
- **B Button**: Back/Cancel
- **X Button**: Quick Action (context-dependent)
- **Y Button**: Toggle View
- **Steam Button**: Open SaveStateReborn overlay (when configured)

### Touch Controls
- Full touch support in UI
- Pinch to zoom in memory hex view
- Swipe for tab navigation

### Steam Input Configuration

Add to Steam as non-Steam game:
1. Desktop Mode → Steam → Games → Add a Non-Steam Game
2. Browse to SaveStateReborn executable
3. Right-click → Properties → Controller
4. Set to "Gamepad with Joystick Trackpad"

## Memory Intelligence on Steam Deck

### Prerequisites

Steam Deck runs Arch Linux with SteamOS. For memory reading:

```bash
# Disable read-only mode
sudo steamos-readonly disable

# Enable ptrace
sudo sysctl kernel.yama.ptrace_scope=0

# Make permanent
echo "kernel.yama.ptrace_scope=0" | sudo tee /etc/sysctl.d/10-ptrace.conf
```

### Attaching to Games

1. Launch game through Steam
2. Switch to Desktop Mode (or use Steam Overlay)
3. Open SaveStateReborn
4. Use "Attach to Process" 
5. Select game process (may be `steam` or the game executable)

### Proton/Wine Games

Most Steam games use Proton:
- Attach to the Windows executable, not wineserver
- Look for process name matching the game
- Memory patterns are the same as Windows

### Native Linux Games

Steam Deck supports native Linux games:
- Factorio, Terraria, Stardew Valley, etc.
- Better memory performance
- Direct ptrace access

## Performance Tips

### Battery Optimization

```bash
# Limit frame rate to 30fps in desktop mode
export AVALONIA_RENDERER=vulkan
export AVALONIA_VSYNC=1
```

### Storage

Steam Deck has limited storage:
- Database is ~170KB (336 games)
- Signatures are cached
- Consider external SD card for large game libraries

### Sleep Mode

SaveStateReborn handles Steam Deck sleep:
- Detaches from processes on sleep
- Re-attaches on wake (if process still exists)
- Saves scan results to resume later

## Big Picture Mode

SaveStateReborn includes a Big Picture mode optimized for TV/Steam Deck:
- Larger UI elements
- Controller-focused navigation
- 10-foot interface design

Enable with: `--big-picture` flag

## Troubleshooting

### "Read-only file system" Error
Steam Deck filesystem is read-only by default:
```bash
sudo steamos-readonly disable
```

### Game Not Appearing in Process List
Steam games run in containers:
```bash
# List all processes including Steam containers
ps aux | grep -i game_name

# Or use pgrep
pgrep -a game_name
```

### Controller Not Working
1. Ensure Steam Input is configured
2. Try different controller templates in Steam
3. Check Desktop Mode controller settings

### Low Performance
Steam Deck has power profiles:
- Plugged in: Better performance
- Battery: Limited TDP
- Set TDP to 15W for best balance

## Known Working Games

### Verified on Steam Deck
- Celeste ✅
- Hollow Knight ✅
- Hades ✅
- Stardew Valley ✅
- Dead Cells ✅
- Slay the Spire ✅
- Risk of Rain 2 ✅

See [MEMORY_INTELLIGENCE.md](MEMORY_INTELLIGENCE.md) for full game list.

## Community

- r/SteamDeck - Steam Deck community
- r/SaveStateReborn - Project subreddit
- Discord: [link]

## Updates

Steam Deck updates may reset system changes:
- Re-run ptrace enable command after major updates
- Keep installation script handy for re-installation
