# RetroArch Emulator Integration Guide

**Last Updated**: January 13, 2026  
**Status**: ✅ Production Ready  
**Phase**: 2 - Emulator Integration

---

## 📖 Overview

Save State Reborn now includes full integration with RetroArch emulator via its network command interface. This allows real-time save state creation, loading, and screenshot capture directly from running games without requiring file system monitoring or process injection.

### Key Features

- ✅ Real-time save state creation via network commands
- ✅ Save state loading from files or memory slots
- ✅ Screenshot capture from running games
- ✅ Process detection and health checking
- ✅ Automatic fallback when emulator not running
- ✅ Comprehensive error handling and logging
- ✅ Configurable network interface settings

---

## 🚀 Quick Start

### 1. Configure RetroArch

Add to your `appsettings.json`:

```json
{
  "RetroArch": {
    "InstallPath": "C:\\RetroArch\\retroarch.exe",
    "AutoDetect": true,
    "NetworkCommandEnabled": true,
    "NetworkCommandPort": 55355,
    "NetworkCommandHost": "127.0.0.1",
    "NetworkCommandTimeout": 5000
  }
}
```

### 2. Launch RetroArch with Network Commands

**Important**: RetroArch must be started with the network command interface enabled:

```bash
retroarch.exe --network-cmd-enable
```

Or add to your RetroArch `retroarch.cfg`:

```ini
network_cmd_enable = "true"
network_cmd_port = 55355
```

### 3. Use Save State Features

Once RetroArch is running with network commands enabled, Save State Reborn will automatically:

- Create save states by communicating with the emulator
- Capture real screenshots from running games
- Load save states directly into the emulator
- Detect when RetroArch is no longer running

---

## 🔧 Configuration Options

### RetroArchOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `InstallPath` | string | "" | Path to retroarch.exe (auto-detected if empty) |
| `AutoDetect` | bool | true | Auto-detect RetroArch installation |
| `NetworkCommandEnabled` | bool | true | Enable network command interface integration |
| `NetworkCommandPort` | int | 55355 | TCP port for network commands (RetroArch default) |
| `NetworkCommandHost` | string | "127.0.0.1" | Host address for network commands |
| `NetworkCommandTimeout` | int | 5000 | Command timeout in milliseconds |

### Environment Variables

No environment variables are required for emulator integration. All configuration is in `appsettings.json`.

---

## 📡 Network Command Protocol

### Technical Details

Save State Reborn communicates with RetroArch via TCP sockets using the RetroArch network command protocol:

**Connection**: TCP on `127.0.0.1:55355` (configurable)  
**Protocol**: Text-based commands terminated with `\n`  
**Encoding**: UTF-8

### Supported Commands

| Command | Description | Implementation |
|---------|-------------|----------------|
| `SAVE_STATE` | Create save state | `CreateSaveStateAsync()` |
| `LOAD_STATE` | Load save state from slot | `LoadSaveStateAsync()` |
| `LOAD_STATE "path"` | Load save state from file | `LoadSaveStateFromFileAsync()` |
| `SCREENSHOT` | Capture screenshot | `CaptureScreenshotAsync()` |
| `VERSION` | Get RetroArch version | `IsRunningAsync()` (health check) |
| `SAVE_STATE_SLOT N` | Set save state slot | Used with SAVE_STATE/LOAD_STATE |

### Example Flow

```csharp
// Check if RetroArch is running
var isRunningResult = await retroArchService.IsRunningAsync();
if (isRunningResult.Value)
{
    // Create a save state
    var saveResult = await retroArchService.CreateSaveStateAsync();
    
    // Capture a screenshot
    var screenshotResult = await retroArchService.CaptureScreenshotAsync();
}
```

---

## 🎮 Usage Examples

### Creating a Save State

```csharp
// Inject IRetroArchService
public class MyGameService
{
    private readonly IRetroArchService _retroArchService;
    
    public MyGameService(IRetroArchService retroArchService)
    {
        _retroArchService = retroArchService;
    }
    
    public async Task CreateQuickSaveAsync()
    {
        // Create save state in auto slot
        var result = await _retroArchService.CreateSaveStateAsync();
        
        if (result.IsSuccess)
        {
            Console.WriteLine($"Save state created: {result.Value}");
        }
        else
        {
            Console.WriteLine($"Failed: {result.Error}");
        }
    }
}
```

### Loading a Save State

```csharp
public async Task LoadQuickSaveAsync()
{
    // Load from specific slot (0-9)
    var result = await _retroArchService.LoadSaveStateAsync(slot: 0);
    
    if (result.IsSuccess)
    {
        Console.WriteLine("Save state loaded successfully");
    }
}

public async Task LoadFromFileAsync(string filePath)
{
    // Load from specific file
    var result = await _retroArchService.LoadSaveStateFromFileAsync(filePath);
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"Loaded save state from: {filePath}");
    }
}
```

