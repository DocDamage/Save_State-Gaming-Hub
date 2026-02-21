# Migration Guide: Phase 2 Emulator Integration

**Target Audience**: Developers working with Save State Reborn  
**Affected Components**: Save States, Emulator Integration, Screenshots  
**Migration Complexity**: Low to Medium  
**Breaking Changes**: None (backward compatible)

---

## 📋 Overview

Phase 2 introduced real emulator integration with RetroArch via network commands. This guide helps you:

1. Understand what changed
2. Update your code to use new features
3. Handle optional emulator integration
4. Test emulator-dependent features

---

## 🔄 What Changed

### Before Phase 2

**SaveStateManager** created placeholder files:
```csharp
// Old behavior - always created dummy files
private static Task<Result<long>> CreateSaveStateFileAsync(SaveStateEntity saveState, CancellationToken ct)
{
    // Created placeholder text file
    var dummyData = $"SaveState:{saveState.Id}...";
    File.WriteAllText(saveState.FilePath, dummyData);
    return Task.FromResult(Result.Success<long>(fileInfo.Length));
}
```

### After Phase 2

**SaveStateManager** uses real emulator integration:
```csharp
// New behavior - uses RetroArch network commands when available
private async Task<Result<long>> CreateSaveStateFileAsync(SaveStateEntity saveState, CancellationToken ct)
{
    // Try RetroArch first
    if (_retroArchService != null && await _retroArchService.IsRunningAsync(ct))
    {
        var result = await _retroArchService.CreateSaveStateAsync(-1, ct);
        if (result.IsSuccess)
        {
            // Copy real save state file
            File.Copy(result.Value, saveState.FilePath, true);
            return Result.Success<long>(new FileInfo(saveState.FilePath).Length);
        }
    }
    
    // Fallback to placeholder when emulator not available
    // ... fallback code ...
}
```

---

## 🔧 Required Changes

### 1. No Breaking Changes! ✅

**Good news**: All changes are backward compatible. Existing code continues to work without modifications.

The new emulator integration is **optional** and **automatically detected**:
- If RetroArch is running: Uses network commands
- If RetroArch is not running: Uses fallback behavior
- If `IRetroArchService` not registered: Works without it

### 2. Recommended Updates

#### Update DI Registration (Optional)

If you want to use emulator integration, ensure `IRetroArchService` is registered:

```csharp
// Already registered in DependencyInjection.cs
services.AddSingleton<IRetroArchService, RetroArchService>();
```

#### Update Configuration (Recommended)

Add RetroArch configuration to `appsettings.json`:

```json
{
  "RetroArch": {
    "InstallPath": "",
    "AutoDetect": true,
    "NetworkCommandEnabled": true,
    "NetworkCommandPort": 55355,
    "NetworkCommandHost": "127.0.0.1",
    "NetworkCommandTimeout": 5000
  }
}
```

---

## 🎯 Feature-Specific Migrations

### Save State Creation

#### Before (Still Works)
```csharp
// This continues to work exactly as before
var result = await saveStateManager.CreateSaveStateAsync(
    gameId, 
    new CreateSaveStateRequest 
    {
        Description = "My save",
        CaptureScreenshot = true
    });
```

#### After (Enhanced)
```csharp
// Same code, but now:
// - Creates REAL save state if RetroArch is running
// - Captures REAL screenshot if game is running
// - Falls back to placeholder if not available

var result = await saveStateManager.CreateSaveStateAsync(
    gameId, 
    new CreateSaveStateRequest 
    {
        Description = "My save",
        CaptureScreenshot = true  // Now captures real screenshot!
    });

if (result.IsSuccess)
{
    // result.Value is the SaveStateEntity
    // - FilePath points to real save state
    // - ThumbnailPath points to real screenshot (if captured)
    // - FileSizeBytes is actual file size
}
```

### Save State Loading

#### Before (Still Works)
```csharp
// This continues to work
var result = await saveStateManager.RestoreSaveStateAsync(saveStateId);
```

#### After (Enhanced)
```csharp
// Same code, but now:
// - Loads save state into running emulator via network command
// - Returns helpful error if game not running

var result = await saveStateManager.RestoreSaveStateAsync(saveStateId);

if (result.IsFailure)
{
    // Now returns user-friendly messages:
    // "No active emulator found. Please launch the game first."
    Console.WriteLine(result.Error);
}
```

### Direct RetroArch Access (New Feature)

#### New Capability
```csharp
public class MyGameController
{
    private readonly IRetroArchService _retroArchService;
    
    public MyGameController(IRetroArchService retroArchService)
    {
        _retroArchService = retroArchService;
    }
    
    [HttpPost("quick-save")]
    public async Task<IActionResult> QuickSave()
    {
        // Check if emulator is running
        var isRunning = await _retroArchService.IsRunningAsync();
        if (!isRunning.Value)
        {
            return BadRequest("Game must be running to create save state");
        }
        
        // Create save state directly via network command
        var result = await _retroArchService.CreateSaveStateAsync(slot: 0);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : BadRequest(result.Error);
    }
}
```

---

