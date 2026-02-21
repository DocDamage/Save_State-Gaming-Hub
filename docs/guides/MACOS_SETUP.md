# macOS Setup Guide

## Overview
SaveStateReborn supports macOS through Avalonia UI and Mach kernel APIs for memory reading.

## Prerequisites

### System Requirements
- macOS 11.0 (Big Sur) or later
- .NET 9.0 SDK
- Xcode Command Line Tools (for native dependencies)

### Installing Prerequisites

```bash
# Install Homebrew (if not already installed)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install .NET SDK
brew install dotnet

# Install Xcode Command Line Tools
xcode-select --install
```

## Installation

### Method 1: Build from Source

```bash
# Clone repository
git clone https://github.com/DocDamage/Save_State-Gaming-Hub.git
cd Save_State-Gaming-Hub

# Build
dotnet build src/SaveState.Presentation -c Release

# Run
dotnet run --project src/SaveState.Presentation -c Release
```

### Method 2: Using Install Script

```bash
curl -sSL https://raw.githubusercontent.com/DocDamage/Save_State-Gaming-Hub/main/scripts/install-macos.sh | bash
```

## Code Signing & Entitlements

### IMPORTANT: Memory Access Requirements

macOS has strict security policies for memory access. To read game memory, SaveStateReborn needs special entitlements.

### Option 1: Run with sudo (Development Only)

```bash
sudo dotnet run --project src/SaveState.Presentation
```

### Option 2: Code Sign with Entitlements (Recommended)

The entitlements file is located at `assets/macos/entitlements.plist`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.debugger</key>
    <true/>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
    <key>com.apple.security.cs.disable-executable-page-protection</key>
    <true/>
</dict>
</plist>
```

Sign the application:

```bash
# Sign with entitlements
codesign --force --deep --sign - \
    --entitlements assets/macos/entitlements.plist \
    bin/Release/net9.0/osx-arm64/SaveStateReborn.app

# Or for x64:
codesign --force --deep --sign - \
    --entitlements assets/macos/entitlements.plist \
    bin/Release/net9.0/osx-x64/SaveStateReborn.app
```

### Option 3: Disable System Integrity Protection (NOT RECOMMENDED)

⚠️ **WARNING**: This reduces system security. Only for advanced users.

```bash
# Boot to Recovery (Cmd+R during startup)
# Open Terminal and run:
csrutil disable
# Restart
```

## Memory Intelligence on macOS

### Supported Features
- ✅ Process attachment via `task_for_pid()`
- ✅ Memory reading via `vm_read()`
- ✅ Memory region enumeration
- ✅ Module base address detection
- ⚠️ Memory writing (requires additional permissions)
- ❌ Value freezing (not yet implemented)

### macOS-Specific Considerations

#### System Integrity Protection (SIP)
SIP protects system processes. You can only attach to:
- User applications
- Games launched by the user
- Not system processes or protected apps

#### Hardened Runtime
Modern macOS apps use Hardened Runtime. Games with this enabled may:
- Block memory reading attempts
- Require specific entitlements
- Crash when attached

#### Universal Binaries
macOS games may be universal (Intel + Apple Silicon):
- Process architecture matches the running code
- Memory layouts may differ between architectures
- Check Activity Monitor for architecture info

### Troubleshooting

#### "task_for_pid failed" Error
This means the app doesn't have permission to read process memory:

1. **Try with sudo:**
   ```bash
   sudo dotnet run --project src/SaveState.Presentation
   ```

2. **Check entitlements:**
   ```bash
   codesign -d --entitlements - /path/to/SaveStateReborn
   ```

3. **Approve in Security settings:**
   - System Preferences > Security & Privacy > Privacy > Developer Tools
   - Add your terminal/IDE

#### "KERN_INVALID_TASK" Error
The target process may be protected:
- System processes cannot be attached
- Some games use anti-cheat
- Try attaching to a different game

#### Game Crashes When Attached
Some games detect and prevent debugging:
- Single-player games usually work
- Multiplayer games may crash
- Check game forums for compatibility

#### "Architecture mismatch" on Apple Silicon
If running on M1/M2/M3 Mac:
```bash
# Check if game is native or Rosetta
ps aux | grep game_name

# Run SaveStateReborn under same architecture
arch -x86_64 dotnet run --project src/SaveState.Presentation
# or
arch -arm64 dotnet run --project src/SaveState.Presentation
```

## Building Universal Binary

To support both Intel and Apple Silicon:

```bash
# Build for both architectures
dotnet publish src/SaveState.Presentation \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -p:PublishSingleFile=true

dotnet publish src/SaveState.Presentation \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true

# Create universal binary
mkdir -p bin/Release/net9.0/osx-universal
lipo -create \
    bin/Release/net9.0/osx-x64/publish/SaveStateReborn \
    bin/Release/net9.0/osx-arm64/publish/SaveStateReborn \
    -output bin/Release/net9.0/osx-universal/SaveStateReborn
```

## Known Issues

1. **Notarization**: If distributing, the app needs to be notarized by Apple
2. **Gatekeeper**: Users may need to right-click > Open the first time
3. **Rosetta**: Intel games on Apple Silicon may have different memory layouts

## Platform Support Status

| Feature | Intel Mac | Apple Silicon | Rosetta |
|---------|-----------|---------------|---------|
| Memory Reading | ✅ | ✅ | ✅ |
| Memory Writing | ⚠️ | ⚠️ | ⚠️ |
| Freeze Values | ❌ | ❌ | ❌ |
| Auto-Discovery | ✅ | ✅ | ✅ |
| Pattern Scanning | ✅ | ✅ | ✅ |

## Mach API Implementation Details

The macOS implementation uses the following Mach kernel APIs:

### `task_for_pid()`
Gets the Mach task port for a process, required for all memory operations.

### `vm_read()`
Reads memory from the target process into a local buffer.

### `vm_region_recurse_64()`
Enumerates memory regions to find valid memory ranges for scanning.

### Permissions Required
- `com.apple.security.cs.debugger` - Required for `task_for_pid()`
- `com.apple.security.cs.allow-jit` - Required for .NET runtime
- `com.apple.security.cs.allow-unsigned-executable-memory` - Required for JIT compilation

## Getting Help

- Check [GitHub Issues](https://github.com/DocDamage/Save_State-Gaming-Hub/issues)
- macOS-specific issues may require code signing expertise
- Some games simply cannot be attached due to anti-cheat
