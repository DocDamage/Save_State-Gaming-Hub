# UI Feature Surfacing Implementation Plan

**Date:** February 21, 2026  
**Version:** 1.0  
**Status:** Draft - Ready for Implementation  
**Priority:** High  
**Estimated Effort:** 120-160 hours (3-4 developer weeks)

---

## 📋 Executive Summary

This plan addresses the **28% UI coverage gap** identified in the UI Feature Coverage Audit. Currently, 32 backend services (out of 115) have no UI exposure - they're either CLI-only or background services without visible controls.

### Target Coverage Improvement
- **Current:** 72% (83/115 services)
- **Target:** 95%+ (110/115 services)
- **Gap to Close:** 32 services across 8 feature areas

---

## 🎯 Priority Matrix

| Priority | Feature | Services | Effort | Impact |
|----------|---------|----------|--------|--------|
| **P0 - Critical** | RetroArch Hub | 3 | 24h | High |
| **P0 - Critical** | Immersive Launch | 2 | 20h | High |
| **P1 - High** | System Health Dashboard | 7 | 16h | Medium |
| **P1 - High** | External Account Linking | 6 | 16h | Medium |
| **P2 - Medium** | Data Import/Export GUI | 2 | 12h | Low |
| **P2 - Medium** | Performance Dashboard | 5 | 16h | Medium |
| **P3 - Low** | Security/Auth UI | 4 | 12h | Low |
| **P3 - Low** | AI Administration | 7 | 16h | Low |

**Total Effort:** 132 hours (~3.5 weeks)

---

## 📦 Phase 1: RetroArch Hub (P0) - 24 hours

### Overview
Complete RetroArch integration UI - backend exists but zero frontend exposure.

### Services to Surface
| Service | Interface | Current Status |
|---------|-----------|---------------|
| RetroArch Service | `IRetroArchService` | Backend only |
| Playlist Parser | `RetroArchPlaylistParser` | Backend only |
| Core Manager | `RetroArchCoreManager` | Backend only |

### UI Components

#### 1.1 RetroArchTabView (New)
**Location:** `Views/Shell/RetroArchTabView.axaml`

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎮 RETROARCH HUB                                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌─────────────────────────────────────────────────────┐ │
│  │  LIBRARY     │  │  RECENTLY PLAYED                                    │ │
│  │              │  │  ┌─────────┬─────────┬─────────┬─────────┐         │ │
│  │ ▶ All Games  │  │  │  Game   │  Game   │  Game   │  Game   │         │ │
│  │ ♥ Favorites  │  │  │  Cover  │  Cover  │  Cover  │  Cover  │         │ │
│  │ 📁 Playlists │  │  └─────────┴─────────┴─────────┴─────────┘         │ │
│  │ 🎮 Cores     │  │                                                     │ │
│  │ ⚙️ Settings  │  │  ALL GAMES                                          │ │
│  │ 🔄 Netplay   │  │  ┌─────────┬─────────┬─────────┬─────────┐         │ │
│  │              │  │  │  Game   │  Game   │  Game   │  Game   │         │ │
│  │ SCAN STATUS  │  │  │  Cover  │  Cover  │  Cover  │  Cover  │         │ │
│  │ 🟢 Ready     │  │  │ Title   │ Title   │ Title   │ Title   │         │ │
│  │              │  │  │ System  │ System  │ System  │ System  │         │ │
│  └──────────────┘  │  └─────────┴─────────┴─────────┴─────────┘         │ │
│                    └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

**ViewModel:** `RetroArchTabViewModel`
- Properties: `Games`, `Playlists`, `Cores`, `RecentGames`, `SelectedPlaylist`
- Commands: `LaunchGameCommand`, `ScanLibraryCommand`, `InstallCoreCommand`

#### 1.2 RetroArchCoreManagerView (New)
**Location:** `Views/RetroArch/CoreManagerView.axaml`

| Component | Description |
|-----------|-------------|
| Core List | Downloaded + Available cores with system icons |
| Core Info | Version, system, compatibility |
| Install Button | Download/install cores |
| Update All | Batch update outdated cores |

**ViewModel:** `RetroArchCoreManagerViewModel`
- Injects: `IRetroArchService`, `RetroArchCoreManager`

#### 1.3 RetroArchPlaylistView (New)
**Location:** `Views/RetroArch/PlaylistView.axaml`

