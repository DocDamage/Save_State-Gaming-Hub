# 🏗️ Part 1: Application Shell & Navigation

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Status**: 📋 DETAILED SPECIFICATION

---

## 1. Application Shell Architecture

### 1.1 Window Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TITLE BAR (Custom Chrome)                                                  │
│  [Logo] SaveState Reborn v2.0     [─] [□] [✕]  (Draggable area)            │
├─────────────────────────────────────────────────────────────────────────────┤
│  HEADER BAR                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [🏠] [📚] [🥊] [📊] [👥] [🛠️] [💻]   🔍 Search...  🔔 ⚙️ 👤      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                                                                             │
│                         CONTENT AREA                                        │
│                         (Tab Content)                                       │
│                                                                             │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  STATUS BAR                                                                 │
│  🟢 Online │ 🎮 142 Games │ ⏱️ 2.5h Today │ 🔄 Syncing... │ CPU 23% GPU 45%│
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Shell Components

#### 1.2.1 MainShell.axaml

| Property | Value | Description |
|----------|-------|-------------|
| **MinWidth** | 1024px | Minimum window width |
| **MinHeight** | 768px | Minimum window height |
| **DefaultWidth** | 1400px | Default startup width |
| **DefaultHeight** | 900px | Default startup height |
| **CanResize** | true | User can resize |
| **WindowState** | Normal/Maximized | Remembers state |

**XAML Structure:**

```xml
<Window x:Class="MainShell">
    <Grid RowDefinitions="Auto, Auto, *, Auto">
        <!-- Row 0: Title Bar -->
        <views:TitleBarView Grid.Row="0" />

        <!-- Row 1: Header/Navigation -->
        <views:HeaderBarView Grid.Row="1" />

        <!-- Row 2: Content Area -->
        <ContentControl Grid.Row="2" Content="{Binding CurrentView}" />

        <!-- Row 3: Status Bar -->
        <views:StatusBarView Grid.Row="3" />

        <!-- Overlays Layer -->
        <views:OverlayContainer Grid.RowSpan="4" />
    </Grid>
</Window>
```

---

## 2. Title Bar Component

### 2.1 TitleBarView Specification

| Element | Position | Description | Binding |
|---------|----------|-------------|---------|
| **Logo** | Left | 24x24 SaveState icon | Static |
| **AppName** | Left+32 | "SaveState Reborn" | `{Binding AppName}` |
| **Version** | Left+160 | "v2.0.0" | `{Binding Version}` |
| **DragRegion** | Center | Draggable area | Window chrome |
| **MinimizeBtn** | Right-64 | [─] button | `MinimizeCommand` |
| **MaximizeBtn** | Right-32 | [□] button | `MaximizeCommand` |
| **CloseBtn** | Right | [✕] button | `CloseCommand` |

### 2.2 TitleBarViewModel

```csharp
public partial class TitleBarViewModel : ObservableObject
{
    public string AppName => "SaveState Reborn";
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.0.0";

    [RelayCommand]
    private void Minimize() => Application.Current.MainWindow.WindowState = WindowState.Minimized;

    [RelayCommand]
    private void Maximize() => /* Toggle maximize/restore */;

    [RelayCommand]
    private void Close() => Application.Current.Shutdown();
}
```

---

## 3. Header Bar Component

### 3.1 HeaderBarView Specification

```
┌──────────────────────────────────────────────────────────────────────────┐
│  [🏠] [📚] [🥊] [📊] [👥] [🛠️] [💻]        🔍 [Search...]  [🔔] [⚙️] [👤] │
│   ▲     ▲                                       ▲           ▲    ▲    ▲  │
│   │     │                                       │           │    │    │  │
│   │     └─ Tab Buttons (TooltipText)            │           │    │    │  │
│   └─ Active indicator (underline + glow)        │           │    │    │  │
│                                                 │           │    │    │  │
│         Universal Search Box ─────────────────┘           │    │    │  │
│         Notification Bell (badge count) ────────────────────┘    │    │  │
│         Settings Quick Access ───────────────────────────────────┘    │  │
│         User Profile Avatar ──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Tab Buttons

| Tab | Icon | Label | Shortcut | ViewModel |
|-----|------|-------|----------|-----------|
| Dashboard | 🏠 | Dashboard | Ctrl+1 | `DashboardViewModel` |
| Library | 📚 | Library | Ctrl+2 | `LibraryViewModel` |
| MUGEN | 🥊 | MUGEN | Ctrl+3 | `MugenViewModel` |
| Analytics | 📊 | Analytics | Ctrl+4 | `AnalyticsViewModel` |
| Social | 👥 | Social | Ctrl+5 | `SocialViewModel` |
| Tools | 🛠️ | Tools | Ctrl+6 | `ToolsViewModel` |
| Terminal | 💻 | Terminal | Ctrl+7 | `TerminalViewModel` |

### 3.3 Tab Button Styling

```css
/* Active State */
.TabButton.Active {
    Background: linear-gradient(180deg, transparent, var(--AccentColor) 10%);
    BorderBottom: 3px solid var(--AccentColor);
    Color: var(--AccentColor);
    BoxShadow: 0 4px 12px var(--AccentColorGlow);
}

