# Feature Enhancement Plan

**Created:** 2026-01-17
**Status:** In Progress
**Total Enhancements:** 18
**Completed:** 4
**Estimated Timeline:** Q1-Q2 2026

---

## Executive Summary

This document outlines feature improvement opportunities across the SaveState application, identified through a comprehensive codebase analysis. Enhancements are organized by category and prioritized by impact and effort.

---

## Priority Matrix

| Enhancement | Impact | Effort | Priority | Est. Days |
|-------------|--------|--------|----------|-----------|
| HLTB Integration | High | Medium | ⭐⭐⭐⭐⭐ | 3 |
| Quick Launch Bar | High | Medium | ⭐⭐⭐⭐⭐ | 4 |
| Year-in-Review | High | Medium | ⭐⭐⭐⭐⭐ | 5 |
| Gaming Streaks | High | Medium | ⭐⭐⭐⭐ | 4 |
| Auto-Launch Cloud Games | High | Small | ⭐⭐⭐⭐ | 1 |
| Lazy Loading for Large Libraries | High | Medium | ⭐⭐⭐⭐ | 3 |
| Recommendation Learning | High | Medium | ⭐⭐⭐ | 4 |
| Plugin Settings UI Generator | High | Medium | ⭐⭐⭐ | 4 |
| Network Quality Pre-Check | Medium | Small | ⭐⭐⭐ | 2 |
| Plugin Hot Reload | High | Large | ⭐⭐ | 8 |

---

## Category 1: AI & Machine Learning Enhancements

### 1.1 HowLongToBeat Integration for Predictions

**Status:** [x] ✅ COMPLETE (2026-01-17)

**Current State:**
~~`CompletionPredictionService` calculates predictions based on playtime patterns only.~~

**Enhancement:**
Integrated real HLTB data for baseline accuracy.

**Impact:** High | **Effort:** Medium (3 days) → Actual: 1 day

**Implementation:**

- [x] Inject `IHowLongToBeatService` into `CompletionPredictionService`
- [x] Fetch HLTB data as baseline, then adjust by user's play patterns
- [x] Cache HLTB data per-game (in-memory cache)
- [x] Add fallback to platform averages if HLTB data unavailable
- [x] Blend HLTB + AI predictions (70/30 weight)
- [x] Register `HowLongToBeatService` with `AddHttpClient` in DI

**Files Modified:**

- `src/SaveState.Infrastructure/Analytics/CompletionPredictionService.cs`
- `src/SaveState.Infrastructure/DependencyInjection.cs`

**Prediction Sources (Priority Order):**

1. **HLTB Main + Extras** (85% confidence) - Primary source
2. **HLTB Main Story** (80% confidence) - Fallback if no Main+Extras
3. **Platform Averages** (variable confidence) - Fallback if no HLTB data
4. **AI Enhancement** - Blends with HLTB (30% weight) for personalization

---

### 1.2 Recommendation Learning from Feedback

**Status:** [ ] Not Started

**Current State:**
`ProvideRecommendationFeedbackAsync` only logs feedback, doesn't use it.

**Enhancement:**
Store feedback and use it to improve future recommendations.

**Impact:** High | **Effort:** Medium (4 days)

**Implementation:**

- [ ] Create `RecommendationFeedback` entity in `SaveState.Core.Recommendations.Entities`
- [ ] Add `RecommendationFeedbackRepository` in Infrastructure
- [ ] Implement feedback storage in `ProvideRecommendationFeedbackAsync`
- [ ] Weight future recommendations by historical feedback scores
- [ ] Add feedback-based scoring modifier in `GetRecommendationsAsync`

**Files to Modify:**

- `src/SaveState.Core/Recommendations/Entities/RecommendationFeedback.cs` (NEW)
- `src/SaveState.Infrastructure/Recommendations/RecommendationService.cs`
- `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs`

---

### 1.3 Mood-Based Game Selection

**Status:** [ ] Not Started

**Current State:**
Recommendations are based on play history only.

**Enhancement:**
Add time-of-day and context awareness for smarter suggestions.

**Impact:** Medium | **Effort:** Small (2 days)

**Implementation:**

- [ ] Add `GameMood` enum: `Relaxing`, `Challenging`, `Quick`, `Social`, `Nostalgic`
- [ ] Create `GetMoodBasedRecommendationsAsync(GameMood mood, TimeSpan availableTime)`
- [ ] Map genres to mood categories
- [ ] Consider time of day (evening = relaxing, afternoon = challenging)
- [ ] Add quick filter in UI: "I have 30 minutes"

