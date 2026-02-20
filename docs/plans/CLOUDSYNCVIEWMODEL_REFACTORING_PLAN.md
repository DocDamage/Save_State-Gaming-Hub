# CloudSyncViewModel Refactoring Plan

## Overview

**Target:** `CloudSyncViewModel.cs` (1,297 lines)  
**Location:** `src/SaveState.Presentation/ViewModels/Shell/CloudSyncViewModel.cs`  
**Challenge:** Heavy use of CommunityToolkit.Mvvm source generators (MVVM Toolkit)  
**Goal:** Split into focused partial classes while preserving source generator functionality

---

## Current State Analysis

### Structure
```
CloudSyncViewModel (1,297 lines)
├── Constants (4 lines)
├── Dependencies/Fields (26 lines)
├── [ObservableProperty] declarations (24 properties)
├── Constructor with DI (42 lines)
├── Collections (3 properties)
├── InitializeAsync() (54 lines)
├── Sync/Push/Pull Commands (100 lines)
├── Conflict Resolution (200 lines)
├── Cloud Catalog/Gaming (150 lines)
├── Network Monitoring (100 lines)
├── Daemon Status/Health (300 lines)
├── Event Handlers (100 lines)
└── Private Helper Classes (3 records at bottom)
```

### Key Characteristics
1. **Source Generator Dependencies:**
   - `[ObservableProperty]` generates 24 properties + change notifications
   - `[RelayCommand]` generates 8+ commands with type-safe parameters
   - Generated code references `NetworkQualityInfo`, `CloudCatalogEntry`, etc.

2. **Mixed Responsibilities:**
   - Cloud sync operations (Sync/Push/Pull)
   - Cloud gaming (Launch games, sessions)
   - Network monitoring
   - Save-state daemon management
   - Backup history
   - Conflict resolution

3. **Event Handler Subscriptions:**
   ```csharp
   _syncService.ProgressChanged += OnSyncProgressChanged;
   _syncService.ConflictDetected += OnSyncConflictDetected;
   _networkMonitor.NetworkQualityChanged += OnNetworkQualityChanged;
   _saveStateCloudSyncMonitor.StatusChanged += OnSaveStateCloudDaemonStatusChanged;
   ```

---

## Refactoring Strategy

### Approach: Partial Classes by Responsibility

Split into 5 partial class files while keeping `[ObservableProperty]` and `[RelayCommand]` attributes in the main file to work with source generators.

### Proposed Structure

```
CloudSyncViewModel/
├── CloudSyncViewModel.cs           (Main - 300 lines)
│   ├── Constructor & DI
│   ├── [ObservableProperty] fields
│   ├── [RelayCommand] methods
│   └── Event handler stubs
├── CloudSyncViewModel.Sync.cs      (Sync operations - 250 lines)
├── CloudSyncViewModel.Gaming.cs    (Cloud gaming - 200 lines)
├── CloudSyncViewModel.Daemon.cs    (Daemon management - 300 lines)
├── CloudSyncViewModel.Network.cs   (Network monitoring - 150 lines)
└── CloudSyncViewModel.Conflicts.cs (Conflict resolution - 200 lines)
```

---

## Implementation Steps

### Phase 1: Preparation (30 min)

1. **Backup & Verify**
   ```bash
   git checkout -b refactor/cloudsync-viewmodel
   dotnet build
   dotnet test --filter "FullyQualifiedName~CloudSync"
   ```

2. **Document Current Behavior**
   - Note all `[ObservableProperty]` fields and their types
   - List all `[RelayCommand]` methods
   - Identify event handler dependencies

### Phase 2: Extract Private Helper Classes (15 min)

Move helper records to separate file first (low risk):

**File:** `CloudSyncViewModel.Models.cs`
```csharp
namespace SaveState.Presentation.ViewModels.Shell;

public class BackupHistoryItem { ... }

file sealed record SaveStateConflictEntry(Guid GameId, SaveStateConflictResolution Conflict);

file sealed record ConflictApplyResult(bool Success, string? Error)
{
    public static ConflictApplyResult Successful() => new(true, null);
    public static ConflictApplyResult Failed(string? error = null) => new(false, error);
}

file sealed record DaemonHealthSnapshot(...);
```