Features:
- Parse and display RetroArch playlists (.lpl files)
- Grid/List view toggle
- Playlist editing (add/remove games)
- Thumbnail support

#### 1.4 RetroArchNetplayView (New)
**Location:** `Views/RetroArch/NetplayView.axaml`

Features:
- Lobby browser
- Host game
- Join by IP
- Connection status

### Navigation Integration
Add to `HeaderBarView.axaml`:
```xml
<TabItem Header="🎮 RetroArch" IsSelected="{Binding IsRetroArchTabSelected}" />
```

### Files to Create
```
src/SaveState.Presentation/
├── ViewModels/
│   └── RetroArch/
│       ├── RetroArchTabViewModel.cs
│       ├── RetroArchCoreManagerViewModel.cs
│       ├── RetroArchPlaylistViewModel.cs
│       └── RetroArchNetplayViewModel.cs
└── Views/
    └── RetroArch/
        ├── RetroArchTabView.axaml
        ├── CoreManagerView.axaml
        ├── PlaylistView.axaml
        └── NetplayView.axaml
```

### Acceptance Criteria
- [ ] Browse all RetroArch games with cover art
- [ ] Install/manage cores
- [ ] View and edit playlists
- [ ] Launch games directly
- [ ] Netplay lobby browser
- [ ] Integration with main library (optional)

---

## 🎬 Phase 2: Immersive Launch Experience (P0) - 20 hours

### Overview
Implement Phase 4 "Immersive Launch" - cinematic game startup with AI briefings.

### Services to Surface
| Service | Interface | Purpose |
|---------|-----------|---------|
| Launch Experience Manager | `ILaunchExperienceManager` | Orchestrates launch sequence |
| Game Briefing Service | `IGameBriefingService` | AI-generated game info |

### UI Components

#### 2.1 LaunchExperienceOverlay (New)
**Location:** `Views/Overlays/LaunchExperienceOverlay.axaml`

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                         [Animated Background]                               │
│                                                                             │
│      ┌─────────────────────────────────────────────────────────────┐       │
│      │                                                             │       │
│      │                    🎮 GAME TITLE                            │       │
│      │                                                             │       │
│      │              [Cover Art - Large, Animated]                  │       │
│      │                                                             │       │
│      │     "AI-generated tagline about the game..."                │       │
│      │                                                             │       │
│      ├─────────────────────────────────────────────────────────────┤       │
│      │  📊 LAST SESSION    🎯 CURRENT OBJECTIVE    🏆 PROGRESS     │       │
│      │  Played: 12.5h      Complete Chapter 3      45% Complete    │       │
│      └─────────────────────────────────────────────────────────────┘       │
│                                                                             │
│      ┌─────────────────────────────────────────────────────────────┐       │
│      │  [████████░░░░░░░░░░░░]  Loading...                         │       │
│      └─────────────────────────────────────────────────────────────┘       │
│                                                                             │
│                         [Tips Carousel]                                     │
│      "Tip: Use stealth in the sewers to avoid the boss fight"               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**ViewModel:** `LaunchExperienceViewModel`
```csharp
public partial class LaunchExperienceViewModel : OverlayViewModelBase
{
    [ObservableProperty] private GameViewModel _game;
    [ObservableProperty] private GameBriefing _briefing;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _currentTip;
    [ObservableProperty] private bool _isVisible;
    
    public ICommand SkipCommand { get; }
    public ICommand CancelLaunchCommand { get; }
}
```

#### 2.2 GameBriefing Model
```csharp
public class GameBriefing
{
    public string GameTitle { get; set; }
    public string Tagline { get; set; }
    public string LastSessionSummary { get; set; }
    public string CurrentObjective { get; set; }
    public double ProgressPercentage { get; set; }
    public List<string> Tips { get; set; }
    public List<Achievement> RecentAchievements { get; set; }
    public TimeSpan TotalPlaytime { get; set; }
}
```

#### 2.3 Launch Configuration Dialog
**Location:** `Views/Dialogs/LaunchExperienceConfigDialog.axaml`

Settings:
- [ ] Enable cinematic launch
- [ ] Animation duration (5s, 10s, 15s, Manual)
- [ ] Show AI briefing
- [ ] Show tips
- [ ] Show last session summary
- [ ] Background style (Game art, Solid color, Animated)

