# 🏠 Part 2: Dashboard Hub Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [01_SHELL_AND_NAVIGATION.md](01_SHELL_AND_NAVIGATION.md)

---

## 1. Dashboard Overview

### 1.1 Purpose

The Dashboard is the central command center - the first thing users see. It provides:

- Personalized gaming activity at a glance
- Quick access to common actions
- Real-time updates from all services
- Fully customizable widget layout

### 1.2 Layout Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  DASHBOARD                                    [Customize] [Reset Layout]    │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────┐ ┌────────────────────┐ ┌────────────────────┐       │
│  │                    │ │                    │ │                    │       │
│  │   WIDGET AREA      │ │   WIDGET AREA      │ │   WIDGET AREA      │       │
│  │   (Draggable)      │ │   (Draggable)      │ │   (Draggable)      │       │
│  │                    │ │                    │ │                    │       │
│  └────────────────────┘ └────────────────────┘ └────────────────────┘       │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                                                                      │    │
│  │                    LARGE WIDGET AREA (Activity Feed)                │    │
│  │                                                                      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │   WIDGET    │ │   WIDGET    │ │   WIDGET    │ │   WIDGET    │            │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘            │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Widget System Architecture

### 2.1 Widget Grid System

| Property | Value | Description |
|----------|-------|-------------|
| **Grid Columns** | 12 | Responsive grid |
| **Column Width** | Fluid (1/12 of container) | Responsive |
| **Row Height** | 120px | Fixed row height |
| **Gap** | 16px | Space between widgets |
| **Snap** | true | Widgets snap to grid |

### 2.2 Widget Sizes

| Size Name | Columns | Rows | Pixel Dimensions (approx) |
|-----------|---------|------|---------------------------|
| **Small** | 3 | 1 | 280×120 |
| **Medium** | 4 | 2 | 380×256 |
| **Large** | 6 | 2 | 580×256 |
| **Wide** | 8 | 1 | 780×120 |
| **Tall** | 4 | 3 | 380×392 |
| **Full** | 12 | 2 | Full width × 256 |

### 2.3 Widget Base Interface

```csharp
public interface IWidget
{
    string Id { get; }
    string Title { get; }
    string Icon { get; }
    WidgetSize DefaultSize { get; }
    WidgetSize[] SupportedSizes { get; }
    int RefreshIntervalMs { get; }
    bool CanMinimize { get; }
    bool CanRemove { get; }

    Task InitializeAsync();
    Task RefreshAsync();
    void Dispose();
}

public abstract class WidgetBase : ObservableObject, IWidget
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isMinimized;

    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract string Icon { get; }
    public virtual WidgetSize DefaultSize => WidgetSize.Medium;
    // ...
}
```

### 2.4 Widget Position Storage

```csharp
public record WidgetPosition(
    string WidgetId,
    int Column,      // 0-11
    int Row,         // 0-n
    int ColumnSpan,  // 1-12
    int RowSpan,     // 1-n
    bool IsVisible
);

public class DashboardLayout
{
    public string LayoutId { get; set; } = "default";
    public List<WidgetPosition> Positions { get; set; } = new();
    public DateTime LastModified { get; set; }
}
```

---

## 3. Widget Catalog (20 Widgets)

### 3.1 Quick Actions Widget

| Property | Value |
|----------|-------|
| **ID** | `quick-actions` |
| **Title** | Quick Actions |
| **Icon** | ⚡ |
| **Default Size** | Medium (4×2) |
| **Refresh** | None (static) |

**Layout:**

```
┌────────────────────────────────────────┐
│ ⚡ QUICK ACTIONS                    ⋯  │
├────────────────────────────────────────┤
│  ┌────────────┐  ┌────────────┐        │
│  │ ▶ Continue │  │ 🔍 Scan    │        │
│  │   Playing  │  │   Games    │        │
│  └────────────┘  └────────────┘        │
│  ┌────────────┐  ┌────────────┐        │
│  │ 🎲 Random  │  │ 🤖 AI      │        │
│  │   Game     │  │ Recommend  │        │
│  └────────────┘  └────────────┘        │
└────────────────────────────────────────┘
```

