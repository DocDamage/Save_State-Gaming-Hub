# GitHub Issues to Create - Technical Debt Tracking

**Date**: 2026-01-16
**Source**: Technical Debt Audit 2026-01-14 (Version 3.2)

These issues track the remaining minor technical debt items after achieving a clean build (0 errors, 0 warnings).

---

## Issue 1: Implement AudioOptimizer Platform-Specific Device Switching

**Title**: Implement Windows Core Audio API for AudioOptimizer device switching

**Labels**: `technical-debt`, `enhancement`, `platform-specific`, `audio`

**Priority**: Medium

**Description**:

The `AudioOptimizer.SetTemporaryDeviceAsync()` method currently returns success but doesn't actually switch audio devices.

**Location**: `src/SaveState.Infrastructure/Performance/AudioOptimizer.cs:193-211`

**Current Behavior**:

- Method logs a warning and returns `Result.Success()` without performing any action
- Misleading API contract - claims success but does nothing

**Expected Behavior**:

- Implement Windows Core Audio API integration for device switching
- Add platform detection (Windows/Linux/macOS)
- Return `Result.Failure` for unsupported platforms with clear messaging
- Add feature flag for gradual rollout

**Acceptance Criteria**:

- [ ] Windows Core Audio API integration working
- [ ] Platform detection implemented
- [ ] Proper error handling for unsupported platforms
- [ ] Feature flag added for safe rollout
- [ ] Unit tests added
- [ ] Integration tests for Windows platform

**Priority Justification**: Medium - Feature exists but is incomplete. Users expect it to work but it silently fails.

---

## Issue 2: Improve Error Handling in LiveSyncService

**Title**: Add proper error propagation in LiveSyncService.SafeHandleAsync

**Labels**: `technical-debt`, `error-handling`, `robustness`

**Priority**: Medium

**Description**:

The `LiveSyncService.SafeHandleAsync()` method swallows exceptions without proper error propagation or retry logic.

**Location**: `src/SaveState.Application/RomManagement/Services/LiveSyncService.cs:307-318`

**Current Behavior**:

```csharp
private async Task SafeHandleAsync(Func<Task> action)
{
    try
    {
        await action().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling FileSystemWatcher event");
        // ❌ Swallowed - no retry, circuit breaker, or error propagation
    }
}
```

**Problems**:

- No retry logic for transient failures
- No circuit breaker pattern
- No error propagation to caller
- Users have no visibility into sync failures

**Expected Behavior**:

- Return `Task<Result>` instead of `Task`
- Implement retry logic with exponential backoff
- Add circuit breaker for repeated failures
- Track failures in telemetry
- Allow caller to handle errors appropriately

**Recommended Pattern**:

```csharp
private async Task<Result> SafeHandleAsync(Func<Task> action, string operationName)
{
    try
    {
        await action().ConfigureAwait(false);
        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in {Operation}", operationName);
        _telemetry.TrackException(ex);
        return Result.Failure($"{operationName} failed: {ex.Message}", ErrorType.Internal);
    }
}
```

**Acceptance Criteria**:

- [ ] Method returns `Task<Result>` with proper error information
- [ ] Retry logic implemented with exponential backoff
- [ ] Circuit breaker pattern added
- [ ] Telemetry tracking exceptions
- [ ] Callers updated to handle Result
- [ ] Unit tests for error scenarios

---

## Issue 3: Enhance PerformanceMonitor GPU Usage Error Handling

**Title**: Add proper error handling and telemetry to GPU usage monitoring

**Labels**: `technical-debt`, `error-handling`, `monitoring`

**Priority**: Low

**Description**:

The `PerformanceMonitor.GetGpuUsageAsync()` method catches all exceptions and returns 0, making it impossible to distinguish between "no GPU usage" and "failed to read GPU usage".

**Location**: `src/SaveState.Infrastructure/Performance/PerformanceMonitor.cs:310-316`

