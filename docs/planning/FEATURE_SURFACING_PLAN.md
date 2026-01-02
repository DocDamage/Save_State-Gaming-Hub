# 🎮 SaveState Reborn - Complete Feature Surfacing Plan

**Created**: January 1, 2026
**Status**: 📋 PLANNING
**Goal**: Surface all 90+ backend services in the UI with full feature parity to CLI

---

## 📊 Executive Summary

This document outlines the complete UI implementation plan to expose all SaveState Reborn features to users. The current UI exposes approximately **5%** of available functionality. This plan will achieve **100% feature coverage**.

### Current State vs Target State

| Metric | Current | Target |
|--------|---------|--------|
| **Views/Screens** | 6 | 45+ |
| **Feature Coverage** | ~5% | 100% |
| **Big Picture Mode** | Partial | Full |
| **CLI Parity** | None | Complete |
| **Customization** | None | Full |

### Design Principles

1. **Dashboard + Tabs Hybrid** - Central hub with tabbed navigation
2. **Progressive Disclosure** - Simple by default, advanced available
3. **Section Personalities** - Each major area has unique styling
4. **Full Customization** - Rearrangeable widgets, themes, layouts
5. **Accessibility First** - All features accessible to all users
6. **Offline Capable** - Graceful degradation with indicators

---

## 🏗️ Application Shell Architecture

