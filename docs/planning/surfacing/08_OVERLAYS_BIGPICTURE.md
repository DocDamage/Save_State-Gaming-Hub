# 🎭📺 Part 8: Overlays & Big Picture Mode Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [07_TERMINAL_SETTINGS.md](07_TERMINAL_SETTINGS.md)

---

# Section A: Overlay System

## 1. Overlay Overview

### 1.1 Purpose

Floating panels, modals, and HUDs that appear over the main content for quick access features.

### 1.2 Overlay Z-Index Stack

| Z-Index | Overlay Type | Description |
|---------|--------------|-------------|
| 1000 | **Critical Modals** | Confirmation dialogs, errors |
| 900 | **Command Palette** | Global command search |
| 800 | **AI Assistant** | Chat panel |
| 700 | **Notifications** | Toast notifications |
| 600 | **Performance HUD** | Floating metrics |
| 500 | **Voice Indicator** | Listening status |
| 400 | **Tooltips** | Hover information |
| 300 | **Context Menus** | Right-click menus |
| 200 | **Dropdowns** | Select menus |
| 100 | **Dim Layer** | Background dimming |

---

## 2. Command Palette

### 2.1 Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  > search...                                                        [ESC]  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  RECENT                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ▶ Launch Elden Ring                                     game       │   │
│  │ 🔍 Scan for games                                       command    │   │
│  │ ⚙️ Open Settings                                        navigation │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  COMMANDS                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 🎮 game list                Show all games                         │   │
│  │ 🎮 game search [query]      Search games                          │   │
│  │ 🎮 game launch [id]         Launch a game                         │   │
│  │ 💾 savestate create         Create save state                     │   │
│  │ 🎙️ voice listen             Start voice recognition               │   │
│  │ 📊 analytics overview       Show analytics                        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  💡 Tip: Type > for commands, @ for games, # for settings, ? for help     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Search Prefixes

| Prefix | Search Type | Example |
|--------|-------------|---------|
| (none) | Everything | `elden ring` |
| `>` | Commands only | `>scan` |
| `@` | Games only | `@hollow knight` |
| `#` | Settings only | `#theme` |
| `?` | Help topics | `?voice` |
| `/` | Quick actions | `/recommend` |

### 2.3 Command Palette ViewModel

```csharp
public partial class CommandPaletteViewModel : ObservableObject
{
    [ObservableProperty] private string _searchQuery;
    [ObservableProperty] private ObservableCollection<PaletteItem> _results;
    [ObservableProperty] private int _selectedIndex;

    partial void OnSearchQueryChanged(string value)
    {
        Results = _searchService.Search(value);
        SelectedIndex = 0;
    }

    [RelayCommand]
    private async Task Execute()
    {
        var item = Results[SelectedIndex];
        await item.ExecuteAsync();
        Close();
    }
}

public abstract record PaletteItem(string Title, string Subtitle, string Icon, string Category)
{
    public abstract Task ExecuteAsync();
}
```

---

## 3. AI Assistant Panel

### 3.1 Layout

```
┌────────────────────────────────────────┐
│  🤖 AI ASSISTANT              [─] [✕] │
├────────────────────────────────────────┤
│                                        │
│  🤖 Hi! I'm your gaming assistant.    │
│  How can I help you today?            │
│                                        │
│  ─────────────────────────────────────│
│                                        │
│  👤 What should I play tonight?       │
│                                        │
│  🤖 Based on your recent activity and │
│  mood, I recommend:                    │
│                                        │
│  1. **Hollow Knight** - You're 80%     │
│     through, perfect for completion   │
│                                        │
│  2. **Hades** - Quick runs, you       │
│     haven't played in a while         │
│                                        │
│  3. **Celeste** - In your backlog,    │
│     similar to games you love         │
│                                        │
│  Would you like me to launch any of   │
│  these?                                │
│                                        │
│  ─────────────────────────────────────│
│                                        │
│  👤 Launch Hollow Knight              │
│                                        │
│  🤖 Launching Hollow Knight now! 🎮    │
│  Last save: Crystal Peak - 67h played │
│                                        │
├────────────────────────────────────────┤
│  [Type a message...        ] [🎤] [→] │
└────────────────────────────────────────┘
```