### Integration Points

Modify `GameCardViewModel`:
```csharp
[RelayCommand]
private async Task LaunchGameAsync()
{
    if (UserPreferences.EnableCinematicLaunch)
    {
        await _overlayService.ShowLaunchExperienceAsync(Game);
    }
    
    await _gameLauncher.LaunchAsync(Game);
}
```

### Animation Requirements
- Cover art: Scale from 0.8 to 1.0, fade in (500ms)
- Text: Staggered fade in (200ms delay between elements)
- Progress bar: Smooth fill animation
- Background: Subtle parallax or gradient animation

### Files to Create
```
src/SaveState.Presentation/
├── ViewModels/
│   ├── Overlays/
│   │   └── LaunchExperienceViewModel.cs
│   └── Dialogs/
│       └── LaunchExperienceConfigDialogViewModel.cs
└── Views/
    ├── Overlays/
    │   └── LaunchExperienceOverlay.axaml
    └── Dialogs/
        └── LaunchExperienceConfigDialog.axaml
```

### Acceptance Criteria
- [ ] Cinematic overlay appears when launching games
- [ ] AI briefing generated and displayed
- [ ] Progress bar tracks game startup
- [ ] Tips carousel rotates
- [ ] Configurable in settings
- [ ] Can be skipped/disabled
- [ ] Smooth 60fps animations

---

## 🏥 Phase 3: System Health Dashboard (P1) - 16 hours

### Overview
Aggregate all health checks and monitoring into a visible system status dashboard.

### Services to Surface
| Service | Purpose |
|---------|---------|
| `IApplicationMetrics` | App performance metrics |
| `ICachePerformanceMonitor` | Cache statistics |
| `DatabaseHealthCheck` | Database connectivity |
| `ExternalApiHealthCheck` | External API status |
| `ResourceHealthCheck` | System resources |
| `PerformanceHealthCheck` | Performance metrics |
| `ErrorTrackingService` | Error tracking |

### UI Components

#### 3.1 SystemHealthView (New)
**Location:** `Views/Settings/SystemHealthView.axaml`

Tab in Settings dialog:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚙️ SETTINGS > SYSTEM HEALTH                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  OVERALL STATUS: 🟢 All Systems Operational                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐  │
│  │  🗄️ DATABASE         │  │  ☁️ EXTERNAL APIs    │  │  💾 CACHE          │  │
│  │                      │  │                      │  │                    │  │
│  │  Status: 🟢 Healthy  │  │  Steam:     🟢       │  │  Hit Rate: 94%     │  │
│  │  Response: 12ms      │  │  GOG:       🟢       │  │  Size: 145 MB      │  │
│  │  Last Backup: 2h ago │  │  Epic:      🟡       │  │  Entries: 1,240    │  │
│  │                      │  │  IGDB:      🟢       │  │  Evictions: 45     │  │
│  │  [Backup Now]        │  │  Discord:   🟢       │  │                    │  │
│  │  [View Logs]         │  │  RetroAch:  🟢       │  │  [Clear Cache]     │  │
│  └──────────────────────┘  └──────────────────────┘  └──────────────────┘  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  📊 SYSTEM RESOURCES                                                │   │
│  │                                                                     │   │
│  │  CPU: [████████░░░░░░░░░░] 45%    Memory: [████████████░░░░] 62%   │   │
│  │  GPU: [██████░░░░░░░░░░░░] 30%    Disk:   [████████████████] 85%   │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  ⚠️ RECENT ERRORS (Last 24h)                                        │   │
│  │                                                                     │   │
│  │  2026-02-21 14:32  Steam API timeout        [View Details]          │   │
│  │  2026-02-21 12:15  Cover art download failed [Retry]                │   │
│  │                                                                     │   │
│  │  [View Full Error Log]                                              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**ViewModel:** `SystemHealthViewModel`
```csharp
public partial class SystemHealthViewModel : ObservableObject
{
    [ObservableProperty] private HealthStatus _overallStatus;
    [ObservableProperty] private DatabaseHealth _databaseHealth;
    [ObservableProperty] private ObservableCollection<ApiHealthStatus> _apiStatuses;
    [ObservableProperty] private CacheStatistics _cacheStats;
    [ObservableProperty] private SystemResources _resources;
    [ObservableProperty] private ObservableCollection<ErrorLogEntry> _recentErrors;
    
    public ICommand RefreshCommand { get; }
    public ICommand ClearCacheCommand { get; }
    public ICommand ViewErrorLogCommand { get; }
}
```