### Main Navigation Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Logo] SaveState Reborn              🔍 Universal Search    🔔 ⚙️ 👤  │
├─────────────────────────────────────────────────────────────────────────┤
│  Dashboard │ Library │ MUGEN │ Analytics │ Social │ Tools │ Terminal   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                         [ Content Area ]                                │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│  Status: 🟢 Online │ 📊 142 Games │ ⏱️ 2.5h Today │ 🔄 Syncing...      │
└─────────────────────────────────────────────────────────────────────────┘
```

### Tab Structure (7 Primary Tabs)

| Tab | Icon | Sections | Personality |
|-----|------|----------|-------------|
| **Dashboard** | 🏠 | Activity, Quick Actions, Widgets | Dynamic, Personal |
| **Library** | 📚 | Games, Collections, Backlog, ROMs | Clean, Organized |
| **MUGEN** | 🥊 | Roster, Battles, Training, Network | Fighting Game Arcade |
| **Analytics** | 📊 | Stats, Heatmaps, Goals, Reports | Data-Driven, Charts |
| **Social** | 👥 | Friends, Reviews, Collections, Discord | Community, Social |
| **Tools** | 🛠️ | Performance, Automation, Voice, Plugins | Technical, Utility |
| **Terminal** | 💻 | CLI, Command History, Scripts | Hacker, Matrix |

---

## 📱 View Specifications

### 1. Dashboard Hub (Home)

**Purpose**: Central command center with personalized widgets

#### 1.1 Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│                         DASHBOARD                                    │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐  ┌─────────────────────┐                   │
│  │ 🎮 QUICK ACTIONS    │  │ 📈 TODAY'S STATS    │                   │
│  │ ▶ Continue Playing  │  │ Playtime: 2.5h      │                   │
│  │ 🔍 Scan for Games   │  │ Sessions: 3         │                   │
│  │ 🎲 Random Game      │  │ Achievements: 5     │                   │
│  │ 🤖 AI Recommend     │  │                     │                   │
│  └─────────────────────┘  └─────────────────────┘                   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │ 📰 ACTIVITY FEED                                             │    │
│  │ • You played Elden Ring for 2 hours                          │    │
│  │ • Achievement Unlocked: Dragon Slayer                        │    │
│  │ • Friend @GamerX started playing Cyberpunk 2077              │    │
│  │ • Sale Alert: Hollow Knight -75% ($3.74)                     │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │
│  │ 🆕 Recently  │ │ 🎯 Goals     │ │ 🔥 Trending  │ │ 📅 Coming  │  │
│  │    Added     │ │   Progress   │ │    Games     │ │    Soon    │  │
│  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

#### 1.2 Dashboard Widgets (Customizable)

| Widget | Size | Description | Services Used |
|--------|------|-------------|---------------|
| **Quick Actions** | 1x2 | Launch, Scan, Random, AI Recommend | Multiple |
| **Today's Stats** | 1x1 | Playtime, sessions, achievements | `IAnalyticsService` |
| **Activity Feed** | 2x2 | Recent activity stream | `IFriendActivityService`, `IGameSessionRepository` |
| **Recently Added** | 1x2 | Games added recently | `IGameRepository` |
| **Goal Progress** | 1x1 | Current goals status | `IGoalService` |
| **Now Playing (Friends)** | 1x2 | What friends are playing | `IDiscordPresenceService` |
| **Sale Alerts** | 1x1 | Wishlisted games on sale | IsThereAnyDeal API |
| **Gaming Heatmap** | 2x1 | GitHub-style activity | `IAnalyticsService` |
| **AI Recommendations** | 1x2 | AI game suggestions | `IRecommendationService` |
| **Performance Monitor** | 1x1 | CPU/GPU/RAM mini view | `IPerformanceMonitor` |
| **Upcoming Games** | 1x1 | Release calendar | IGDB API |
| **MUGEN Quick Match** | 1x1 | Start random MUGEN battle | `IMugenLauncher` |
| **Voice Command Status** | 1x1 | Voice listening indicator | `IVoiceCommandService` |
| **Cloud Sync Status** | 1x1 | Sync progress/status | `ISyncService` |
| **Year in Review Teaser** | 1x1 | Gaming wrapped preview | `IAnalyticsService` |

---

### 2. Library Tab

**Purpose**: Complete game collection management

#### 2.1 Library Main View

```
┌─────────────────────────────────────────────────────────────────────┐
│  LIBRARY                                                             │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ [Grid ▼] [Sort: Recently Played ▼] [Filter ▼] 🔍 Search...     ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                      │
│  📁 All Games (142) │ ⭐ Favorites │ 📋 Backlog │ ✅ Completed │ ...│
│                                                                      │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐            │
│  │ Cover  │ │ Cover  │ │ Cover  │ │ Cover  │ │ Cover  │            │
│  │ Art    │ │ Art    │ │ Art    │ │ Art    │ │ Art    │            │
│  │        │ │        │ │        │ │        │ │        │            │
│  │ Title  │ │ Title  │ │ Title  │ │ Title  │ │ Title  │            │
│  │ 12.5h  │ │ 3.2h   │ │ --     │ │ 45h    │ │ 8h     │            │
│  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘            │
└─────────────────────────────────────────────────────────────────────┘
```

#### 2.2 Library Sub-Views

| Sub-View | Description | Services |
|----------|-------------|----------|
| **All Games** | Complete game list with filters | `IGameRepository` |
| **Favorites** | Starred games | `IVirtualCollectionService` |
| **Backlog** | Games to play | `IBacklogService` |
| **Completed** | Finished games | Status filter |
| **Currently Playing** | Active games | Session tracking |
| **Collections** | Virtual collections | `IVirtualCollectionService` |
| **Platforms** | By platform | `IPlatformRepository` |
| **ROMs** | ROM management | `IRomFileRepository` |
| **Recently Added** | New additions | Date filter |
| **Hidden** | Hidden games | Visibility filter |

#### 2.3 Game Detail View (Full Management)

```
┌─────────────────────────────────────────────────────────────────────┐
│  ← Back                                           ⋮ More Actions    │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐                                                   │
│  │              │  ELDEN RING                                       │
│  │   Cover Art  │  ⭐⭐⭐⭐⭐ (Your Rating: 10/10)                    │
│  │              │  FromSoftware • 2022 • Action RPG                 │
│  │              │                                                   │
│  │              │  [▶ PLAY]  [⚙️ Settings]  [📁 Files]             │
│  └──────────────┘                                                   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ Overview │ Save States │ Achievements │ Sessions │ Notes │ Mods ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                      │
│  📊 STATS                    📝 DESCRIPTION                         │
│  Playtime: 125.5 hours       Open-world action RPG...               │
│  Sessions: 47                                                        │
│  Last Played: Yesterday      🎯 HLTB: 55h Main │ 132h Complete      │
│  Achievements: 42/42 (100%)                                          │
│                              🏷️ TAGS                                │
│  🤖 AI BRIEFING              Souls-like, Open World, Fantasy        │
│  "You left off at..."                                                │
│                              💰 PRICE HISTORY                        │
│                              Current: $59.99 │ Lowest: $41.99       │
└─────────────────────────────────────────────────────────────────────┘
```

#### 2.4 Game Detail Tabs

| Tab | Features | Services |
|-----|----------|----------|
| **Overview** | Description, stats, HLTB, prices | `IMetadataService`, HLTB API |
| **Save States** | Branch tree, auto-save config, timeline | `ISaveStateManager`, `ISaveStateBranchingService` |
| **Achievements** | Achievement list, progress, RetroAchievements | `IAchievementRepository`, `IRetroAchievementsClient` |
| **Sessions** | Play history, session details | `IGameSessionRepository` |
| **Notes** | Personal notes, journal | New: `IGameNotesRepository` |
| **Mods** | Installed mods, Nexus integration | New: `IModManagerService` |
| **Screenshots** | Screenshot gallery | New: `IScreenshotService` |
| **Performance** | Optimization profile, FPS history | `IPerformanceProfiler` |

---

### 3. MUGEN Tab (Fighting Game Personality)

**Purpose**: Complete MUGEN/IKEMEN fighting game management

#### 3.1 MUGEN Navigation

| Section | Description | Services |
|---------|-------------|----------|
| **Roster** | Character grid with selection | `IMugenCharacterLoader` |
| **Death Battle** | AI battle simulator | `IDeathMatchSimulator` |
| **Training Mode** | Combo practice, frame data | `IMugenTrainingService` |
| **Replay Theater** | Watch/analyze matches | Plugin: MugenReplay |
| **Online Hub** | Lobbies, matchmaking | Plugin: MugenNetwork |
| **Character Fusion** | AI character creation | Plugin: MugenFusion |
| **Tournament Mode** | Bracket management | `IMugenTournamentService` |
| **Achievements** | MUGEN achievements | Plugin: MugenAchievements |
| **Leaderboards** | Rankings, stats | `IMugenStatsService` |
| **Coach Panel** | AI coaching, predictions | `IMugenCoachService` |
| **Stage Manager** | Stage selection/editing | New: `IMugenStageManager` |
| **Character Editor** | Edit character properties | New: `IMugenCharacterEditor` |

#### 3.2 MUGEN Training Mode View

```
┌─────────────────────────────────────────────────────────────────────┐
│  TRAINING MODE                                         [Exit]       │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │                    [ GAME VIEWPORT ]                          │   │
│  │                                                               │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │
│  │ FRAME DATA   │ │ INPUT DISPLAY│ │ COMBO COUNTER│ │ RECORDING  │  │
│  │ Startup: 5f  │ │ ↓↘→ + P     │ │ 23 HITS      │ │ ● REC      │  │
│  │ Active: 3f   │ │              │ │ 4523 DMG     │ │ ▶ PLAY     │  │
│  │ Recovery: 12f│ │              │ │              │ │ 💾 SAVE    │  │
│  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │
│                                                                      │
│  AI DUMMY: [Aggressive ▼]  INFINITE HEALTH: [✓]  RESET: [Space]    │
└─────────────────────────────────────────────────────────────────────┘
```

---

### 4. Analytics Tab (Data Visualization Personality)

**Purpose**: Gaming statistics, tracking, and insights

#### 4.1 Analytics Dashboard

```
┌─────────────────────────────────────────────────────────────────────┐
│  ANALYTICS                               [Export PDF] [Date Range]  │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ 📅 GAMING HEATMAP (GitHub Style)                                ││
│  │ ░░▓▓░░▓▓▓▓░░░░▓▓▓▓░░░░▓▓░░▓▓▓▓░░░░▓▓▓▓░░░░▓▓░░▓▓▓▓░░          ││
│  │ Mon Tue Wed Thu Fri Sat Sun                                     ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                      │
│  Overview │ Playtime │ Sessions │ Achievements │ Goals │ Reports    │
│                                                                      │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐        │
│  │ 📊 TOTAL TIME   │ │ 🎮 TOP GAMES    │ │ 🏆 ACHIEVEMENTS │        │
│  │    1,247 hours  │ │ 1. Elden Ring   │ │ This Week: 12   │        │
│  │    +23h vs last │ │ 2. Cyberpunk    │ │ Total: 1,847    │        │
│  │    ▲ 12%        │ │ 3. Hollow Knight│ │ Completion: 67% │        │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘        │
└─────────────────────────────────────────────────────────────────────┘
```

#### 4.2 Analytics Sub-Views

| Sub-View | Features | Services |
|----------|----------|----------|
| **Overview** | Summary dashboard | `IAnalyticsService` |
| **Playtime** | Time analytics, trends | `IAnalyticsService` |
| **Sessions** | Session history, details | `IGameSessionRepository` |
| **Achievements** | Achievement tracking | `IAchievementRepository` |
| **Goals** | Goal setting, progress | `IGoalService` |
| **Reports** | Exportable reports (PDF/HTML) | New: `IReportGeneratorService` |
| **Year in Review** | Annual gaming wrapped | New: `IYearInReviewService` |
| **Insights** | AI-powered insights | `IRecommendationService` |

---

### 5. Social Tab (Community Personality)

**Purpose**: Social features, reviews, sharing

#### 5.1 Social Hub

```
┌─────────────────────────────────────────────────────────────────────┐
│  SOCIAL HUB                                                          │
├─────────────────────────────────────────────────────────────────────┤
│  Friends │ Reviews │ Collections │ Activity │ Discord               │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ 🟢 FRIENDS ONLINE (3/12)                                        ││
│  │ ┌────────┐ ┌────────┐ ┌────────┐                                ││
│  │ │ Avatar │ │ Avatar │ │ Avatar │                                ││
│  │ │ Alex   │ │ Sam    │ │ Jordan │                                ││
│  │ │ Playing│ │ Playing│ │ Online │                                ││
│  │ │ Elden  │ │ Hades  │ │        │                                ││
│  │ └────────┘ └────────┘ └────────┘                                ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ 📰 ACTIVITY FEED                                                ││
│  │ • @Alex started playing Elden Ring                    2m ago    ││
│  │ • @Sam unlocked achievement "God Slayer"               15m ago   ││
│  │ • @Jordan reviewed Hollow Knight ⭐⭐⭐⭐⭐                 1h ago   ││
│  └─────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
```

#### 5.2 Social Sub-Views

| Sub-View | Features | Services |
|----------|----------|----------|
| **Friends** | Friend list, activity | `IFriendRepository`, `IFriendActivityService` |
| **Reviews** | Write/read game reviews | `IGameReviewService` |
| **Collections** | Shared collections, share codes | `ISharedCollectionService` |
| **Activity** | Social activity feed | `IFriendActivityService` |
| **Discord** | Discord integration settings | `IDiscordPresenceService` |

---

### 6. Tools Tab (Technical/Utility Personality)

**Purpose**: Power user tools, system management

#### 6.1 Tools Navigation

```
┌─────────────────────────────────────────────────────────────────────┐
│  TOOLS                                                               │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐                                               │
│  │ 🔧 CATEGORIES    │  ┌───────────────────────────────────────┐   │
│  │                  │  │                                        │   │
│  │ ⚡ Performance   │  │         [ Tool Content Area ]          │   │
│  │ 🎙️ Voice         │  │                                        │   │
│  │ 🤖 Automation    │  │                                        │   │
│  │ ☁️ Cloud         │  └───────────────────────────────────────┘   │
│  │ 💾 Save States   │                                               │
│  │ 🔌 Plugins       │                                               │
│  │ 🎨 Themes        │                                               │
│  │ 📥 Import        │                                               │
│  │ 📤 Export        │                                               │
│  │ 🔍 Diagnostics   │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