### 3.2 AI Assistant Features

| Feature | Description | Service |
|---------|-------------|---------|
| Game Recommendations | AI-powered suggestions | `IRecommendationService` |
| Game Briefing | Where you left off | `IGameAssistantService` |
| Strategy Help | Tips and strategies | `IGameAssistantService` |
| Voice Input | Speech-to-text | `ISpeechRecognitionService` |
| Quick Actions | Launch, save, etc. | Various |
| Context Awareness | Knows current view/game | Context service |

---

## 4. Notification System

### 4.1 Toast Notifications

```
┌────────────────────────────────────────┐
│ 🎮 Scan Complete                    ✕ │
│ Found 5 new games in your library      │
│ [View Games]                           │
└────────────────────────────────────────┘
```

### 4.2 Notification Types

| Type | Icon | Duration | Sound |
|------|------|----------|-------|
| Success | ✅ | 5s | Chime |
| Info | ℹ️ | 5s | Soft |
| Warning | ⚠️ | 8s | Alert |
| Error | ❌ | Persistent | Error |
| Achievement | 🏆 | 8s | Fanfare |
| Friend | 👤 | 5s | Social |
| Sale | 💰 | 10s | Coin |

### 4.3 Notification Center

```
┌────────────────────────────────────────┐
│  🔔 NOTIFICATIONS                Clear │
├────────────────────────────────────────┤
│  TODAY                                 │
│  ────────────────────────────────────  │
│  🎮 Scan complete: 5 new games    2m   │
│  🏆 Achievement: Dragon Slayer   15m   │
│  💰 Hollow Knight 75% off         1h   │
│                                        │
│  YESTERDAY                             │
│  ────────────────────────────────────  │
│  👤 @Alex is now online          18h   │
│  📥 Update available: v2.0.1    23h   │
├────────────────────────────────────────┤
│  SETTINGS                              │
│  [ ] Play sounds                       │
│  [ ] Show friend activity              │
│  [ ] Sale notifications                │
└────────────────────────────────────────┘
```

---

## 5. Performance HUD

### 5.1 Compact Mode

```
┌──────────────────────────────┐
│ FPS: 144 │ CPU: 45% │ GPU: 67% │ RAM: 12.4GB │
└──────────────────────────────┘
```

### 5.2 Expanded Mode

```
┌────────────────────────────────────────┐
│  📊 PERFORMANCE              [─] [✕]  │
├────────────────────────────────────────┤
│  FPS         CPU          GPU         │
│  144         45%          67%         │
│  ████████    █████░░░░    ███████░░   │
│  Avg: 138    5900X 65°C   3080 72°C   │
├────────────────────────────────────────┤
│  Frame Time: 6.9ms       RAM: 12.4GB  │
│  1% Low: 98              VRAM: 8.2GB  │
└────────────────────────────────────────┘
```

### 5.3 HUD Positioning

- **Draggable**: User can position anywhere
- **Anchor Points**: Top-left, Top-right, Bottom-left, Bottom-right
- **Opacity**: Adjustable 20-100%
- **Always on Top**: Optional

---

## 6. Voice Indicator

### 6.1 Layout

```
┌────────────────────────────────────────┐
│  🎤 Listening...                        │
│  ▁▂▃▅▇▅▃▂▁▂▃▅▇▅▃▂▁  "Launch Elden..."  │
└────────────────────────────────────────┘
```

### 6.2 States

| State | Appearance | Animation |
|-------|------------|-----------|
| Idle | Hidden | - |
| Listening | Waveform animating | Pulse glow |
| Processing | "Processing..." | Spinner |
| Recognized | Shows transcribed text | Fade out |
| Error | Red highlight | Shake |

---

# Section B: Big Picture Mode

## 7. Big Picture Overview