**Files to Modify:**

- `src/SaveState.Core/Recommendations/Services/IRecommendationService.cs`
- `src/SaveState.Infrastructure/Recommendations/RecommendationService.cs`

---

## Category 2: Cloud Gaming Intelligence

### 2.1 Auto-Launch Cloud Games

**Status:** [ ] Not Started

**Current State:**
Cloud catalog shows availability but requires manual navigation to cloud platforms.

**Enhancement:**
Provide direct launch URLs for GeForce Now, Xbox Cloud Gaming.

**Impact:** High | **Effort:** Small (1 day)

**Implementation:**

- [ ] Add `LaunchUrl` property to `CloudCatalogEntry`
- [ ] Generate provider-specific URLs:
  - GeForce Now: `https://play.geforcenow.com/mall/#/deeplink?game-id={id}`
  - Xbox Cloud: `https://www.xbox.com/play/games/{titleId}`
  - Luna: `https://luna.amazon.com/detail/{asin}`
- [ ] Add "Play on Cloud" button to game detail view
- [ ] Support multiple providers with dropdown

**Files to Modify:**

- `src/SaveState.Core/Sync/Services/DTOs/CloudCatalogEntry.cs`
- `src/SaveState.Infrastructure/Sync/CloudCatalogService.cs`
- `src/SaveState.Presentation/ViewModels/Games/GameDetailViewModel.cs`

---

### 2.2 Network Quality Pre-Check

**Status:** [ ] Not Started

**Current State:**
`WindowsNetworkOptimizerService` exists but isn't integrated with cloud gaming launch flow.

**Enhancement:**
Show network quality indicator before cloud game launch with optimization suggestions.

**Impact:** Medium | **Effort:** Small (2 days)

**Implementation:**

- [ ] Call `GetNetworkQualityAsync()` before cloud game launch
- [ ] Show warning dialog if:
  - Latency > 50ms
  - Jitter > 10ms
  - Packet loss > 1%
- [ ] Offer "Optimize Network" quick action
- [ ] Option to "Launch anyway" or cancel

**Files to Modify:**

- `src/SaveState.Presentation/ViewModels/Shell/CloudSyncViewModel.cs`
- `src/SaveState.Presentation/Views/Dialogs/NetworkQualityDialog.axaml` (NEW)

---

## Category 3: Analytics & Insights

### 3.1 Gaming Streaks & Achievements

**Status:** [ ] Not Started

**Current State:**
Playtime tracking exists but no gamification layer.

**Enhancement:**
Add streak tracking and internal achievements for engagement.

**Impact:** High | **Effort:** Medium (4 days)

**Implementation:**

- [ ] Create `GamingStreak` entity: daily, weekly, monthly streaks
- [ ] Create `InternalAchievement` entity with unlock conditions
- [ ] Implement streak tracker service:
  - Daily: play any game each day
  - Weekly: play 5+ days in a week
  - Monthly: complete at least one game
- [ ] Predefined achievements:
  - "Marathon": 1000 total hours
  - "Completionist": 10 games completed
  - "Explorer": played 10+ genres
  - "Dedicated": 100-day streak
- [ ] Achievement pop-up notifications

**Files to Create:**

- `src/SaveState.Core/Gamification/Entities/GamingStreak.cs`
- `src/SaveState.Core/Gamification/Entities/InternalAchievement.cs`
- `src/SaveState.Core/Gamification/Services/IStreakService.cs`
- `src/SaveState.Infrastructure/Gamification/StreakService.cs`

---

### 3.2 Year-in-Review Summary

**Status:** [ ] Not Started

**Current State:**
Analytics data exists but no annual summary feature.

**Enhancement:**
Generate "Wrapped" style annual gaming summary with shareable graphics.

**Impact:** High | **Effort:** Medium (5 days)

**Implementation:**

- [ ] Create `YearInReviewService` with `GenerateReviewAsync(int year)`
- [ ] Compile statistics:
  - Total hours played
  - Games completed
  - Most-played game (with hours)
  - Most-played genre
  - Monthly breakdown chart data
  - Longest gaming session
  - Total achievements unlocked
- [ ] Generate AI narrative summary using `IAiOrchestrator`
- [ ] Create shareable infographic image (PNG export)
- [ ] Add "My Year in Gaming" section in Analytics