/* Hover State */
.TabButton:hover {
    Background: var(--SurfaceHover);
    Transform: translateY(-2px);
    Transition: all 0.2s ease;
}

/* Icon Animation on Active */
.TabButton.Active .Icon {
    Animation: pulse 2s ease-in-out infinite;
}
```

### 3.4 Universal Search Box

| Property | Value |
|----------|-------|
| **Width** | 300px (expandable to 500px on focus) |
| **Placeholder** | "Search games, commands, settings... (Ctrl+K)" |
| **Mode** | Contextual (changes based on current tab) |

**Search Contexts:**

| Current Tab | Search Scope | Example Results |
|-------------|--------------|-----------------|
| Dashboard | Everything | Games, commands, settings, help |
| Library | Games, collections | Game titles, platforms |
| MUGEN | Characters, stages | Character names |
| Analytics | Reports, stats | Report types |
| Social | Friends, reviews | Friend names |
| Tools | Tools, settings | Tool names |
| Terminal | Commands | CLI commands |

### 3.5 Notification Bell

**Badge Behavior:**

- Shows count of unread notifications (max "99+")
- Red badge for critical, blue for info
- Pulses on new notification

**Dropdown Panel:**

```
┌────────────────────────────────────────┐
│  NOTIFICATIONS                  Clear  │
├────────────────────────────────────────┤
│  🎮 Scan complete: 5 new games    2m   │
│  🏆 Achievement: Dragon Slayer    15m  │
│  💰 Sale: Hollow Knight -75%      1h   │
│  📥 Update available: v2.0.1     3h   │
├────────────────────────────────────────┤
│             View All →                 │
└────────────────────────────────────────┘
```

### 3.6 User Profile

**Avatar Dropdown:**

```
┌────────────────────────────────────────┐
│  ┌────────┐  Username                  │
│  │ Avatar │  user@email.com            │
│  └────────┘  ● Online                  │
├────────────────────────────────────────┤
│  👤 Profile                            │
│  ⚙️ Settings                           │
│  🎨 Switch Theme                       │
│  📊 My Stats                           │
├────────────────────────────────────────┤
│  🚪 Sign Out                           │
└────────────────────────────────────────┘
```

---

## 4. Status Bar Component

### 4.1 StatusBarView Specification

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ 🟢 Online │ 🎮 142 Games │ ⏱️ 2.5h Today │ 🔄 Syncing... │ CPU 23% GPU 45% │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Status Bar Segments

| Segment | Description | Service | Click Action |
|---------|-------------|---------|--------------|
| **Connection** | Online/Offline indicator | Network check | Show network details |
| **Game Count** | Total games in library | `IGameRepository` | Open Library |
| **Today's Playtime** | Hours played today | `IAnalyticsService` | Open Analytics |
| **Sync Status** | Cloud sync progress | `ISyncService` | Show sync details |
| **Performance** | CPU/GPU usage | `IPerformanceMonitor` | Open Performance tools |

### 4.3 StatusBarViewModel

```csharp
public partial class StatusBarViewModel : ObservableObject, IDisposable
{
    private readonly IGameRepository _gameRepository;
    private readonly IAnalyticsService _analyticsService;
    private readonly ISyncService _syncService;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly Timer _refreshTimer;

    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private int _gameCount;
    [ObservableProperty] private TimeSpan _todayPlaytime;
    [ObservableProperty] private string _syncStatus;
    [ObservableProperty] private int _cpuUsage;
    [ObservableProperty] private int _gpuUsage;

    public StatusBarViewModel(/* dependencies */)
    {
        _refreshTimer = new Timer(RefreshStats, null, 0, 5000); // Refresh every 5s
    }