### 7.1 Purpose

Full-screen, controller-optimized UI for couch gaming, Steam Deck, and TV use.

### 7.2 Design Principles

- **10-foot UI**: Large text, high contrast
- **Controller-first**: All actions via gamepad
- **Touch-friendly**: Works with Steam Deck touchscreen
- **Simplified**: Focus on core actions

---

## 8. Big Picture Shell

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  [🏠] [📚] [🥊] [📊] [⚙️]                                          ⏱️ 3:45 PM │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                                                                             │
│                                                                             │
│                                                                             │
│                         CONTENT AREA                                        │
│                         (Optimized for 10-foot viewing)                     │
│                                                                             │
│                                                                             │
│                                                                             │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  [A] Select    [B] Back    [Y] Search    [X] Options    [≡] Menu           │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 9. Big Picture Views

### 9.1 Home/Dashboard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  CONTINUE PLAYING                                                           │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐                │
│  │                │  │                │  │                │                │
│  │   ELDEN RING   │  │   CYBERPUNK    │  │    HADES       │                │
│  │                │  │                │  │                │                │
│  │  125.5 hours   │  │   89.2 hours   │  │   67.3 hours   │                │
│  │  Last: Today   │  │  Last: 3 days  │  │  Last: 1 week  │                │
│  └────────────────┘  └────────────────┘  └────────────────┘                │
│         ▲ Selected                                                          │
│                                                                             │
│  QUICK ACTIONS                                                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐                    │
│  │ 🎲 Random│  │ 🤖 AI    │  │ 🎙️ Voice │  │ ⚙️ Settings│                    │
│  │   Game   │  │ Recommend│  │ Commands │  │          │                    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Library Grid

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  LIBRARY                              🔍 Search              Filter: All ▼  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐   │
│  │        │  │        │  │        │  │        │  │        │  │        │   │
│  │ Game 1 │  │ Game 2 │  │ Game 3 │  │ Game 4 │  │ Game 5 │  │ Game 6 │   │
│  │        │  │ ▲      │  │        │  │        │  │        │  │        │   │
│  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘   │
│  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐   │
│  │        │  │        │  │        │  │        │  │        │  │        │   │
│  │ Game 7 │  │ Game 8 │  │ Game 9 │  │ Game 10│  │ Game 11│  │ Game 12│   │
│  │        │  │        │  │        │  │        │  │        │  │        │   │
│  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  [LB] Previous    142 games • Page 1 of 6    [RB] Next                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 9.3 Game Detail (Big Picture)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  ┌──────────────────────┐                                                   │
│  │                      │   ELDEN RING                                      │
│  │                      │   ★★★★★                                           │
│  │       COVER ART      │                                                   │
│  │                      │   FromSoftware • 2022 • Action RPG               │
│  │                      │   Steam • 125.5 hours • Last: Today              │
│  │                      │                                                   │
│  └──────────────────────┘   ┌─────────────────────────────────────────┐    │
│                              │                                         │    │
│  ┌──────────────────────┐   │  You last left off at Leyndell, Royal  │    │
│  │                      │   │  Capital. You had just defeated         │    │
│  │    [A] PLAY NOW      │   │  Morgott and were heading towards the  │    │
│  │                      │   │  Mountaintops of the Giants...         │    │
│  └──────────────────────┘   │                                         │    │
│                              │  🤖 AI Briefing                         │    │
│  [Y] Save States            │                                         │    │
│  [X] Options                └─────────────────────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Controller Mapping

### 10.1 Standard Mapping

| Button | Action |
|--------|--------|
| **A / Cross** | Select / Confirm |
| **B / Circle** | Back / Cancel |
| **X / Square** | Context Action 1 |
| **Y / Triangle** | Context Action 2 / Search |
| **LB / L1** | Previous Tab / Page |
| **RB / R1** | Next Tab / Page |
| **LT / L2** | Scroll Up (fast) |
| **RT / R2** | Scroll Down (fast) |
| **D-Pad** | Navigation |
| **Left Stick** | Navigation (smooth) |
| **Right Stick** | Scroll |
| **Start / Options** | Menu / Command Palette |
| **Select / Share** | Quick Settings |
| **Guide / PS** | Exit Big Picture |

