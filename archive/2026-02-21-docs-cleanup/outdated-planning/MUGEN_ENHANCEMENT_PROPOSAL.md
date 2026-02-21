# MUGEN/IKEMEN Enhancement Proposal

This document outlines potential improvements and feature additions to the MUGEN/IKEMEN integration within SaveStateReborn. These suggestions aim to transform the MUGEN Hub from a simple launcher into a comprehensive, meta-game ecosystem.

## 🚀 Phase 1: Quality of Life & Metadata (Short-Term)

### 1. Visual Roster Manager

- **Feature**: A drag-and-drop interface for managing the `select.def` file.
- **Benefit**: Eliminates the need for manual text editing. Users can visually organize their roster, create categories (e.g., Marvel vs Capcom, Dragon Ball), and assign stages.
- **Implementation**: Parse `select.def` into a visual grid; save changes back to the file on exit.

### 2. Automated Move Extraction (Integration with `iguana`)

- **Feature**: Automatically extract and display character movesets, combos, and frame data within the Hub.
- **Benefit**: Provides users with a "Move List" overlay or side panel without needing to memorize commands or check external wikis.
- **Status**: Identified in [ikemen_repositories_analysis.md](file:///c:/Users/Doc/Desktop/SaveStateReborn/docs/planning/ikemen_repositories_analysis.md).

### 3. Netplay Lobby Browser (IKEMEN GO)

- **Feature**: A built-in browser for Ikemen GO netplay lobbies.
- **Benefit**: Simplifies online play by allowing users to find and join matches directly from the SaveStateReborn dashboard.
- **Integration**: Potentially integrate with [Discord Rich Presence](file:///c:/Users/Doc/Desktop/SaveStateReborn/docs/planning/V2_FEATURE_ROADMAP.md) to show "Joinable" states.

---

## 🎮 Phase 2: Meta-Game & Simulation (Medium-Term)

### 4. SaltyBet-Style AI Tournaments

- **Feature**: An automated tournament mode where the AI controls both players.
- **Benefit**: Creates a "spectator" experience where users can watch the computer battle.
- **Enhancement**: Add a virtual currency/points system for users to "bet" on outcomes, with leaderboards.

### 5. Roster-Wide ELO & Performance Tracking

- **Feature**: Track the Win/Loss ratio and ELO rating of every character across all AI battles and tournaments.
- **Benefit**: Identify "Top Tier" and "Bottom Tier" characters in your specific roster, allowing for automated balance reporting.

### 6. Dynamic Roster Discovery

- **Feature**: Integrated search for popular MUGEN repositories (Mugen Archive, GitHub).
- **Benefit**: One-click install for new characters and stages directly into the SaveStateReborn library.

---

## 🛠️ Phase 3: Advanced Development & Modding (Long-Term)

### 7. Integrated Sprite & Animation Previewer

- **Feature**: View SFF (sprite) and AIR (animation) files directly in the SaveStateReborn UI.
- **Benefit**: Preview characters before committing them to the roster or use it for quick asset extraction for modding.

### 8. MUGEN to IKEMEN Auto-Converter

- **Feature**: A tool to automatically detect and fix compatibility issues when moving old MUGEN 1.0/1.1 characters to Ikemen GO.
- **Benefit**: Future-proofs legacy collections.

### 9. Mortal Kombat Logic Toolkit (OpenMK)

- **Feature**: Deep integration of [OpenMK](https://github.com/Lazin3ss/OpenMK) mechanics.
- **Benefit**: Adds "Fatality" triggers, "Tower" modes, and specialized MK-style HUDs to compatible characters.
- **Status**: Detailed in [character_development_integration_plan.md](file:///c:/Users/Doc/Desktop/SaveStateReborn/docs/planning/character_development_integration_plan.md).

---

## 📊 Feature Priority Matrix

| Feature | Impact | Effort | Priority |
| :--- | :---: | :---: | :---: |
| Visual Roster Manager | 🔥 High | Med | **P0** |
| Move List Extraction | Medium | Low | **P1** |
| AI Tournament Sim | 🔥 High | High | **P1** |
| Netplay Lobby Browser | Medium | Med | **P2** |
| Sprite Previewer | Medium | Med | **P2** |
| Auto-Converter | Low | High | **P3** |

---

## 🔗 Related Documentation

- [Character Development Integration Plan](file:///c:/Users/Doc/Desktop/SaveStateReborn/docs/planning/character_development_integration_plan.md)
- [Ikemen Repository Analysis](file:///c:/Users/Doc/Desktop/SaveStateReborn/docs/planning/ikemen_repositories_analysis.md)
- [Mugen Character Repositories Analysis](file:///c:/Users/Doc/Desktop/SaveStateReborn/docs/planning/mugen_character_repositories_analysis.md)
