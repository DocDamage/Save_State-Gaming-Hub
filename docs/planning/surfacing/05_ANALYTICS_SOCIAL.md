# 📊👥 Part 5: Analytics & Social Tabs Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [04_MUGEN_TAB.md](04_MUGEN_TAB.md)

---

# Section A: Analytics Tab

## 1. Analytics Overview

### 1.1 Purpose

Comprehensive gaming statistics, visualizations, goal tracking, and exportable reports.

### 1.2 Design Personality

- **Theme**: Data-driven dashboard aesthetic
- **Colors**: Clean greys with accent chart colors
- **Typography**: Clear, readable with data focus
- **Visualizations**: Charts, graphs, heatmaps

---

## 2. Analytics Shell Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📊 ANALYTICS                                [Date Range ▼] [Export ▼]      │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ Overview | Playtime | Sessions | Achievements | Goals | Reports | YIR ]  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                          TAB CONTENT AREA                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Analytics Sections

### 3.1 Overview Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  OVERVIEW                                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 📅 GAMING HEATMAP                                                    │   │
│  │      Jan    Feb    Mar    Apr    May    Jun    Jul    Aug           │   │
│  │  M  ░▒▓█▓░░░▒▒▓█░░░░▒▓▓████░░░░▒▓░░░░░▒▒▓▓░░▓▓██▓░░▒▒▓▓░           │   │
│  │  T  ░░▒▓█▓░░▒▒▓░░░░▒▓█████░░░░░▓░░░░░░▒▓▓░░▓███░░░░▓▓▓░            │   │
│  │  W  ░▒▓██▓░░▒▓▓░░░▒▓███░░░░░░░▓▓░░░░░░▒░░░▒▓██▓░░░░▒▓░░            │   │
│  │  ...                                                                 │   │
│  │  Less ░ ▒ ▓ █ More                          Total: 1,247h this year │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌───────────┐ │
│  │ ⏱️ TOTAL TIME   │ │ 🎮 GAMES PLAYED│ │ 🏆 ACHIEVEMENTS │ │ 📅 STREAK │ │
│  │   1,247 hours   │ │     142 games  │ │    1,847 total  │ │  23 days  │ │
│  │   +12% vs 2023  │ │    +34 this yr │ │    67% avg rate │ │ Best: 45  │ │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘ └───────────┘ │
│                                                                             │
│  ┌────────────────────────────────────┐ ┌────────────────────────────────┐ │
│  │ 📊 PLAYTIME BY GENRE              │ │ 🎮 TOP 5 GAMES                 │ │
│  │ [PIE CHART]                       │ │ 1. Elden Ring      125.5h     │ │
│  │ Action RPG: 45%                   │ │ 2. Cyberpunk       89.2h      │ │
│  │ FPS: 23%                          │ │ 3. Hollow Knight   67.3h      │ │
│  │ Strategy: 15%                     │ │ 4. Hades           56.8h      │ │
│  │ Other: 17%                        │ │ 5. Dark Souls 3    49.1h      │ │
│  └────────────────────────────────────┘ └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Playtime Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PLAYTIME ANALYTICS                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 📈 PLAYTIME TREND (Last 12 Months)                                   │   │
│  │   150h ┤                                                             │   │
│  │   120h ┤     ▓▓                                            ▓▓       │   │
│  │    90h ┤ ▓▓  ▓▓▓▓      ▓▓▓▓  ▓▓      ▓▓▓▓               ▓▓▓▓▓▓     │   │
│  │    60h ┤▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓     │   │
│  │    30h ┤▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓     │   │
│  │     0h └─────────────────────────────────────────────────────────   │   │
│  │         Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌────────────────────────────────────┐ ┌────────────────────────────────┐ │
│  │ ⏰ BY TIME OF DAY                 │ │ 📅 BY DAY OF WEEK              │ │
│  │ Morning: 12%   [██░░░░░░░░]      │ │ Mon: 8%    [██░░░░░░░░]        │ │
│  │ Afternoon: 25% [█████░░░░░]      │ │ Tue: 10%   [██░░░░░░░░]        │ │
│  │ Evening: 45%   [█████████░]      │ │ Wed: 12%   [███░░░░░░░]        │ │
│  │ Night: 18%     [████░░░░░░]      │ │ Thu: 10%   [██░░░░░░░░]        │ │
│  │                                   │ │ Fri: 15%   [████░░░░░░]        │ │
│  │                                   │ │ Sat: 25%   [██████░░░░]        │ │
│  │                                   │ │ Sun: 20%   [█████░░░░░]        │ │
│  └────────────────────────────────────┘ └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Goals Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎯 GAMING GOALS                                              [+ Add Goal]  │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Active] [Completed] [Expired]                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ 🎮 Complete 5 games this month                                     │    │
│  │ ████████████████████░░░░░░░░░░░░░░░░░░░░░░░░  3/5 (60%)           │    │
│  │ Deadline: Jan 31, 2026   Reward: 🏆 Completionist Badge            │    │
│  │ Games: Hollow Knight ✓, Hades ✓, Celeste ✓                        │    │
│  │ [Edit] [Delete]                                                    │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ ⏱️ Play 20 hours this week                                         │    │
│  │ ██████████████████████████████████████████░░  18/20h (90%)        │    │
│  │ Deadline: Sunday   Reward: 🎖️ Dedicated Gamer                      │    │
│  │ [Edit] [Delete]                                                    │    │
│  └────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.4 Year In Review Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎉 2025 YEAR IN REVIEW                                   [Share] [Export]  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│    ╔═══════════════════════════════════════════════════════════════════╗    │
│    ║                                                                    ║    │
│    ║   🎮 YOUR GAMING YEAR 2025 🎮                                     ║    │
│    ║                                                                    ║    │
│    ║   Total Playtime:        1,247 hours                              ║    │
│    ║   That's 52 days of gaming!                                       ║    │
│    ║                                                                    ║    │
│    ║   Games Played:          87 games                                 ║    │
│    ║   Games Completed:       23 games                                 ║    │
│    ║   Achievements:          1,234 unlocked                           ║    │
│    ║                                                                    ║    │
│    ║   ───────────────────────────────────────                         ║    │
│    ║   TOP GAME                                                        ║    │
│    ║   Elden Ring - 125.5 hours                                        ║    │
│    ║   ───────────────────────────────────────                         ║    │
│    ║   FAVORITE GENRE                                                  ║    │
│    ║   Action RPG (45% of playtime)                                    ║    │
│    ║   ───────────────────────────────────────                         ║    │
│    ║   LONGEST SESSION                                                 ║    │
│    ║   8 hours 34 min - Baldur's Gate 3                               ║    │
│    ║                                                                    ║    │
│    ╚═══════════════════════════════════════════════════════════════════╝    │
│                                                                             │
│    [← Previous Year]                               [Generate Shareable] →   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Analytics Services Mapping

