# 🛠️ Part 6: Tools Tab Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [05_ANALYTICS_SOCIAL.md](05_ANALYTICS_SOCIAL.md)

---

## 1. Tools Overview

### 1.1 Purpose

Power user utilities including performance, voice, automation, cloud, plugins, and system tools.

### 1.2 Design Personality

- **Theme**: Technical/utility focused
- **Colors**: Clean, functional with accent highlights
- **Typography**: Monospace for data, clear labels
- **Layout**: Category sidebar with detail panel

---

## 2. Tools Shell Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🛠️ TOOLS                                                                   │
├────────────────────┬────────────────────────────────────────────────────────┤
│  CATEGORIES        │  TOOL CONTENT                                          │
│  ┌──────────────┐  │  ┌──────────────────────────────────────────────────┐  │
│  │ ⚡ Performance│  │  │                                                  │  │
│  │ 🎙️ Voice     │  │  │              SELECTED TOOL VIEW                  │  │
│  │ 🤖 Automation│  │  │                                                  │  │
│  │ ☁️ Cloud     │  │  │                                                  │  │
│  │ 💾 Save State│  │  │                                                  │  │
│  │ 🔌 Plugins   │  │  │                                                  │  │
│  │ 🎨 Themes    │  │  │                                                  │  │
│  │ 📥 Import    │  │  └──────────────────────────────────────────────────┘  │
│  │ 📤 Export    │  │                                                        │
│  │ 🔍 Diagnostics│ │                                                        │
│  └──────────────┘  │                                                        │
└────────────────────┴────────────────────────────────────────────────────────┘
```

---

## 3. Tool Categories

### 3.1 Performance Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚡ PERFORMANCE                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Monitor] [Optimization] [Display] [Audio] [Battery] [Profiles]           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SYSTEM MONITOR (Real-time)                                                 │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ CPU                          GPU                          RAM        │  │
│  │ [████████░░] 78%            [██████░░░░] 56%             [██████░░] │  │
│  │ AMD Ryzen 9 5900X           RTX 3080                     12.4/32GB  │  │
│  │ Temp: 65°C                  Temp: 72°C                    78%       │  │
│  ├──────────────────────────────────────────────────────────────────────┤  │
│  │ FPS: 144        Frame Time: 6.9ms        VRAM: 8.2/10GB            │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌────────────────────────────────────┐ ┌────────────────────────────────┐ │
│  │ 📊 CPU HISTORY                    │ │ 📊 GPU HISTORY                 │ │
│  │ [LINE GRAPH - Last 60 seconds]    │ │ [LINE GRAPH - Last 60 seconds] │ │
│  └────────────────────────────────────┘ └────────────────────────────────┘ │
│                                                                             │
│  QUICK ACTIONS                                                              │
│  [🚀 Game Mode] [🔇 Quiet Mode] [⚡ Performance] [🔋 Power Saver]          │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Performance Sub-tabs:**

| Tab | Features | Service |
|-----|----------|---------|
| Monitor | Real-time CPU/GPU/RAM/FPS | `IPerformanceMonitor` |
| Optimization | System resource manager | `ISystemResourceManager` |
| Display | Resolution, refresh, HDR | `IDisplayCalibrator` |
| Audio | Audio device, per-game | `IAudioOptimizer` |
| Battery | Power profiles (Steam Deck) | `IBatteryOptimizer` |
| Profiles | Per-game optimization | `IPerformanceProfiler` |

### 3.2 Voice Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎙️ VOICE COMMANDS                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STATUS: 🟢 Listening                              [Stop Listening]  │   │
│  │ Wake Word: "Hey SaveState"                        [Change]          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [Commands] [Calibration] [History] [Settings]                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  AVAILABLE COMMANDS                                    [+ Add Custom]       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Command              │ Action                      │ Enabled        │   │
│  ├──────────────────────┼─────────────────────────────┼────────────────┤   │
│  │ "Launch [game]"      │ Start specified game        │ [✓]            │   │
│  │ "Take screenshot"    │ Capture screenshot          │ [✓]            │   │
│  │ "Quick save"         │ Create save state           │ [✓]            │   │
│  │ "What should I play" │ AI recommendation           │ [✓]            │   │
│  │ "Show performance"   │ Toggle performance HUD      │ [✓]            │   │
│  │ "Mute"               │ Mute game audio             │ [✓]            │   │
│  │ "Random game"        │ Launch random game          │ [ ]            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  VOICE FEEDBACK                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 🎤 Waveform: ▁▂▃▅▇▅▃▂▁▂▃▅▇▅▃▂▁                                     │   │
│  │ Detected: "Launch Elden Ring"                                      │   │
│  │ Status: ✓ Executing...                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Automation Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🤖 AUTOMATION                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Macros] [Workflows] [Backups] [Scheduled Tasks]                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  VISUAL WORKFLOW BUILDER                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                                                                      │   │
│  │  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐       │   │
│  │  │ TRIGGER  │───▶│  ACTION  │───▶│ CONDITION│───▶│  ACTION  │       │   │
│  │  │ On Game  │    │ Optimize │    │ If FPS   │    │ Notify   │       │   │
│  │  │ Launch   │    │ System   │    │ < 30     │    │ User     │       │   │
│  │  └──────────┘    └──────────┘    └──────────┘    └──────────┘       │   │
│  │                                       │                              │   │
│  │                                       ▼                              │   │
│  │                                 ┌──────────┐                         │   │
│  │                                 │  ACTION  │                         │   │
│  │                                 │ Lower    │                         │   │
│  │                                 │ Settings │                         │   │
│  │                                 └──────────┘                         │   │
│  │                                                                      │   │
│  │  [Save Workflow] [Test] [Delete]                    Drag blocks →   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  BLOCKS PALETTE                                                             │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐              │
│  │Triggers │ │Actions  │ │Conditions│ │ Loops  │ │Variables│              │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Automation Sub-tabs:**

| Tab | Features | Service |
|-----|----------|---------|
| Macros | Record/playback input macros | `IMacroManager` |
| Workflows | Visual workflow builder | `IWorkflowAutomationService` |
| Backups | Scheduled backup config | `IBackupScheduler` |
| Scheduled Tasks | Task calendar view | `IWorkflowAutomationService` |

### 3.4 Cloud Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ☁️ CLOUD & NETWORK                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Cloud Gaming] [Network Quality] [Sync] [Streaming]                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  CLOUD GAMING PROVIDERS                                                     │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐            │
│  │ 🟢 GeForce NOW   │ │ 🟡 Xbox Cloud    │ │ ⚫ Luna          │            │
│  │ Connected        │ │ Sign In Required │ │ Not Available    │            │
│  │ [Launch] [Config]│ │ [Connect]        │ │ [Learn More]     │            │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘            │
│                                                                             │
│  NETWORK QUALITY                                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Latency: 23ms     Jitter: 2ms     Packet Loss: 0.1%    Speed: 120Mb│   │
│  │ Rating: ★★★★★ Excellent for cloud gaming                           │   │
│  │                                                                      │   │
│  │ [LINE GRAPH - Latency over time]                                    │   │
│  │                                                                      │   │
│  │ [Run Speed Test]                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  CLOUD-AVAILABLE GAMES (23 games in your library)                          │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ [View All →]                   │
│  │Game│ │Game│ │Game│ │Game│ │Game│ │Game│                                 │
│  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘                                 │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.5 Plugin Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🔌 PLUGINS                                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Installed] [Marketplace] [Updates] [Settings]                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  🔍 Search plugins...                              [Categories ▼]          │
│                                                                             │
│  FEATURED                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ┌────┐ Discord Rich Presence            ★★★★★ (1,234 reviews)      │   │
│  │ │ 🎮 │ Show your gaming activity        Downloads: 45,678          │   │
│  │ └────┘ on Discord                       [Install]                   │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ┌────┐ Twitch Integration               ★★★★☆ (567 reviews)       │   │
│  │ │ 📺 │ Stream game info to Twitch       Downloads: 12,345          │   │
│  │ └────┘ chat                             [Install]                   │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ┌────┐ RetroWave Theme                  ★★★★★ (890 reviews)       │   │
│  │ │ 🎨 │ 80s aesthetic theme pack         Downloads: 23,456          │   │
│  │ └────┘                                  [Install]                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  CATEGORIES                                                                 │
│  [Integrations] [Themes] [Metadata] [Importers] [Game Specific]            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.6 Theme Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎨 THEMES                                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Browse] [Editor] [My Themes] [Marketplace]                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  CURRENT THEME: Deep Space (Default)                    [Apply] [Edit]     │
│                                                                             │
│  THEME EDITOR                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ COLORS                                                               │   │
│  │ Primary:    [#58A6FF] 🔵  Background: [#0D1117] ⬛                  │   │
│  │ Secondary:  [#161B22] ⬛  Text:       [#C9D1D9] ⬜                  │   │
│  │ Accent:     [#238636] 🟢  Error:      [#F85149] 🔴                  │   │
│  │                                                                      │   │
│  │ TYPOGRAPHY                                                           │   │
│  │ Font:       [Inter ▼]     Size:   [14px ▼]    Weight: [Normal ▼]   │   │
│  │                                                                      │   │
│  │ EFFECTS                                                              │   │
│  │ [✓] Glassmorphism  [✓] Shadows  [✓] Gradients  [ ] Reduced Motion  │   │
│  │                                                                      │   │
│  │ PREVIEW                                                              │   │
│  │ ┌───────────────────────────────────────────────────────────────┐   │   │
│  │ │ [Live preview of theme changes]                                │   │   │
│  │ └───────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [Save Theme] [Export] [Share to Marketplace]                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.7 Import/Export Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📥 IMPORT / 📤 EXPORT                                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Import] [Export] [Physical Collection]                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  IMPORT FROM                                                                │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐            │
│  │ 📦 Playnite      │ │ 📦 LaunchBox    │ │ 🔗 Link Accounts │            │
│  │ Import from      │ │ Import from      │ │ Steam, Epic,     │            │
│  │ Playnite DB      │ │ LaunchBox DB     │ │ GOG, etc.        │            │
│  │ [Start Import]   │ │ [Start Import]   │ │ [Connect]        │            │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘            │
│                                                                             │
│  EXPORT TO                                                                  │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐            │
│  │ 📄 JSON/CSV      │ │ 📊 Full Backup   │ │ 📱 Portable      │            │
│  │ Export library   │ │ Complete profile │ │ For new PC       │            │
│  │ metadata         │ │ with all data    │ │ migration        │            │
│  │ [Export]         │ │ [Create Backup]  │ │ [Generate]       │            │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.8 Diagnostics Tools

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🔍 DIAGNOSTICS                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Health Check] [Connections] [Logs] [Database] [Debug]                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SYSTEM HEALTH                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Component              │ Status        │ Last Checked │ Action      │   │
│  ├────────────────────────┼───────────────┼──────────────┼─────────────┤   │
│  │ Database               │ 🟢 Healthy    │ Just now     │ [Test]      │   │
│  │ Steam API              │ 🟢 Connected  │ 2m ago       │ [Test]      │   │
│  │ IGDB API               │ 🟢 Connected  │ 5m ago       │ [Test]      │   │
│  │ OpenAI API             │ 🟡 Rate Limited│ 1m ago      │ [Test]      │   │
│  │ Discord                │ 🟢 Connected  │ 30s ago      │ [Test]      │   │
│  │ Cloud Sync             │ 🟢 Synced     │ 10m ago      │ [Sync Now]  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  DATABASE TOOLS                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Size: 245 MB    Games: 142    Sessions: 1,247    Last Backup: 1d   │   │
│  │                                                                      │   │
│  │ [Compact Database] [Repair] [Clear Cache] [Reset to Default]        │   │
│  │ ⚠️ Warning: Some actions cannot be undone                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Tools Services Mapping

| Category | Service | Key Methods |
|----------|---------|-------------|
| Performance | `IPerformanceMonitor` | `GetCpuUsage()`, `GetGpuUsage()`, `GetFps()` |
| Performance | `ISystemResourceManager` | `OptimizeForGaming()`, `SetPowerProfile()` |
| Performance | `IDisplayCalibrator` | `GetDisplaySettings()`, `ApplySettings()` |
| Performance | `IAudioOptimizer` | `GetAudioDevices()`, `SetPerGameAudio()` |
| Performance | `IBatteryOptimizer` | `GetBatteryInfo()`, `SetProfile()` |
| Voice | `IVoiceCommandService` | `StartListening()`, `RegisterCommand()` |
| Voice | `ISpeechRecognitionService` | `Calibrate()`, `GetHistory()` |
| Automation | `IMacroManager` | `Record()`, `Playback()`, `Save()` |
| Automation | `IWorkflowAutomationService` | `CreateWorkflow()`, `Execute()` |
| Automation | `IBackupScheduler` | `Schedule()`, `GetBackups()` |
| Cloud | `ICloudGamingManager` | `GetProviders()`, `StartSession()` |
| Cloud | `INetworkQualityMonitor` | `MeasureLatency()`, `RunSpeedTest()` |
| Plugins | `IPluginManager` | `Install()`, `Uninstall()`, `GetMarketplace()` |
| Themes | `IThemeService` | `GetThemes()`, `ApplyTheme()`, `CreateTheme()` |
| Diagnostics | Health Checks | `CheckHealthAsync()` |

---

## 5. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Tools/ToolsView.axaml` | View | Tools shell container |
| `Views/Tools/ToolsSidebar.axaml` | View | Category sidebar |
| `Views/Tools/Performance/*.axaml` | Views | Performance tool views |
| `Views/Tools/Voice/*.axaml` | Views | Voice tool views |
| `Views/Tools/Automation/*.axaml` | Views | Automation tool views |
| `Views/Tools/Cloud/*.axaml` | Views | Cloud tool views |
| `Views/Tools/Plugins/*.axaml` | Views | Plugin tool views |
| `Views/Tools/Themes/*.axaml` | Views | Theme tool views |
| `Views/Tools/Import/*.axaml` | Views | Import/Export views |
| `Views/Tools/Diagnostics/*.axaml` | Views | Diagnostics views |
| `ViewModels/Tools/**/*.cs` | ViewModels | All Tools ViewModels |

---

*Next: [07_TERMINAL_SETTINGS.md](07_TERMINAL_SETTINGS.md)*