**Actions:**

| Button | Command | Service |
|--------|---------|---------|
| Continue Playing | Launch last played game | `IGameRepository`, `IMugenLauncher` |
| Scan Games | Trigger game detection | `IGameDetectorService` |
| Random Game | Pick random from library | `IGameRandomizerService` |
| AI Recommend | Get AI suggestion | `IRecommendationService` |

**ViewModel:**

```csharp
public partial class QuickActionsWidgetViewModel : WidgetBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string? _lastPlayedGameTitle;
    [ObservableProperty] private Guid? _lastPlayedGameId;

    [RelayCommand]
    private async Task ContinuePlaying()
    {
        if (_lastPlayedGameId.HasValue)
            await _mediator.Send(new LaunchGameCommand(_lastPlayedGameId.Value));
    }

    [RelayCommand]
    private async Task ScanGames() => await _mediator.Send(new ScanForGamesCommand());

    [RelayCommand]
    private async Task RandomGame() => await _mediator.Send(new LaunchRandomGameCommand());

    [RelayCommand]
    private async Task AiRecommend() => NavigationService.Navigate("Library", new { ShowRecommendations = true });
}
```

---

### 3.2 Today's Stats Widget

| Property | Value |
|----------|-------|
| **ID** | `today-stats` |
| **Title** | Today's Stats |
| **Icon** | 📊 |
| **Default Size** | Small (3×1) |
| **Refresh** | 60000ms (1 min) |

**Layout:**

```
┌──────────────────────────────────┐
│ 📊 TODAY                      ⋯  │
├──────────────────────────────────┤
│  ⏱️ 2.5h    🎮 3    🏆 5        │
│  Playtime  Sessions Achievements│
└──────────────────────────────────┘
```

**Data Source:** `IAnalyticsService.GetTodayStatsAsync()`

---

### 3.3 Activity Feed Widget

| Property | Value |
|----------|-------|
| **ID** | `activity-feed` |
| **Title** | Activity Feed |
| **Icon** | 📰 |
| **Default Size** | Full (12×2) |
| **Refresh** | 30000ms (30 sec) |

