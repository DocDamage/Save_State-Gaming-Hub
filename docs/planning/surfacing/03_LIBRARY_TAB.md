# 📚 Part 3: Library Tab Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [02_DASHBOARD_HUB.md](02_DASHBOARD_HUB.md)

---

## 1. Library Overview

### 1.1 Purpose

Complete game collection management with rich filtering, multiple views, and full game detail management.

### 1.2 Library Shell Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  LIBRARY                                                                    │
├──────────────────┬──────────────────────────────────────────────────────────┤
│  SIDEBAR         │  TOOLBAR                                                 │
│  ┌────────────┐  │  [Grid ▼] [Sort ▼] [Filter ▼] 🔍 Search    [+ Add Game]  │
│  │ All Games  │  ├──────────────────────────────────────────────────────────┤
│  │ ★ Favorites│  │                                                          │
│  │ 📋 Backlog │  │                                                          │
│  │ ✅ Complete│  │              GAME GRID / LIST VIEW                       │
│  │ 🎮 Playing │  │                                                          │
│  │ 👁️ Hidden  │  │                                                          │
│  ├────────────┤  │                                                          │
│  │ COLLECTIONS│  │                                                          │
│  │ • RPGs     │  │                                                          │
│  │ • Couch Co │  │                                                          │
│  │ + Create   │  │                                                          │
│  ├────────────┤  │                                                          │
│  │ PLATFORMS  │  │                                                          │
│  │ • Steam    │  │                                                          │
│  │ • GOG      │  │                                                          │
│  │ • Epic     │  │                                                          │
│  │ • ROMs     │  │                                                          │
│  └────────────┘  │                                                          │
└──────────────────┴──────────────────────────────────────────────────────────┘
```

---

## 2. Library Sidebar

### 2.1 Sidebar Sections

| Section | Items | Source |
|---------|-------|--------|
| **Smart Filters** | All, Favorites, Backlog, Completed, Playing, Hidden | Status filters |
| **Collections** | User-created virtual collections | `IVirtualCollectionService` |
| **Platforms** | Steam, GOG, Epic, Origin, ROMs, Custom | `IPlatformRepository` |

### 2.2 Sidebar Item Component

```xml
<DataTemplate x:DataType="local:SidebarItem">
    <Button Classes="SidebarButton" Command="{Binding SelectCommand}">
        <Grid ColumnDefinitions="Auto, *, Auto">
            <TextBlock Grid.Column="0" Text="{Binding Icon}" Margin="0,0,8,0" />
            <TextBlock Grid.Column="1" Text="{Binding Name}" />
            <TextBlock Grid.Column="2" Text="{Binding Count}" Classes="Badge" />
        </Grid>
    </Button>
</DataTemplate>
```

### 2.3 Sidebar ViewModel

```csharp
public partial class LibrarySidebarViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<SidebarItem> _smartFilters;
    [ObservableProperty] private ObservableCollection<SidebarItem> _collections;
    [ObservableProperty] private ObservableCollection<SidebarItem> _platforms;
    [ObservableProperty] private SidebarItem? _selectedItem;

    public record SidebarItem(string Id, string Name, string Icon, int Count, ICommand SelectCommand);
}
```

---

## 3. Library Toolbar

### 3.1 View Mode Selector

| Mode | Icon | Description |
|------|------|-------------|
| **Grid** | ▦ | Cover art grid (default) |
| **List** | ≡ | Detailed list with columns |
| **Compact** | ⋯ | Dense list, icons only |
| **Table** | ▤ | Spreadsheet-style table |

### 3.2 Sort Options

| Sort By | Direction | Description |
|---------|-----------|-------------|
| Title | A-Z / Z-A | Alphabetical |
| Playtime | Most/Least | Total playtime |
| Last Played | Recent/Oldest | Last session date |
| Date Added | Newest/Oldest | When added to library |
| Release Date | Newest/Oldest | Game release date |
| Rating | Highest/Lowest | User rating |
| HLTB | Shortest/Longest | How Long To Beat |

### 3.3 Filter Panel

```
┌────────────────────────────────────────────────────────────────────────────┐
│  FILTERS                                                    [Clear All]    │
├────────────────────────────────────────────────────────────────────────────┤
│  Platform         Genre              Status          Rating              │
│  [x] Steam        [x] Action         [x] Not Started ☆☆☆☆☆ and up     │
│  [x] GOG          [x] RPG            [x] In Progress                     │
│  [ ] Epic         [ ] Strategy       [ ] Completed                       │
│  [ ] Origin       [ ] Indie          [ ] On Hold                         │
│  [ ] ROMs         [ ] Platformer     [ ] Dropped                         │
│                                                                            │
│  Year             Playtime           Tags                                 │
│  [2020] - [2024]  [0h] - [100h+]    [ ] Couch Co-op  [ ] Story Rich    │
│                                      [ ] Multiplayer  [ ] Open World    │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Game Views

