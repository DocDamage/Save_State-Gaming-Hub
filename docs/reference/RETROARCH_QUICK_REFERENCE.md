# RetroArch Integration - Quick Reference

**Quick lookup for common operations and troubleshooting**

---

## 🚀 Quick Start

### 1. Configuration (appsettings.json)

```json
{
  "RetroArch": {
    "NetworkCommandEnabled": true,
    "NetworkCommandPort": 55355,
    "NetworkCommandHost": "127.0.0.1"
  }
}
```

### 2. Launch RetroArch

```bash
retroarch.exe --network-cmd-enable
```

### 3. Use in Code

```csharp
// Check if running
var isRunning = await _retroArchService.IsRunningAsync();

// Create save state
var result = await _retroArchService.CreateSaveStateAsync();

// Capture screenshot
var screenshot = await _retroArchService.CaptureScreenshotAsync();
```

---

## 📚 API Quick Reference

### IRetroArchService Methods

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `IsRunningAsync()` | `CancellationToken` | `Result<bool>` | Check if RetroArch is active |
| `CreateSaveStateAsync()` | `int slot`, `CancellationToken` | `Result<string>` | Create save state (slot -1 = auto) |
| `LoadSaveStateAsync()` | `int slot`, `CancellationToken` | `Result` | Load save state by slot |
| `LoadSaveStateFromFileAsync()` | `string filePath`, `CancellationToken` | `Result` | Load save state from file |
| `CaptureScreenshotAsync()` | `CancellationToken` | `Result<string>` | Capture screenshot |
| `SendCommandAsync()` | `string command`, `CancellationToken` | `Result<string>` | Send raw command |

### SaveStateManager Methods (Auto-Enhanced)

| Method | Enhancement | Fallback |
|--------|-------------|----------|
| `CreateSaveStateAsync()` | Uses RetroArch network command | Creates placeholder file |
| `RestoreSaveStateAsync()` | Loads into running emulator | Returns helpful error |
| `GetThumbnailAsync()` | Returns real screenshot | Returns placeholder image |

---

## 🔧 Configuration Options

| Option | Default | Purpose |
|--------|---------|---------|
| `NetworkCommandEnabled` | `true` | Enable/disable network integration |
| `NetworkCommandPort` | `55355` | TCP port for commands |
| `NetworkCommandHost` | `127.0.0.1` | Host address (use localhost) |
| `NetworkCommandTimeout` | `5000` | Timeout in milliseconds |
| `InstallPath` | `""` | Path to retroarch.exe (auto-detect if empty) |
| `AutoDetect` | `true` | Auto-detect RetroArch installation |

---

## 🐛 Troubleshooting

### Error: "RetroArch is not currently running"

**Solutions**:
1. Start RetroArch
2. Add `--network-cmd-enable` flag
3. Check `network_cmd_enable = "true"` in retroarch.cfg

### Error: "Connection to RetroArch timed out"

**Solutions**:
1. Verify RetroArch is running (Task Manager)
2. Check port 55355 is not in use
3. Increase `NetworkCommandTimeout` in config
4. Check firewall settings

### Error: "Save state file not found"

**Solutions**:
1. Check RetroArch save state directory
2. Verify disk space
3. Check write permissions
4. Review RetroArch logs

### Screenshots Are Placeholders

**Solutions**:
1. Ensure game is running
2. Check RetroArch video output
3. Verify screenshot directory writable
4. Test manual screenshot in RetroArch

---

## 💡 Code Snippets

### Check Emulator Status

```csharp
var isRunning = await _retroArchService.IsRunningAsync();
if (isRunning.IsSuccess && isRunning.Value)
{
    Console.WriteLine("RetroArch is running and ready");
}
else
{
    Console.WriteLine("RetroArch is not available");
}
```

### Create and Capture

```csharp
// Check if running first
var running = await _retroArchService.IsRunningAsync();
if (!running.Value) 
{
    return BadRequest("Game must be running");
}

// Create save state
var saveResult = await _retroArchService.CreateSaveStateAsync();
if (!saveResult.IsSuccess)
{
    return BadRequest(saveResult.Error);
}

// Capture screenshot
var screenshotResult = await _retroArchService.CaptureScreenshotAsync();

return Ok(new { 
    SaveStatePath = saveResult.Value,
    ScreenshotPath = screenshotResult.Value 
});
```

### Load Save State

```csharp
// Method 1: By slot
var result = await _retroArchService.LoadSaveStateAsync(slot: 0);

// Method 2: By file path
var result = await _retroArchService.LoadSaveStateFromFileAsync(
    "/path/to/savestate.state");

if (result.IsSuccess)
{
    Console.WriteLine("Save state loaded!");
}
```

### Custom Command

```csharp
// Send any RetroArch command
var result = await _retroArchService.SendCommandAsync("PAUSE_TOGGLE");
if (result.IsSuccess)
{
    Console.WriteLine($"Response: {result.Value}");
}
```

---

## 📊 Performance Guidelines

### Expected Latency

| Operation | Time | Notes |
|-----------|------|-------|
| `IsRunningAsync()` | 50-500ms | Quick health check |
| `CreateSaveStateAsync()` | 500-2000ms | Depends on game state size |
| `LoadSaveStateAsync()` | 500-2000ms | Depends on game complexity |
| `CaptureScreenshotAsync()` | 200-1000ms | Depends on resolution |

### Best Practices

✅ **DO**:
- Check `IsRunningAsync()` before commands
- Handle `Result.IsFailure` cases
- Use `CancellationToken` for long operations
- Configure appropriate timeouts
- Log errors for debugging

❌ **DON'T**:
- Call network commands in tight loops
- Assume emulator is always available
- Ignore error results
- Use very short timeouts
- Block UI thread with sync calls

---

## 🔐 Security Notes

- Network commands are localhost-only by default
- No authentication required (localhost trust model)
- RetroArch must explicitly enable network commands
- Port 55355 should not be exposed externally
- Use firewall rules for additional security

---

## 📖 Links to Full Documentation

- **User Guide**: [RETROARCH_INTEGRATION.md](../features/RETROARCH_INTEGRATION.md)
- **Migration Guide**: [PHASE2_MIGRATION_GUIDE.md](../guides/PHASE2_MIGRATION_GUIDE.md)
- **Completion Summary**: [PHASE2_COMPLETION_SUMMARY.md](../status/PHASE2_COMPLETION_SUMMARY.md)
- **Project Status**: [DEVELOPMENT_STATUS.md](../status/DEVELOPMENT_STATUS.md)

---

## 🆘 Support

- **GitHub Issues**: Report bugs and request features
- **Documentation**: Check docs folder for detailed guides
- **Logs**: Enable Debug logging for troubleshooting
- **RetroArch Docs**: [docs.libretro.com](https://docs.libretro.com/)

---

**Last Updated**: January 13, 2026  
**Version**: Phase 2 Complete  
**Status**: ✅ Production Ready