    private async void RefreshStats(object? state)
    {
        IsOnline = await CheckConnectivity();
        GameCount = await _gameRepository.GetCountAsync();
        TodayPlaytime = await _analyticsService.GetTodayPlaytimeAsync();
        CpuUsage = _performanceMonitor.GetCpuUsage();
        GpuUsage = _performanceMonitor.GetGpuUsage();
    }
}
```

---

## 5. Overlay Container

### 5.1 Overlay Types

| Overlay | Z-Index | Trigger | Position |
|---------|---------|---------|----------|
| **Command Palette** | 1000 | Ctrl+Shift+P | Center top |
| **Quick Search** | 1000 | Ctrl+K | Center top |
| **AI Assistant** | 900 | Ctrl+Shift+A / Chat icon | Right slide-in |
| **Notifications** | 800 | Bell click / Auto | Top right toast |
| **Performance HUD** | 700 | F3 | Floating, draggable |
| **Voice Indicator** | 600 | Voice active | Bottom center |
| **Loading** | 500 | Async operations | Center with dim |

### 5.2 Overlay Container XAML

```xml
<Grid x:Class="OverlayContainer">
    <!-- Dimming Background (for modals) -->
    <Border Background="#80000000"
            IsVisible="{Binding ShowDim}"
            PointerPressed="OnDimClicked" />

    <!-- Command Palette -->
    <views:CommandPaletteView
        IsVisible="{Binding ShowCommandPalette}"
        VerticalAlignment="Top"
        Margin="0,100,0,0" />

    <!-- AI Assistant Panel -->
    <views:AiAssistantPanel
        IsVisible="{Binding ShowAiAssistant}"
        HorizontalAlignment="Right"
        Width="400" />

    <!-- Toast Notifications -->
    <ItemsControl ItemsSource="{Binding Toasts}"
                  HorizontalAlignment="Right"
                  VerticalAlignment="Top"
                  Margin="20" />

    <!-- Performance HUD (draggable) -->
    <views:PerformanceHud
        IsVisible="{Binding ShowPerformanceHud}" />

    <!-- Voice Indicator -->
    <views:VoiceIndicator
        IsVisible="{Binding IsVoiceActive}"
        VerticalAlignment="Bottom"
        HorizontalAlignment="Center" />
</Grid>
```

---

## 6. Navigation Service

### 6.1 INavigationService Interface

```csharp
public interface INavigationService
{
    ObservableObject CurrentViewModel { get; }
    string CurrentTab { get; }
    Stack<NavigationEntry> History { get; }

    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void NavigateTo(string tabName);
    void NavigateTo(string tabName, object parameter);
    void GoBack();
    bool CanGoBack { get; }

    event EventHandler<NavigationEventArgs> Navigated;
}

public record NavigationEntry(string Tab, Type ViewModelType, object? Parameter, DateTime Timestamp);
```

### 6.2 Tab Registration

```csharp
public static class TabRegistry
{
    public static readonly Dictionary<string, TabDefinition> Tabs = new()
    {
        ["Dashboard"] = new("Dashboard", "🏠", typeof(DashboardViewModel), Key.D1, ModifierKeys.Control),
        ["Library"] = new("Library", "📚", typeof(LibraryViewModel), Key.D2, ModifierKeys.Control),
        ["MUGEN"] = new("MUGEN", "🥊", typeof(MugenViewModel), Key.D3, ModifierKeys.Control),
        ["Analytics"] = new("Analytics", "📊", typeof(AnalyticsViewModel), Key.D4, ModifierKeys.Control),
        ["Social"] = new("Social", "👥", typeof(SocialViewModel), Key.D5, ModifierKeys.Control),
        ["Tools"] = new("Tools", "🛠️", typeof(ToolsViewModel), Key.D6, ModifierKeys.Control),
        ["Terminal"] = new("Terminal", "💻", typeof(TerminalViewModel), Key.D7, ModifierKeys.Control),
    };
}

public record TabDefinition(string Name, string Icon, Type ViewModelType, Key Shortcut, ModifierKeys Modifiers);
```

---

## 7. Keyboard Shortcuts System

### 7.1 Global Shortcuts

| Shortcut | Action | Context |
|----------|--------|---------|
| `Ctrl+Shift+P` | Open Command Palette | Global |
| `Ctrl+K` | Quick Search | Global |
| `Ctrl+Shift+A` | Toggle AI Assistant | Global |
| `Ctrl+,` | Open Settings | Global |
| `F3` | Toggle Performance HUD | Global |
| `F11` | Toggle Big Picture Mode | Global |
| `Ctrl+M` | Toggle Voice Listening | Global |
| `Ctrl+1` to `Ctrl+7` | Switch to Tab 1-7 | Global |
| `Ctrl+Tab` | Next Tab | Global |
| `Ctrl+Shift+Tab` | Previous Tab | Global |
| `Escape` | Close overlay/Go back | Global |
| `Alt+Left` | Navigate Back | Global |

### 7.2 Shortcut Service

```csharp
public interface IShortcutService
{
    void RegisterGlobal(KeyGesture gesture, Action action, string description);
    void RegisterContextual(string context, KeyGesture gesture, Action action, string description);
    void Unregister(KeyGesture gesture);
    IReadOnlyList<ShortcutBinding> GetAllBindings();
    void LoadUserCustomizations();
    void SaveUserCustomizations();
}