### Phase 3: Extract Sync Operations (45 min)

**File:** `CloudSyncViewModel.Sync.cs`
```csharp
using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CloudSyncViewModel
{
    [RelayCommand]
    private async Task SyncAsync() { ... }

    [RelayCommand]
    private async Task PushAsync() { ... }

    [RelayCommand]
    private async Task PullAsync() { ... }

    [RelayCommand]
    private async Task ResolveConflictsAsync() { ... }

    [RelayCommand]
    private async Task ViewBackupHistoryAsync() { ... }

    private async Task RefreshBackupHistoryAsync() { ... }
}
```

### Phase 4: Extract Cloud Gaming (30 min)

**File:** `CloudSyncViewModel.Gaming.cs`
```csharp
using CommunityToolkit.Mvvm.Input;
using SaveState.Application.CloudServices.Queries;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CloudSyncViewModel
{
    [RelayCommand]
    private async Task LaunchCloudGameAsync(CloudCatalogEntry game) { ... }

    [RelayCommand]
    private async Task RefreshCloudCatalogAsync() { ... }

    private async Task LoadCloudCatalogAsync() { ... }
}
```

### Phase 5: Extract Daemon Management (60 min)

**File:** `CloudSyncViewModel.Daemon.cs`
```csharp
using SaveState.Core.Common.Services;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CloudSyncViewModel
{
    private void ApplyDaemonStatus(SaveStateCloudDaemonStatus status) { ... }
    
    private static DaemonHealthSnapshot EvaluateDaemonHealth(...) { ... }
    
    private void ProcessDaemonAlertNotifications(...) { ... }
    
    private static int ClampDaemonAlertCooldownSeconds(int value) { ... }
    
    private static string BuildBackgroundSyncDetails(...) { ... }
    
    private async Task<Dictionary<string, SaveStateConflictEntry>> AppendSaveStateConflictsAsync(...) { ... }
    
    private async Task<ConflictApplyResult> ResolveSaveStateConflictAsync(...) { ... }
    
    private static string BuildEncryptionCacheKey(...) { ... }
    
    private static string BuildFailureSummary(...) { ... }
}
```

### Phase 6: Extract Network Monitoring (30 min)

**File:** `CloudSyncViewModel.Network.cs`
```csharp
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.Sync;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CloudSyncViewModel
{
    [RelayCommand]
    private async Task ToggleNetworkMonitoringAsync() { ... }

    private void OnNetworkQualityChanged(object? sender, NetworkQualityChangedEventArgs e) { ... }
    
    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e) { ... }
    
    private void OnSyncConflictDetected(object? sender, ConflictDetectedEventArgs e) { ... }
    
    private void OnSaveStateCloudDaemonStatusChanged(object? sender, SaveStateCloudDaemonStatus status) { ... }
}
```

### Phase 7: Extract Configuration (30 min)

**File:** `CloudSyncViewModel.Config.cs`
```csharp
using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CloudSyncViewModel
{
    [RelayCommand]
    private async Task ConfigureProviderAsync() { ... }

    private async Task LoadCloudSyncSettingsAsync() { ... }
}
```

### Phase 8: Main File Cleanup (30 min)

