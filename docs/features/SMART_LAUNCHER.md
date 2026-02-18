# Smart Game Launcher

The Smart Game Launcher is an advanced system optimization feature that enhances gaming performance by managing system resources, suspending background processes, and applying targeted optimizations when launching games.

## Overview

The Smart Launcher provides:
- **System Optimization**: Automatically adjusts Windows settings for maximum gaming performance
- **Launch Profiles**: Pre-configured optimization profiles (Performance, Balanced, Power Saver)
- **Process Management**: Suspends background processes during gameplay
- **Session Tracking**: Monitors gaming sessions and performance metrics
- **Automatic Cleanup**: Restores system settings after gaming

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌──────────────┐  ┌─────────────────┐  ┌──────────────┐   │
│  │ SmartLauncher│  │ LaunchProfile   │  │  GameExe     │   │
│  │    View      │  │  Editor Dialog  │  │ Config Dialog│   │
│  └──────────────┘  └─────────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                          │
│              SmartLauncherService.cs                         │
│         - Orchestrates launch process                        │
│         - Manages profiles and sessions                      │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │SystemOptimizer│  │LaunchProfile │  │LaunchSession │      │
│  │   Service     │  │  Repository  │  │  Repository  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌─────────────────────────────────────┐  │
│  │GameProcess   │  │   SmartLauncherBackgroundService    │  │
│  │  Monitor     │  │       (Session monitoring)          │  │
│  └──────────────┘  └─────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Launch Profiles

### Maximum Performance
- **Priority**: Real-Time
- **Optimizations**:
  - Memory optimization enabled
  - Clear standby memory list
  - Disable Windows visual effects
  - Suspend web browsers and communication apps
- **Use Case**: Competitive gaming, esports titles

### Balanced (Default)
- **Priority**: High
- **Optimizations**:
  - Memory optimization enabled
  - Minimal process suspension (Chrome, Discord)
- **Use Case**: Single-player games, general gaming

### Power Saver
- **Priority**: Above Normal
- **Optimizations**:
  - Target 30 FPS for battery conservation
  - Minimal system changes
- **Use Case**: Gaming on battery power

## System Optimizations

### Process Management
- Suspends specified background processes
- Resumes processes after gaming
- Uses `NtSuspendProcess`/`NtResumeProcess` (Windows only)

### Power Management
- Switches to high-performance power plan
- Restores original power plan after gaming

### Memory Optimization
- Clears working set for current process
- Triggers garbage collection
- Clears standby memory list (optional)

### Visual Effects
- Disables Windows visual effects during gameplay
- Restores original settings after session

## Database Schema

### LaunchProfiles Table
| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| Name | string | Profile name |
| Description | string | Profile description |
| GameId | Guid? | Associated game (null = global) |
| Priority | int | Process priority level |
| ProcessesToSuspend | string | JSON array of process names |
| ServicesToStop | string | JSON array of service names |
| PowerPlanGuid | string | Windows power plan GUID |
| IsDefault | bool | Default profile for game |
| IsActive | bool | Soft delete flag |

### LaunchSessions Table
| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| GameId | Guid | Game being played |
| GameName | string | Game title |
| ProfileId | Guid? | Profile used |
| StartedAt | DateTime | Session start time |
| EndedAt | DateTime? | Session end time |
| ExitCode | int? | Process exit code |
| PerformanceMetrics | JSON | Collected metrics |

## Usage

### Basic Launch
```csharp
var result = await _launcherService.LaunchGameAsync(gameId, profileId);
if (result.Success)
{
    Console.WriteLine($"Game launched! PID: {result.ProcessId}");
}
```

### Create Custom Profile
```csharp
var profile = new LaunchProfile
{
    Name = "My Custom Profile",
    Priority = ProcessPriority.High,
    PerformanceSettings = new PerformanceSettings
    {
        EnableMemoryOptimization = true,
        ClearStandbyList = true
    },
    ProcessesToSuspend = new List<string> { "chrome", "discord" }
};

await _launcherService.CreateProfileAsync(profile);
```

### End Session
```csharp
var result = await _launcherService.EndSessionAsync(sessionId);
// Automatically restores system state
```

## Configuration

### appsettings.json
```json
{
  "SmartLauncher": {
    "DefaultProfileId": null,
    "AutoEndSessionHours": 8,
    "WarningSessionHours": 4,
    "EnableProcessSuspension": true,
    "EnablePowerPlanSwitching": true
  }
}
```

## Testing

Run Smart Launcher tests:
```bash
dotnet test --filter "FullyQualifiedName~SmartLauncher"
```

Test coverage includes:
- Profile creation and management
- Game launching with various configurations
- Session tracking and cleanup
- System state restoration

## Platform Support

| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Process Priority | ✅ | ✅ | ✅ |
| Process Suspension | ✅ | ❌ | ❌ |
| Power Plan Switching | ✅ | ❌ | ❌ |
| Visual Effects Toggle | ✅ | ❌ | ❌ |
| Memory Optimization | ✅ | ⚠️ Limited | ⚠️ Limited |

## Troubleshooting

### Game won't launch
- Check executable path is configured
- Verify game is installed
- Check for active sessions (only one game at a time)

### Optimizations not applied
- Ensure running on Windows for full features
- Check if user has administrator privileges
- Review logs for specific error messages

### Process suspension not working
- Some system processes cannot be suspended
- Antivirus may block process manipulation
- Requires Windows platform

## Statistics & Analytics

The Smart Launcher provides comprehensive usage statistics:

### Overall Statistics
- Total gaming sessions and time
- Optimization adoption rate
- Most played games
- Performance improvements
- Time saved through optimizations

### Profile Analytics
- Profile usage counts
- Average performance gains
- Per-game effectiveness

### Export/Import
Profiles can be exported and imported as JSON:

```csharp
// Export all profiles
var json = await _importExportService.ExportAllProfilesAsync();

// Import profiles
var result = await _importExportService.ImportProfilesAsync(json, overwriteExisting: false);
```

## Voice Commands

The Smart Launcher supports voice commands (requires Voice Command feature):
- "Launch [Game Name]"
- "Start game with [Profile Name] profile"
- "Stop current game"
- "Show launcher statistics"

## Future Enhancements

- [ ] Per-game automatic profile selection based on genre
- [ ] Machine learning for optimal settings recommendation
- [ ] Integration with GPU control panels (NVIDIA Control Panel, AMD Radeon)
- [ ] Temperature-based throttling
- [ ] Game-specific optimizations database
- [ ] Cloud sync for profiles across devices
- [ ] Achievement integration for session tracking
- [ ] Streamer mode (disable optimizations for streaming software)
- [ ] VR-specific optimization profiles