**Layout:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 📰 ACTIVITY FEED                                              View All │
├─────────────────────────────────────────────────────────────────────────┤
│  🎮 You played Elden Ring for 2 hours                         2m ago   │
│  🏆 Achievement Unlocked: Dragon Slayer in Dark Souls 3       15m ago  │
│  👤 Friend @GamerX started playing Cyberpunk 2077             32m ago  │
│  💰 Sale Alert: Hollow Knight is 75% off ($3.74)              1h ago   │
│  📥 5 new games discovered during scan                         2h ago  │
└─────────────────────────────────────────────────────────────────────────┘
```

**Activity Types:**

| Type | Icon | Source |
|------|------|--------|
| Play Session | 🎮 | `IGameSessionRepository` |
| Achievement | 🏆 | `IAchievementRepository` |
| Friend Activity | 👤 | `IFriendActivityService` |
| Sale Alert | 💰 | `IDealTrackingService` |
| Scan Result | 📥 | `IGameDetectorService` |
| Goal Progress | 🎯 | `IGoalService` |
| MUGEN Match | 🥊 | `IMugenStatsService` |

---

### 3.4 Recently Added Widget

| Property | Value |
|----------|-------|
| **ID** | `recently-added` |
| **Title** | Recently Added |
| **Icon** | 🆕 |
| **Default Size** | Medium (4×2) |
| **Refresh** | 300000ms (5 min) |

**Layout:**

```
┌────────────────────────────────────────┐
│ 🆕 RECENTLY ADDED                   ⋯  │
├────────────────────────────────────────┤
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐          │
│  │ 🎮 │ │ 🎮 │ │ 🎮 │ │ 🎮 │          │
│  │Art │ │Art │ │Art │ │Art │          │
│  └────┘ └────┘ └────┘ └────┘          │
│  Game1  Game2  Game3  Game4           │
│  Today  Today  Yester 2d ago          │
└────────────────────────────────────────┘
```

**Data Source:** `IGameRepository.GetRecentlyAddedAsync(count: 8)`

---

### 3.5 Goal Progress Widget

| Property | Value |
|----------|-------|
| **ID** | `goal-progress` |
| **Title** | Goal Progress |
| **Icon** | 🎯 |
| **Default Size** | Medium (4×2) |
| **Refresh** | 60000ms |

**Layout:**

```
┌────────────────────────────────────────┐
│ 🎯 GOALS                            ⋯  │
├────────────────────────────────────────┤
│  Complete 5 games this month           │
│  ████████████░░░░░░░░  3/5 (60%)      │
│                                        │
│  Play 20 hours this week               │
│  ██████████████████░░  18/20h (90%)   │
│                                        │
│  [+ Add Goal]                          │
└────────────────────────────────────────┘
```

**Data Source:** `IGoalService.GetActiveGoalsAsync()`

---

### 3.6 Gaming Heatmap Widget

| Property | Value |
|----------|-------|
| **ID** | `gaming-heatmap` |
| **Title** | Gaming Activity |
| **Icon** | 🔥 |
| **Default Size** | Large (6×2) |
| **Refresh** | 3600000ms (1 hour) |

**Layout (GitHub-style):**

```
┌──────────────────────────────────────────────────────────────┐
│ 🔥 GAMING ACTIVITY                                        ⋯  │
├──────────────────────────────────────────────────────────────┤
│      Jan   Feb   Mar   Apr   May   Jun                       │
│  Mon  ░▒▓█▓░░░▒▒▓█░░░░▒▓▓████░░░░▒▓░░░░░▒▒▓▓░░               │
│  Tue  ░░▒▓█▓░░▒▒▓░░░░▒▓█████░░░░░▓░░░░░░▒▓▓░░                │
│  Wed  ░▒▓██▓░░▒▓▓░░░▒▓███░░░░░░░▓▓░░░░░░▒░░░                 │
│  ...                                                          │
│                                                               │
│  247 hours this year • 156 sessions • 42 games               │
└──────────────────────────────────────────────────────────────┘
```

**Data Source:** `IAnalyticsService.GetActivityHeatmapAsync(year)`

---

### 3.7 Additional Widgets Summary

| Widget | ID | Size | Service | Description |
|--------|-----|------|---------|-------------|
| **Now Playing (Friends)** | `friends-playing` | Medium | `IFriendActivityService` | Friends currently online |
| **Sale Alerts** | `sale-alerts` | Small | `IDealTrackingService` | Wishlisted games on sale |
| **AI Recommendations** | `ai-recommendations` | Medium | `IRecommendationService` | AI game suggestions |
| **Performance Monitor** | `performance-mini` | Small | `IPerformanceMonitor` | CPU/GPU/RAM mini |
| **Upcoming Games** | `upcoming-games` | Medium | IGDB API | Release calendar |
| **MUGEN Quick Match** | `mugen-quick` | Small | `IMugenLauncher` | Start quick battle |
| **Voice Status** | `voice-status` | Small | `IVoiceCommandService` | Voice listening status |
| **Cloud Sync Status** | `cloud-sync` | Small | `ISyncService` | Sync progress |
| **Year Review Teaser** | `year-review` | Small | `IYearInReviewService` | Gaming wrapped preview |
| **Top Games** | `top-games` | Medium | `IAnalyticsService` | Most played games |
| **Achievements Recent** | `achievements-recent` | Medium | `IAchievementRepository` | Recent achievements |
| **Backlog Priority** | `backlog-priority` | Medium | `IBacklogService` | Top backlog items |
| **Network Quality** | `network-quality` | Small | `INetworkQualityMonitor` | Current latency |

---

## 4. Widget Customization

### 4.1 Customize Mode UI

When "Customize" is clicked:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  DASHBOARD (EDITING)                          [Done] [Cancel] [Reset]       │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐                   │
│  │                                                       │  ┌─────────────┐ │
│  │   Widgets become draggable with handles               │  │ WIDGET      │ │
│  │   Grid lines visible                                  │  │ DRAWER      │ │
│  │   Resize handles on corners                           │  │             │ │
│  │                                                       │  │ [+ Quick    │ │
│  │   ┌─────────┐ ← Drag handle                          │  │    Actions] │ │
│  │   │ Widget  │                                         │  │             │ │
│  │   │ ↔ ↕     │ ← Resize                               │  │ [+ Stats]   │ │
│  │   │    [✕]  │ ← Remove                               │  │             │ │
│  │   └─────────┘                                         │  │ [+ Feed]    │ │
│  │                                                       │  │             │ │
│  └──────────────────────────────────────────────────────┘  │ ...         │ │
│                                                             └─────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Widget Drawer

```csharp
public class WidgetDrawerViewModel : ObservableObject
{
    public ObservableCollection<WidgetCatalogItem> AvailableWidgets { get; }
    public ObservableCollection<WidgetCatalogItem> ActiveWidgets { get; }

