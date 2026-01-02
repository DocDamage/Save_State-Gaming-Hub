# 📅 Part 9: Implementation Timeline & Task Breakdown

**Parent Document**: [FEATURE_SURFACING_PLAN.md](../FEATURE_SURFACING_PLAN.md)
**Previous**: [08_OVERLAYS_BIGPICTURE.md](08_OVERLAYS_BIGPICTURE.md)

---

## 1. Implementation Overview

### 1.1 Total Effort Estimate

| Metric | Count |
|--------|-------|
| **Total Views** | 118 |
| **Total ViewModels** | 85 |
| **New Services** | 15 |
| **Total Weeks** | 16 |
| **Estimated Dev Days** | 80 |

### 1.2 Phase Summary

| Phase | Weeks | Focus | Views | Priority |
|-------|-------|-------|-------|----------|
| 1 | 1-2 | Core Shell & Navigation | 12 | 🔴 Critical |
| 2 | 3-4 | Dashboard Hub | 20 | 🔴 Critical |
| 3 | 5-6 | Library Enhancement | 18 | 🔴 Critical |
| 4 | 7-8 | Analytics & Social | 13 | 🟡 High |
| 5 | 9-10 | Tools & Utilities | 25 | 🟡 High |
| 6 | 11 | Terminal & CLI | 5 | 🟢 Medium |
| 7 | 12 | MUGEN Enhancement | 12 | 🟢 Medium |
| 8 | 13-14 | Big Picture Mode | 8 | 🟡 High |
| 9 | 15-16 | Polish & Accessibility | 5 | 🟢 Medium |

---

## 2. Phase 1: Core Shell & Navigation (Weeks 1-2)

### 2.1 Week 1 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **1.1** | Create MainShell.axaml window structure | 8h | - |
| **1.2** | Implement TitleBarView with custom chrome | 4h | 1.1 |
| **1.3** | Create HeaderBarView with tab navigation | 8h | 1.1 |
| **1.4** | Implement NavigationService | 6h | 1.3 |
| **1.5** | Create StatusBarView with live indicators | 6h | 1.1 |
| **1.6** | Set up tab view routing | 4h | 1.4 |
| **1.7** | Create placeholder views for all 7 tabs | 4h | 1.6 |

**Week 1 Deliverable**: Working shell with tab navigation, placeholder content

### 2.2 Week 2 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **2.1** | Create OverlayContainer and overlay system | 8h | 1.1 |
| **2.2** | Implement CommandPalette with search | 10h | 2.1 |
| **2.3** | Create ShortcutService and global shortcuts | 6h | - |
| **2.4** | Implement NotificationService and toasts | 6h | 2.1 |
| **2.5** | Create theme switching foundation | 6h | 1.1 |
| **2.6** | Add responsive breakpoint handling | 4h | 1.1 |

**Week 2 Deliverable**: Command palette, notifications, keyboard shortcuts working

### 2.3 Phase 1 Files

```
Views/Shell/
├── MainShell.axaml
├── TitleBarView.axaml
├── HeaderBarView.axaml
├── StatusBarView.axaml
└── OverlayContainer.axaml

Views/Overlays/
├── CommandPalette.axaml
└── NotificationToast.axaml

ViewModels/Shell/
├── MainShellViewModel.cs
├── TitleBarViewModel.cs
├── HeaderBarViewModel.cs
└── StatusBarViewModel.cs

Services/
├── NavigationService.cs
├── ShortcutService.cs
├── OverlayService.cs
└── NotificationService.cs
```

---

## 3. Phase 2: Dashboard Hub (Weeks 3-4)

### 3.1 Week 3 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **3.1** | Create widget grid system with drag/drop | 12h | Phase 1 |
| **3.2** | Implement WidgetBase abstract class | 4h | 3.1 |
| **3.3** | Create LayoutService for widget persistence | 6h | 3.1 |
| **3.4** | Build QuickActionsWidget | 4h | 3.2 |
| **3.5** | Build TodayStatsWidget | 4h | 3.2 |
| **3.6** | Build ActivityFeedWidget | 6h | 3.2 |
| **3.7** | Build RecentlyAddedWidget | 4h | 3.2 |