### 4.1 Grid View

```
┌────────────────────────────────────────────────────────────────────────────┐
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │          │  │          │  │          │  │          │  │          │    │
│  │  Cover   │  │  Cover   │  │  Cover   │  │  Cover   │  │  Cover   │    │
│  │   Art    │  │   Art    │  │   Art    │  │   Art    │  │   Art    │    │
│  │          │  │          │  │          │  │          │  │          │    │
│  ├──────────┤  ├──────────┤  ├──────────┤  ├──────────┤  ├──────────┤    │
│  │Title     │  │Title     │  │Title     │  │Title     │  │Title     │    │
│  │⏱️ 12.5h  │  │⏱️ 3.2h   │  │⏱️ --     │  │⏱️ 45h    │  │⏱️ 8h     │    │
│  │★★★★☆    │  │★★★★★    │  │☆☆☆☆☆    │  │★★★★★    │  │★★★☆☆    │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
└────────────────────────────────────────────────────────────────────────────┘
```

**Grid Card Sizes:**

| Size | Dimensions | Info Shown |
|------|------------|------------|
| Small | 120×180 | Cover, Title only |
| Medium | 160×240 | Cover, Title, Playtime |
| Large | 200×300 | Cover, Title, Playtime, Rating, Platform |

### 4.2 List View

```
┌────────────────────────────────────────────────────────────────────────────┐
│ [☐] │ Cover │ Title              │ Platform │ Playtime │ Last Played     │
├─────┼───────┼────────────────────┼──────────┼──────────┼─────────────────┤
│ [ ] │ [img] │ Elden Ring         │ Steam    │ 125.5h   │ Yesterday       │
│ [✓] │ [img] │ Cyberpunk 2077     │ GOG      │ 89.2h    │ 3 days ago      │
│ [ ] │ [img] │ Hollow Knight      │ Steam    │ 45.0h    │ 1 week ago      │
│ [ ] │ [img] │ Hades              │ Epic     │ 67.3h    │ 2 weeks ago     │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Multi-Select Operations

### 5.1 Selection Modes

| Mode | Trigger | Behavior |
|------|---------|----------|
| Single | Click | Select one game |
| Multi | Ctrl+Click | Toggle selection |
| Range | Shift+Click | Select range |
| All | Ctrl+A | Select all visible |

### 5.2 Bulk Actions Bar

When multiple games selected:

```
┌────────────────────────────────────────────────────────────────────────────┐
│  ✓ 5 games selected                                                        │
│  [Add to Collection ▼] [Set Status ▼] [Add Tags ▼] [Delete] [More ▼]      │
└────────────────────────────────────────────────────────────────────────────┘
```

**Bulk Operations:**

| Action | Description | Service |
|--------|-------------|---------|
| Add to Collection | Add to virtual collection | `IVirtualCollectionService` |
| Remove from Collection | Remove from collection | `IVirtualCollectionService` |
| Set Status | Change play status | `IGameRepository` |
| Add Tags | Apply tags to games | `IGameRepository` |
| Apply Optimization | Set performance profile | `IPerformanceProfiler` |
| Export Metadata | Export selected games | Export service |
| Delete | Remove from library | `IGameRepository` |
| Hide/Unhide | Toggle visibility | `IGameRepository` |

---

## 6. Game Detail View

### 6.1 Game Detail Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ← Back to Library                                          [⋯ More]       │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐                                                       │
│  │                  │  ELDEN RING                                           │
│  │                  │  ★★★★★ (Your Rating: 10/10) [Rate]                   │
│  │     Cover Art    │  FromSoftware • Feb 25, 2022 • Action RPG            │
│  │                  │  Steam • Installed • 50.2 GB                          │
│  │                  │                                                       │
│  │                  │  [▶ PLAY]  [⚙️ Configure]  [📁 Browse Files]          │
│  └──────────────────┘                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Overview] [Save States] [Achievements] [Sessions] [Notes] [Mods] [Media] │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                         TAB CONTENT AREA                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Game Detail Header ViewModel

```csharp
public partial class GameDetailHeaderViewModel : ObservableObject
{
    [ObservableProperty] private GameDto _game;
    [ObservableProperty] private byte[]? _coverArt;
    [ObservableProperty] private int _userRating;
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private string _installPath;
    [ObservableProperty] private long _sizeBytes;

