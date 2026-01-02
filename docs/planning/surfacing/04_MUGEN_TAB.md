# 🥊 Part 4: MUGEN Tab Specification

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [03_LIBRARY_TAB.md](03_LIBRARY_TAB.md)

---

## 1. MUGEN Overview

### 1.1 Purpose

Complete MUGEN/IKEMEN fighting game management with arcade-style UI, training tools, tournaments, and AI features.

### 1.2 Design Personality

- **Theme**: Arcade fighting game aesthetic (Street Fighter / Marvel vs Capcom)
- **Colors**: Fire gradients, neon accents, dramatic blacks
- **Typography**: Bold, italicized, skewed headers
- **Animations**: Impact effects, screen shake, energy pulses

---

## 2. MUGEN Shell Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ████ MUGEN BATTLE HUB ████                           [← Back] [⚙️ Config] │
├──────────────────┬──────────────────────────────────────────────────────────┤
│  NAVIGATION      │  PLAYER SELECTION BAR                                   │
│  ┌────────────┐  │  ┌─────────────────────────────────────────────────────┐│
│  │ 🎮 ROSTER  │  │  │ P1: [Selected]     VS     P2: [Selected]           ││
│  │ 💀 DEATH   │  │  └─────────────────────────────────────────────────────┘│
│  │    BATTLE  │  ├──────────────────────────────────────────────────────────┤
│  │ 🥋 TRAINING│  │                                                          │
│  │ 🎬 REPLAYS │  │                                                          │
│  │ 🌐 ONLINE  │  │              SECTION CONTENT                             │
│  │ 🧬 FUSION  │  │                                                          │
│  │ 🏆 TOURNEY │  │                                                          │
│  │ 📊 STATS   │  │                                                          │
│  │ 🎓 COACH   │  │                                                          │
│  └────────────┘  │                                                          │
└──────────────────┴──────────────────────────────────────────────────────────┘
```

---

## 3. MUGEN Sections

### 3.1 Roster Section

**Purpose**: Character selection grid with filtering and search

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎮 CHARACTER ROSTER                           🔍 Search   [Scan Directory] │
├─────────────────────────────────────────────────────────────────────────────┤
│  [All] [Street Fighter] [Marvel] [Capcom] [SNK] [Original] [Favorites]     │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │
│  │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │        │
│  │Portrait│ │Portrait│ │Portrait│ │Portrait│ │Portrait│ │Portrait│        │
│  │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │ │ ▓▓▓▓▓▓ │        │
│  ├────────┤ ├────────┤ ├────────┤ ├────────┤ ├────────┤ ├────────┤        │
│  │ RYU    │ │ KEN    │ │ CHUN-LI│ │WOLVERINE│ │ AKUMA  │ │ GUILE  │        │
│  │ ★★★★★  │ │ ★★★★☆  │ │ ★★★★★  │ │ ★★★★★  │ │ ★★★★★  │ │ ★★★★☆  │        │
│  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘ └────────┘        │
│                                                                             │
│  Showing 156 characters                                  Page 1 of 26      │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Character Card Actions**:

- Left Click: Select as P1
- Right Click: Select as P2
- Middle Click: View character details
- Double Click: Quick match

### 3.2 Death Battle Section

**Purpose**: AI battle simulator with prediction and analysis

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  💀 DEATH BATTLE SIMULATOR                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│    ┌──────────────────┐           VS           ┌──────────────────┐         │
│    │                  │                        │                  │         │
│    │    PLAYER 1      │     ████████████       │    PLAYER 2      │         │
│    │    PORTRAIT      │                        │    PORTRAIT      │         │
│    │                  │                        │                  │         │
│    ├──────────────────┤                        ├──────────────────┤         │
│    │ RYU              │                        │ WOLVERINE        │         │
│    │ Street Fighter   │                        │ Marvel           │         │
│    │ Win Rate: 67%    │                        │ Win Rate: 72%    │         │
│    └──────────────────┘                        └──────────────────┘         │
│                                                                             │
│    ┌────────────────────────────────────────────────────────────────┐       │
│    │ 🤖 AI PREDICTION                                               │       │
│    │ Wolverine has 58% chance of winning based on:                  │       │
│    │ • Higher aggression rating                                     │       │
│    │ • Faster recovery frames                                       │       │
│    │ • Superior combo potential                                     │       │
│    └────────────────────────────────────────────────────────────────┘       │
│                                                                             │
│    Simulation Settings:                                                     │
│    Matches: [1] [10] [100] [1000]     AI Level: [Easy ▼]                  │
│                                                                             │
│    [ 🔥 RUN DEATH BATTLE 🔥 ]                                               │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  RESULTS                                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ Match 1: WOLVERINE wins (Perfect)                                  │    │
│  │ Match 2: RYU wins (Close)                                          │    │
│  │ Match 3: WOLVERINE wins (Dominant)                                 │    │
│  │ ...                                                                │    │
│  │ FINAL: WOLVERINE wins 623/1000 (62.3%)                            │    │
│  └────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.3 Training Section

**Purpose**: Frame data analysis, combo practice, recording

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🥋 TRAINING MODE                                              [Start Game] │
├─────────────────────────────────────────────────────────────────────────────┤
│  Character: [RYU ▼]              Dummy: [KEN ▼]              Stage: [Grid] │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │                        [ TRAINING VIEWPORT ]                           │  │
│  │                                                                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐│
│ │ FRAME DATA  │ │ INPUT       │ │ COMBO       │ │ RECORDING               ││
│ │             │ │ DISPLAY     │ │ COUNTER     │ │                         ││
│ │ Move: LP    │ │  ↓ ↘ → P   │ │ 15 HITS     │ │ ● REC  ▶ PLAY  💾 SAVE ││
│ │ Startup: 4f │ │             │ │ 3420 DMG    │ │                         ││
│ │ Active: 2f  │ │             │ │             │ │ Saved: 3 combos         ││
│ │ Recovery: 6f│ │             │ │ Best: 23    │ │                         ││
│ │ On Block: +2│ │             │ │             │ │                         ││
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────────────┘│
├─────────────────────────────────────────────────────────────────────────────┤
│ DUMMY SETTINGS                                                              │
│ Behavior: [Stand ▼]  Guard: [None ▼]  Recovery: [Normal ▼]  HP: [∞ ▼]    │
│ Counter Attack: [Off ▼]                                                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.4 Replay Theater Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🎬 REPLAY THEATER                                           [Import Replay]│
├─────────────────────────────────────────────────────────────────────────────┤
│  Filter: [All ▼]  Sort: [Date ▼]                            🔍 Search      │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ★ RYU vs WOLVERINE                              Dec 30, 2024  3:45PM│   │
│  │ Result: RYU wins (2-1)  Duration: 4:32          [▶ Play] [📊 Analyze]│   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ AKUMA vs KEN                                    Dec 29, 2024  8:12PM│   │
│  │ Result: AKUMA wins (2-0)  Duration: 2:15        [▶ Play] [📊 Analyze]│   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ CHUN-LI vs GUILE                                Dec 28, 2024  5:30PM│   │
│  │ Result: CHUN-LI wins (2-1)  Duration: 5:00      [▶ Play] [📊 Analyze]│   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.5 Tournament Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🏆 TOURNAMENT MODE                                     [+ Create Tournament]│
├─────────────────────────────────────────────────────────────────────────────┤
│  [Active] [Completed] [Templates]                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        BRACKET VIEW                                  │   │
│  │                                                                      │   │
│  │   Round 1         Quarter        Semi          Finals                │   │
│  │   ┌─────┐                                                            │   │
│  │   │ RYU ├────┐                                                       │   │
│  │   └─────┘    ├───┐                                                   │   │
│  │   ┌─────┐    │   │                                                   │   │
│  │   │ KEN ├────┘   ├───┐                                               │   │
│  │   └─────┘        │   │                                               │   │
│  │   ┌─────┐        │   │        ┌─────┐                                │   │
│  │   │AKUMA├────┐   │   ├────────│ ??? │                                │   │
│  │   └─────┘    ├───┘   │        └─────┘                                │   │
│  │   ┌─────┐    │       │            │                                  │   │
│  │   │GUILE├────┘       │            │                                  │   │
│  │   └─────┘            │            ├────────► CHAMPION                │   │
│  │   ...                │            │                                  │   │
│  │                      │        ┌─────┐                                │   │
│  │                      └────────│ ??? │                                │   │
│  │                               └─────┘                                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  [▶ Run Next Match]  [⏩ Simulate All]  [📊 View Stats]                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.6 Character Fusion Section

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🧬 CHARACTER FUSION LABORATORY                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│    ┌──────────────────┐     +     ┌──────────────────┐    =    ┌────────┐  │
│    │                  │           │                  │         │        │  │
│    │   CHARACTER 1    │           │   CHARACTER 2    │         │ FUSION │  │
│    │    [Select]      │           │    [Select]      │         │PREVIEW │  │
│    │                  │           │                  │         │        │  │
│    │   RYU            │           │   WOLVERINE      │         │  ???   │  │
│    └──────────────────┘           └──────────────────┘         └────────┘  │
│                                                                             │
│    ┌────────────────────────────────────────────────────────────────────┐   │
│    │ FUSION SETTINGS                                                    │   │
│    │                                                                    │   │
│    │ Name: [RYU-VERINE          ]                                       │   │
│    │                                                                    │   │
│    │ Sprite Blend:  [████████░░] 80% Char1 / 20% Char2                 │   │
│    │ Moveset Split: [█████░░░░░] 50% Char1 / 50% Char2                 │   │
│    │ Stats Balance: [██████░░░░] 60% Char1 / 40% Char2                 │   │
│    │                                                                    │   │
│    │ Balance Mode:  [Automatic ▼]                                       │   │
│    │                                                                    │   │
│    └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│    [ 🧬 GENERATE FUSION ]                                                   │
│                                                                             │
│    ┌────────────────────────────────────────────────────────────────────┐   │
│    │ 🤖 AI ANALYSIS                                                     │   │
│    │ "This fusion combines Ryu's projectile game with Wolverine's      │   │
│    │ rushdown capabilities. Predicted tier: A-. Weakness: Recovery..."  │   │
│    └────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. MUGEN Services Mapping

| Section | Service | Methods Used |
|---------|---------|--------------|
| Roster | `IMugenCharacterLoader` | `GetAllCharactersAsync()`, `ScanDirectoryAsync()` |
| Roster | `IMugenCharacterRepository` | `GetByIdAsync()`, `GetByFranchiseAsync()` |
| Death Battle | `IDeathMatchSimulator` | `SimulateMatchAsync()`, `SimulateSeriesAsync()` |
| Death Battle | `IMatchPredictionEngine` | `PredictWinnerAsync()`, `AnalyzeMatchupAsync()` |
| Training | `IMugenTrainingService` | `StartSession()`, `RecordCombo()`, `GetFrameData()` |
| Replays | Plugin: MugenReplay | `GetReplaysAsync()`, `PlayReplayAsync()`, `AnalyzeAsync()` |
| Online | Plugin: MugenNetwork | `GetLobbiesAsync()`, `JoinLobbyAsync()`, `CreateLobbyAsync()` |
| Fusion | Plugin: MugenFusion | `CreateFusionAsync()`, `GenerateSpriteAsync()` |
| Tournament | `IMugenTournamentService` | `CreateTournamentAsync()`, `SimulateRoundAsync()` |
| Stats | `IMugenStatsService` | `GetCharacterStatsAsync()`, `GetPlayerStatsAsync()` |
| Coach | `IMugenCoachService` | `GetMatchAdviceAsync()`, `AnalyzePlaystyleAsync()` |

---

## 5. Files to Create

| File | Type | Description |
|------|------|-------------|
| `Views/Mugen/MugenShell.axaml` | View | MUGEN container (replaces current) |
| `Views/Mugen/Sections/RosterSection.axaml` | View | Character roster |
| `Views/Mugen/Sections/DeathBattleSection.axaml` | View | Death battle simulator |
| `Views/Mugen/Sections/TrainingSection.axaml` | View | Training mode |
| `Views/Mugen/Sections/ReplaySection.axaml` | View | Replay theater |
| `Views/Mugen/Sections/OnlineSection.axaml` | View | Online hub |
| `Views/Mugen/Sections/FusionSection.axaml` | View | Character fusion |
| `Views/Mugen/Sections/TournamentSection.axaml` | View | Tournament brackets |
| `Views/Mugen/Sections/StatsSection.axaml` | View | Statistics |
| `Views/Mugen/Sections/CoachSection.axaml` | View | AI coaching |
| `Views/Mugen/Components/CharacterCard.axaml` | Component | Character grid card |
| `Views/Mugen/Components/PlayerSlot.axaml` | Component | P1/P2 selection |
| `Views/Mugen/Components/BracketView.axaml` | Component | Tournament bracket |
| `ViewModels/Mugen/*.cs` | ViewModels | All MUGEN ViewModels |

---

*Next: [05_ANALYTICS_SOCIAL.md](05_ANALYTICS_SOCIAL.md)*