#### 3.2 ErrorLogViewer Dialog (New)
**Location:** `Views/Dialogs/ErrorLogViewerDialog.axaml`

Features:
- Filter by severity, date, component
- Export log
- Clear log
- Detailed error view with stack trace

### Files to Create
```
src/SaveState.Presentation/
├── ViewModels/
│   ├── Settings/
│   │   └── SystemHealthViewModel.cs
│   └── Dialogs/
│       └── ErrorLogViewerDialogViewModel.cs
└── Views/
    ├── Settings/
│   │   └── SystemHealthView.axaml
│   └── Dialogs/
│       └── ErrorLogViewerDialog.axaml
```

### Acceptance Criteria
- [ ] Real-time health status display
- [ ] Individual component status (DB, APIs, Cache)
- [ ] System resource monitoring
- [ ] Recent errors list
- [ ] Auto-refresh every 30 seconds
- [ ] Alert notification on issues

---

## 🔗 Phase 4: External Account Linking (P1) - 16 hours

### Overview
Add UI for connecting Steam, GOG, Epic Games accounts.

### Services to Surface
| Service | Purpose |
|---------|---------|
| `ISteamApiClient` | Steam Web API |
| `IGogApiClient` | GOG Galaxy API |
| `IEpicApiClient` | Epic Games API |
| `IRetroAchievementsClient` | RetroAchievements.org |
| Discord services | Rich Presence |

### UI Components

#### 4.1 ConnectedAccountsView (New)
**Location:** `Views/Settings/ConnectedAccountsView.axaml`

Tab in Settings:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚙️ SETTINGS > CONNECTED ACCOUNTS                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  🎮 GAMING PLATFORMS                                                │   │
│  │                                                                     │   │
│  │  Steam        🟢 Connected as SteamUser        [Disconnect] [Sync]  │   │
│  │  GOG          ⚫ Not Connected                   [Connect]          │   │
│  │  Epic Games   🟢 Connected as EpicUser         [Disconnect] [Sync]  │   │
│  │  Origin       ⚪ Not Available (Deprecated)                         │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  🏆 ACHIEVEMENT SERVICES                                            │   │
│  │                                                                     │   │
│  │  RetroAchievements  🟢 Connected as RAUser  [Disconnect] [Sync]     │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  💬 SOCIAL                                                          │   │
│  │                                                                     │   │
│  │  Discord   🟢 Connected  [Disconnect]                               │   │
│  │                                                                     │   │
│  │  Rich Presence: [x] Show current game                               │   │
│  │  [x] Show playtime      [x] Allow join requests                     │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**ViewModel:** `ConnectedAccountsViewModel`
```csharp
public partial class ConnectedAccountsViewModel : ObservableObject
{
    [ObservableProperty] private AccountConnectionStatus _steamStatus;
    [ObservableProperty] private AccountConnectionStatus _gogStatus;
    [ObservableProperty] private AccountConnectionStatus _epicStatus;
    [ObservableProperty] private AccountConnectionStatus _retroAchievementsStatus;
    [ObservableProperty] private AccountConnectionStatus _discordStatus;
    
    public ICommand ConnectSteamCommand { get; }
    public ICommand DisconnectSteamCommand { get; }
    public ICommand SyncSteamCommand { get; }
    // ... etc
}
```

#### 4.2 AccountConnectionWizard (New)
**Location:** `Views/Dialogs/AccountConnectionWizard.axaml`

Step-by-step wizard for connecting accounts:
1. Select platform
2. OAuth or API key input
3. Permissions selection
4. Test connection
5. Import preferences

### OAuth Flow
For Steam (OpenID):
```csharp
// Open browser for Steam OAuth
var authUrl = $"https://steamcommunity.com/openid/login?openid.mode=checkid_setup&openid.return_to={callbackUrl}";
Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
```

For Epic (OAuth 2.0):
- Use embedded WebView or external browser
- Handle callback with custom protocol handler