#### 6.2 Tools Sub-Sections

##### 6.2.1 Performance Tools

| View | Features | Services |
|------|----------|----------|
| **System Monitor** | Real-time CPU/GPU/RAM/FPS | `IPerformanceMonitor` |
| **Optimization** | System resource manager | `ISystemResourceManager` |
| **Display Calibration** | HDR, refresh rate, VSync | `IDisplayCalibrator` |
| **Audio Optimizer** | Audio settings per game | `IAudioOptimizer` |
| **Battery Manager** | Steam Deck power profiles | `IBatteryOptimizer` |
| **Game Profiles** | Per-game optimization profiles | `IPerformanceProfiler` |

##### 6.2.2 Voice Tools

| View | Features | Services |
|------|----------|----------|
| **Voice Dashboard** | On/off, listening indicator | `IVoiceCommandService` |
| **Command Editor** | Create/edit voice commands | `IVoiceCommandService` |
| **Calibration** | Microphone calibration wizard | `ISpeechRecognitionService` |
| **Command History** | Recent voice command log | `IVoiceCommandService` |
| **Language Settings** | Language/accent selection | `ISpeechRecognitionService` |

##### 6.2.3 Automation Tools

| View | Features | Services |
|------|----------|----------|
| **Macro Manager** | Record/playback macros | `IMacroManager` |
| **Visual Workflow Builder** | Drag-drop automation | `IWorkflowAutomationService` |
| **Backup Scheduler** | Scheduled backups calendar | `IBackupScheduler` |
| **Task List** | Scheduled automation tasks | `IWorkflowAutomationService` |

