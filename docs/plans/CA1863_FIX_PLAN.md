# CA1863 Fix Plan: CompositeFormat Caching

**Date:** February 21, 2026  
**Status:** Ready for implementation  
**Priority:** Low (warnings only, not errors)  
**Effort:** 1-2 hours

---

## Overview

CA1863 is a performance warning that detects repeated `string.Format` calls with the same format string. The fix is to cache a `CompositeFormat` instance for repeated use.

### Current State

- **Location:** `src/SaveState.Plugins.GamingAnalytics/GamingAnalyticsPlugin.cs`
- **Lines:** 281, 282, 283, 284
- **Warning count:** 4

### Why This Matters

When `string.Format` is called repeatedly with the same format string:
1. The format string is parsed every time
2. This creates unnecessary allocations
3. Performance impact is small but measurable in hot paths

---

## Current Code (Problematic)

```csharp
// Lines 281-284 in GamingAnalyticsPlugin.cs
_logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogTotalSessions, totalSessions));
_logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogTotalPlayTime, totalPlayTime.TotalHours));
_logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogAverageFps, avgFps));
_logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogPatternsDetected, patternsDetected));
```

---

## Solution Options

### Option 1: Cache CompositeFormat (Recommended)

Create static `CompositeFormat` instances for each format string:

```csharp
public class GamingAnalyticsPlugin : IPlugin
{
    // Cache the CompositeFormat instances
    private static readonly CompositeFormat _totalSessionsFormat = 
        CompositeFormat.Parse(GamingAnalyticsStrings.LogTotalSessions);
    private static readonly CompositeFormat _totalPlayTimeFormat = 
        CompositeFormat.Parse(GamingAnalyticsStrings.LogTotalPlayTime);
    private static readonly CompositeFormat _averageFpsFormat = 
        CompositeFormat.Parse(GamingAnalyticsStrings.LogAverageFps);
    private static readonly CompositeFormat _patternsDetectedFormat = 
        CompositeFormat.Parse(GamingAnalyticsStrings.LogPatternsDetected);

    private async Task ShowAnalyticsDashboardAsync()
    {
        // ... existing code ...
        
        // Use the cached formats
        _logger?.LogInformation(string.Format(_totalSessionsFormat, totalSessions));
        _logger?.LogInformation(string.Format(_totalPlayTimeFormat, totalPlayTime.TotalHours));
        _logger?.LogInformation(string.Format(_averageFpsFormat, avgFps));
        _logger?.LogInformation(string.Format(_patternsDetectedFormat, patternsDetected));
        
        // ... rest of method ...
    }
}
```

**Pros:**
- Format string parsed only once (at type initialization)
- Best performance for repeated calls
- Clean, maintainable code

**Cons:**
- Slightly more code
- Static fields hold memory for the lifetime of the app

### Option 2: Use Interpolated Strings (Simplest)

Replace `string.Format` with C# interpolated strings:

```csharp
_logger?.LogInformation($"Total gaming sessions: {totalSessions}");
_logger?.LogInformation($"Total play time: {totalPlayTime.TotalHours:F1} hours");
_logger?.LogInformation($"Average FPS: {avgFps:F1}");
_logger?.LogInformation($"Gameplay patterns detected: {patternsDetected}");
```

**Pros:**
- Simplest code
- No CA1863 warning
- Modern C# style

**Cons:**
- Loses the benefit of localized strings from `GamingAnalyticsStrings`
- Harder to maintain if strings need localization

### Option 3: Logger Message Templates (Best for Logging)

Use structured logging with message templates:

```csharp
// Define static LoggerMessage delegates
private static readonly Action<ILogger, int, Exception?> _logTotalSessions = 
    LoggerMessage.Define<int>(
        LogLevel.Information, 
        new EventId(1, "TotalSessions"), 
        GamingAnalyticsStrings.LogTotalSessions);

private static readonly Action<ILogger, double, Exception?> _logTotalPlayTime = 
    LoggerMessage.Define<double>(
        LogLevel.Information, 
        new EventId(2, "TotalPlayTime"), 
        GamingAnalyticsStrings.LogTotalPlayTime);

// Usage in method
_logTotalSessions(_logger, totalSessions, null);
_logTotalPlayTime(_logger, totalPlayTime.TotalHours, null);
```

**Pros:**
- Best performance for logging
- Structured logging support
- No CA1863 warning

**Cons:**
- Most verbose
- Overkill for simple scenarios

---

## Recommended Approach: Option 1 (CompositeFormat Caching)

### Implementation Steps

1. **Add using statement:**
```csharp
using System.Text;  // For CompositeFormat
```

2. **Add static format fields** at the top of the class:
```csharp
#region Cached Formats (CA1863)

private static readonly CompositeFormat TotalSessionsFormat = 
    CompositeFormat.Parse(GamingAnalyticsStrings.LogTotalSessions);
private static readonly CompositeFormat TotalPlayTimeFormat = 
    CompositeFormat.Parse(GamingAnalyticsStrings.LogTotalPlayTime);
private static readonly CompositeFormat AverageFpsFormat = 
    CompositeFormat.Parse(GamingAnalyticsStrings.LogAverageFps);
private static readonly CompositeFormat PatternsDetectedFormat = 
    CompositeFormat.Parse(GamingAnalyticsStrings.LogPatternsDetected);

#endregion
```

3. **Update the method calls** in `ShowAnalyticsDashboardAsync`:
```csharp
_logger?.LogInformation(string.Format(TotalSessionsFormat, totalSessions));
_logger?.LogInformation(string.Format(TotalPlayTimeFormat, totalPlayTime.TotalHours));
_logger?.LogInformation(string.Format(AverageFpsFormat, avgFps));
_logger?.LogInformation(string.Format(PatternsDetectedFormat, patternsDetected));
```

### Verification

After implementation, verify:
```bash
dotnet build src/SaveState.Plugins.GamingAnalytics/SaveState.Plugins.GamingAnalytics.csproj --nologo
# Should show: 0 warnings
```

---

## Alternative: Global Suppression (Not Recommended)

If fixing is not feasible, add to `.editorconfig`:

```ini
[*.cs]
# CA1863: Cache a 'CompositeFormat' for repeated use
# Rationale: Performance impact is negligible for this plugin's usage pattern
dotnet_diagnostic.CA1863.severity = suggestion
```

**Not recommended** because:
- The fix is simple and low-risk
- Sets a bad precedent for ignoring analyzer warnings
- Other plugins may copy this pattern

---

## Related Files to Check

Run this command to find other CA1863 instances:
```bash
dotnet build SaveStateReborn.sln -v:diag 2>&1 | grep "CA1863"
```

Currently only GamingAnalyticsPlugin.cs has these warnings.

---

## Acceptance Criteria

- [ ] All 4 CA1863 warnings resolved in GamingAnalyticsPlugin.cs
- [ ] Build passes with 0 warnings
- [ ] No functional changes (output identical)
- [ ] Code review approved

---

## References

- [CA1863 Documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1863)
- [CompositeFormat Class](https://learn.microsoft.com/en-us/dotnet/api/system.text.compositeformat)
- [High Performance Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)