### Files to Create
```
src/SaveState.Presentation/
├── ViewModels/
│   ├── Settings/
│   │   └── ConnectedAccountsViewModel.cs
│   └── Dialogs/
│       └── AccountConnectionWizardViewModel.cs
└── Views/
    ├── Settings/
│   │   └── ConnectedAccountsView.axaml
│   └── Dialogs/
│       └── AccountConnectionWizard.axaml
```

### Acceptance Criteria
- [ ] Connect/disconnect Steam account
- [ ] Connect/disconnect GOG account
- [ ] Connect/disconnect Epic Games account
- [ ] Connect/disconnect RetroAchievements
- [ ] Discord Rich Presence settings
- [ ] OAuth flow for supported platforms
- [ ] Sync button for manual library refresh
- [ ] Show connection errors

---

## 💾 Phase 5: Data Import/Export GUI (P2) - 12 hours

### Overview
Move data export/import from CLI-only to full GUI.

### Services to Surface
| Service | Purpose |
|---------|---------|
| `IDataExportService` | JSON/CSV/HTML export |
| `IDataImportService` | System restore/import |

### UI Components

#### 5.1 DataManagementView (New)
**Location:** `Views/Settings/DataManagementView.axaml`

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚙️ SETTINGS > DATA MANAGEMENT                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  📤 EXPORT DATA                                                     │   │
│  │                                                                     │   │
│  │  What to export:                                                    │   │
│  │  [x] Game Library        [x] Save States      [x] Achievements      │   │
│  │  [x] Play Sessions       [x] Collections      [x] Settings        │   │
│  │  [ ] MUGEN Data          [ ] Macros           [ ] ROMs            │   │
│  │                                                                     │   │
│  │  Format: [JSON ▼]  [CSV ▼]  [HTML ▼]                                │   │
│  │                                                                     │   │
│  │  [Export to File...]                                                │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  📥 IMPORT DATA                                                     │   │
│  │                                                                     │   │
│  │  Source: [Browse...]                                                │   │
│  │                                                                     │   │
│  │  Import Strategy:                                                   │   │
│  │  (•) Merge with existing data                                       │   │
│  │  ( ) Replace all data                                               │   │
│  │  ( ) Import only new entries                                        │   │
│  │                                                                     │   │
│  │  [Preview Import]  [Import Now]                                     │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  💾 BACKUP                                                          │   │
│  │                                                                     │   │
│  │  Last backup: 3 days ago                                            │   │
│  │  Backup location: C:\SaveState\Backups                              │   │
│  │                                                                     │   │
│  │  [Create Backup Now]  [Restore from Backup...]                      │   │
│  │  [Configure Auto-Backup]                                            │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**ViewModel:** `DataManagementViewModel`
```csharp
public partial class DataManagementViewModel : ObservableObject
{
    [ObservableProperty] private ExportOptions _exportOptions;
    [ObservableProperty] private string _selectedExportPath;
    [ObservableProperty] private string _selectedImportPath;
    [ObservableProperty] private ImportStrategy _importStrategy;
    [ObservableProperty] private DateTime _lastBackupDate;
    
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand PreviewImportCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand RestoreBackupCommand { get; }
}
```

#### 5.2 ImportPreviewDialog (New)
Show what will be imported before confirming:
- Games to add
- Conflicts to resolve
- Estimated time

### Files to Create
```
src/SaveState.Presentation/
├── ViewModels/
│   ├── Settings/
│   │   └── DataManagementViewModel.cs
│   └── Dialogs/
│       └── ImportPreviewDialogViewModel.cs
└── Views/
    ├── Settings/
│   │   └── DataManagementView.axaml
│   └── Dialogs/
│       └── ImportPreviewDialog.axaml
```

### Acceptance Criteria
- [ ] Export library to JSON/CSV/HTML
- [ ] Import from file with preview
- [ ] Selective export (choose data types)
- [ ] Merge/replace strategies
- [ ] Backup/restore functionality
- [ ] Progress indicators for long operations

---

## 📊 Phase 6: Enhanced Performance Dashboard (P2) - 16 hours

### Overview
Expand existing performance monitoring with full dashboard.

### Services to Surface
| Service | Purpose |
|---------|---------|
| `ISystemResourceManager` | Resource optimization |
| `IPerformanceMonitor` | Real-time monitoring |
| `ICachePerformanceMonitor` | Cache stats |
| `IApplicationMetrics` | App metrics |
| Error tracking | Error monitoring |