### Capturing Screenshots

```csharp
public async Task<string?> CaptureGameScreenshotAsync()
{
    var result = await _retroArchService.CaptureScreenshotAsync();
    
    if (result.IsSuccess)
    {
        return result.Value; // Returns path to screenshot file
    }
    
    return null;
}
```

### Checking RetroArch Status

```csharp
public async Task<bool> IsEmulatorRunningAsync()
{
    var result = await _retroArchService.IsRunningAsync();
    return result.IsSuccess && result.Value;
}
```

---

## 🔍 Fallback Behavior

### When RetroArch is Not Running

The `SaveStateManager` includes intelligent fallback behavior:

1. **Save State Creation**:
   - Checks if RetroArch is running
   - If yes: Uses network commands
   - If no: Creates metadata file for later processing
   - Logs appropriate warnings

2. **Save State Loading**:
   - Checks if RetroArch is running
   - If yes: Loads via network commands
   - If no: Returns user-friendly error message
   - Suggests launching game first

3. **Screenshot Capture**:
   - Attempts RetroArch capture
   - Falls back to placeholder thumbnail
   - Ensures operations never fail completely

### Example Log Output

```
[2026-01-13 10:30:45] Information: Creating save state via RetroArch network command interface
[2026-01-13 10:30:45] Information: Save state created via RetroArch: 524288 bytes
[2026-01-13 10:30:46] Information: Screenshot captured: C:\RetroArch\screenshots\game_20260113_103045.png
```

---

## 🐛 Troubleshooting

### "RetroArch is not currently running"

**Problem**: Network commands fail because RetroArch isn't running or network interface isn't enabled.

**Solutions**:
1. Ensure RetroArch is running
2. Start RetroArch with `--network-cmd-enable` flag
3. Check `retroarch.cfg` has `network_cmd_enable = "true"`
4. Verify firewall isn't blocking port 55355

### "Connection to RetroArch timed out"

**Problem**: Cannot connect to RetroArch network interface.

**Solutions**:
1. Check if RetroArch is actually running (Task Manager)
2. Verify port 55355 is not used by another application
3. Increase `NetworkCommandTimeout` in configuration
4. Check RetroArch logs for network command initialization

### Save States Not Creating

**Problem**: `CreateSaveStateAsync()` returns success but file not found.

**Solutions**:
1. Check RetroArch save state directory configuration
2. Ensure disk has sufficient space
3. Verify user has write permissions
4. Check RetroArch logs for save errors

### Screenshots Are Placeholders

**Problem**: Screenshots are 1x1 pixel placeholders instead of game images.

**Solutions**:
1. Ensure RetroArch is running with video output
2. Check RetroArch screenshot settings
3. Verify screenshot directory is writable
4. Try capturing screenshot manually in RetroArch first

---

## 📚 API Reference

### IRetroArchService Interface

```csharp
public interface IRetroArchService
{
    // Save State Operations
    Task<Result<string>> CreateSaveStateAsync(int slot = -1, CancellationToken ct = default);
    Task<Result> LoadSaveStateAsync(int slot, CancellationToken ct = default);
    Task<Result> LoadSaveStateFromFileAsync(string filePath, CancellationToken ct = default);
    
    // Screenshot Operations
    Task<Result<string>> CaptureScreenshotAsync(CancellationToken ct = default);
    
    // Network Command Operations
    Task<Result<string>> SendCommandAsync(string command, CancellationToken ct = default);
    Task<Result<bool>> IsRunningAsync(CancellationToken ct = default);
    
    // ... other methods
}
```

### ISaveStateManager Integration

The `SaveStateManager` automatically uses `IRetroArchService` when available:

```csharp
public class SaveStateManager : ISaveStateManager
{
    public SaveStateManager(
        ISaveStateRepository saveStateRepository,
        IGameRepository gameRepository,
        IRomFileRepository romRepository,
        ISessionTrackingService sessionTrackingService,
        ILogger<SaveStateManager> logger,
        IRetroArchService? retroArchService = null,  // Optional dependency
        IEmulatorService? emulatorService = null)    // Optional dependency
    {
        // Automatically uses RetroArch when available
    }
}
```

---

## 🔐 Security Considerations

### Network Command Interface

**Risk**: Local TCP port exposed (127.0.0.1 only by default)

**Mitigation**:
- Network commands bound to localhost only
- No external network access
- RetroArch must explicitly enable network commands
- Configurable port to avoid conflicts