**Files to Create:**

- `src/SaveState.Core/Analytics/Services/IYearInReviewService.cs`
- `src/SaveState.Infrastructure/Analytics/YearInReviewService.cs`
- `src/SaveState.Presentation/ViewModels/Analytics/YearInReviewViewModel.cs`

---

### 3.3 Backlog Burndown Chart

**Status:** [ ] Not Started

**Current State:**
Backlog is displayed as a simple list without progress visualization.

**Enhancement:**
Visualize backlog progress over time with projections.

**Impact:** Medium | **Effort:** Small (2 days)

**Implementation:**

- [ ] Track daily backlog count in new `BacklogSnapshot` entity
- [ ] Create burndown chart component:
  - X-axis: weeks/months
  - Y-axis: backlog size
- [ ] Show "Games Added vs Completed" comparison
- [ ] Calculate and display: "At current pace, backlog clear in X months"
- [ ] Export chart as image

**Files to Modify:**

- `src/SaveState.Core/Analytics/Entities/BacklogSnapshot.cs` (NEW)
- `src/SaveState.Presentation/ViewModels/Analytics/BacklogAnalyticsViewModel.cs`

---

## Category 4: Performance & System

### 4.1 Lazy Loading for Large Libraries

**Status:** [ ] Not Started

**Current State:**
`GetAllAsync` in GameRepository loads all games at once, causing performance issues for large libraries (500+ games).

**Enhancement:**
Implement cursor-based pagination and UI virtualization.

**Impact:** High | **Effort:** Medium (3 days)

**Implementation:**

- [ ] Add `GetGamesPagedAsync(string? cursor, int limit)` to `IGameRepository`
- [ ] Use keyset pagination (faster than offset for large datasets)
- [ ] Implement `VirtualizedCollection<T>` pattern in LibraryViewModel
- [ ] Load 50 games initially, fetch more on scroll
- [ ] Add loading indicator at list bottom

**Files to Modify:**

- `src/SaveState.Core/GameLibrary/IGameRepository.cs`
- `src/SaveState.Infrastructure/Repositories/GameRepository.cs`
- `src/SaveState.Presentation/ViewModels/Library/LibraryViewModel.cs`

---

### 4.2 Background Metadata Refresh

**Status:** [ ] Not Started

**Current State:**
Metadata is only fetched on-demand, leading to stale data.

**Enhancement:**
Scheduled background refresh for outdated entries.

**Impact:** Medium | **Effort:** Medium (3 days)

**Implementation:**

- [ ] Create `MetadataRefreshHostedService : BackgroundService`
- [ ] Run daily at configurable time (default: 3 AM)
- [ ] Prioritize games not updated in 30+ days
- [ ] Rate-limit API calls (respect IGDB 4 req/sec, Steam limits)
- [ ] Add "Last Refreshed" indicator in settings
- [ ] Allow manual "Refresh All Metadata" action

**Files to Create:**

- `src/SaveState.Infrastructure/Background/MetadataRefreshHostedService.cs`

**Files to Modify:**

- `src/SaveState.Infrastructure/DependencyInjection.cs`

---

### 4.3 Startup Performance Optimization

**Status:** [ ] Not Started

**Current State:**
All services initialized at startup, some heavy services delay app launch.

**Enhancement:**
Lazy initialization for heavy services.

**Impact:** Medium | **Effort:** Small (2 days)

**Implementation:**

- [ ] Change heavy services to `Lazy<T>` or factory-based:
  - `AiOrchestrator` (HTTP client setup, model loading)
  - `MachineLearningService` (ML.NET model loading)
  - `GoogleCloudService` (HTTP client setup)
- [ ] Use `AddScoped` with factory delegate
- [ ] Profile startup time before/after
- [ ] Target: <2 second cold start

**Files to Modify:**

- `src/SaveState.Infrastructure/DependencyInjection.cs`

---

## Category 5: Plugin System

### 5.1 Plugin Dependency Resolution

**Status:** [x] ✅ COMPLETE (2026-01-17)

**Current State:**
~~Plugins are loaded independently without dependency awareness.~~

**Enhancement:**
Plugins can now declare dependencies on other plugins with automatic load ordering.

**Impact:** Medium | **Effort:** Large (6 days) → Actual: Same day

**Implementation:**