### UI Components

#### 6.1 PerformanceDashboardView (Enhanced)
**Location:** `Views/Settings/PerformanceDashboardView.axaml` (enhance existing)

Add sections:
- **Real-time Graphs:** CPU, GPU, Memory over time
- **Game Performance:** Per-game FPS statistics
- **Cache Performance:** Hit rates, evictions
- **Optimization Recommendations:** AI-suggested tweaks

### Files to Modify
```
src/SaveState.Presentation/
├── ViewModels/
│   └── Settings/
│       └── PerformanceDashboardViewModel.cs (enhance)
└── Views/
    └── Settings/
        └── PerformanceDashboardView.axaml (enhance)
```

---

## 🔐 Phase 7: Security & Auth UI (P3) - 12 hours

### Overview
Add UI for user management, API keys, and authentication settings.

### Services to Surface
| Service | Purpose |
|---------|---------|
| `IJwtTokenService` | JWT handling |
| `IUserRepository` | User management |
| `IRoleRepository` | Role management |
| `IApiKeyRepository` | API key management |

### UI Components

#### 7.1 UserManagementView (New)
**Location:** `Views/Settings/UserManagementView.axaml`

For multi-user scenarios:
- User list
- Create/edit users
- Role assignment
- Password reset
- Session management

#### 7.2 ApiKeyManagerView (New)
**Location:** `Views/Settings/ApiKeyManagerView.axaml`

