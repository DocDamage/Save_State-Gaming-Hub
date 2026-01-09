# UI Feature Surfacing Plan (Placeholder)

**Status**: 🏗️ Phases 1-6 Complete (approx. 66% UI implementation surfaced)
**Current Focus**: MUGEN Hub & Big Picture Mode
**Last Updated**: January 8, 2026
**See**: [DEVELOPMENT_STATUS.md](../status/DEVELOPMENT_STATUS.md) for current implementation progress.

---

## 🗺️ UI Surfacing Map

This document bridges the gap between backend services (90+ services) and UI views (118+ planned views/panels).

### Current Surfacing Status

| Phase | Feature Set | Status | UI Target |
|-------|-------------|--------|-----------|
| **Phase 1** | Shell & Navigation | ✅ 100% | Main Window, Sidebar, Navigation Rail |
| **Phase 2** | Dashboard Hub | ✅ 100% | Home View, Recent Games, News Feed |
| **Phase 3** | Library Enhancement | 🏗️ 90% | Grid/List/Compact/Table Views, Filtering, Sorting, Search |
| **Phase 4** | Analytics & Social | ✅ 100% | Stats Dashboard, Heatmaps, Activity Feed |
| **Phase 5** | Cloud & Sync | 🔓 Planned | Cloud Settings, Sync Status Overlay |
| **Phase 6** | Voice & AI | 🔓 Planned | AI Assistant Panel (Overlay implemented) |
| **Phase 7** | Automation | 🔓 Planned | Workflow Editor, Macro Panel |
| **Phase 8** | Memory Intelligence | 🔓 Planned | Game Debugger View, Memory Monitor |
| **Phase 9** | MUGEN Hub | 🔓 Planned | Tournament Bracket View, Fight Settings |

---

## 🏗️ Implementation Guidelines

- All views must follow the [ENGINEERING_RULES.md](../architecture/ENGINEERING_RULES.md).
- Use `SaveState.Presentation.ViewModels` for all logic.
- Ensure 0 `async void` methods (currently 3 violations being fixed).
- Use the Result pattern for all service-to-view communication.

---

*Note: This is a living document. Detailed surfacing specifications for each view are located in the `docs/planning/surfacing/` subdirectory.*