### 10.2 Context-Specific Buttons

| Context | LB/RB | LT/RT |
|---------|-------|-------|
| Library | Filter tabs | Sort options |
| Game Detail | Tab switch | - |
| MUGEN | P1/P2 select | - |
| Settings | Categories | - |

---

## 11. On-Screen Keyboard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Search: elden r█                                                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐             │
│  │ 1 │ │ 2 │ │ 3 │ │ 4 │ │ 5 │ │ 6 │ │ 7 │ │ 8 │ │ 9 │ │ 0 │             │
│  └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘             │
│  ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐             │
│  │ Q │ │ W │ │ E │ │ R │ │ T │ │ Y │ │ U │ │ I │ │ O │ │ P │             │
│  └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘             │
│    ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐                 │
│    │ A │ │ S │ │ D │ │ F │ │ G │ │ H │ │ J │ │ K │ │ L │                 │
│    └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘                 │
│      ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌─────────┐               │
│      │ Z │ │ X │ │ C │ │ V │ │ B │ │ N │ │ M │ │ ⌫ Back  │               │
│      └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └───┘ └─────────┘               │
│  ┌───────┐ ┌───────────────────────────────────┐ ┌───────────┐           │
│  │ ⇧ ABC │ │            SPACE                  │ │   Done ✓  │           │
│  └───────┘ └───────────────────────────────────┘ └───────────┘           │
│                                                                             │
│  [LB] Symbols    [RB] Shift    [Y] Clear    [B] Cancel    [A] Select       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 12. Big Picture Settings Overlay

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                          ┌─────────────────────────────────┐│
│                                          │ ⚙️ QUICK SETTINGS               ││
│                                          ├─────────────────────────────────┤│
│                                          │ 🔊 Volume         [████████░░] ││
│                                          │ 🔆 Brightness     [██████░░░░] ││
│       MAIN CONTENT                       │                                 ││
│       (Dimmed)                           ├─────────────────────────────────┤│
│                                          │ 🎮 Controller: Xbox One         ││
│                                          │ 🔋 Battery: 78%                 ││
│                                          │ 📶 Network: Connected           ││
│                                          ├─────────────────────────────────┤│
│                                          │ [More Settings...]              ││
│                                          │ [Exit Big Picture]              ││
│                                          └─────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 13. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Overlays/CommandPalette.axaml` | View | Command palette |
| `Views/Overlays/AiAssistantPanel.axaml` | View | AI chat panel |
| `Views/Overlays/NotificationToast.axaml` | View | Toast notification |
| `Views/Overlays/NotificationCenter.axaml` | View | Notification history |
| `Views/Overlays/PerformanceHud.axaml` | View | Performance overlay |
| `Views/Overlays/VoiceIndicator.axaml` | View | Voice status |
| `Views/BigPicture/BigPictureShell.axaml` | View | Big Picture container |
| `Views/BigPicture/BPHomeView.axaml` | View | BP home/dashboard |
| `Views/BigPicture/BPLibraryView.axaml` | View | BP library grid |
| `Views/BigPicture/BPGameDetailView.axaml` | View | BP game detail |
| `Views/BigPicture/OnScreenKeyboard.axaml` | View | Virtual keyboard |
| `Views/BigPicture/BPSettingsOverlay.axaml` | View | BP quick settings |
| `ViewModels/Overlays/*.cs` | ViewModels | Overlay ViewModels |
| `ViewModels/BigPicture/*.cs` | ViewModels | BP ViewModels |
| `Services/OverlayService.cs` | Service | Overlay management |
| `Services/ControllerInputService.cs` | Service | Gamepad input |

---

*Next: [09_IMPLEMENTATION_TIMELINE.md](09_IMPLEMENTATION_TIMELINE.md)*