##### 6.2.4 Cloud Tools

| View | Features | Services |
|------|----------|----------|
| **Cloud Providers** | GeForce Now, Xbox Cloud, Luna | `ICloudGamingManager` |
| **Network Quality** | Latency, jitter, packet loss | `INetworkQualityMonitor` |
| **Cloud Library** | Cloud-available games | `ICloudGamingManager` |
| **Sync Settings** | Sync configuration | `ISyncService` |

##### 6.2.5 Save State Tools

| View | Features | Services |
|------|----------|----------|
| **Branch Explorer** | Visual branch tree | `ISaveStateBranchingService` |
| **Auto-Save Config** | Global auto-save settings | `IAutoSaveManager` |
| **State Diff Viewer** | Compare save states | `ISaveStateManager` |

##### 6.2.6 Plugin Tools

| View | Features | Services |
|------|----------|----------|
| **Plugin Marketplace** | Browse/install plugins | `IPluginManager` |
| **Installed Plugins** | Manage installed plugins | `IPluginManager` |
| **Plugin Settings** | Per-plugin configuration | Plugin-specific |
| **Create Plugin** | Plugin development guide | Documentation |

##### 6.2.7 Theme Tools

| View | Features | Services |
|------|----------|----------|
| **Theme Browser** | Pre-built themes | `IThemeService` |
| **Theme Editor** | Full theme customization | `IThemeService` |
| **Accent Picker** | Quick accent color | `IThemeService` |
| **Theme Marketplace** | Download/share themes | New: Theme API |