For plugin developers:
```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚙️ SETTINGS > API KEYS                                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Your API Keys:                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  │ Name           │ Key (masked)     │ Created    │ Last Used   │ Actions │  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  │ Plugin Dev     │ ssk_••••••••xxxx │ 2026-01-15 │ 2026-02-20  │ [Revoke]│  │
│  │ External App   │ ssk_••••••••yyyy │ 2026-02-01 │ Never       │ [Revoke]│  │
│                                                                             │
│  [+ Generate New Key]                                                       │
│                                                                             │
│  ⚠️ Never share your API keys. Keep them secure.                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🤖 Phase 8: AI Administration (P3) - 16 hours

### Overview
Add UI for managing AI providers, models, and feedback.

### Services to Surface
| Service | Purpose |
|---------|---------|
| `ILlmProvider` | OpenAI/Groq |
| `IFeedbackLoop` | ML learning |
| `IShortTermMemory` | Conversation context |
| `IKnowledgeStore` | Vector storage |

### UI Components

#### 8.1 AiAdministrationView (New)
**Location:** `Views/Settings/AiAdministrationView.axaml`

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚙️ SETTINGS > AI ADMINISTRATION                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  🧠 LLM PROVIDERS                                                           │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  OpenAI:            [x] Enabled    Model: [GPT-4 ▼]    [Configure]          │
│  Groq:              [ ] Enabled               [Configure]                   │
│  Local (Ollama):    [ ] Enabled               [Configure]                   │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  💬 CONVERSATION MEMORY                                                     │
│                                                                             │
│  Context Window: [10] messages  [Clear Memory]                              │
│  Stored Conversations: 45                                                   │
│  Memory Usage: 12 MB                                                        │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  📚 KNOWLEDGE BASE                                                          │
│                                                                             │
│  Vector Store: SQLite (local)                                               │
│  Documents: 1,240                                                           │
│  Last Updated: 2026-02-20                                                   │
│                                                                             │
│  [Rebuild Index]  [Import Documents...]  [Export Knowledge]                 │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  🎯 FEEDBACK & LEARNING                                                     │
│                                                                             │
│  [x] Allow anonymous usage data collection                                  │
│  [x] Use feedback to improve recommendations                                │
│                                                                             │
│  Learning Stats:                                                            │
│  • Recommendations improved: 145                                            │
│  • User feedback incorporated: 89                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📅 Implementation Timeline

### Week 1: Critical Features
| Day | Task | Effort |
|-----|------|--------|
| 1-2 | RetroArch Hub - Core UI | 8h |
| 3-4 | RetroArch Hub - Netplay & Polish | 8h |
| 5 | Immersive Launch - Overlay | 8h |

### Week 2: Critical Features (cont.)
| Day | Task | Effort |
|-----|------|--------|
| 1-2 | Immersive Launch - Config & Animations | 12h |
| 3 | System Health Dashboard | 8h |
| 4-5 | External Account Linking | 12h |

### Week 3: Medium Priority
| Day | Task | Effort |
|-----|------|--------|
| 1-2 | Data Import/Export GUI | 12h |
| 3-4 | Performance Dashboard Enhancement | 12h |
| 5 | Security & Auth UI | 8h |

### Week 4: Polish & AI
| Day | Task | Effort |
|-----|------|--------|
| 1-2 | AI Administration | 12h |
| 3-4 | Testing & Bug Fixes | 12h |
| 5 | Documentation & Polish | 8h |

---

## ✅ Success Metrics

### Coverage Targets
| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Overall Coverage | 72% | TBD | 95%+ |
| RetroArch | 0% | 100% | ✅ |
| Immersive Launch | 0% | 100% | ✅ |
| System Health | 14% | 100% | ✅ |
| External APIs | 0% | 100% | ✅ |
| Data Import/Export | 0% | 100% | ✅ |

### Quality Metrics
- [ ] All new views have ViewModels with 80%+ test coverage
- [ ] UI animations run at 60fps
- [ ] Accessibility: All interactive elements keyboard accessible
- [ ] Localization: All strings externalized

---

## 📁 File Inventory

### New Files (Estimated)
```
src/SaveState.Presentation/
├── ViewModels/
│   ├── RetroArch/
│   │   ├── RetroArchTabViewModel.cs
│   │   ├── RetroArchCoreManagerViewModel.cs
│   │   ├── RetroArchPlaylistViewModel.cs
│   │   └── RetroArchNetplayViewModel.cs
│   ├── Overlays/
│   │   └── LaunchExperienceViewModel.cs
│   ├── Settings/
│   │   ├── SystemHealthViewModel.cs
│   │   ├── ConnectedAccountsViewModel.cs
│   │   ├── DataManagementViewModel.cs
│   │   ├── UserManagementViewModel.cs
│   │   ├── ApiKeyManagerViewModel.cs
│   │   └── AiAdministrationViewModel.cs
│   └── Dialogs/
│       ├── LaunchExperienceConfigDialogViewModel.cs
│       ├── ErrorLogViewerDialogViewModel.cs
│       ├── AccountConnectionWizardViewModel.cs
│       ├── ImportPreviewDialogViewModel.cs
│       └── ApiKeyGenerationDialogViewModel.cs
└── Views/
    ├── RetroArch/
    │   ├── RetroArchTabView.axaml
    │   ├── CoreManagerView.axaml
    │   ├── PlaylistView.axaml
    │   └── NetplayView.axaml
    ├── Overlays/
    │   └── LaunchExperienceOverlay.axaml
    ├── Settings/
    │   ├── SystemHealthView.axaml
    │   ├── ConnectedAccountsView.axaml
    │   ├── DataManagementView.axaml
    │   ├── UserManagementView.axaml
    │   ├── ApiKeyManagerView.axaml
    │   └── AiAdministrationView.axaml
    └── Dialogs/
        ├── LaunchExperienceConfigDialog.axaml
        ├── ErrorLogViewerDialog.axaml
        ├── AccountConnectionWizard.axaml
        ├── ImportPreviewDialog.axaml
        └── ApiKeyGenerationDialog.axaml
```

**Total New Files:** ~40 files (20 ViewModels, 20 Views)

---

## 🔗 Dependencies

### External Libraries (if needed)
- `Avalonia.Xaml.Interactions` - For animations (may already be included)
- `LiveChartsCore.SkiaSharpView.Avalonia` - For performance graphs

### Backend Dependencies
- All services already exist (backend complete)
- May need DTOs for some new features

---

## 📝 Notes

### Design Principles
1. **Consistency:** Follow existing UI patterns (Brushes, spacing, animations)
2. **Accessibility:** All features keyboard accessible, screen reader friendly
3. **Performance:** Virtualize lists, async loading, debounced inputs
4. **Localization:** All user-facing strings in resources

### Testing Strategy
- Unit tests for all ViewModels
- UI automation tests for critical paths
- Manual testing on Windows, Linux, macOS

---

**Plan Version:** 1.0  
**Last Updated:** February 21, 2026  
**Status:** Ready for Implementation