**Week 3 Deliverable**: Working widget grid with 4 core widgets

### 3.2 Week 4 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **4.1** | Build GamingHeatmapWidget with visualization | 8h | 3.2 |
| **4.2** | Build GoalProgressWidget | 4h | 3.2 |
| **4.3** | Build remaining widgets (8) | 16h | 3.2 |
| **4.4** | Create widget customization drawer | 6h | 3.1 |
| **4.5** | Implement widget resize handles | 4h | 3.1 |
| **4.6** | Add widget settings per widget | 2h | 4.4 |

**Week 4 Deliverable**: Full dashboard with all 15 widgets, customization

### 3.3 Phase 2 Files

```
Views/Dashboard/
├── DashboardView.axaml
├── WidgetContainer.axaml
├── WidgetDrawer.axaml
└── Widgets/
    ├── QuickActionsWidget.axaml
    ├── TodayStatsWidget.axaml
    ├── ActivityFeedWidget.axaml
    ├── RecentlyAddedWidget.axaml
    ├── GoalProgressWidget.axaml
    ├── GamingHeatmapWidget.axaml
    ├── FriendsPlayingWidget.axaml
    ├── SaleAlertsWidget.axaml
    ├── AiRecommendationsWidget.axaml
    ├── PerformanceMiniWidget.axaml
    ├── UpcomingGamesWidget.axaml
    ├── MugenQuickWidget.axaml
    ├── VoiceStatusWidget.axaml
    ├── CloudSyncWidget.axaml
    └── YearReviewWidget.axaml

ViewModels/Dashboard/
└── (Corresponding ViewModels)

Services/
├── WidgetService.cs
└── LayoutService.cs
```

---

## 4. Phase 3: Library Enhancement (Weeks 5-6)

### 4.1 Week 5 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **5.1** | Create LibraryView shell with sidebar | 8h | Phase 1 |
| **5.2** | Implement LibrarySidebar with filters | 6h | 5.1 |
| **5.3** | Create LibraryToolbar with view modes | 6h | 5.1 |
| **5.4** | Build GameGridView | 8h | 5.3 |
| **5.5** | Build GameListView | 6h | 5.3 |
| **5.6** | Create enhanced GameCard component | 4h | 5.4 |
| **5.7** | Implement multi-select with BulkActionsBar | 6h | 5.4 |

**Week 5 Deliverable**: Enhanced library with grid/list views, multi-select

### 4.2 Week 6 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **6.1** | Create GameDetailView container | 6h | 5.1 |
| **6.2** | Build OverviewTab with HLTB, prices | 8h | 6.1 |
| **6.3** | Build SaveStatesTab with branch tree | 8h | 6.1 |
| **6.4** | Build AchievementsTab | 4h | 6.1 |
| **6.5** | Build SessionsTab | 4h | 6.1 |
| **6.6** | Build NotesTab | 4h | 6.1 |
| **6.7** | Create AddGameWizard modal | 6h | 5.1 |

**Week 6 Deliverable**: Complete game detail view with all tabs, add wizard

### 4.3 Phase 3 Files

```
Views/Library/
├── LibraryView.axaml
├── LibrarySidebar.axaml
├── LibraryToolbar.axaml
├── GameGridView.axaml
├── GameListView.axaml
├── GameCard.axaml
├── BulkActionsBar.axaml
├── AddGameWizard.axaml
└── GameDetail/
    ├── GameDetailView.axaml
    ├── OverviewTab.axaml
    ├── SaveStatesTab.axaml
    ├── AchievementsTab.axaml
    ├── SessionsTab.axaml
    ├── NotesTab.axaml
    ├── ModsTab.axaml
    └── MediaTab.axaml
```

---

## 5. Phase 4: Analytics & Social (Weeks 7-8)