##### 6.2.8 Import/Export Tools

| View | Features | Services |
|------|----------|----------|
| **Import Wizard** | Multi-source import | Multiple |
| **Playnite Import** | Playnite library import | Plugin: PlayniteImporter |
| **LaunchBox Import** | LaunchBox import | New: LaunchBoxImporter |
| **Steam Link** | Steam account linking | `ISteamApiClient` |
| **Export Library** | JSON/CSV export | Export service |
| **Full Backup** | Complete profile export | Backup service |
| **Physical Collection** | Physical media tracker | New: `IPhysicalCollectionService` |

##### 6.2.9 Diagnostics

| View | Features | Services |
|------|----------|----------|
| **Health Check** | System health dashboard | Health checks |
| **Connection Test** | Test external API connections | Multiple |
| **Error Log** | Error history viewer | `ErrorTrackingService` |
| **Database Tools** | Cleanup, compact, repair | Database utilities |

---

### 7. Terminal Tab (Hacker/Matrix Personality)

**Purpose**: Full CLI access within the UI

#### 7.1 Terminal View

```
┌─────────────────────────────────────────────────────────────────────┐
│  TERMINAL                                              [Clear] [⚙️] │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ SaveState Reborn CLI v2.0.0                                     ││
│  │ Type 'help' for available commands                              ││
│  │                                                                  ││
│  │ > savestate list                                                 ││
│  │ Found 142 games in library                                       ││
│  │ ┌────────────────────────────────────────────────────────────┐  ││
│  │ │ ID     Title              Platform    Playtime             │  ││
│  │ │ 1      Elden Ring         Steam       125.5h               │  ││
│  │ │ 2      Cyberpunk 2077     GOG         89.2h                │  ││
│  │ │ ...                                                         │  ││
│  │ └────────────────────────────────────────────────────────────┘  ││
│  │                                                                  ││
│  │ > _                                                              ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 💡 QUICK COMMANDS: [list] [scan] [recommend] [voice listen] │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

#### 7.2 Terminal Features

| Feature | Description |
|---------|-------------|
| **Full CLI Access** | All 14 command groups available |
| **Command History** | Arrow up/down navigation |
| **Auto-Complete** | Tab completion for commands |
| **Quick Commands** | One-click common commands |
| **Script Editor** | Create/save command scripts |
| **Output Formatting** | Rich output with tables/colors |
| **Copy/Export** | Copy output to clipboard |

---

## 🎛️ Floating Panels & Overlays

### Overlay System

| Overlay | Trigger | Description | Services |
|---------|---------|-------------|----------|
| **AI Assistant** | Chat bubble / Ctrl+Shift+A | Sliding chat panel | `IGameAssistantService` |
| **Performance HUD** | Toggle / F3 | Gaming overlay | `IPerformanceMonitor` |
| **Voice Indicator** | When listening | Waveform + transcription | `IVoiceCommandService` |
| **Notifications** | Automatic | Toast notifications | Notification system |
| **Command Palette** | Ctrl+Shift+P | Universal command search | All services |
| **Quick Search** | Ctrl+K | Universal search | Contextual |

### AI Assistant Panel

```
┌─────────────────────────────────────────┐
│  🤖 AI ASSISTANT                    ✕   │
├─────────────────────────────────────────┤
│                                         │
│  🗣️ "How do I beat Margit in Elden Ring?"
│                                         │
│  🤖 Margit is a challenging boss...     │
│  Here are some strategies:              │
│  1. Level up to at least 25            │
│  2. Use Spirit Ashes for distraction   │
│  3. Roll through his delayed attacks   │
│  ...                                    │
│                                         │
├─────────────────────────────────────────┤
│  [Type message...          ] [🎤] [Send]│
└─────────────────────────────────────────┘
```

### Command Palette

```
┌─────────────────────────────────────────────────────────────────┐
│  > savestate                                                     │
├─────────────────────────────────────────────────────────────────┤
│  🎮 savestate list              List all games                  │
│  🔍 savestate scan              Scan for new games              │
│  🎲 savestate randomize         Pick random game                │
│  🎙️ savestate voice listen     Start voice recognition          │
│  📊 savestate stats             Show gaming statistics          │
│  ⚙️ savestate settings          Open settings                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Settings Architecture