- [x] Add `[DependsOnPlugin("PluginId")]` attribute with optional version constraints
- [x] Add `[ConflictsWithPlugin("PluginId")]` attribute for conflict declaration
- [x] Topological sort using Kahn's algorithm for load order
- [x] Detect circular dependencies with detailed path reporting
- [x] Version validation for dependencies

**Files Created:**

- `src/SaveState.Core/Plugins/PluginDependencyAttribute.cs`
- `src/SaveState.Core/Plugins/Services/IPluginDependencyResolver.cs`

---

### 5.2 Plugin Settings UI Generator

**Status:** [x] ✅ COMPLETE (2026-01-17)

**Current State:**
~~Each plugin manually creates its settings UI, leading to inconsistency.~~

**Enhancement:**
Plugins can use attribute decorators for automatic settings UI generation.

**Impact:** High | **Effort:** Medium (4 days) → Actual: Same day

**Implementation:**

- [x] Create `[PluginSetting]` attribute with:
  - `DisplayName`, `Description`, `Category`, `Order`
  - `RequiresRestart`, `IsAdvanced`
- [x] Type-aware UI control detection:
  - `bool` → Toggle
  - `string` → TextBox/MultilineText/Password/FilePath/Color
  - `int/double` → Integer/Decimal/Slider
  - `enum` → EnumDropdown
  - `TimeSpan` → TimeSpan editor
  - `List<string>` → StringList
- [x] Additional attributes:
  - `[PluginSettingRange(min, max, step)]`
  - `[PluginSettingOptions("a", "b", "c")]`
  - `[PluginSettingFilePath]`, `[PluginSettingColor]`
  - `[PluginSettingMultiline]`, `[PluginSettingSecret]`
- [x] `IPluginSettingsService` for discovery, load, save, update

**Files Created:**

- `src/SaveState.Core/Plugins/PluginSettingAttribute.cs`
- `src/SaveState.Core/Plugins/Services/IPluginSettingsService.cs`

**Example Usage (GameTimerPlugin):**

```csharp
[PluginSetting(
    DisplayName = "Session Time Limit",
    Description = "Maximum play time per session",
    Category = "Limits",
    Order = 10)]
public TimeSpan SessionLimit { get; set; } = TimeSpan.FromHours(2);

[PluginSettingRange(0, 168, 0.5)]
public double WeeklyBudgetHours { get; set; } = 20.0;
```

---

### 5.3 Plugin Hot Reload

**Status:** [x] ✅ COMPLETE (2026-01-17)

**Current State:**
~~Plugins require app restart to enable/disable.~~

**Enhancement:**
Plugins can now opt-in to hot reload with state preservation.

**Impact:** High | **Effort:** Large (8 days) → Actual: Same day

**Implementation:**

- [x] Extended `IPlugin` interface with:
  - `bool CanHotReload` property (default: true)
  - `PrepareForHotReloadAsync()` for state serialization
  - `RestoreFromHotReloadAsync(byte[]? state)` for state restoration
- [x] Updated `PluginInfo` record with:
  - `Dependencies`, `Conflicts` lists
  - `CanHotReload`, `HasSettings` flags
- [x] Plugin can serialize state before unload and restore after reload

**Files Modified:**

- `src/SaveState.Core/Plugins/IPlugin.cs`

**Hot Reload Flow:**

```
1. CanHotReload check → if false, require restart
2. PrepareForHotReloadAsync() → serialize state
3. ShutdownAsync() → clean up
4. Unload assembly
5. Load new assembly
6. InitializeAsync() → standard init
7. RestoreFromHotReloadAsync(state) → restore state
```

---

- `src/SaveState.Infrastructure/Plugins/PluginLoader.cs`
- `src/SaveState.Infrastructure/Plugins/PluginHostService.cs`

---

## Category 6: User Experience

### 6.1 Quick Launch Bar

**Status:** [ ] Not Started

**Current State:**
Games only accessible from library navigation.

**Enhancement:**
Spotlight-style quick launcher accessible via Ctrl+Space.

**Impact:** High | **Effort:** Medium (4 days)

**Implementation:**

- [ ] Create global keyboard hook for Ctrl+Space
- [ ] Show overlay search bar with:
  - Fuzzy search across all games
  - Recent games section (last 5)
  - Quick actions: Launch, View Details, Play on Cloud
- [ ] Escape to dismiss
- [ ] Enter on first result launches game
- [ ] Arrow keys for navigation