**Current Behavior**:

```csharp
private async Task<float> GetGpuUsageAsync(CancellationToken ct)
{
    try
    {
        // ... implementation
    }
    catch
    {
        return 0; // ❌ Silent failure
    }
}
```

**Problems**:

- Catches ALL exceptions without logging
- Returns 0 on failure (indistinguishable from "no GPU usage")
- No telemetry or diagnostics
- Users cannot debug GPU monitoring issues

**Expected Behavior**:

- Log exceptions with full context
- Return `float?` to distinguish failure (null) from zero usage
- Add telemetry to track monitoring failures
- Provide fallback strategies for different GPU types

**Acceptance Criteria**:

- [ ] Exceptions logged with context
- [ ] Return type changed to `float?`
- [ ] Telemetry added for failures
- [ ] Callers updated to handle nullable return
- [ ] Tests for error scenarios

---

## Issue 4: Remove Remaining TODO Comments

**Title**: Address remaining TODO comments in production code

**Labels**: `technical-debt`, `cleanup`, `documentation`

**Priority**: Low

**Description**:

There are 6-7 TODO comments remaining in production code that should be either implemented or converted to proper GitHub issues.

**Current TODOs**:

| File | Line | Issue | Priority |
|------|------|-------|----------|
| `LibraryViewModel.cs` | 12 | Implement library functionality | MEDIUM |
| `TournamentBracketViewModel.cs` | 31 | Implement bracket update logic | MEDIUM |
| `AutomationDashboardViewModel.cs` | 58 | Implement MacroMarketplaceViewModel | LOW |
| `GameModsTabViewModel.cs` | 130 | Available updates check | MEDIUM |
| `SaveState.Infrastructure.xml` | 1509 | Enhance with play history/preferences | LOW |

**Note**: The TODO in `RecommendationsViewModel.cs:50` about hardcoded user ID has been resolved.

**Acceptance Criteria**:

- [ ] Each TODO converted to a GitHub issue or implemented
- [ ] All TODO comments removed from production code
- [ ] Features tracked in proper issue system
- [ ] Each issue has clear acceptance criteria

---

## Issue 5: Reduce Remaining Guid.Empty Usages (Optional)

**Title**: Review and potentially refactor remaining Guid.Empty usages

**Labels**: `technical-debt`, `code-quality`, `optional`

**Priority**: Very Low

**Description**:

There are 5 remaining `Guid.Empty` usages in the codebase. These have been reviewed and are valid guard clauses, but we should document why they're acceptable or refactor if possible.

**Current Status**: 16 → 5 (69% reduction)

**Remaining Locations** (to be verified):

- Guard clauses checking for invalid IDs
- Default value comparisons

**Expected Behavior**:

- Document why each remaining usage is valid
- Consider using `Guid?` with null checks where appropriate
- Ensure consistent pattern across codebase

**Acceptance Criteria**:

- [ ] All 5 usages documented with inline comments
- [ ] Review if any can be refactored to `Guid?`
- [ ] Add analyzer suppression with justification if needed

---

## Summary

After achieving **0 errors and 0 warnings**, these issues represent polish items and minor technical debt:

- **Medium Priority**: Issues 1, 2 (error handling and platform features)
- **Low Priority**: Issues 3, 4 (monitoring improvements and cleanup)
- **Very Low Priority**: Issue 5 (optional code quality improvement)

**Total Issues**: 5
**Estimated Total Effort**: 2-3 sprints

**Current Health Score**: 98/100
**Target Health Score**: 99-100/100 after addressing these items

---

## How to Create These Issues

You can create these issues using the GitHub CLI:

```bash
# Login first
gh auth login

# Create each issue
gh issue create --title "Issue Title" --body-file issue_body.md --label "technical-debt,enhancement"
```

Or visit: <https://github.com/DocDamage/Save_State-Gaming-Hub/issues/new>
