# 💻⚙️ Part 7: Terminal Tab & Settings Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [06_TOOLS_TAB.md](06_TOOLS_TAB.md)

---

# Section A: Terminal Tab

## 1. Terminal Overview

### 1.1 Purpose

Full CLI access within the UI, providing power users direct command-line interaction with all SaveState features.

### 1.2 Design Personality

- **Theme**: Hacker/Matrix terminal aesthetic
- **Colors**: Green/cyan text on dark background
- **Typography**: Monospace (Cascadia Code, JetBrains Mono)
- **Effects**: Subtle scanlines, CRT glow (optional)

---

## 2. Terminal Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  💻 TERMINAL                                    [Clear] [New Tab +] [⚙️]    │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Tab 1: Main] [Tab 2: Script] [+]                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ SaveState Reborn CLI v2.0.0                                          │   │
│  │ Copyright © 2026 SaveState. All rights reserved.                     │   │
│  │ Type 'help' for available commands, 'exit' to close.                 │   │
│  │                                                                       │   │
│  │ > savestate list --platform steam                                    │   │
│  │ Found 87 Steam games:                                                │   │
│  │ ┌────────────────────────────────────────────────────────────────┐  │   │
│  │ │ ID     Title                    Playtime    Last Played        │  │   │
│  │ │ 1234   Elden Ring               125.5h      Yesterday          │  │   │
│  │ │ 2345   Cyberpunk 2077           89.2h       3 days ago         │  │   │
│  │ │ 3456   Hollow Knight            67.3h       1 week ago         │  │   │
│  │ │ ...                                                             │  │   │
│  │ └────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                       │   │
│  │ > _                                                                  │   │
│  │                                                                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│  💡 Quick: [list] [scan] [launch] [recommend] [voice] [help]              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Terminal Features

### 3.1 Command Input

| Feature | Description | Implementation |
|---------|-------------|----------------|
| **Auto-complete** | Tab completion for commands | Fuzzy match on command registry |
| **History** | Arrow up/down navigation | Circular buffer, 1000 entries |
| **Syntax Highlighting** | Color-coded commands | Regex-based tokenizer |
| **Multi-line** | Shift+Enter for multi-line | Line continuation support |
| **Copy/Paste** | Standard clipboard ops | Ctrl+C/V |
| **Selection** | Mouse text selection | Highlight + copy |

### 3.2 Command Registry

All 14 CLI command groups exposed:

| Group | Example Commands |
|-------|------------------|
| **game** | `game list`, `game search`, `game launch [id]`, `game import` |
| **savestate** | `savestate list`, `savestate create`, `savestate branch` |
| **mugen** | `mugen chars`, `mugen simulate`, `mugen tournament` |
| **voice** | `voice listen`, `voice stop`, `voice register` |
| **cloud** | `cloud providers`, `cloud start`, `cloud quality` |
| **performance** | `performance monitor`, `performance optimize` |
| **network** | `network quality`, `network speedtest` |
| **automation** | `automation list`, `automation run [name]` |
| **social** | `social friends`, `social reviews` |
| **backlog** | `backlog add`, `backlog list`, `backlog prioritize` |
| **analytics** | `analytics overview`, `analytics export` |
| **coaching** | `coaching advice`, `coaching matchup` |
| **memory** | `memory scan`, `memory status` |
| **config** | `config get [key]`, `config set [key] [value]` |

### 3.3 Output Formatting

```csharp
public interface ITerminalOutput
{
    void WriteLine(string text, ConsoleColor? color = null);
    void WriteTable(DataTable table);
    void WriteProgress(string label, int percent);
    void WriteJson(object obj, bool pretty = true);
    void WriteError(string message);
    void WriteSuccess(string message);
    void WriteWarning(string message);
}
```

### 3.4 Script Editor

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📝 SCRIPT EDITOR                                [Save] [Run] [New] [Close] │
├─────────────────────────────────────────────────────────────────────────────┤
│  File: daily_routine.ss                                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  1 │ # Daily gaming routine                                                 │
│  2 │ echo "Starting daily routine..."                                       │
│  3 │ savestate backup --all                                                 │
│  4 │ game scan --quick                                                      │
│  5 │ analytics export --format json --output ./daily-stats.json            │
│  6 │ echo "Routine complete!"                                               │
│  7 │                                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  SAVED SCRIPTS                                                              │
│  [daily_routine.ss] [weekly_backup.ss] [mugen_tournament.ss]               │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Terminal ViewModel