    [RelayCommand]
    private async Task PlayGame() => await _mediator.Send(new LaunchGameCommand(Game.Id));

    [RelayCommand]
    private void Configure() => _navigationService.Navigate("GameConfig", Game.Id);

    [RelayCommand]
    private void BrowseFiles() => Process.Start("explorer.exe", _installPath);

    [RelayCommand]
    private async Task SetRating(int rating)
    {
        UserRating = rating;
        await _mediator.Send(new UpdateGameRatingCommand(Game.Id, rating));
    }
}
```

---

## 7. Game Detail Tabs

### 7.1 Overview Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  OVERVIEW                                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌───────────────────────────────┐  ┌───────────────────────────────────┐  │
│  │ 📊 YOUR STATS                 │  │ 📝 DESCRIPTION                    │  │
│  │ Total Playtime: 125.5 hours   │  │ Elden Ring is an action RPG      │  │
│  │ Sessions: 47                  │  │ developed by FromSoftware...     │  │
│  │ Last Played: Yesterday        │  │                                   │  │
│  │ First Played: Mar 1, 2022     │  │ [Read More]                       │  │
│  │ Achievements: 42/42 (100%)    │  └───────────────────────────────────┘  │
│  └───────────────────────────────┘                                          │
│                                                                             │
│  ┌───────────────────────────────┐  ┌───────────────────────────────────┐  │
│  │ 🎯 HOW LONG TO BEAT           │  │ 💰 PRICE HISTORY                  │  │
│  │ Main Story: 55 hours          │  │ Current: $59.99                   │  │
│  │ Main + Extras: 98 hours       │  │ Lowest: $41.99 (Steam, Dec 2023)  │  │
│  │ Completionist: 132 hours      │  │ [Price Chart]                     │  │
│  │ Your Time: 125.5h (96% comp)  │  │ [🔔 Set Alert]                    │  │
│  └───────────────────────────────┘  └───────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ 🏷️ TAGS                                                               │  │
│  │ [Souls-like] [Open World] [Fantasy] [Difficult] [Co-op] [+ Add Tag]  │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ 🤖 AI BRIEFING                                                        │  │
│  │ "You last left off at Leyndell, Royal Capital. You had just defeated│  │
│  │ Morgott and were heading towards the Mountaintops of the Giants..."  │  │
│  │ [Generate New Briefing]                                               │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Save States Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  SAVE STATES                                    [🔧 Auto-Save Settings]     │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ 📊 BRANCH TREE                                                       │    │
│  │                                                                       │    │
│  │  main ●────●────●────●────●────●  (current)                          │    │
│  │              \                                                        │    │
│  │               ●────●  experimental-build                              │    │
│  │                    \                                                  │    │
│  │                     ●  different-ending                               │    │
│  │                                                                       │    │
│  │  [+ Create Branch] [Merge] [Compare]                                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ 📁 SAVES ON: main                                                    │    │
│  ├─────────────────────────────────────────────────────────────────────┤    │
│  │ ● Save 15 - Leyndell Royal Capital      Today 3:45 PM    [Restore]  │    │
│  │ ● Save 14 - Altus Plateau               Today 1:20 PM    [Restore]  │    │
│  │ ○ Auto-Save                             Today 12:55 PM   [Restore]  │    │
│  │ ● Save 13 - Raya Lucaria                Yesterday        [Restore]  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.3 Notes Tab

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  NOTES                                                       [+ Add Note]   │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ 📝 Where I left off                                    Dec 30, 2024 │    │
│  │ ─────────────────────────────────────────────────────────────────── │    │
│  │ Just beat Morgott, heading to Mountaintops. Need to find the       │    │
│  │ medallion halves for the secret area.                              │    │
│  │ [Edit] [Delete]                                                    │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ 💡 Boss strategies                                     Nov 15, 2024 │    │
│  │ ─────────────────────────────────────────────────────────────────── │    │
│  │ - Margit: Roll INTO his delayed attacks                            │    │
│  │ - Godrick: Stay close, punish arm slam                             │    │
│  │ - Rennala: Books phase 1, summon dragon phase 2                    │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Add Game Wizard

### 8.1 Add Game Modal

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ADD GAME                                                             [✕]  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  How would you like to add a game?                                          │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ 🔍 Auto Scan    │  │ 📁 Browse       │  │ ✏️ Manual       │              │
│  │                 │  │                 │  │                 │              │
│  │ Scan computer   │  │ Select game     │  │ Enter details   │              │
│  │ for games       │  │ executable      │  │ manually        │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ 📥 Import       │  │ 🔗 Link Account │  │ 🎮 ROM          │              │
│  │                 │  │                 │  │                 │              │
│  │ From Playnite   │  │ Steam, Epic,    │  │ Add ROM with    │              │
│  │ or LaunchBox    │  │ GOG accounts    │  │ emulator        │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 9. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Library/LibraryView.axaml` | View | Library main container |
| `Views/Library/LibrarySidebar.axaml` | View | Sidebar navigation |
| `Views/Library/LibraryToolbar.axaml` | View | Toolbar with filters |
| `Views/Library/GameGridView.axaml` | View | Grid view |
| `Views/Library/GameListView.axaml` | View | List view |
| `Views/Library/GameCard.axaml` | View | Grid card component |
| `Views/Library/BulkActionsBar.axaml` | View | Multi-select actions |
| `Views/Library/GameDetail/GameDetailView.axaml` | View | Game detail container |
| `Views/Library/GameDetail/OverviewTab.axaml` | View | Overview tab |
| `Views/Library/GameDetail/SaveStatesTab.axaml` | View | Save states tab |
| `Views/Library/GameDetail/AchievementsTab.axaml` | View | Achievements tab |
| `Views/Library/GameDetail/SessionsTab.axaml` | View | Sessions tab |
| `Views/Library/GameDetail/NotesTab.axaml` | View | Notes tab |
| `Views/Library/GameDetail/ModsTab.axaml` | View | Mods tab |
| `Views/Library/GameDetail/MediaTab.axaml` | View | Screenshots/videos |
| `Views/Library/AddGameWizard.axaml` | View | Add game modal |
| `ViewModels/Library/*.cs` | ViewModels | All Library ViewModels |

---

*Next: [04_MUGEN_TAB.md](04_MUGEN_TAB.md)*