### Settings Hub (Unified + Inline)

```
┌─────────────────────────────────────────────────────────────────────┐
│  SETTINGS                                          🔍 Search...     │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐                                               │
│  │ 📁 CATEGORIES    │  ┌───────────────────────────────────────┐   │
│  │                  │  │                                        │   │
│  │ 👤 Account       │  │       [ Settings Content Area ]        │   │
│  │ 🎨 Appearance    │  │                                        │   │
│  │ 🎮 Gaming        │  │                                        │   │
│  │ 🔊 Audio         │  └───────────────────────────────────────┘   │
│  │ ⌨️ Shortcuts     │                                               │
│  │ 🔌 Integrations  │                                               │
│  │ ☁️ Cloud & Sync  │                                               │
│  │ 🔒 Privacy       │                                               │
│  │ ♿ Accessibility │                                               │
│  │ 🗃️ Data          │                                               │
│  │ 🔧 Advanced      │                                               │
│  │ ℹ️ About         │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Settings Categories

| Category | Settings |
|----------|----------|
| **Account** | Profile, login, API keys |
| **Appearance** | Theme, accent, font size, layout |
| **Gaming** | Default launch options, Steam Deck |
| **Audio** | System audio, notification sounds |
| **Shortcuts** | Keyboard shortcuts, customization |
| **Integrations** | Discord, Twitch, RetroAchievements |
| **Cloud & Sync** | Cloud providers, sync settings |
| **Privacy** | Data collection, analytics opt-out |
| **Accessibility** | High contrast, screen reader, motion, font scaling |
| **Data** | Retention, cleanup, database |
| **Advanced** | Developer options, logging, debug |
| **About** | Version, licenses, credits |

---

## 🎮 Big Picture Mode (Controller UI)

### Big Picture Shell

All views must be accessible via controller with:

- D-pad navigation
- A = Select, B = Back
- Bumpers = Tab switching
- Triggers = Quick actions
- Start = Command palette
- Select = Settings

### Controller-Optimized Views

| View | Controller Optimizations |
|------|--------------------------|
| **Dashboard** | Widget focus navigation |
| **Library Grid** | Game grid with quick launch |
| **Game Detail** | Tab carousel navigation |
| **MUGEN** | Fighting game inputs |
| **Settings** | Large touch targets |
| **On-Screen Keyboard** | Virtual keyboard |

---

## 📋 New Services Required

### Backend Services to Add

| Service | Purpose | Priority |
|---------|---------|----------|
| `IGameNotesService` | Personal game notes/journal | High |
| `IModManagerService` | Mod integration (Nexus) | Medium |
| `IScreenshotService` | Screenshot management | Medium |
| `IYearInReviewService` | Annual gaming wrapped | Medium |
| `IReportGeneratorService` | PDF/HTML report export | Medium |
| `IPhysicalCollectionService` | Physical media tracking | Low |
| `IDealTrackingService` | IsThereAnyDeal integration | High |
| `IHowLongToBeatService` | HLTB integration | High |
| `IDuplicateDetectionService` | Find duplicate games | Medium |
| `IGameStreamingService` | Local game streaming | Low |
| `IAttractModeService` | Screensaver/attract mode | Low |
| `IGameRandomizerService` | Random game picker | Medium |
| `IInternalAchievementService` | App gamification | Low |
| `IMugenStageManager` | MUGEN stage management | Medium |
| `IMugenCharacterEditor` | MUGEN character editing | Low |

---

## 📐 View Count Summary

### Total Views Required

| Area | Views | Sub-Views | Total |
|------|-------|-----------|-------|
| **Dashboard** | 1 | 15 widgets | 16 |
| **Library** | 10 | 8 game detail tabs | 18 |
| **MUGEN** | 12 | - | 12 |
| **Analytics** | 8 | - | 8 |
| **Social** | 5 | - | 5 |
| **Tools** | 9 categories | 25+ sub-views | 34 |
| **Terminal** | 1 | - | 1 |
| **Settings** | 12 | - | 12 |
| **Overlays** | 6 | - | 6 |
| **Big Picture** | 6 | - | 6 |
| **TOTAL** | | | **118** |

---

## 🗓️ Implementation Phases

### Phase 1: Core Shell (Week 1-2) ✅ COMPLETED

- [x] Main navigation shell with 7 tabs
- [x] Status bar with indicators
- [x] Command palette (Ctrl+Shift+P)
- [x] Notification system
- [x] Settings hub structure

### Phase 2: Dashboard (Week 3-4) ✅ COMPLETED

- [x] Dashboard widget framework
- [x] 5 core widgets implemented (Quick Actions, Today's Stats, Activity Feed, Recently Added, Goals Progress)
- [x] Widget customization/rearrangement
- [x] Activity feed

### Phase 3: Library Enhancement (Week 5-6)

- [ ] Enhanced library views
- [ ] Full game detail view with all tabs
- [ ] Multi-select operations
- [ ] Import/export wizards

### Phase 4: Analytics & Social (Week 7-8)

- [ ] Analytics dashboard with charts
- [ ] Gaming heatmap
- [ ] Year in review
- [ ] Social hub and activity feed

### Phase 5: Tools & Utilities (Week 9-10)

- [ ] Performance tools
- [ ] Voice command UI
- [ ] Automation visual builder
- [ ] Plugin marketplace

### Phase 6: Terminal & CLI (Week 11)

- [ ] Integrated terminal
- [ ] All CLI commands exposed
- [ ] Script editor

### Phase 7: MUGEN Enhancement (Week 12)

- [ ] Training mode UI
- [ ] Tournament brackets
- [ ] Replay theater
- [ ] Character/stage editor stubs

### Phase 8: Big Picture Mode (Week 13-14)

- [ ] Controller navigation for all views
- [ ] On-screen keyboard
- [ ] Big Picture optimizations

### Phase 9: Polish & Accessibility (Week 15-16)

- [ ] High contrast themes
- [ ] Screen reader optimization
- [ ] Reduced motion mode
- [ ] Font scaling
- [ ] Multi-monitor support

---

## 📊 Success Metrics

| Metric | Target |
|--------|--------|
| **Feature Coverage** | 100% of services exposed |
| **CLI Parity** | All CLI commands in UI |
| **Big Picture Coverage** | 100% controller-navigable |
| **Accessibility Score** | WCAG 2.1 AA compliant |
| **Performance** | <100ms view transitions |
| **Customization** | All widgets rearrangeable |

---

## 📎 Appendices

### A. CLI to UI Mapping

| CLI Command Group | UI Location |
|-------------------|-------------|
| `GameCommands` | Library tab |
| `SaveStateCommands` | Tools > Save States, Game Detail |
| `MugenCommands` | MUGEN tab |
| `VoiceCommands` | Tools > Voice |
| `CloudCommands` | Tools > Cloud |
| `PerformanceCommands` | Tools > Performance |
| `NetworkCommands` | Tools > Cloud > Network |
| `AutomationCommands` | Tools > Automation |
| `SocialCommands` | Social tab |
| `BacklogCommands` | Library > Backlog |
| `CoachingCommands` | MUGEN > Coach Panel |
| `MemoryCommands` | Tools > Diagnostics |

### B. Keyboard Shortcuts (Default)

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+P` | Command Palette |
| `Ctrl+K` | Quick Search |
| `Ctrl+Shift+A` | AI Assistant |
| `F3` | Performance Overlay |
| `Ctrl+,` | Settings |
| `Ctrl+Tab` | Next Tab |
| `Ctrl+Shift+Tab` | Previous Tab |
| `Ctrl+1-7` | Jump to Tab 1-7 |
| `F11` | Toggle Big Picture |
| `Ctrl+M` | Toggle Voice Listening |