public record ShortcutBinding(KeyGesture Gesture, string Description, string Context, bool IsCustomized);
```

---

## 8. Theme System Integration

### 8.1 Shell Theme Properties

```csharp
public class ShellTheme
{
    // Window Chrome
    public Color TitleBarBackground { get; set; }
    public Color TitleBarForeground { get; set; }

    // Header
    public Color HeaderBackground { get; set; }
    public Color TabActiveBackground { get; set; }
    public Color TabActiveIndicator { get; set; }

    // Content
    public Color ContentBackground { get; set; }

    // Status Bar
    public Color StatusBarBackground { get; set; }
    public Color StatusBarForeground { get; set; }

    // Accents
    public Color AccentPrimary { get; set; }
    public Color AccentSecondary { get; set; }
    public Color AccentGlow { get; set; }
}
```

### 8.2 Built-in Shell Themes

| Theme | Title Bar | Header | Accent | Personality |
|-------|-----------|--------|--------|-------------|
| **Deep Space** (Default) | #0D1117 | #161B22 | #58A6FF | Dark, modern |
| **Cyberpunk** | #0F0F1A | #1A1A2E | #FF00FF | Neon, vibrant |
| **RetroWave** | #2B1055 | #4A1074 | #FF6B6B | 80s nostalgia |
| **Minimal Light** | #FFFFFF | #F6F8FA | #0366D6 | Clean, bright |
| **OLED Black** | #000000 | #0A0A0A | #00FF88 | True black |
| **Forest** | #1B2D1B | #2D3D2D | #4CAF50 | Earthy, calm |

---

## 9. Responsive Behavior

### 9.1 Breakpoints

| Breakpoint | Width | Layout Changes |
|------------|-------|----------------|
| **Compact** | < 1024px | Collapse tab labels, icon-only mode |
| **Normal** | 1024-1400px | Standard layout |
| **Wide** | 1400-1920px | Expanded panels |
| **Ultra-Wide** | > 1920px | Multi-column dashboard |

### 9.2 Compact Mode

When width < 1024px:

- Tab buttons show icons only
- Search box collapses to icon
- Status bar shows essential info only
- Notifications stack vertically

---

## 10. Multi-Monitor Support

### 10.1 Window Memory

```csharp
public class WindowPositionService
{
    public async Task SaveWindowState(WindowState state);
    public async Task<WindowState?> LoadWindowState();
    public async Task RememberPerMonitor(string monitorId, WindowState state);
}

public record WindowState(
    double Left,
    double Top,
    double Width,
    double Height,
    WindowStateEnum State,
    string? MonitorId
);
```

### 10.2 Pop-out Windows

Panels that can be popped out to separate windows:

- AI Assistant
- Terminal
- Performance Monitor
- Game Detail
- Analytics Charts

---

## 11. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Shell/MainShell.axaml` | View | Main window |
| `Views/Shell/TitleBarView.axaml` | View | Title bar |
| `Views/Shell/HeaderBarView.axaml` | View | Header/navigation |
| `Views/Shell/StatusBarView.axaml` | View | Status bar |
| `Views/Shell/OverlayContainer.axaml` | View | Overlay layer |
| `ViewModels/Shell/MainShellViewModel.cs` | ViewModel | Main shell logic |
| `ViewModels/Shell/TitleBarViewModel.cs` | ViewModel | Title bar logic |
| `ViewModels/Shell/HeaderBarViewModel.cs` | ViewModel | Header logic |
| `ViewModels/Shell/StatusBarViewModel.cs` | ViewModel | Status bar logic |
| `Services/NavigationService.cs` | Service | Navigation |
| `Services/ShortcutService.cs` | Service | Keyboard shortcuts |
| `Services/OverlayService.cs` | Service | Overlay management |
| `Services/WindowPositionService.cs` | Service | Window state |

---

*Next: [02_DASHBOARD_HUB.md](02_DASHBOARD_HUB.md)*
