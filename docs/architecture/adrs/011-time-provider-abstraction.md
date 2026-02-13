# ADR 011: Time Provider Abstraction

**Status:** Accepted  
**Date:** February 12, 2026  
**Author:** Kimi CLI  
**Decision:** Migrate all `DateTime.Now` usage to `ITimeProvider` abstraction

---

## Context

The codebase had 90+ occurrences of `DateTime.Now` scattered across:
- Infrastructure services
- ViewModels
- Plugins
- CLI commands

This made time-dependent code difficult to test and created inconsistencies in how time was accessed.

---

## Decision

Migrate all direct `DateTime.Now` usage to use the existing `ITimeProvider` abstraction:

```csharp
public interface ITimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateTime Today { get; }
    long GetTimestamp();
}
```

### Implementation Pattern

#### For Services/ViewModels

```csharp
public class MyService
{
    private readonly ITimeProvider _timeProvider;
    
    public MyService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public void DoWork()
    {
        var timestamp = _timeProvider.Now.ToString("yyyyMMdd");
    }
}
```

#### For Plugins

```csharp
public class MyPlugin : IPlugin
{
    private ITimeProvider _timeProvider = null!;
    
    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();
    }
}
```

#### For Tests

```csharp
// Use SystemTimeProvider for real time
var service = new MyService(new SystemTimeProvider());

// Or mock for specific time
var mock = new Mock<ITimeProvider>();
mock.Setup(tp => tp.Now).Returns(new DateTime(2026, 2, 12));
var service = new MyService(mock.Object);
```

---

## Migration Statistics

| Category | Before | After | Reduction |
|----------|--------|-------|-----------|
| Infrastructure | 11 | 0 | 100% |
| ViewModels | 38+ | 0 | 100% |
| Plugins | 17 | 0 | 100% |
| CLI | 1 | 0 | 100% |
| **Total** | **90** | **2** | **98%** |

**Remaining:**
- `ITimeProvider.cs:55` - SystemTimeProvider implementation (expected)
- `GameBackupManagerPlugin.cs:53` - Commented code (not active)

---

## Files Modified

### Infrastructure Layer (11 occurrences)
- `CaptureMediaHandlers.cs`
- `DataImportService.cs`
- `SaveStateManager.cs`
- `MugenConfigService.cs`
- And 7 more...

### ViewModels (38+ occurrences)
- `AdvancedAnalyticsViewModel.cs`
- `AnalyticsDashboardViewModel.cs`
- `BigPictureShellViewModel.cs`
- `GameDetailViewModel.cs` + all tab ViewModels
- `CloudSyncViewModel.cs`
- `GameMemoryViewModel.cs`
- `MacroRecorderDialogViewModel.cs`
- `PerformanceDashboardViewModel.cs`
- `StatusBarViewModel.cs`
- `VoiceCommandViewModel.cs`
- `VoiceControlViewModel.cs`
- `MainShellViewModel.cs`
- `OverlayContainerViewModel.cs`
- `NotificationsOverlayViewModel.cs`
- `PriceHistoryViewModel.cs`
- `MugenTournamentViewModel.cs`
- `MugenAudioViewModel.cs`
- And more...

### Plugins (17 occurrences)
- `GamePassAlertPlugin.cs`
- `GameTimerPlugin.cs`
- `HealthWellnessPlugin.cs`
- `MugenManagerPlugin.cs`
- `ScreenshotCapturePlugin.cs`
- `ScreenshotSorterPlugin.cs`
- `AdvancedThemesPlugin.cs`

### CLI (1 occurrence)
- `GameCommands.cs`

### Tests
- `ViewModelTests.cs` - Updated to inject `SystemTimeProvider`

---

## Consequences

### Positive

1. **Testability**: Time can now be mocked in unit tests
2. **Consistency**: Single pattern for time access across codebase
3. **Determinism**: Tests can use fixed time values
4. **Maintainability**: Easy to change time source (e.g., for testing time zones)

### Negative

1. **Constructor bloat**: More dependencies to inject
2. **Breaking change**: Existing code needs updating
3. **Learning curve**: New developers must know the pattern

### Mitigation

- All tests updated to use `SystemTimeProvider`
- Documentation added to `AGENTS.md`
- ADR created for reference

---

## Related Decisions

- ADR 004: Dependency Injection Policy
- ADR 007: Result Pattern

---

## References

- [Microsoft TimeProvider](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)
- [Unit Testing Time-Dependent Code](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