```csharp
public partial class TerminalViewModel : ObservableObject
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly ICommandRegistry _commandRegistry;

    [ObservableProperty] private ObservableCollection<TerminalLine> _outputLines;
    [ObservableProperty] private string _currentInput;
    [ObservableProperty] private ObservableCollection<string> _history;
    [ObservableProperty] private int _historyIndex;
    [ObservableProperty] private ObservableCollection<string> _suggestions;

    [RelayCommand]
    private async Task ExecuteCommand()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput)) return;

        OutputLines.Add(new TerminalLine($"> {CurrentInput}", LineType.Input));
        History.Add(CurrentInput);

        var result = await _commandExecutor.ExecuteAsync(CurrentInput);

        foreach (var line in result.Output)
            OutputLines.Add(new TerminalLine(line, result.Success ? LineType.Output : LineType.Error));

        CurrentInput = string.Empty;
    }

    [RelayCommand]
    private void NavigateHistory(int direction)
    {
        HistoryIndex = Math.Clamp(HistoryIndex + direction, 0, History.Count - 1);
        CurrentInput = History[HistoryIndex];
    }

    [RelayCommand]
    private void AutoComplete()
    {
        Suggestions = _commandRegistry.GetSuggestions(CurrentInput);
    }
}
```

---

# Section B: Settings Hub

## 5. Settings Overview

### 5.1 Purpose

Unified settings management with category-based navigation and search.