## 🧪 Testing Changes

### Unit Tests (No Changes Required)

Existing unit tests continue to work. Emulator integration is optional:

```csharp
[Fact]
public async Task CreateSaveState_ShouldSucceed()
{
    // Arrange
    var manager = new SaveStateManager(
        saveStateRepository,
        gameRepository,
        romRepository,
        sessionTrackingService,
        logger,
        retroArchService: null,  // Optional - tests work without it
        emulatorService: null);  // Optional - tests work without it
    
    // Act
    var result = await manager.CreateSaveStateAsync(gameId, request);
    
    // Assert
    Assert.True(result.IsSuccess);  // Still works!
}
```

### Integration Tests (Enhanced)

Test with mocked emulator service:

```csharp
[Fact]
public async Task CreateSaveState_WithRetroArchRunning_ShouldUseNetworkCommand()
{
    // Arrange
    var mockRetroArch = new Mock<IRetroArchService>();
    mockRetroArch
        .Setup(x => x.IsRunningAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success(true));
    mockRetroArch
        .Setup(x => x.CreateSaveStateAsync(-1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success("/path/to/savestate.state"));
    
    var manager = new SaveStateManager(
        saveStateRepository,
        gameRepository,
        romRepository,
        sessionTrackingService,
        logger,
        mockRetroArch.Object,
        emulatorService: null);
    
    // Act
    var result = await manager.CreateSaveStateAsync(gameId, request);
    
    // Assert
    Assert.True(result.IsSuccess);
    mockRetroArch.Verify(
        x => x.CreateSaveStateAsync(-1, It.IsAny<CancellationToken>()), 
        Times.Once);
}
```

---

## 📝 Code Examples

### Example 1: Basic Save State Operations

```csharp
public class SaveStateController : ControllerBase
{
    private readonly ISaveStateManager _saveStateManager;
    
    [HttpPost("games/{gameId}/savestates")]
    public async Task<IActionResult> CreateSaveState(
        Guid gameId,
        [FromBody] CreateSaveStateRequest request)
    {
        // No changes needed - enhanced automatically
        var result = await _saveStateManager.CreateSaveStateAsync(gameId, request);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : BadRequest(result.Error);
    }
    
    [HttpPost("savestates/{id}/restore")]
    public async Task<IActionResult> RestoreSaveState(Guid id)
    {
        // No changes needed - enhanced automatically
        var result = await _saveStateManager.RestoreSaveStateAsync(id);
        
        return result.IsSuccess 
            ? Ok() 
            : BadRequest(result.Error);
    }
}
```

### Example 2: Checking Emulator Status

```csharp
public class GameLauncherService
{
    private readonly IEmulatorService _emulatorService;
    private readonly IRetroArchService _retroArchService;
    
    public async Task<LaunchResult> LaunchGameAsync(Guid romFileId)
    {
        // Launch the emulator
        var launchResult = await _emulatorService.LaunchRomAsync(romFileId);
        
        if (!launchResult.Success)
        {
            return new LaunchResult { Success = false };
        }
        
        // Wait for RetroArch to be ready
        var maxWait = TimeSpan.FromSeconds(10);
        var started = DateTime.UtcNow;
        
        while (DateTime.UtcNow - started < maxWait)
        {
            var isRunning = await _retroArchService.IsRunningAsync();
            if (isRunning.Value)
            {
                return new LaunchResult 
                { 
                    Success = true, 
                    EmulatorReady = true 
                };
            }
            
            await Task.Delay(500);
        }
        
        return new LaunchResult 
        { 
            Success = true, 
            EmulatorReady = false,
            Message = "Emulator launched but network commands not ready"
        };
    }
}
```

### Example 3: Custom Screenshot Logic

```csharp
public class ScreenshotService
{
    private readonly IRetroArchService _retroArchService;
    private readonly ILogger<ScreenshotService> _logger;
    
    public async Task<string?> CaptureGameScreenshotAsync()
    {
        // Check if RetroArch is running
        var isRunning = await _retroArchService.IsRunningAsync();
        
        if (!isRunning.Value)
        {
            _logger.LogWarning("Cannot capture screenshot - RetroArch not running");
            return null;
        }
        
        // Capture screenshot
        var result = await _retroArchService.CaptureScreenshotAsync();
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Screenshot captured: {Path}", result.Value);
            return result.Value;
        }
        
        _logger.LogError("Screenshot capture failed: {Error}", result.Error);
        return null;
    }
}
```

---

## ⚠️ Common Pitfalls

### 1. Assuming Emulator is Always Available

**❌ Wrong**:
```csharp
// This will fail if emulator not running
var result = await retroArchService.CreateSaveStateAsync();
// Assumes success without checking
var filePath = result.Value;  // May be null/empty!
```

**✅ Correct**:
```csharp
var result = await retroArchService.CreateSaveStateAsync();

if (result.IsSuccess)
{
    var filePath = result.Value;
    // Safe to use filePath
}
else
{
    // Handle error - emulator may not be running
    logger.LogWarning("Save state creation failed: {Error}", result.Error);
}
```