---

## 📚 Detailed Specification Documents

This plan is supported by 9 detailed specification documents with complete wireframes, ViewModels, and implementation details:

| # | Document | Description | Views Covered |
|---|----------|-------------|---------------|
| 1 | [01_SHELL_AND_NAVIGATION.md](surfacing/01_SHELL_AND_NAVIGATION.md) | Application shell, title bar, header, status bar, overlays, navigation service, shortcuts | 12 |
| 2 | [02_DASHBOARD_HUB.md](surfacing/02_DASHBOARD_HUB.md) | Widget system, 20 widget specifications, customization, layout persistence | 20 |
| 3 | [03_LIBRARY_TAB.md](surfacing/03_LIBRARY_TAB.md) | Library sidebar, toolbar, game views, multi-select, game detail tabs, add wizard | 18 |
| 4 | [04_MUGEN_TAB.md](surfacing/04_MUGEN_TAB.md) | Roster, death battle, training mode, replays, tournaments, fusion, coach | 12 |
| 5 | [05_ANALYTICS_SOCIAL.md](surfacing/05_ANALYTICS_SOCIAL.md) | Analytics dashboard, heatmaps, goals, year in review, friends, reviews, Discord | 13 |
| 6 | [06_TOOLS_TAB.md](surfacing/06_TOOLS_TAB.md) | Performance, voice, automation, cloud, plugins, themes, import/export, diagnostics | 25 |
| 7 | [07_TERMINAL_SETTINGS.md](surfacing/07_TERMINAL_SETTINGS.md) | Terminal with CLI integration, script editor, settings hub, all categories | 17 |
| 8 | [08_OVERLAYS_BIGPICTURE.md](surfacing/08_OVERLAYS_BIGPICTURE.md) | Command palette, AI assistant, notifications, performance HUD, Big Picture Mode | 13 |
| 9 | [09_IMPLEMENTATION_TIMELINE.md](surfacing/09_IMPLEMENTATION_TIMELINE.md) | 16-week timeline, task breakdown, phase gates, success metrics | - |