### 5.1 Week 7 Tasks (Analytics)

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **7.1** | Create AnalyticsView shell | 4h | Phase 1 |
| **7.2** | Build OverviewSection with charts | 10h | 7.1 |
| **7.3** | Build PlaytimeSection with trends | 6h | 7.1 |
| **7.4** | Build GoalsSection with CRUD | 6h | 7.1 |
| **7.5** | Create HeatmapChart component | 8h | 7.2 |
| **7.6** | Build YearInReviewSection | 6h | 7.1 |

**Week 7 Deliverable**: Analytics tab with charts, heatmap, year in review

### 5.2 Week 8 Tasks (Social)

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **8.1** | Create SocialView shell | 4h | Phase 1 |
| **8.2** | Build FriendsSection | 6h | 8.1 |
| **8.3** | Build ActivitySection feed | 8h | 8.1 |
| **8.4** | Build ReviewsSection | 6h | 8.1 |
| **8.5** | Build CollectionsSection | 6h | 8.1 |
| **8.6** | Build DiscordSection | 4h | 8.1 |
| **8.7** | Create AI Assistant Panel | 6h | Phase 1 |

**Week 8 Deliverable**: Social tab complete, AI assistant panel

---

## 6. Phase 5: Tools & Utilities (Weeks 9-10)

### 6.1 Week 9 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **9.1** | Create ToolsView shell with sidebar | 6h | Phase 1 |
| **9.2** | Build Performance tools (4 views) | 12h | 9.1 |
| **9.3** | Build Voice tools (3 views) | 10h | 9.1 |
| **9.4** | Build Automation tools (3 views) | 12h | 9.1 |

**Week 9 Deliverable**: Performance, Voice, Automation tools

### 6.2 Week 10 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **10.1** | Build Cloud tools (3 views) | 8h | 9.1 |
| **10.2** | Build Plugin marketplace | 10h | 9.1 |
| **10.3** | Build Theme editor | 8h | 9.1 |
| **10.4** | Build Import/Export tools | 6h | 9.1 |
| **10.5** | Build Diagnostics tools | 8h | 9.1 |

**Week 10 Deliverable**: All tools complete

---

## 7. Phase 6: Terminal & CLI (Week 11)

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **11.1** | Create TerminalView with output | 8h | Phase 1 |
| **11.2** | Implement command input with autocomplete | 8h | 11.1 |
| **11.3** | Create CommandExecutor service | 8h | 11.2 |
| **11.4** | Build Script Editor view | 6h | 11.1 |
| **11.5** | Add syntax highlighting | 4h | 11.2 |
| **11.6** | Implement command history | 4h | 11.2 |

**Week 11 Deliverable**: Full CLI integration in UI

---

## 8. Phase 7: MUGEN Enhancement (Week 12)

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **12.1** | Refactor MugenView to MugenShell | 6h | Phase 1 |
| **12.2** | Build Training section | 8h | 12.1 |
| **12.3** | Build Replay section | 6h | 12.1 |
| **12.4** | Build Tournament bracket view | 10h | 12.1 |
| **12.5** | Build Stats section | 4h | 12.1 |
| **12.6** | Build Coach section | 6h | 12.1 |

**Week 12 Deliverable**: Enhanced MUGEN with training, tournaments

---

## 9. Phase 8: Big Picture Mode (Weeks 13-14)

### 9.1 Week 13 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **13.1** | Create BigPictureShell | 8h | Phase 1 |
| **13.2** | Implement ControllerInputService | 10h | 13.1 |
| **13.3** | Build BP Home/Dashboard | 8h | 13.1 |
| **13.4** | Build BP Library grid | 8h | 13.1 |
| **13.5** | Create OnScreenKeyboard | 6h | 13.1 |

**Week 13 Deliverable**: Big Picture shell, controller input, basic navigation