### 2. Not Checking IsRunning Before Commands

**❌ Wrong**:
```csharp
// Directly send command without checking
await retroArchService.CreateSaveStateAsync();
```

**✅ Correct**:
```csharp
// Check if emulator is ready first
var isRunning = await retroArchService.IsRunningAsync();

if (isRunning.IsSuccess && isRunning.Value)
{
    var result = await retroArchService.CreateSaveStateAsync();
    // Process result
}
else
{
    // Show user-friendly message
    Console.WriteLine("Please launch the game in RetroArch first");
}
```

### 3. Ignoring Fallback Behavior

**❌ Wrong**:
```csharp
// Assuming save state creation always uses emulator
var result = await saveStateManager.CreateSaveStateAsync(gameId, request);
// File may be placeholder if emulator not running!
```

**✅ Correct**:
```csharp
var result = await saveStateManager.CreateSaveStateAsync(gameId, request);

if (result.IsSuccess)
{
    // Check if this is a real save state or placeholder
    var saveState = result.Value;
    
    if (saveState.FileSizeBytes < 1000)
    {
        // Likely a placeholder - show notice to user
        Console.WriteLine("Save state created, but emulator not running. Will be populated when you save in-game.");
    }
}
```

---

## 🔍 Debugging Tips

### Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SaveState.Infrastructure.RetroArch": "Debug",
      "SaveState.Infrastructure.SaveStates": "Debug"
    }
  }
}
```

### Check Network Command Status

```csharp
// Diagnostic method to check RetroArch status
public async Task<RetroArchStatus> CheckRetroArchStatusAsync()
{
    var status = new RetroArchStatus();
    
    // Check if running
    var isRunning = await _retroArchService.IsRunningAsync();
    status.IsRunning = isRunning.Value;
    
    if (status.IsRunning)
    {
        // Check if network commands work
        var versionResult = await _retroArchService.SendCommandAsync("VERSION");
        status.NetworkCommandsWorking = versionResult.IsSuccess;
        status.Version = versionResult.Value;
    }
    
    return status;
}

public class RetroArchStatus
{
    public bool IsRunning { get; set; }
    public bool NetworkCommandsWorking { get; set; }
    public string? Version { get; set; }
}
```

### Monitor Network Traffic

Use tools like Wireshark or tcpdump to monitor TCP traffic on port 55355:

```bash
# Monitor RetroArch network commands
tcpdump -i lo -A 'tcp port 55355'
```

---

## 📊 Performance Considerations

### Network Command Latency

```csharp
// Commands are synchronous and block
// Expected latency: 50-2000ms per command

// ❌ Don't call in tight loops
for (int i = 0; i < 100; i++)
{
    await retroArchService.CreateSaveStateAsync(i);  // Very slow!
}

// ✅ Batch operations or use async patterns
var saveStateTasks = Enumerable.Range(0, 10)
    .Select(async i => await retroArchService.CreateSaveStateAsync(i));
await Task.WhenAll(saveStateTasks);  // Parallel execution
```

### Timeout Configuration

```json
{
  "RetroArch": {
    "NetworkCommandTimeout": 5000,  // Default: 5 seconds
    
    // Increase for slow systems or complex games
    "NetworkCommandTimeout": 10000  // 10 seconds
  }
}
```

---

## ✅ Migration Checklist

Use this checklist to ensure complete migration:

### Configuration
- [ ] Added RetroArch section to `appsettings.json`
- [ ] Configured network command options
- [ ] Set appropriate timeout values
- [ ] Tested configuration with `--network-cmd-enable` flag

### Code Updates
- [ ] Updated DI registrations (if needed)
- [ ] Added error handling for emulator not running
- [ ] Updated UI to show emulator status
- [ ] Added user-friendly error messages

### Testing
- [ ] Tested save state creation with emulator running
- [ ] Tested save state creation without emulator
- [ ] Tested save state loading with emulator running
- [ ] Tested save state loading without emulator
- [ ] Tested screenshot capture functionality
- [ ] Verified fallback behavior works correctly

### Documentation
- [ ] Updated user documentation
- [ ] Added troubleshooting guides
- [ ] Documented configuration options
- [ ] Created example code snippets

### Deployment
- [ ] Tested in development environment
- [ ] Tested in staging environment
- [ ] Verified network port accessibility
- [ ] Confirmed RetroArch installation paths
- [ ] Tested on different operating systems

---

## 🎓 Next Steps

After completing migration:

1. **Read Integration Guide**: See `docs/features/RETROARCH_INTEGRATION.md` for detailed usage
2. **Test Thoroughly**: Verify all emulator scenarios work
3. **Monitor Logs**: Watch for any integration issues
4. **Gather Feedback**: Get user input on emulator integration
5. **Plan Phase 3**: Review cloud configuration requirements

---

## 📞 Support

Need help with migration?

- **Questions**: File an issue on GitHub
- **Bugs**: Report in issue tracker with logs
- **Improvements**: Submit pull requests
- **Discussion**: Join community Discord

---

**Happy Migrating!** The new emulator integration brings real save state management to Save State Reborn. 🎮✨

