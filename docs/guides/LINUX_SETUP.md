# Linux Setup Guide

## Overview
SaveStateReborn supports Linux through the Avalonia UI framework. Memory Intelligence features work on Linux using ptrace and /proc filesystem.

## Prerequisites

### Required Packages

#### Ubuntu/Debian
```bash
sudo apt-get update
sudo apt-get install -y \
    dotnet-sdk-9.0 \
    libgtk-3-dev \
    libssl-dev \
    libicu-dev
```

#### Fedora
```bash
sudo dnf install -y \
    dotnet-sdk-9.0 \
    gtk3-devel \
    openssl-devel \
    libicu-devel
```

#### Arch Linux
```bash
sudo pacman -S \
    dotnet-sdk \
    gtk3 \
    openssl \
    icu
```

### Permissions for Memory Reading

To read game memory on Linux, you need appropriate permissions:

#### Option 1: Run with sudo (Not Recommended for Daily Use)
```bash
sudo ./SaveStateReborn
```

#### Option 2: Add CAP_SYS_PTRACE Capability (Recommended)
```bash
sudo setcap cap_sys_ptrace=eip ./SaveStateReborn
```

#### Option 3: Add User to Game Group
Some distributions have a 'games' group:
```bash
sudo usermod -a -G games $USER
# Log out and back in
```

## Installation

### Method 1: Build from Source

```bash
# Clone repository
git clone https://github.com/DocDamage/Save_State-Gaming-Hub.git
cd Save_State-Gaming-Hub

# Build
dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj -c Release

# Run
dotnet run --project src/SaveState.Presentation -c Release
```

### Method 2: Using Install Script

```bash
curl -sSL https://raw.githubusercontent.com/DocDamage/Save_State-Gaming-Hub/main/scripts/install-linux.sh | bash
```

## Memory Intelligence on Linux

### Supported Features
- ✅ Process attachment via ptrace
- ✅ Memory scanning and pattern detection
- ✅ Reading game values (health, currency, etc.)
- ⚠️ Memory writing (requires CAP_SYS_PTRACE)
- ❌ Value freezing (not yet implemented)

### Linux-Specific Considerations

#### Wine/Proton Games
Wine and Proton run Windows games on Linux. Memory patterns may differ:
- Process name will be `wine-preloader` or similar
- Look for the actual game executable in process maps
- Memory signatures may need Wine-specific variants

#### Native Linux Games
Native games (Unity, Godot, etc.) have direct memory access:
- Process name matches executable
- Standard memory patterns work
- Better performance than Wine

#### Steam Runtime
Steam games run in a container:
- May need to attach to steam runtime processes
- Check `/proc/{pid}/maps` for game-specific regions

### Troubleshooting

#### "Permission Denied" on Memory Read
```bash
# Check if you have ptrace permissions
cat /proc/sys/kernel/yama/ptrace_scope
# 0 = allowed, 1 = restricted, 2 = admin-only, 3 = no ptrace

# Temporarily allow (until reboot)
sudo sysctl kernel.yama.ptrace_scope=0

# Permanently allow
echo "kernel.yama.ptrace_scope=0" | sudo tee -a /etc/sysctl.d/10-ptrace.conf
```

#### "Process Not Found"
- Ensure game is running
- Check correct process ID with `pgrep game_name`
- For Wine games, attach to the Windows process, not wine-preloader

#### Game Crashes When Attached
Some games have anti-cheat that detects ptrace:
- Single-player games usually work fine
- Multiplayer games may crash or ban
- Check game forums for compatibility

## Desktop Environment Integration

### KDE Plasma
SaveStateReborn integrates with KDE's system tray:
```bash
# Install dependencies
sudo apt-get install kde-config-gtk-style
```

### GNOME
For GNOME shell integration:
```bash
sudo apt-get install gnome-shell-extension-appindicator
```

## Known Issues

1. **Wayland**: Some features may work better on X11
   - Set `AVALONIA_SCREEN_SCALE_FACTORS` for HiDPI
   - Use XWayland if experiencing issues

2. **NVIDIA Proprietary Drivers**: 
   - May require `__GL_THREADED_OPTIMIZATIONS=0`

3. **AMDGPU**: Generally works best on open-source drivers
