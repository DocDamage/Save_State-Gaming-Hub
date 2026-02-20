# Memory Intelligence Guide

**Version:** 2.5.1  
**Last Updated:** February 20, 2026  
**Applies to:** SaveStateReborn v2.5+

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Quick Start Guide](#quick-start-guide)
3. [Auto-Discovery Feature](#auto-discovery-feature)
4. [ML Prediction System](#ml-prediction-system)
5. [Signature Verification](#signature-verification)
6. [Adding Custom Games](#adding-custom-games)
7. [Troubleshooting](#troubleshooting)
8. [Best Practices](#best-practices)

---

## Getting Started

### What is Memory Intelligence?

Memory Intelligence is SaveStateReborn's advanced system for reading and monitoring game memory in real-time. It allows you to:

- **Track game values** like health, ammo, currency, and score in real-time
- **Freeze values** to prevent them from changing (god mode, infinite ammo)
- **Auto-discover unknown values** using AI-powered heuristics
- **Import Cheat Engine tables** from the community
- **Create custom signatures** for unsupported games
- **Verify signature health** to ensure reliability

### Supported Games (336+ with Signatures)

SaveStateReborn includes built-in signatures for **336+ games** across multiple genres:

| Genre | Examples |
|-------|----------|
| **Action RPG** | Elden Ring, Dark Souls series, The Witcher 3, Monster Hunter |
| **FPS** | Call of Duty, Counter-Strike, Valorant, Apex Legends |
| **Indie** | Hollow Knight, Hades, Celeste, Dead Cells |
| **RPG** | Skyrim, Fallout, Baldur's Gate 3, Cyberpunk 2077 |
| **Platformer** | Cuphead, Ori series, Shovel Knight |
| **Strategy** | Civilization VI, XCOM 2, Total War series |

**View all supported games:** Tools → Memory Intelligence → Game Database

### System Requirements and Permissions

#### Minimum Requirements
- **OS:** Windows 10/11 (64-bit), Linux, macOS
- **RAM:** 4GB (8GB recommended for large games)
- **Permissions:** Administrator/Root access for memory reading

#### Required Permissions

| Platform | Requirements |
|----------|-------------|
| **Windows** | Run as Administrator for most games |
| **Linux** | Add user to `ptrace` scope or run with `sudo` |
| **macOS** | Disable SIP (System Integrity Protection) for some games |

#### Security Software
Some antivirus software may flag memory reading as suspicious. Add SaveStateReborn to your exclusions list if needed.

---

## Quick Start Guide

### Attaching to a Game

1. **Launch your game** - Start the game you want to monitor
2. **Open SaveStateReborn** → Navigate to **Memory Intelligence** tab
3. **Click "Attach"** - Select your game process from the list
4. **Wait for detection** - The system will scan for known signatures

![Attach Dialog](assets/screenshots/memory-attach.png)
*The attach dialog shows running processes with game icons*

### Scanning for Values

After attaching, choose one of three methods:

#### Method 1: Auto-Discovery (Recommended)
Best for: Games without existing signatures

1. Click **"Start Discovery"**
2. Follow the on-screen prompts
3. The system learns from your actions

#### Method 2: Cheat Engine Import
Best for: Games with existing Cheat Engine tables

1. Download a `.CT` file from [Fearless Revolution](https://fearlessrevolution.com/)
2. Drag & drop into the Import dialog
3. Review and select entries to import

#### Method 3: Manual Debugger
Best for: Advanced users

1. Click the **"Debugger"** tab
2. Add memory addresses manually
3. Set value types and offsets

### Freezing Values

Once values are detected:

1. **Find the value** in the watch list
2. **Toggle the "Freeze" checkbox** next to it
3. The value will remain constant regardless of gameplay

| Value Type | Freeze Effect |
|------------|--------------|
| Health | God mode - never take damage |
| Ammo | Infinite ammunition |
| Currency | Money never decreases |
| Timer | Freeze countdown timers |

⚠️ **Warning:** Freezing values may cause game instability or crashes. Use with caution.

---

## Auto-Discovery Feature

The AI-powered auto-discovery system uses **24 heuristics** to detect game values without prior knowledge of the game.

### The 24 Heuristics

The system employs specialized heuristics organized into categories:

#### Core Values (7)
| Heuristic | Detects | Typical Range |
|-----------|---------|---------------|
| **Health** | HP, hit points, lives | 1-10,000 |
| **Currency** | Gold, credits, money | 0-999,999,999 |
| **Experience** | XP, skill points | 0-999,999,999 |
| **Ammo** | Bullets, arrows, charges | 0-999 |
| **Score** | Points, rankings | 0-9,999,999,999 |
| **Position** | X, Y, Z coordinates | -100,000 to +100,000 |
| **Timer** | Countdowns, speedrun timers | 0-86,400 seconds |

#### Movement & Physics (4)
| Heuristic | Detects | Behavior |
|-----------|---------|----------|
| **Speed** | Movement speed | Changes with sprint/walk |
| **Velocity** | Directional velocity | Vector components |
| **Jump Height** | Vertical leap force | Triggered on jump |
| **Gravity** | Gravitational constant | Affects fall speed |

#### Combat Mechanics (4)
| Heuristic | Detects | Typical Use |
|-----------|---------|-------------|
| **Cooldown** | Ability cooldowns | Decreases over time |
| **Damage** | Attack power | Affects enemy HP |
| **Critical Chance** | Crit % | 0-100 range |
| **Armor Rating** | Damage reduction | Reduces incoming damage |

#### RPG Progression (3)
| Heuristic | Detects | Notes |
|-----------|---------|-------|
| **Skill Points** | Available points to spend | Spent on level up |
| **Reputation** | Faction standing | -100 to +100 |
| **Carry Weight** | Inventory weight | Current/max |

#### Resource Management (3)
| Heuristic | Detects | Examples |
|-----------|---------|----------|
| **Mana** | Magic energy | 0-100 or 0-1000 |
| **Durability** | Item condition | 0-100% |
| **Resource Count** | Crafting materials | 0-9999 |

#### Game State (3)
| Heuristic | Detects | Values |
|-----------|---------|--------|
| **Difficulty** | Game difficulty | 0-4 (Easy-Nightmare) |
| **Game Time** | Total playtime | Increases constantly |
| **Completion** | Progress % | 0-100% |

### How It Works

```
┌─────────────────┐
│  Initial Scan   │ ← Finds all values in common ranges
│  (All memory)   │
└────────┬────────┘
         ▼
┌─────────────────┐
│  User Action    │ ← You perform an action (take damage)
│  (Take damage)  │
└────────┬────────┘
         ▼
┌─────────────────┐
│  Filtering      │ ← System eliminates values that didn't
│  (Health ↓)     │   decrease (not health)
└────────┬────────┘
         ▼
┌─────────────────┐
│  Heuristic      │ ← AI ranks remaining candidates
│  Scoring        │   by confidence (0.0-1.0)
└────────┬────────┘
         ▼
┌─────────────────┐
│  Results        │ ← Top candidates presented with
│  (Confidence)   │   confidence scores
└─────────────────┘
```

### Using Auto-Discovery

1. **Click "Start Discovery"** in the Memory Intelligence tab

2. **Follow the prompts:**
   - "Take damage" → finds Health
   - "Spend money" → finds Currency
   - "Fire weapon" → finds Ammo
   - "Move character" → finds Position

3. **Review discovered values** with confidence scores:
   - 🟢 **0.8-1.0** - High confidence (likely correct)
   - 🟡 **0.5-0.8** - Medium confidence (verify manually)
   - 🔴 **0.0-0.5** - Low confidence (probably incorrect)

4. **Add high-confidence values** to your watch list

### Discovery Tips

- **Be specific** with actions - "Take exactly 10 damage" is better than "Take damage"
- **Multiple actions** improve accuracy - perform the same action 2-3 times
- **Wait between actions** - let the game state settle (1-2 seconds)
- **Start with health** - it's usually the easiest to detect

---

## ML Prediction System

The Machine Learning system learns from successful discoveries to predict patterns in new games.

### Genre Detection

Automatically detects game genre from process name:

| Process Pattern | Detected Genre | Confidence |
|-----------------|----------------|------------|
| `cod.exe`, `bf*.exe` | FirstPersonShooter | 95% |
| `eldenring.exe`, `dark*.exe` | ActionRPG | 92% |
| `hades.exe`, `deadcells.exe` | Roguelike | 88% |
| `civ*.exe`, `aow*.exe` | TurnBasedStrategy | 85% |
| `fifa*.exe`, `nba*.exe` | Sports | 90% |

### Engine Detection

Identifies game engine from loaded modules:

| Module Pattern | Engine | Common Values |
|----------------|--------|---------------|
| `unityplayer.dll` | Unity | Float positions, Int health |
| `UE4Editor*.dll`, `Unreal*.dll` | Unreal Engine 4/5 | Float health, complex pointers |
| `Source*.dll` | Source Engine | Int ammo, Float position |
| `CrySystem.dll` | CryEngine | Float coordinates |
| `Godot*.dll` | Godot | Varied (custom schemas) |
| `GameMaker*.dll` | GameMaker | Simple Int values |

### Pattern Recommendations

Based on genre/engine, the system suggests:

```json
{
  "gameGenre": "ActionRPG",
  "engine": "Unreal Engine 5",
  "recommendedRanges": {
    "health": { "type": "float", "min": 0, "max": 1000 },
    "stamina": { "type": "float", "min": 0, "max": 100 },
    "position": { "type": "float", "isVector": true }
  },
  "historicalSuccess": {
    "health": 0.94,
    "stamina": 0.87,
    "currency": 0.91
  }
}
```

### ML Training Data

The system improves over time using:

- **Community contributions** - Verified signatures from users
- **Successful discoveries** - Auto-discovery that were confirmed correct
- **Game updates** - Pattern changes tracked across versions

**Contribute to ML improvement:**
1. After auto-discovery, confirm if values were correct
2. Submit feedback via "Was this correct?" dialog
3 Your feedback trains the heuristics

---

## Signature Verification

Before using signatures, verify they work correctly with the built-in testing tools.

### Verification Types

#### Static Verification
Basic checks performed without gameplay:

| Check | Description | Pass Criteria |
|-------|-------------|---------------|
| **Pattern Match** | Pattern found in memory | Address resolved |
| **Value Range** | Value is in expected range | Min ≤ Value ≤ Max |
| **Memory Access** | Region is readable | ReadProcessMemory succeeds |
| **Type Validation** | Value type matches | Int/float conversion valid |

#### Dynamic Verification
Requires user interaction:

| Check | User Action | Expected Result |
|-------|-------------|-----------------|
| **Health** | Take damage | Value decreases |
| **Currency** | Spend money | Value decreases |
| **Ammo** | Fire weapon | Value decreases by 1 |
| **Position** | Move character | Value changes smoothly |
| **Score** | Earn points | Value increases |

#### Stability Verification
Long-term reliability check:

| Test | Duration | Pass Criteria |
|------|----------|---------------|
| **Value Stability** | 30 seconds | Value changes as expected |
| **Address Stability** | 5 minutes | Address remains valid |
| **Pointer Chain** | Each access | All pointers resolve |

### Health Scores

Signatures receive a health score (0-100) based on verification results:

| Score | Rating | Color | Action |
|-------|--------|-------|--------|
| **90-100** | Excellent | 🟢 | Fully verified, reliable |
| **70-89** | Good | 🟢 | Works with minor issues |
| **50-69** | Fair | 🟡 | Use with caution |
| **30-49** | Poor | 🟠 | Likely broken |
| **0-29** | Broken | 🔴 | Do not use |

### Running Verification

1. **Select a signature** in the Memory Intelligence tab
2. **Click "Verify"** button
3. **Follow prompts** for dynamic tests
4. **Review results** and health score

### Batch Verification

Verify multiple signatures at once:

1. **Tools** → **Signature Tester**
2. **Select game** from dropdown
3. **Check signatures** to test
4. **Click "Run All Tests"**

---

## Adding Custom Games

### Method 1: Cheat Engine Import (Easiest)

1. **Download a .CT file** from Fearless Revolution or other sources
2. **Open SaveStateReborn** → Tools → Import Cheat Engine Table
3. **Drag & drop** the `.CT` file or click Browse
4. **Review entries** in the preview dialog:
   - Check value types (4 Bytes, Float, etc.)
   - Verify addresses look valid
   - Deselect entries you don't need
5. **Select import options:**
   - Overwrite existing signatures
   - Skip duplicates
   - Import with verification required
6. **Click Import**
7. **Test in-game** to verify signatures work

**Supported Cheat Engine Formats:**
- ✅ Cheat Engine 6.0+ XML format
- ✅ Compressed tables (auto-detected)
- ✅ Plain XML structure
- ⚠️ Lua scripts (imported with warning)
- ❌ Array of Byte (not supported)

### Method 2: Manual Discovery

For games without existing tables:

1. **Use Auto-Discovery** to find values
2. **Verify with dynamic tests**
3. **Export to JSON** via File → Export Signatures
4. **Edit the JSON** to add metadata:

```json
{
  "id": "my-game",
  "title": "My Game",
  "processNames": ["MyGame.exe", "mygame_launcher.exe"],
  "platform": "PC",
  "category": "ActionRPG",
  "signatures": [
    {
      "name": "Health",
      "category": "Combat",
      "pattern": "8B 45 ?? 89 45 ??",
      "offset": 8,
      "valueType": "float",
      "minFloatValue": 0,
      "maxFloatValue": 1000,
      "description": "Player health points",
      "moduleName": "MyGame.exe",
      "tags": ["critical", "combat"]
    }
  ]
}
```

### JSON Format Reference

#### Game Entry

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | ✅ | Unique identifier (lowercase, no spaces) |
| `title` | string | ✅ | Display name |
| `processNames` | string[] | ✅ | Executable names to match |
| `platform` | string | ✅ | PC, Switch, PS4, etc. |
| `category` | string | ✅ | Genre classification |
| `signatures` | array | ✅ | Memory signatures |

#### Signature Entry

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | ✅ | Value name (e.g., "Health") |
| `category` | string | ✅ | Combat, Currency, Progress, etc. |
| `pattern` | string | ✅ | Hex pattern with wildcards (??) |
| `offset` | integer | ✅ | Offset from pattern to value |
| `valueType` | string | ✅ | int32, float, double, int64 |
| `description` | string | ❌ | Human-readable description |
| `minValue` | integer | ❌ | Minimum valid value |
| `maxValue` | integer | ❌ | Maximum valid value |
| `moduleName` | string | ❌ | DLL/exe to search within |
| `tags` | string[] | ❌ | Tags for organization |

### Adding to Database

Place your JSON file in:

```
%APPDATA%/SaveStateReborn/Signatures/
├── custom/
│   └── my-game.json
└── overrides/
    └── health-tweaks.json
```

Restart SaveStateReborn to load new signatures.

---

## Troubleshooting

### Common Issues

#### "Failed to attach to process"

| Cause | Solution |
|-------|----------|
| Game not running | Launch the game first |
| Wrong architecture | Ensure 64-bit SaveStateReborn for 64-bit games |
| Insufficient permissions | Run as Administrator |
| Anti-cheat protection | Some games block memory reading |

#### "Pattern not found"

| Cause | Solution |
|-------|----------|
| Game version mismatch | Update signature for new version |
| Pattern too specific | Use more wildcards (??) |
| Wrong module | Remove module constraint or try different DLL |
| Address randomized | Use pointer chains instead of static addresses |

#### "Value doesn't change"

| Cause | Solution |
|-------|----------|
| Wrong value type | Try float instead of int, or vice versa |
| Encrypted value | Some games encrypt values in memory |
| Visual-only value | Value is for display, not game logic |

#### "Game crashes when freezing"

| Cause | Solution |
|-------|----------|
| Wrong address | Verify signature with tests |
| Critical system value | Don't freeze engine-internal values |
| Anti-cheat detection | Some games detect memory modifications |

#### "Auto-discovery finds nothing"

| Cause | Solution |
|-------|----------|
| Game uses unusual ranges | Adjust scan range in options |
| Values are encrypted | Try pattern-based detection instead |
| 64-bit values | Enable int64 scanning in options |

### Debug Logging

Enable detailed logging to diagnose issues:

1. **Settings** → **Advanced** → **Debug Mode**
2. **Reproduce the issue**
3. **Check logs:** `%APPDATA%/SaveStateReborn/Logs/`

### Getting Help

- **Discord:** [Join our community](https://discord.gg/savestate)
- **GitHub Issues:** [Report bugs](https://github.com/savestate/issues)
- **Documentation:** [Full docs](https://docs.savestate.dev)

---

## Best Practices

### Ethics and Safety

✅ **DO:**
- Use Memory Intelligence for single-player games only
- Respect game developers' work
- Share verified signatures with the community
- Report broken signatures to help others
- Use for accessibility (e.g., reducing difficulty)

❌ **DON'T:**
- Use in multiplayer/online games
- Use for competitive advantage against other players
- Share methods to bypass anti-cheat
- Modify values in esports/competitive titles
- Use to unlock paid content illegally

### Performance Tips

- **Limit frozen values** - Each frozen value uses CPU cycles
- **Use value validation** - Set min/max ranges to catch bad reads
- **Disable unused signatures** - Uncheck signatures you're not using
- **Close when not needed** - Detach from game when done

### Backup and Export

Regularly export your signatures:

1. **Tools** → **Export Signatures**
2. **Choose format:** JSON or CT
3. **Save to cloud storage** for backup

### Contributing Signatures

Help grow the database:

1. **Create working signatures** using Auto-Discovery or import
2. **Verify thoroughly** with all test types
3. **Export and share** on:
   - [Fearless Revolution](https://fearlessrevolution.com/)
   - GitHub Gist
   - Our Discord #signatures channel
4. **Include metadata:** Game version, platform, notes

### Version Compatibility

Game updates often break signatures:

| Update Type | Signature Impact |
|-------------|-----------------|
| **Hotfix** | Usually unchanged |
| **Patch** | May change some addresses |
| **Major Update** | Likely breaks all signatures |
| **Engine Update** | Will break all signatures |

**After game updates:**
1. Run signature verification
2. Update broken patterns
3. Re-verify before using

---

## Quick Reference

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+A` | Attach to process |
| `Ctrl+D` | Start discovery |
| `Ctrl+F` | Freeze/unfreeze selected value |
| `Ctrl+E` | Export signatures |
| `Ctrl+I` | Import Cheat Engine table |
| `F5` | Refresh values |
| `Delete` | Remove selected watch |

### Value Type Quick Reference

| Type | Size | Use For |
|------|------|---------|
| **Byte** | 1 byte | Small counters, flags |
| **Int16** | 2 bytes | Short values |
| **Int32** | 4 bytes | Most game values |
| **Int64** | 8 bytes | Large counters, Steam IDs |
| **Float** | 4 bytes | Health, position, time |
| **Double** | 8 bytes | Precise values |

### Pattern Wildcards

| Wildcard | Meaning | Example |
|----------|---------|---------|
| `??` | Any byte | `A1 ?? ?? ?? ??` matches any 5-byte sequence starting with A1 |
| `**` | Same as ?? | Alternative wildcard syntax |
| `?` | Nibble wildcard | `A?` matches A0-AF |

---

**Related Documentation:**
- [Cheat Engine Table Sources](./CHEAT_TABLE_SOURCES.md)
- [Plugin SDK](./PLUGIN_SDK.md)
- [Quick Reference](./QUICK_REFERENCE.md)

**Need more help?** Visit our [Discord community](https://discord.gg/savestate) or [GitHub Discussions](https://github.com/savestate/discussions).