| Section | Service | Methods |
|---------|---------|---------|
| Overview | `IAnalyticsService` | `GetOverviewAsync()`, `GetHeatmapAsync()` |
| Playtime | `IAnalyticsService` | `GetPlaytimeByPeriodAsync()`, `GetPlaytimeByTimeOfDay()` |
| Sessions | `IGameSessionRepository` | `GetSessionsAsync()`, `GetSessionDetailsAsync()` |
| Achievements | `IAchievementRepository` | `GetAchievementsAsync()`, `GetCompletionRate()` |
| Goals | `IGoalService` | `GetActiveGoalsAsync()`, `CreateGoalAsync()` |
| Reports | `IReportGeneratorService` | `GeneratePdfAsync()`, `GenerateHtmlAsync()` |
| Year In Review | `IYearInReviewService` | `GenerateReviewAsync()`, `GetShareableImage()` |

---

# Section B: Social Tab

## 5. Social Overview

### 5.1 Purpose

Community features including friends, reviews, shared collections, and social activity.

### 5.2 Design Personality

- **Theme**: Community/social network feel
- **Colors**: Friendly, approachable palette
- **Typography**: Conversational, readable
- **Layout**: Activity feed focused

---

## 6. Social Shell Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  👥 SOCIAL HUB                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  [ Friends | Activity | Reviews | Collections | Discord ]                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                          TAB CONTENT AREA                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Social Sections

