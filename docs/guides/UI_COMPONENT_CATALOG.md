# UI Component Catalog

**Project:** SaveState Reborn  
**Last Updated:** February 21, 2026

Complete reference of all UI components in the Presentation layer.

---

## Table of Contents

1. [Main Views](#main-views)
2. [Dialog Views](#dialog-views)
3. [Overlay Views](#overlay-views)
4. [Custom Controls](#custom-controls)
5. [Value Converters](#value-converters)
6. [Styles and Themes](#styles-and-themes)

---

## Main Views

### Application Shell

| View | ViewModel | Purpose |
|------|-----------|---------|
| `MainWindow.axaml` | `MainViewModel` | Root window container, handles onboarding vs main app |
| `MainShell.axaml` | `MainShellViewModel` | Main application shell with title bar, header, content, status bar |
| `TitleBarView.axaml` | `TitleBarViewModel` | Custom window chrome (minimize, maximize, close) |
| `HeaderBarView.axaml` | `HeaderBarViewModel` | Navigation tabs and global search |
| `StatusBarView.axaml` | `StatusBarViewModel` | Bottom status bar with sync status, game count |

### Library Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `LibraryView.axaml` | `LibraryViewModel` | Main library container with sidebar, toolbar, content |
| `LibrarySidebar.axaml` | `LibrarySidebarViewModel` | Left sidebar with collections, platforms, filters |
| `LibraryToolbar.axaml` | `LibraryToolbarViewModel` | Search, view mode toggle, sort, bulk actions |
| `GameGridView.axaml` | `GameGridViewModel` | Grid layout for game cards |
| `GameListView.axaml` | `GameListViewModel` | List layout for games |
| `GameCard.axaml` | `GameCardViewModel` | Individual game card component |

### Game Detail Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `GameDetailView.axaml` | `GameDetailViewModel` | Game detail container with tabs |
| `GameOverviewTabView.axaml` | `GameOverviewTabViewModel` | Cover art, description, quick actions |
| `GameSaveStatesTabView.axaml` | `GameSaveStatesTabViewModel` | Save state tree, branch management |
| `GameAchievementsTabView.axaml` | `GameAchievementsTabViewModel` | Achievement list and progress |
| `GameSessionsTabView.axaml` | `GameSessionsTabViewModel` | Play session history |
| `GameNotesTabView.axaml` | `GameNotesTabViewModel` | User notes for game |
| `GameModsTabView.axaml` | `GameModsTabViewModel` | Mod management |
| `GameMediaTabView.axaml` | `GameMediaTabViewModel` | Screenshots and videos |
| `GamePerformanceTabView.axaml` | `GamePerformanceTabViewModel` | Performance metrics |

### Dashboard Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `DashboardView.axaml` | `DashboardViewModel` | Main dashboard container |
| `ActivityFeedWidgetView.axaml` | `ActivityFeedWidget` | Recent activity feed |
| `EmulatorStatusWidgetView.axaml` | `EmulatorStatusWidget` | Emulator status indicators |
| `GoalsProgressWidgetView.axaml` | `GoalsProgressWidget` | Goal completion progress |
| `QuickActionsWidgetView.axaml` | `QuickActionsWidget` | Quick action buttons |
| `RecentlyAddedWidgetView.axaml` | `RecentlyAddedWidget` | Recently added games |
| `TodaysStatsWidgetView.axaml` | `TodaysStatsWidget` | Today's gaming statistics |

### Cloud Sync Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `CloudSyncView.axaml` | `CloudSyncViewModel` | Cloud sync management dashboard |

### MUGEN Hub Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `MugenHubView.axaml` | `MugenHubViewModel` | MUGEN hub container |
| `MugenView.axaml` | `MugenViewModel` | Legacy MUGEN view |
| `RosterSection.axaml` | `MugenRosterViewModel` | Character roster management |
| `TournamentSection.axaml` | `MugenTournamentViewModel` | Tournament setup |
| `TournamentBracketView.axaml` | `TournamentBracketViewModel` | Bracket visualization |
| `DownloadsSection.axaml` | `MugenDownloadsViewModel` | Character downloads |
| `TrainingSection.axaml` | `MugenTrainingViewModel` | Training mode |
| `DeathBattleSection.axaml` | `MugenDeathBattleViewModel` | Death battle simulations |
| `FusionSection.axaml` | - | Character fusion (DBZ style) |
| `GraphicsSection.axaml` | `MugenGraphicsViewModel` | Graphics settings |
| `AudioSection.axaml` | `MugenAudioViewModel` | Audio settings |
| `StatsSection.axaml` | - | Statistics and analytics |
| `EngineModsSection.axaml` | - | Engine mod management |
| `ReplaySection.axaml` | - | Replay viewer |
| `CoachSection.axaml` | - | AI coaching |
| `MachineLearningView.axaml` | `MachineLearningViewModel` | ML training |
| `MoveCreationView.axaml` | `MoveCreationViewModel` | Move editor |
| `MoveTemplateBrowserView.axaml` | - | Move template library |

### Big Picture Mode Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `BigPictureShell.axaml` | `BigPictureShellViewModel` | 10-foot UI container |
| `GameGridView.axaml` | `GameGridViewModel` | Large grid for TV display |
| `GameDetailPanel.axaml` | `GameDetailViewModel` | Full-screen game details |
| `LaunchExperienceView.axaml` | `LaunchExperienceViewModel` | Game launch animation |
| `OnScreenKeyboard.axaml` | `OnScreenKeyboardViewModel` | Controller-friendly keyboard |
| `SettingsOverlay.axaml` | - | Settings in big picture mode |

### Automation Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `AutomationDashboardView.axaml` | `AutomationDashboardViewModel` | Automation overview |
| `MacroMarketplaceView.axaml` | `MacroMarketplaceViewModel` | Macro sharing and downloads |
| `MacroRecorderView.axaml` | `MacroRecorderViewModel` | Macro recording interface |

### ROM Management Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `RomManagementView.axaml` | `RomManagementViewModel` | ROM library management |
| `RetroArchView.axaml` | `RetroArchViewModel` | RetroArch integration |
| `EmulatorSetupWizard.axaml` | `EmulatorSetupWizardViewModel` | Emulator configuration wizard |

### Settings Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `SettingsView.axaml` | `SettingsViewModel` | Settings container |
| `AudioOptimizationView.axaml` | `AudioOptimizationViewModel` | Audio settings |
| `ControllerProfilesView.axaml` | `ControllerProfilesViewModel` | Controller configuration |
| `SmartLauncherSettingsView.axaml` | `SmartLauncherViewModel` | Smart launcher settings |

### Analytics Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `AnalyticsDashboardView.axaml` | `AnalyticsDashboardViewModel` | Gaming analytics |
| `AnalyticsView.axaml` | `AnalyticsViewModel` | Shell analytics container |

### Tools Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `ToolsView.axaml` | `ToolsViewModel` | Tools hub |
| `GameMemoryView.axaml` | `GameMemoryViewModel` | Memory editing interface |
| `SignatureTesterView.axaml` | `SignatureTesterViewModel` | Memory signature testing |
| `MemoryScannerView.axaml` | `MemoryScannerViewModel` | Memory scanning tool |
| `TerminalView.axaml` | `TerminalViewModel` | Built-in terminal |
| `TaskSchedulerView.axaml` | `TaskSchedulerViewModel` | Scheduled tasks |

### Social Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `SocialView.axaml` | `SocialViewModel` | Social features hub |

### Game Deals Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `GameDealsView.axaml` | `GameDealsViewModel` | Game deals aggregator |

### Subscription Management Views

| View | ViewModel | Purpose |
|------|-----------|---------|
| `SubscriptionManagerView.axaml` | `SubscriptionManagerViewModel` | Subscription tracking |
| `GameDetailsDialog.axaml` | - | Game details in subscription context |

---

## Dialog Views

### Message Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `MessageDialog.axaml` | `MessageDialogViewModel` | Generic message dialog (info/warning/error) |
| `ConfirmationDialog.axaml` | `ConfirmationDialogViewModel` | Yes/no confirmation |
| `TextInputDialog.axaml` | `TextInputDialogViewModel` | Single text input |

### Game Library Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `AddGameDialog.axaml` | `AddGameDialogViewModel` | Add game manually |
| `AddGameWizard.axaml` | `AddGameWizardViewModel` | Multi-step game addition wizard |
| `LaunchConfigDialog.axaml` | `LaunchConfigDialogViewModel` | Launch parameters configuration |
| `GameExecutableConfigDialog.axaml` | - | Executable path configuration |
| `LaunchProfileEditorDialog.axaml` | - | Launch profile editor |
| `TagEditorDialog.axaml` | `TagEditorDialogViewModel` | Tag management |
| `NoteEditorDialog.axaml` | `NoteEditorDialogViewModel` | Note editor |
| `ReviewEditorDialog.axaml` | `ReviewEditorDialogViewModel` | Game review editor |
| `GameRatingDialog.axaml` | `GameRatingDialogViewModel` | Rating dialog |
| `PriceAlertDialog.axaml` | `PriceAlertDialogViewModel` | Price drop alerts |
| `CollectionSelectionDialog.axaml` | `CollectionSelectionDialogViewModel` | Select collection for game |
| `CreateCollectionDialog.axaml` | `CreateCollectionDialogViewModel` | Create new collection |
| `LibraryImportDialog.axaml` | `LibraryImportDialogViewModel` | Import games from platforms |

### Save State Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `BranchCreationDialog.axaml` | `BranchCreationDialogViewModel` | Create new branch |
| `BranchSelectionDialog.axaml` | `BranchSelectionDialogViewModel` | Switch between branches |
| `BranchComparisonDialog.axaml` | `BranchComparisonDialogViewModel` | Compare branches |
| `BranchMergeDialog.axaml` | `BranchMergeDialogViewModel` | Merge branches |
| `SaveStateSettingsDialog.axaml` | `SaveStateSettingsDialogViewModel` | Save state configuration |
| `AutoSaveConfigurationDialog.axaml` | `AutoSaveConfigurationDialogViewModel` | Auto-save settings |

### Cloud Sync Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `CloudProviderConfigDialog.axaml` | `CloudProviderConfigDialogViewModel` | Configure cloud provider |
| `ConflictResolutionDialog.axaml` | `ConflictResolutionDialogViewModel` | Resolve sync conflicts |
| `ProviderConfigurationDialog.axaml` | `ProviderConfigurationDialogViewModel` | Provider settings |

### ROM/Emulator Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `EmulatorConfigDialog.axaml` | `EmulatorConfigDialogViewModel` | Emulator configuration |
| `EmulatorEditorDialog.axaml` | `EmulatorEditorDialogViewModel` | Edit emulator settings |
| `RomDetailsDialog.axaml` | `RomDetailsDialogViewModel` | ROM file details |
| `RomScanProgressDialog.axaml` | `RomScanProgressDialogViewModel` | ROM scanning progress |

### Automation Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `MacroRecorderDialog.axaml` | `MacroRecorderDialogViewModel` | Record macros |
| `AutomationSettingsDialog.axaml` | `AutomationSettingsDialogViewModel` | Automation configuration |
| `WorkflowCreationDialog.axaml` | `WorkflowCreationDialogViewModel` | Create workflow |
| `WorkflowEditorDialog.axaml` | `WorkflowEditorDialogViewModel` | Visual workflow editor |
| `TaskCreationDialog.axaml` | `TaskCreationDialogViewModel` | Create scheduled task |

### Goal/Task Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `GoalCreationDialog.axaml` | `GoalCreationDialogViewModel` | Create gaming goal |

### Memory Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `ImportCheatTableDialog.axaml` | `ImportCheatTableViewModel` | Import Cheat Engine tables |
| `ProcessSelectorDialog.axaml` | `ProcessSelectorDialogViewModel` | Select running process |

### Dashboard Dialogs

| View | ViewModel | Purpose |
|------|-----------|---------|
| `DashboardCustomizationDialog.axaml` | `DashboardCustomizationDialogViewModel` | Customize dashboard widgets |

---

## Overlay Views

Overlays are floating UI elements that appear above the main content.

| View | ViewModel | Purpose |
|------|-----------|---------|
| `OverlayContainer.axaml` | `OverlayContainerViewModel` | Container for all overlays |
| `AiAssistantPanel.axaml` | `AiAssistantViewModel` | AI assistant sidebar |
| `CommandPaletteView.axaml` | `CommandPaletteViewModel` | Quick command palette (Ctrl+Shift+P) |
| `QuickSearchView.axaml` | `QuickSearchViewModel` | Global search overlay |
| `NotificationContainerView.axaml` | `INotificationService` | Toast notification container |
| `NotificationToastView.axaml` | - | Individual toast notification |
| `PerformanceHud.axaml` | `PerformanceHudViewModel` | In-game performance overlay |
| `VoiceIndicator.axaml` | `VoiceIndicatorViewModel` | Voice command visual feedback |
| `AchievementDetailsOverlay.axaml` | `AchievementDetailsOverlayViewModel` | Achievement details popup |
| `AutoDiscoveryOverlay.axaml` | `AutoDiscoveryOverlayViewModel` | Memory auto-discovery guide |
| `MemoryMonitorOverlay.axaml` | `MemoryMonitorOverlayViewModel` | Live memory value display |
| `ModDetailsOverlay.axaml` | `ModDetailsOverlayViewModel` | Mod details popup |
| `SessionDetailsOverlay.axaml` | `SessionDetailsOverlayViewModel` | Session details popup |
| `UserProfileOverlay.axaml` | `UserProfileOverlayViewModel` | User profile dropdown |
| `SyncStatusOverlay.axaml` | `SyncStatusOverlayViewModel` | Sync status details |
| `NetworkDiagnosticsOverlay.axaml` | `NetworkDiagnosticsOverlayViewModel` | Network diagnostics |
| `ConflictsResolutionOverlay.axaml` | `ConflictsResolutionOverlayViewModel` | Conflict resolution panel |

---

## Custom Controls

### Converters

Located in `Converters/` folder. Value converters transform data for binding.

| Converter | Purpose |
|-----------|---------|
| `ViewModeToClassConverter` | Converts ViewMode to CSS class name |
| `BoolToClassConverter` | Converts bool to CSS class with parameter |
| `BoolToBrushConverter` | Converts bool to SolidColorBrush |
| `BoolToStatusBrushConverter` | Converts bool to success/error brush |
| `BoolToColorConverter` | Converts bool to Color with true/false colors |
| `BoolToFlowDirectionConverter` | Converts bool to FlowDirection |
| `TabContentConverter` | Multi-value converter for tab selection |
| `GreaterThanConverter` | Checks if value > threshold |
| `EqualToConverter` | Checks if value equals parameter |
| `EqualityConverter` | Checks equality for styling |
| `InequalityConverter` | Checks inequality |
| `EqualityToFontWeightConverter` | Converts equality to FontWeight |
| `PercentageConverter` | Converts value to percentage width |
| `StringNotEmptyConverter` | Checks if string is not empty |
| `StringEqualsConverter` | String equality check |
| `StringContainsConverter` | String contains check |
| `ReferenceEqualsConverter` | Reference equality check |
| `NullOrEmptyToBoolConverter` | Null/empty string to bool |
| `IntToBoolConverter` | Non-zero int to bool |
| `FilePathToBitmapConverter` | File path to Bitmap image |
| `NetworkStatusConverter` | Network status to display text |

### Usage Example

```xml
<!-- In App.axaml - Register converters -->
<converters:BoolToBrushConverter x:Key="BoolToBrushConverter" />

<!-- In View - Use converter -->
<Border Background="{Binding IsActive, 
                 Converter={StaticResource BoolToBrushConverter}}" />
```

---

## Styles and Themes

### Brush Resources

Located in `Styles/Brushes.axaml`.

#### Base Colors

| Key | Color | Usage |
|-----|-------|-------|
| `BackgroundBrush` | #121212 | Main window background |
| `SurfaceBrush` | #1A1B1E | Sidebar, header surfaces |
| `CardBackgroundBrush` | #25262B | Card backgrounds |
| `CardHoverBrush` | #2C2E33 | Card hover state |

#### Text Colors

| Key | Color | Usage |
|-----|-------|-------|
| `TextPrimaryBrush` | #FFFFFF | Primary text |
| `TextSecondaryBrush` | #A1A1AA | Secondary text |
| `TextTertiaryBrush` | #71717A | Tertiary/muted text |
| `TextMutedBrush` | #52525B | Disabled text |

#### Accent Colors

| Key | Color | Usage |
|-----|-------|-------|
| `AccentBrush` | #10B981 | Primary accent (green) |
| `AccentHoverBrush` | #059669 | Accent hover state |
| `SecondaryAccentBrush` | #3B82F6 | Secondary accent (blue) |

#### Status Colors

| Key | Color | Usage |
|-----|-------|-------|
| `SuccessBrush` | #10B981 | Success states |
| `WarningBrush` | #F59E0B | Warning states |
| `ErrorBrush` | #EF4444 | Error states |
| `InfoBrush` | #3B82F6 | Info states |

### Control Styles

Located in `Styles/Controls.axaml`.

#### Button Styles

| Style Class | Description |
|-------------|-------------|
| `Primary` | Large pill-shaped, gradient background |
| `Secondary` | Rounded rectangle, card background |
| `Nav` | Navigation button, transparent with hover |
| `Outline` | Transparent with border |

#### Container Styles

| Style Class | Description |
|-------------|-------------|
| `Card` | Standard card with shadow |
| `GlassContainer` | Semi-transparent with border |
| `GameCard` | Hover-lifting game card |

#### Text Styles

| Style Class | Font Size | Weight |
|-------------|-----------|--------|
| `H1` | 28px | Bold |
| `H2` | 20px | SemiBold |
| `Header` | 24px | Bold |
| `Subtitle` | 18px | SemiBold |
| `Body` | 14px | Regular |
| `Caption` | 12px | Regular |

### Animation Resources

Located in `Styles/Animations.axaml`.

Common transitions:

```xml
<!-- Standard transition -->
<Setter Property="Transitions">
    <Transitions>
        <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.1"/>
        <DoubleTransition Property="Opacity" Duration="0:0:0.1"/>
    </Transitions>
</Setter>
```

---

## Component Usage Examples

### Creating a New View

```xml
<!-- MyFeatureView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:SaveState.Presentation.ViewModels.MyFeature"
             x:Class="SaveState.Presentation.Views.MyFeature.MyFeatureView"
             x:DataType="vm:MyFeatureViewModel">
    
    <Grid>
        <Border Classes="Card">
            <TextBlock Text="{Binding Title}" Classes="Header" />
        </Border>
    </Grid>
</UserControl>
```

### Creating a New ViewModel

```csharp
// MyFeatureViewModel.cs
namespace SaveState.Presentation.ViewModels.MyFeature;

public partial class MyFeatureViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "My Feature";
    
    [RelayCommand]
    private async Task DoSomethingAsync()
    {
        // Implementation
    }
}
```

### Registering in DI

```csharp
// In ServiceRegistration or App.axaml.cs
services.AddTransient<MyFeatureViewModel>();
```

---

## Statistics

| Category | Count |
|----------|-------|
| Total Views | 255 |
| Total ViewModels | 149 |
| Dialog Views | 35 |
| Overlay Views | 17 |
| Custom Converters | 20+ |
| Widgets | 7 |