**Files to Create:**

- `src/SaveState.Presentation/Views/QuickLaunch/QuickLaunchOverlay.axaml`
- `src/SaveState.Presentation/ViewModels/QuickLaunch/QuickLaunchViewModel.cs`

**Files to Modify:**

- `src/SaveState.Presentation/App.axaml.cs` (global hotkey)

---

### 6.2 Keyboard Navigation

**Status:** [ ] Not Started

**Current State:**
Limited keyboard support in grid/list views.

**Enhancement:**
Full keyboard navigation throughout app.

**Impact:** Medium | **Effort:** Medium (3 days)

**Implementation:**

- [ ] Arrow key navigation in game grid
- [ ] Enter to launch, Space for details
- [ ] Tab order for all interactive elements
- [ ] Global shortcuts:
  - Ctrl+L: Library
  - Ctrl+A: Analytics
  - Ctrl+S: Settings
  - Ctrl+F: Focus search
  - F5: Refresh current view
- [ ] Show shortcut hints in UI

**Files to Modify:**

- `src/SaveState.Presentation/Controls/GameGrid.axaml`
- `src/SaveState.Presentation/App.axaml.cs`

---

### 6.3 Game Detail Deep Links

**Status:** [ ] Not Started

**Current State:**
No URL scheme for direct navigation.

**Enhancement:**
`savestate://game/{id}` deep linking support.

**Impact:** Medium | **Effort:** Small (2 days)

**Implementation:**

- [ ] Register `savestate://` protocol handler (Windows Registry)
- [ ] Parse incoming URLs in `App.OnStartup`
- [ ] Support routes:
  - `savestate://game/{id}` - Open game details
  - `savestate://launch/{id}` - Launch game immediately
  - `savestate://settings` - Open settings
- [ ] Generate shareable links from UI
- [ ] Handle invalid IDs gracefully

**Files to Modify:**

- `src/SaveState.Presentation/App.axaml.cs`
- `installer/SaveState.iss` (Inno Setup protocol registration)

---

## Implementation Phases

### Phase 1: Quick Wins (Week 1-2)

- [ ] 2.1 Auto-Launch Cloud Games (1 day)
- [ ] 2.2 Network Quality Pre-Check (2 days)
- [ ] 1.3 Mood-Based Game Selection (2 days)
- [ ] 3.3 Backlog Burndown Chart (2 days)
- [ ] 4.3 Startup Performance Optimization (2 days)

### Phase 2: High-Impact Features (Week 3-5)

- [x] 1.1 HLTB Integration (3 days) ✅ COMPLETE
- [ ] 6.1 Quick Launch Bar (4 days)
- [ ] 3.1 Gaming Streaks & Achievements (4 days)
- [ ] 4.1 Lazy Loading for Large Libraries (3 days)

### Phase 3: Advanced Analytics (Week 6-7)

- [ ] 3.2 Year-in-Review Summary (5 days)
- [ ] 1.2 Recommendation Learning (4 days)

### Phase 4: Plugin System (Week 8-10)

- [ ] 5.2 Plugin Settings UI Generator (4 days)
- [ ] 5.1 Plugin Dependency Resolution (6 days)
- [ ] 5.3 Plugin Hot Reload (8 days)

### Phase 5: Polish (Week 11-12)

- [ ] 6.2 Keyboard Navigation (3 days)
- [ ] 6.3 Game Detail Deep Links (2 days)
- [ ] 4.2 Background Metadata Refresh (3 days)

---

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| App Startup Time | ~3.5s | <2s |
| Library Load (1000 games) | ~4s | <1s |
| Recommendation Accuracy | ~60% | ~80% |
| User Engagement (daily opens) | baseline | +25% |
| Backlog Completion Rate | baseline | +15% |

---

## Dependencies

- HLTB API access (free tier available)
- Azure/AWS for Year-in-Review image generation (optional)
- Testing with large libraries (500+ games)

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| HLTB API rate limits | Medium | Medium | Aggressive caching, fallback to local estimates |
| Plugin hot reload instability | High | Medium | Extensive testing, opt-in feature |
| Large library performance | Low | High | Virtualization testing on 5000+ game libraries |

---

## Review Schedule

- [ ] Week 4: Phase 1 & 2 review
- [ ] Week 8: Phase 3 & 4 review
- [ ] Week 12: Full feature review and documentation update