**File:** `CloudSyncViewModel.cs` (reduced to ~300 lines)
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
// ... other usings

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CloudSyncViewModel : ObservableObject
{
    // Constants only
    private const int MinDaemonAlertCooldownSeconds = 15;
    private const int MaxDaemonAlertCooldownSeconds = 600;
    private const int DefaultDaemonAlertCooldownSeconds = 60;
    private static readonly TimeSpan ManualConflictAlertCooldown = TimeSpan.FromSeconds(15);

    // Dependencies only
    private readonly IMediator _mediator;
    private readonly ISyncService _syncService;
    // ... etc

    // Observable properties (source generated)
    [ObservableProperty] private SyncStatus _currentSyncStatus;
    [ObservableProperty] private string _lastSyncTime = "Never";
    // ... etc (all 24 properties)

    // Public collections
    public ObservableCollection<CloudGamingProvider> CloudProviders { get; }
    public ObservableCollection<CloudSession> ActiveSessions { get; }
    public ObservableCollection<BackupHistoryItem> BackupHistory { get; }

    // Constructor
    public CloudSyncViewModel(...) { ... }

    // Initialize method
    private async Task InitializeAsync() { ... }
}
```

---

## Testing Strategy

### Build Verification
```bash
dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj
```

### Functional Tests
```bash
dotnet test tests/SaveState.Presentation.Tests --filter "FullyQualifiedName~CloudSync"
```

### Manual Testing Checklist
- [ ] Cloud sync operations work (Sync/Push/Pull)
- [ ] Network monitoring toggle works
- [ ] Daemon status updates display correctly
- [ ] Conflict resolution dialog opens
- [ ] Cloud catalog loads and displays
- [ ] Backup history view opens
- [ ] Provider configuration dialog opens

---

## Risk Mitigation

### Risk 1: Source Generator Breakage
**Mitigation:** Keep all `[ObservableProperty]` and `[RelayCommand]` attributes in the main partial class. Only move method bodies to other files.

### Risk 2: Event Handler Unsubscription
**Mitigation:** Verify event handler assignments stay in main constructor. The handler methods can be in other partial files.

### Risk 3: Circular Dependencies
**Mitigation:** All partial files share the same type, so no circular dependency issues. Private fields accessible everywhere.

### Risk 4: DI Container Issues
**Mitigation:** Constructor stays in main file, DI registration unchanged.

---

## Expected Results

| File | Before | After |
|------|--------|-------|
| CloudSyncViewModel.cs | 1,297 lines | ~300 lines |
| CloudSyncViewModel.Sync.cs | - | ~250 lines |
| CloudSyncViewModel.Gaming.cs | - | ~200 lines |
| CloudSyncViewModel.Daemon.cs | - | ~300 lines |
| CloudSyncViewModel.Network.cs | - | ~150 lines |
| CloudSyncViewModel.Config.cs | - | ~100 lines |
| CloudSyncViewModel.Models.cs | - | ~50 lines |

**Total Reduction:** 1,297 → 300 lines (-77% for main file)

---

## Timeline

| Phase | Duration | Cumulative |
|-------|----------|------------|
| Preparation | 30 min | 30 min |
| Extract Models | 15 min | 45 min |
| Extract Sync | 45 min | 1.5 hrs |
| Extract Gaming | 30 min | 2 hrs |
| Extract Daemon | 60 min | 3 hrs |
| Extract Network | 30 min | 3.5 hrs |
| Extract Config | 30 min | 4 hrs |
| Cleanup & Test | 30 min | 4.5 hrs |

**Total Estimated Time:** 4-5 hours

---

## Success Criteria

- [ ] All 1,297 lines distributed across 7 files
- [ ] Main file under 350 lines
- [ ] Build succeeds with 0 errors
- [ ] All existing tests pass
- [ ] Manual UI testing confirms no regressions
- [ ] Source generators still work (property changes propagate to UI)

---

## Notes

### Why Not Extract ObservableProperties?
The MVVM Toolkit source generators create backing fields and property implementations at compile time. Moving `[ObservableProperty]` to another partial class file works, but keeping them in the main file makes the class contract clearer.

### Why Partial Classes Instead of Composition?
ViewModels in MVVM are typically tightly coupled to their view. Composition would require significant refactoring of XAML bindings and event handling. Partial classes maintain the single class identity while organizing code by responsibility.

### Post-Refactoring Opportunities
After splitting, consider:
1. Extracting `DaemonHealthSnapshot` logic to a dedicated service
2. Creating a `ConflictResolutionService` to move business logic out of VM
3. Moving network monitoring to a dedicated `NetworkMonitorViewModel`

---

*Created: February 19, 2026*  
*Status: Ready for implementation*  
*Priority: High (largest ViewModel in codebase)*