### 5.2 Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⚙️ SETTINGS                                          🔍 Search settings... │
├────────────────────┬────────────────────────────────────────────────────────┤
│  CATEGORIES        │  SETTINGS CONTENT                                      │
│  ┌──────────────┐  │  ┌────────────────────────────────────────────────────┐│
│  │ 👤 Account   │  │  │                                                    ││
│  │ 🎨 Appearance│  │  │         SELECTED CATEGORY SETTINGS                 ││
│  │ 🎮 Gaming    │  │  │                                                    ││
│  │ 🔊 Audio     │  │  │                                                    ││
│  │ ⌨️ Shortcuts │  │  │                                                    ││
│  │ 🔌 Integrations│ │  │                                                    ││
│  │ ☁️ Cloud     │  │  │                                                    ││
│  │ 🔒 Privacy   │  │  └────────────────────────────────────────────────────┘│
│  │ ♿ Access    │  │                                                        │
│  │ 🗃️ Data     │  │                                                        │
│  │ 🔧 Advanced │  │                                                        │
│  │ ℹ️ About    │  │                                                        │
│  └──────────────┘  │                                                        │
└────────────────────┴────────────────────────────────────────────────────────┘
```

---

## 6. Settings Categories

### 6.1 Account Settings

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  👤 ACCOUNT                                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  PROFILE                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ┌────────┐  Username: [YourUsername        ]                        │   │
│  │ │ Avatar │  Email:    [user@email.com      ]                        │   │
│  │ │  [📷]  │  Display:  [Gamer Display Name  ]                        │   │
│  │ └────────┘                                                           │   │
│  │            [Change Avatar] [Update Profile]                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  API KEYS                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ OpenAI API Key:        [••••••••••••••••] [Show] [Edit] 🟢 Valid   │   │
│  │ IGDB Client ID:        [••••••••••••••••] [Show] [Edit] 🟢 Valid   │   │
│  │ SteamGridDB Key:       [••••••••••••••••] [Show] [Edit] 🟡 Missing │   │
│  │ RetroAchievements:     [••••••••••••••••] [Show] [Edit] 🟢 Valid   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  LINKED ACCOUNTS                                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Steam:    🟢 Connected as SteamUser          [Disconnect]          │   │
│  │ GOG:      🟢 Connected as GOGUser            [Disconnect]          │   │
│  │ Epic:     ⚫ Not Connected                    [Connect]             │   │
│  │ Discord:  🟢 Connected as User#1234          [Disconnect]          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Appearance Settings

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎨 APPEARANCE                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  THEME                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Current: [Deep Space ▼]                         [Customize]         │   │
│  │                                                                      │   │
│  │ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐        │   │
│  │ │ Deep    │ │ Cyber-  │ │ Retro-  │ │ Light   │ │ OLED    │        │   │
│  │ │ Space   │ │ punk    │ │ Wave    │ │ Mode    │ │ Black   │        │   │
│  │ │   ●     │ │         │ │         │ │         │ │         │        │   │
│  │ └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ACCENT COLOR                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [🔵] [🟢] [🟣] [🔴] [🟡] [🟠] [Custom: #______]                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  LAYOUT                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Sidebar Position:     [Left ▼]                                      │   │
│  │ Compact Mode:         [Off ▼]                                       │   │
│  │ Dashboard columns:    [Auto ▼]                                      │   │
│  │ Card Size:            [Medium ▼]                                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.3 Accessibility Settings

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ♿ ACCESSIBILITY                                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│  VISION                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ High Contrast Mode:      [Off ▼]                                    │   │
│  │ Color Blind Mode:        [None ▼] (Protanopia, Deuteranopia, etc.) │   │
│  │ Text Size:               [████████░░] 100%    [-] [+]              │   │
│  │ UI Scale:                [████████░░] 100%    [-] [+]              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  MOTION                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Reduce Motion:           [Off ▼]                                    │   │
│  │ Disable Animations:      [ ]                                        │   │
│  │ Reduce Transparency:     [ ]                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  SCREEN READER                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Screen Reader Support:   [✓] Enabled                                │   │
│  │ Announce Notifications:  [✓]                                        │   │
│  │ Read Tooltips:           [✓]                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  KEYBOARD                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Focus Indicators:        [High Visibility ▼]                        │   │
│  │ Keyboard Navigation:     [✓] Enabled (Tab order)                   │   │
│  │ Sticky Keys:             [ ]                                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.4 Keyboard Shortcuts Settings

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ⌨️ KEYBOARD SHORTCUTS                              [Reset All] [Export]    │
├─────────────────────────────────────────────────────────────────────────────┤
│  🔍 Search shortcuts...                                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  GLOBAL                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Action                     │ Shortcut         │ Custom              │   │
│  ├────────────────────────────┼──────────────────┼─────────────────────┤   │
│  │ Command Palette            │ Ctrl+Shift+P     │ [Record New]        │   │
│  │ Quick Search               │ Ctrl+K           │ [Record New]        │   │
│  │ AI Assistant               │ Ctrl+Shift+A     │ [Record New]        │   │
│  │ Settings                   │ Ctrl+,           │ [Record New]        │   │
│  │ Performance HUD            │ F3               │ [Record New]        │   │
│  │ Big Picture Mode           │ F11              │ [Record New]        │   │
│  │ Voice Commands             │ Ctrl+M           │ [Record New]        │   │
│  ├────────────────────────────┼──────────────────┼─────────────────────┤   │
│  │ Dashboard Tab              │ Ctrl+1           │ [Record New]        │   │
│  │ Library Tab                │ Ctrl+2           │ [Record New]        │   │
│  │ MUGEN Tab                  │ Ctrl+3           │ [Record New]        │   │
│  │ ...                        │ ...              │ ...                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.5 Data Management Settings

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🗃️ DATA MANAGEMENT                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  STORAGE                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Database Size:        245 MB                                        │   │
│  │ Cache Size:           128 MB                                        │   │
│  │ Save States:          1.2 GB                                        │   │
│  │ Screenshots:          450 MB                                        │   │
│  │ ─────────────────────────────                                       │   │
│  │ Total:                2.02 GB                   [Clear Cache]       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  DATA RETENTION                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Session History:      [Keep Forever ▼]                              │   │
│  │ Voice Command Log:    [30 Days ▼]                                   │   │
│  │ Performance Logs:     [7 Days ▼]                                    │   │
│  │ Error Logs:           [90 Days ▼]                                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  CLEANUP                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [🧹 Run Cleanup]  Delete old data based on retention settings       │   │
│  │ [📦 Compact DB]   Optimize database performance                     │   │
│  │ [🔄 Reset App]    ⚠️ Reset to factory defaults (keeps games)        │   │
│  │ [🗑️ Delete All]   ⚠️ Delete all data including games               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Settings Services

| Category | Service | Configuration Class |
|----------|---------|---------------------|
| Account | `IUserPreferencesService` | `UserSettings` |
| Appearance | `IThemeService` | `ThemeSettings` |
| Gaming | Multiple | `GamingSettings` |
| Audio | `IAudioOptimizer` | `AudioSettings` |
| Shortcuts | `IShortcutService` | `ShortcutBindings` |
| Integrations | Multiple API clients | `IntegrationSettings` |
| Cloud | `ISyncService` | `CloudSettings` |
| Privacy | - | `PrivacySettings` |
| Accessibility | - | `AccessibilitySettings` |
| Data | - | `DataSettings` |

---

## 8. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Terminal/TerminalView.axaml` | View | Terminal container |
| `Views/Terminal/TerminalOutput.axaml` | View | Output display |
| `Views/Terminal/TerminalInput.axaml` | View | Input with autocomplete |
| `Views/Terminal/ScriptEditor.axaml` | View | Script editing |
| `Views/Settings/SettingsView.axaml` | View | Settings shell |
| `Views/Settings/Categories/*.axaml` | Views | Settings category views |
| `ViewModels/Terminal/TerminalViewModel.cs` | ViewModel | Terminal logic |
| `ViewModels/Settings/SettingsViewModel.cs` | ViewModel | Settings logic |
| `Services/CommandExecutor.cs` | Service | CLI command execution |
| `Services/CommandRegistry.cs` | Service | Command registry |

---

*Next: [08_OVERLAYS_BIGPICTURE.md](08_OVERLAYS_BIGPICTURE.md)*