    [RelayCommand]
    private void AddWidget(WidgetCatalogItem widget)
    {
        // Add to dashboard at first available position
    }
}

public record WidgetCatalogItem(
    string Id,
    string Title,
    string Icon,
    string Description,
    WidgetSize DefaultSize,
    bool IsActive
);
```

---

## 5. Dashboard ViewModel

```csharp
public partial class DashboardViewModel : ObservableObject
{
    private readonly IWidgetService _widgetService;
    private readonly ILayoutService _layoutService;

    [ObservableProperty] private ObservableCollection<WidgetInstance> _widgets;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private DashboardLayout _layout;

    public DashboardViewModel(IWidgetService widgetService, ILayoutService layoutService)
    {
        _widgetService = widgetService;
        _layoutService = layoutService;
        Widgets = new ObservableCollection<WidgetInstance>();
    }

    public async Task InitializeAsync()
    {
        Layout = await _layoutService.LoadLayoutAsync("dashboard");
        foreach (var position in Layout.Positions.Where(p => p.IsVisible))
        {
            var widget = await _widgetService.CreateWidgetAsync(position.WidgetId);
            Widgets.Add(new WidgetInstance(widget, position));
        }
    }

    [RelayCommand]
    private void EnterEditMode() => IsEditMode = true;

    [RelayCommand]
    private async Task SaveLayout()
    {
        IsEditMode = false;
        await _layoutService.SaveLayoutAsync(Layout);
    }

    [RelayCommand]
    private async Task ResetLayout()
    {
        Layout = _layoutService.GetDefaultLayout();
        await SaveLayout();
    }
}
```

---

## 6. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Dashboard/DashboardView.axaml` | View | Dashboard container |
| `Views/Dashboard/WidgetContainer.axaml` | View | Widget wrapper with drag/resize |
| `Views/Dashboard/WidgetDrawer.axaml` | View | Widget catalog drawer |
| `Views/Dashboard/Widgets/QuickActionsWidget.axaml` | View | Quick actions widget |
| `Views/Dashboard/Widgets/TodayStatsWidget.axaml` | View | Today stats widget |
| `Views/Dashboard/Widgets/ActivityFeedWidget.axaml` | View | Activity feed widget |
| `Views/Dashboard/Widgets/RecentlyAddedWidget.axaml` | View | Recently added widget |
| `Views/Dashboard/Widgets/GoalProgressWidget.axaml` | View | Goal progress widget |
| `Views/Dashboard/Widgets/GamingHeatmapWidget.axaml` | View | Heatmap widget |
| `Views/Dashboard/Widgets/...` | View | Other widget views |
| `ViewModels/Dashboard/DashboardViewModel.cs` | ViewModel | Dashboard logic |
| `ViewModels/Dashboard/Widgets/*.cs` | ViewModel | Widget ViewModels |
| `Services/WidgetService.cs` | Service | Widget factory |
| `Services/LayoutService.cs` | Service | Layout persistence |

---

*Next: [03_LIBRARY_TAB.md](03_LIBRARY_TAB.md)*