**Best Practices**:
1. Keep `NetworkCommandHost` as `127.0.0.1` (localhost)
2. Don't expose port 55355 to external networks
3. Use firewall rules if concerned about local security
4. Only enable when actively using emulator integration

---

## 🧪 Testing

### Manual Testing

1. **Start RetroArch**:
   ```bash
   retroarch.exe --network-cmd-enable
   ```

2. **Load a game** in RetroArch

3. **Test network commands**:
   ```bash
   # Using netcat or similar TCP client
   echo "VERSION" | nc 127.0.0.1 55355
   ```

4. **Use Save State Reborn** to create/load save states

### Automated Testing

Integration tests should mock `IRetroArchService`:

```csharp
[Fact]
public async Task CreateSaveState_WhenRetroArchRunning_ShouldUseNetworkCommand()
{
    // Arrange
    var mockRetroArch = new Mock<IRetroArchService>();
    mockRetroArch.Setup(x => x.IsRunningAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success(true));
    mockRetroArch.Setup(x => x.CreateSaveStateAsync(-1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success("/path/to/savestate.state"));
    
    var manager = new SaveStateManager(/* ... */, mockRetroArch.Object);
    
    // Act
    var result = await manager.CreateSaveStateAsync(gameId, new CreateSaveStateRequest());
    
    // Assert
    Assert.True(result.IsSuccess);
    mockRetroArch.Verify(x => x.CreateSaveStateAsync(-1, It.IsAny<CancellationToken>()), Times.Once);
}
```

---

## 📊 Performance Characteristics

| Operation | Latency | Network Overhead |
|-----------|---------|------------------|
| `IsRunningAsync()` | ~100-500ms | TCP handshake + VERSION command |
| `CreateSaveStateAsync()` | ~500-2000ms | Command + disk write by RetroArch |
| `LoadSaveStateAsync()` | ~500-2000ms | Command + disk read by RetroArch |
| `CaptureScreenshotAsync()` | ~200-1000ms | Command + frame buffer write |
| `SendCommandAsync()` | ~50-200ms | TCP handshake + command |

**Notes**:
- Latency depends on game complexity and system load
- Network commands are blocking operations
- Timeout defaults to 5000ms (configurable)
- Failed commands fail fast (connection refused ~10ms)

---

## 🔮 Future Enhancements

### Planned Features

1. **Support for other emulators**:
   - Dolphin network protocol
   - PCSX2 command interface
   - PPSSPP remote API

2. **Advanced save state features**:
   - Branching save state trees
   - Automatic periodic saves
   - Cloud backup integration

3. **Enhanced screenshot capture**:
   - Video recording support
   - Thumbnail generation
   - Automatic cleanup of old screenshots

4. **Performance optimizations**:
   - Connection pooling
   - Async command batching
   - Caching of emulator status

---

## 📝 Changelog

### Version 1.0.0 (Phase 2 - January 13, 2026)

**Added**:
- ✅ Full RetroArch network command protocol implementation
- ✅ TCP socket communication with RetroArch
- ✅ Real save state creation and loading
- ✅ Real-time screenshot capture
- ✅ Process detection and health checking
- ✅ Comprehensive logging and error handling
- ✅ Configurable network interface options
- ✅ Automatic fallback when emulator not running

**Technical**:
- Implemented `CreateSaveStateAsync()` method
- Implemented `LoadSaveStateAsync()` method  
- Implemented `LoadSaveStateFromFileAsync()` method
- Implemented `CaptureScreenshotAsync()` method
- Implemented `SendCommandAsync()` generic command sender
- Implemented `IsRunningAsync()` health check
- Integrated into `SaveStateManager` with fallbacks

---

## 🤝 Contributing

If you'd like to contribute to emulator integration:

1. **Test with different cores**: Verify functionality with various RetroArch cores
2. **Report bugs**: File issues for network command problems
3. **Add emulator support**: Implement interfaces for other emulators
4. **Improve documentation**: Help others understand the integration

---

## 📞 Support

### Common Issues

- **Network commands not working**: See Troubleshooting section
- **Save states corrupted**: Check RetroArch save state settings
- **Screenshots blank**: Verify RetroArch video output settings

### Resources

- [RetroArch Documentation](https://docs.libretro.com/)
- [RetroArch Network Commands](https://docs.libretro.com/development/retroarch/network-control-interface/)
- [Save State Reborn Wiki](https://github.com/DocDamage/Save_State-Gaming-Hub/wiki)

---

**Congratulations!** You now have full RetroArch emulator integration with real-time save state management and screenshot capture. 🎮✨