### Document Contents Summary

Each specification document includes:

- **ASCII Wireframes** - Visual layout specifications for every view
- **Component Specifications** - Property tables, bindings, styling requirements
- **ViewModel Interfaces** - Complete C# code samples with commands and properties
- **Service Mappings** - Which backend services each view consumes
- **Files to Create** - Complete list of AXAML and CS files needed

### Quick Navigation

| Category | Document |
|----------|----------|
| **Start Here** | [01_SHELL_AND_NAVIGATION.md](surfacing/01_SHELL_AND_NAVIGATION.md) |
| **Timeline & Tasks** | [09_IMPLEMENTATION_TIMELINE.md](surfacing/09_IMPLEMENTATION_TIMELINE.md) |
| **Dashboard Widgets** | [02_DASHBOARD_HUB.md](surfacing/02_DASHBOARD_HUB.md) |
| **Game Library** | [03_LIBRARY_TAB.md](surfacing/03_LIBRARY_TAB.md) |
| **Fighting Games** | [04_MUGEN_TAB.md](surfacing/04_MUGEN_TAB.md) |
| **Statistics** | [05_ANALYTICS_SOCIAL.md](surfacing/05_ANALYTICS_SOCIAL.md) |
| **Power Tools** | [06_TOOLS_TAB.md](surfacing/06_TOOLS_TAB.md) |
| **CLI Access** | [07_TERMINAL_SETTINGS.md](surfacing/07_TERMINAL_SETTINGS.md) |
| **Controller UI** | [08_OVERLAYS_BIGPICTURE.md](surfacing/08_OVERLAYS_BIGPICTURE.md) |

---

*Document Version: 2.0*
*Last Updated: January 1, 2026*
*Total Specifications: 10 documents, 500+ detailed component specifications*
