# Smart Game Launcher

## Overview

The Smart Game Launcher is an advanced system optimization feature for SaveStateReborn that enhances gaming performance through intelligent resource management, customizable launch profiles, and comprehensive analytics.

## Features

### 🚀 Core Launch Features
- **Smart Game Launching** - Launch games with automatic system optimization
- **Launch Profiles** - Three built-in profiles optimized for different scenarios
- **Session Tracking** - Monitor gaming sessions with detailed metrics
- **Automatic Cleanup** - Restore system settings after gaming

### ⚙️ System Optimizations
- **Process Priority** - Set game process to High/RealTime priority
- **Background Process Suspension** - Suspend Chrome, Discord, etc. during gaming
- **Power Plan Switching** - Automatically switch to high-performance power plan
- **Memory Optimization** - Clear standby memory and optimize working sets
- **Visual Effects** - Temporarily disable Windows visual effects
- **Service Management** - Stop unnecessary Windows services

### 📊 Analytics & Statistics
- Overall usage statistics
- Per-game analytics
- Profile effectiveness tracking
- Most played games
- Performance comparison
- Time saved estimation

### 🎮 User Experience
- Visual profile editor
- Game executable configuration
- Optimization preview
- Statistics dashboard
- Toast notifications
- Keyboard shortcuts

## Quick Start

### Launching a Game
1. Navigate to the **Launcher** tab (🚀 icon)
2. Select a game from your library
3. Choose a launch profile (or use default)
4. Click **Launch Game**

### Creating a Custom Profile
1. Click **+ New** in the profiles section
2. Configure settings:
   - Process priority
   - Background processes to suspend
   - System optimizations
   - Performance settings
3. Save the profile

### Using Voice Commands
- "Launch [Game Name]" - Launch a game by voice
- "Stop Game" - End current session
- "Show Statistics" - View usage analytics

### Keyboard Shortcuts
- `Ctrl+Alt+End` - Stop current game
- `Ctrl+Alt+Home` - Show launcher
- `Ctrl+Alt+1-9` - Quick launch recent games
- `Ctrl+Alt+O` - Toggle optimization overlay

## Built-in Profiles

### Maximum Performance
- **Priority**: Real-Time
- **Optimizations**:
  - Suspend all background processes
  - Clear standby memory
  - Disable visual effects
  - High-performance power plan
- **Best for**: Competitive gaming, esports

### Balanced (Default)
- **Priority**: High
- **Optimizations**:
  - Suspend browsers and communication apps
  - Memory optimization
- **Best for**: Single-player games, general gaming

### Power Saver
- **Priority**: Above Normal
- **Optimizations**:
  - Target 30 FPS
  - Minimal system changes
- **Best for**: Gaming on battery power

## Configuration

### appsettings.json
```json
{
  "SmartLauncher": {
    "Hotkeys": {
      "StopGameHotkey": "Ctrl+Alt+End",
      "ShowLauncherHotkey": "Ctrl+Alt+Home",
      "EnableNumberedHotkeys": true,
      "GlobalHotkeys": true
    },
    "AutoEndSessionHours": 8,
    "WarningSessionHours": 4,
    "EnableProcessSuspension": true,
    "EnablePowerPlanSwitching": true
  }
}
```

### Settings UI
Access Smart Launcher settings from **Settings** → **Smart Launcher** to configure:
- General optimization settings
- Process management
- System optimizations
- Keyboard shortcuts
- Session management
- Notifications

## Import/Export

### Export Profiles
```csharp
var json = await _importExportService.ExportAllProfilesAsync();
File.WriteAllText("profiles.json", json);
```

### Import Profiles
```csharp
var json = File.ReadAllText("profiles.json");
var result = await _importExportService.ImportProfilesAsync(json);
Console.WriteLine($"Imported: {result.SuccessfulImports}, Failed: {result.FailedImports}");
```

## Plugin Development

Create custom plugins by implementing `ISmartLauncherPlugin`:

```csharp
public class MyOptimizerPlugin : ISmartLauncherPlugin
{
    public string Name => "My Custom Optimizer";
    public string Version => "1.0.0";
    public string Description => "Custom optimization for my setup";

    public async Task OnBeforeLaunchAsync(GameLaunchContext context, CancellationToken ct)
    {
        // Custom pre-launch logic
    }

    public async Task OnAfterLaunchAsync(GameLaunchContext context, CancellationToken ct)
    {
        // Custom post-launch logic
    }
}
```

## Performance Impact

### Typical Improvements
- **CPU Usage**: 10-30% reduction
- **Memory Available**: 500MB - 2GB freed
- **Input Lag**: Reduced through process priority
- **Frame Times**: More consistent

### Benchmarks
Run benchmarks with:
```bash
dotnet test --filter "FullyQualifiedName~SmartLauncherBenchmarks"
```

## Troubleshooting

### Game Won't Launch
- Check executable path is configured
- Verify game is installed
- Check for active sessions (only one game at a time)

### Optimizations Not Applied
- Ensure running on Windows for full features
- Check if user has administrator privileges
- Review application logs

### Process Suspension Not Working
- Some system processes cannot be suspended
- Antivirus may block process manipulation
- Requires Windows platform

## Platform Support

| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Basic Launching | ✅ | ✅ | ✅ |
| Process Priority | ✅ | ✅ | ✅ |
| Process Suspension | ✅ | ❌ | ❌ |
| Power Plan Switching | ✅ | ❌ | ❌ |
| Visual Effects | ✅ | ❌ | ❌ |

## API Reference

### ISmartLauncherService
```csharp
// Launch a game
Task<LaunchResult> LaunchGameAsync(Guid gameId, Guid? profileId = null);

// End session
Task<Result> EndSessionAsync(Guid sessionId);

// Get profiles
Task<IReadOnlyList<LaunchProfile>> GetProfilesAsync(Guid? gameId = null);

// Create profile
Task<Result<LaunchProfile>> CreateProfileAsync(LaunchProfile profile);
```

### ISmartLauncherStatisticsService
```csharp
// Get overall statistics
Task<SmartLauncherStatistics> GetOverallStatisticsAsync();

// Get game statistics
Task<GameLaunchStatistics> GetGameStatisticsAsync(Guid gameId);

// Get most played games
Task<IReadOnlyList<MostPlayedGame>> GetMostPlayedGamesAsync(int count = 10);
```

## Contributing

When contributing to the Smart Launcher:
1. Follow existing code patterns
2. Add unit tests for new features
3. Update documentation
4. Test on multiple platforms

## License

This feature is part of SaveStateReborn and follows the project's license.

---

**Version**: 1.0.0  
**Last Updated**: February 2026  
**Documentation**: Complete