### 7.1 Friends Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  👥 FRIENDS                                               [+ Add Friend]    │
├─────────────────────────────────────────────────────────────────────────────┤
│  🟢 ONLINE (3)                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ┌────┐ Alex                          Playing: Elden Ring            │   │
│  │ │ 👤 │ @alexgamer                    Session: 2h 15m                │   │
│  │ └────┘                               [View Profile] [Message]       │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ┌────┐ Sam                           Playing: Hades                 │   │
│  │ │ 👤 │ @samplays                     Session: 45m                   │   │
│  │ └────┘                               [View Profile] [Message]       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ⚫ OFFLINE (9)                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Jordan, Casey, Taylor, Morgan, Riley, Drew, Quinn, Avery, Blake    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Activity Feed Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📰 ACTIVITY FEED                                     [Filter ▼] [Refresh]  │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 👤 Alex started playing Elden Ring                         2m ago  │   │
│  │ "Time to die again 💀"                                              │   │
│  │ [👍 3] [💬 1]                                                       │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ 👤 Sam unlocked achievement "God Slayer" in Hades          15m ago │   │
│  │ 🏆 Defeated the final boss without taking damage                   │   │
│  │ [👍 12] [💬 4]                                                      │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ 👤 Jordan reviewed Cyberpunk 2077                          1h ago  │   │
│  │ ★★★★☆ "Finally playable after all the patches..."                 │   │
│  │ [Read Full Review] [👍 8] [💬 2]                                    │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ 👤 Casey completed Hollow Knight                           3h ago  │   │
│  │ 🎮 67 hours played • 112% completion                               │   │
│  │ [👍 15] [💬 6]                                                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.3 Reviews Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⭐ REVIEWS                                              [+ Write Review]   │
├─────────────────────────────────────────────────────────────────────────────┤
│  [My Reviews] [Friends' Reviews] [All Reviews]                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ┌────┐ ELDEN RING                                                   │   │
│  │ │Art │ ★★★★★ (10/10)                    Reviewed by: You           │   │
│  │ └────┘ Playtime at review: 125 hours                                │   │
│  │                                                                      │   │
│  │ "A masterpiece of open-world design. FromSoftware has outdone      │   │
│  │ themselves. The sense of exploration and discovery is unmatched.   │   │
│  │ Every corner hides something incredible..."                         │   │
│  │                                                                      │   │
│  │ 👍 Recommended                                   [Edit] [Delete]    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.4 Shared Collections Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📁 SHARED COLLECTIONS                              [+ Create Collection]   │
├─────────────────────────────────────────────────────────────────────────────┤
│  [My Collections] [Shared With Me] [Public]                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 🎮 Best Couch Co-op Games                            🔗 Share Code │   │
│  │ Created by: You                                      Code: ABC123   │   │
│  │ 15 games • Public                                                   │   │
│  │                                                                      │   │
│  │ [img] [img] [img] [img] [img] +10 more                              │   │
│  │                                                                      │   │
│  │ [View] [Edit] [Share] [Delete]                                      │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ 🎮 Hidden Gems of 2024                               By: @samplays │   │
│  │ 8 games • Shared with you                                          │   │
│  │                                                                      │   │
│  │ [img] [img] [img] [img] [img] +3 more                               │   │
│  │                                                                      │   │
│  │ [View] [Copy to My Library]                                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.5 Discord Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎮 DISCORD INTEGRATION                                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STATUS: Connected as @YourDiscord#1234                   [Disconnect]│   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  RICH PRESENCE SETTINGS                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [✓] Show currently playing game                                     │   │
│  │ [✓] Show play time                                                   │   │
│  │ [✓] Show achievement progress                                        │   │
│  │ [ ] Show join button (allow friends to join)                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  PREVIEW                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 🎮 Playing Elden Ring                                               │   │
│  │    125.5 hours played                                               │   │
│  │    🏆 42/42 achievements                                            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Social Services Mapping

| Section | Service | Methods |
|---------|---------|---------|
| Friends | `IFriendRepository` | `GetFriendsAsync()`, `AddFriendAsync()` |
| Friends | `IFriendActivityService` | `GetFriendStatusAsync()` |
| Activity | `IFriendActivityService` | `GetActivityFeedAsync()` |
| Reviews | `IGameReviewService` | `GetReviewsAsync()`, `CreateReviewAsync()` |
| Collections | `ISharedCollectionService` | `GetCollectionsAsync()`, `ShareAsync()` |
| Discord | `IDiscordPresenceService` | `UpdatePresenceAsync()`, `GetStatus()` |

---

## 9. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Analytics/AnalyticsView.axaml` | View | Analytics container |
| `Views/Analytics/OverviewSection.axaml` | View | Overview dashboard |
| `Views/Analytics/PlaytimeSection.axaml` | View | Playtime analytics |
| `Views/Analytics/GoalsSection.axaml` | View | Goal tracking |
| `Views/Analytics/YearInReviewSection.axaml` | View | Year in review |
| `Views/Analytics/Components/HeatmapChart.axaml` | Component | Heatmap visualization |
| `Views/Social/SocialView.axaml` | View | Social container |
| `Views/Social/FriendsSection.axaml` | View | Friends list |
| `Views/Social/ActivitySection.axaml` | View | Activity feed |
| `Views/Social/ReviewsSection.axaml` | View | Reviews manager |
| `Views/Social/CollectionsSection.axaml` | View | Shared collections |
| `Views/Social/DiscordSection.axaml` | View | Discord integration |
| `ViewModels/Analytics/*.cs` | ViewModels | Analytics ViewModels |
| `ViewModels/Social/*.cs` | ViewModels | Social ViewModels |

---

*Next: [06_TOOLS_TAB.md](06_TOOLS_TAB.md)*