### 9.2 Week 14 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **14.1** | Build BP Game Detail | 6h | 13.4 |
| **14.2** | Build BP Settings Overlay | 4h | 13.1 |
| **14.3** | Adapt all views for controller focus | 16h | 13.2 |
| **14.4** | Add focus navigation indicators | 8h | 14.3 |
| **14.5** | Test Steam Deck compatibility | 6h | 14.3 |

**Week 14 Deliverable**: Complete Big Picture Mode

---

## 10. Phase 9: Polish & Accessibility (Weeks 15-16)

### 10.1 Week 15 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **15.1** | Implement high contrast themes | 8h | All |
| **15.2** | Add screen reader support | 10h | All |
| **15.3** | Implement reduced motion mode | 6h | All |
| **15.4** | Add font scaling | 6h | All |
| **15.5** | Create color blind modes | 6h | All |
| **15.6** | Add focus indicators | 4h | All |

**Week 15 Deliverable**: WCAG 2.1 AA compliance

### 10.2 Week 16 Tasks

| Task | Description | Est. Hours | Dependencies |
|------|-------------|------------|--------------|
| **16.1** | Multi-monitor pop-out support | 8h | All |
| **16.2** | Performance optimization pass | 10h | All |
| **16.3** | Animation polish | 6h | All |
| **16.4** | Error handling improvements | 6h | All |
| **16.5** | Final testing and bug fixes | 10h | All |

**Week 16 Deliverable**: Production-ready UI

---

## 11. New Services to Implement

| Service | Phase | Priority | Est. Hours |
|---------|-------|----------|------------|
| `IGameNotesService` | 3 | High | 8h |
| `IDealTrackingService` | 2 | High | 12h |
| `IHowLongToBeatService` | 3 | High | 8h |
| `IYearInReviewService` | 4 | Medium | 10h |
| `IReportGeneratorService` | 4 | Medium | 8h |
| `IModManagerService` | 3 | Medium | 16h |
| `IGameRandomizerService` | 2 | Medium | 4h |
| `IDuplicateDetectionService` | 5 | Low | 8h |
| `IAttractModeService` | 9 | Low | 6h |
| `IPhysicalCollectionService` | 5 | Low | 8h |
| `IInternalAchievementService` | 9 | Low | 12h |
| `IGameStreamingService` | 9 | Low | 20h |
| `IMugenStageManager` | 7 | Medium | 8h |
| `IMugenCharacterEditor` | 7 | Low | 16h |
| `ICommandExecutor` | 6 | High | 8h |

**Total New Service Hours**: ~142h

---

## 12. Success Metrics

### 12.1 Phase Gates

| Phase | Gate Criteria |
|-------|---------------|
| 1 | Shell navigates between 7 tabs, command palette works |
| 2 | Dashboard shows 15 widgets, customization works |
| 3 | Library shows games, detail view with 7 tabs works |
| 4 | Analytics shows charts, social shows activity feed |
| 5 | All tools accessible and functional |
| 6 | Terminal executes all CLI commands |
| 7 | MUGEN training and tournament work |
| 8 | Big Picture navigable with controller |
| 9 | Accessibility audit passes, performance <100ms |

### 12.2 Final Acceptance Criteria

| Metric | Target |
|--------|--------|
| Feature Coverage | 100% of services exposed |
| CLI Parity | All 14 command groups in UI |
| Big Picture Coverage | 100% controller-navigable |
| Accessibility | WCAG 2.1 AA compliant |
| View Transitions | <100ms |
| Memory Usage | <500MB baseline |
| Test Coverage | >80% for new ViewModels |

---

## 13. Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Widget drag/drop complexity | Use existing DragDrop library (Avalonia.Xaml.Behaviors) |
| Chart rendering performance | Use SkiaSharp for hardware-accelerated rendering |
| Controller input handling | Leverage Avalonia's built-in gamepad support |
| Theme switching complexity | CSS-variable-like approach with dynamic resources |
| CLI integration depth | Reuse existing MediatR command handlers |

---

*End of Feature Surfacing Plan Documentation*
