# LoggerMessage Migration Workflow

## Quick Reference for File-by-File Migration

### Prerequisites

- File must use `Microsoft.Extensions.Logging`
- Class must have an `ILogger` field (typically `_logger`)

---

## Step-by-Step Process

### 1. Make the class `partial`

```csharp
// Change:
public class MyService : IMyService

// To:
public partial class MyService : IMyService
```

### 2. Identify all logging calls in the file

Search for:

- `_logger.LogTrace(`
- `_logger.LogDebug(`
- `_logger.LogInformation(`
- `_logger.LogWarning(`
- `_logger.LogError(`
- `_logger.LogCritical(`

### 3. Create LoggerMessage definitions

Add after the constructor:

```csharp
#region LoggerMessage Definitions

// Information level - use for normal flow events
[LoggerMessage(Level = LogLevel.Information, Message = "Operation started for {ItemId}")]
private static partial void LogOperationStarted(ILogger logger, Guid itemId);

// Warning level - use for recoverable issues
[LoggerMessage(Level = LogLevel.Warning, Message = "Item not found: {ItemId}")]
private static partial void LogItemNotFound(ILogger logger, Guid itemId);

// Error level with exception - use for failures
[LoggerMessage(Level = LogLevel.Error, Message = "Operation failed for {ItemId}")]
private static partial void LogOperationFailed(ILogger logger, Exception ex, Guid itemId);

// Error level without exception
[LoggerMessage(Level = LogLevel.Error, Message = "Validation failed: {Reason}")]
private static partial void LogValidationError(ILogger logger, string reason);

// Debug level
[LoggerMessage(Level = LogLevel.Debug, Message = "Processing item {Index} of {Total}")]
private static partial void LogProcessingProgress(ILogger logger, int index, int total);

#endregion
```

### 4. Replace call sites

```csharp
// Before:
_logger.LogInformation("Operation started for {ItemId}", itemId);
_logger.LogWarning("Item not found: {ItemId}", itemId);
_logger.LogError(ex, "Operation failed for {ItemId}", itemId);

// After:
LogOperationStarted(_logger, itemId);
LogItemNotFound(_logger, itemId);
LogOperationFailed(_logger, ex, itemId);
```

---

## Naming Conventions

### By Log Level

| Level | Naming Pattern | Examples |
|-------|----------------|----------|
| Trace | `LogXxxTrace` | `LogMethodEntryTrace` |
| Debug | `LogXxxDebug`, `LogXxx` | `LogProcessingDebug` |
| Information | `LogXxxStarted`, `LogXxxCompleted`, `LogXxxFound` | `LogGameLoaded`, `LogSyncCompleted` |
| Warning | `LogXxxNotFound`, `LogXxxSkipped`, `LogXxxMissing` | `LogFileNotFound`, `LogItemSkipped` |
| Error | `LogXxxFailed`, `LogXxxError` | `LogSaveFailed`, `LogConnectionError` |
| Critical | `LogXxxCritical` | `LogDatabaseCritical` |

### Exception Parameter Position

When including an exception, it should be the **first parameter after logger**:

```csharp
[LoggerMessage(Level = LogLevel.Error, Message = "Operation failed")]
private static partial void LogOperationFailed(ILogger logger, Exception ex);

[LoggerMessage(Level = LogLevel.Error, Message = "Operation failed for {ItemId}")]
private static partial void LogOperationFailedForItem(ILogger logger, Exception ex, Guid itemId);
```

---

## Common Patterns

### Pattern 1: Simple message (no parameters)

```csharp
// Definition:
[LoggerMessage(Level = LogLevel.Information, Message = "Service initialized")]
private static partial void LogServiceInitialized(ILogger logger);

// Usage:
LogServiceInitialized(_logger);
```

### Pattern 2: With parameters

```csharp
// Definition:
[LoggerMessage(Level = LogLevel.Information, Message = "Processing game {GameName} ({GameId})")]
private static partial void LogProcessingGame(ILogger logger, string gameName, Guid gameId);

// Usage:
LogProcessingGame(_logger, game.Name, game.Id);
```

### Pattern 3: With exception

```csharp
// Definition:
[LoggerMessage(Level = LogLevel.Error, Message = "Failed to save game {GameId}")]
private static partial void LogSaveGameFailed(ILogger logger, Exception ex, Guid gameId);

// Usage:
catch (Exception ex)
{
    LogSaveGameFailed(_logger, ex, gameId);
}
```

### Pattern 4: Conditional logging

```csharp
// For expensive operations, check if level is enabled first:
if (_logger.IsEnabled(LogLevel.Debug))
{
    var details = ComputeExpensiveDebugInfo();
    LogDebugDetails(_logger, details);
}
```

---

## Deduplication Tips

### Reuse definitions for identical messages

If multiple methods log the same message, define it once:

```csharp
[LoggerMessage(Level = LogLevel.Warning, Message = "Main window not found")]
private static partial void LogMainWindowNotFound(ILogger logger);

// Can be called from multiple methods
```

### Use descriptive names for similar but different messages

```csharp
[LoggerMessage(Level = LogLevel.Error, Message = "Failed to show note editor dialog")]
private static partial void LogNoteEditorFailed(ILogger logger, Exception ex);

[LoggerMessage(Level = LogLevel.Error, Message = "Failed to show tag editor dialog")]
private static partial void LogTagEditorFailed(ILogger logger, Exception ex);
```

---

## Verification

After migrating a file:

1. **Build the project**: `dotnet build src/ProjectName`
2. **Check for CA1848**: Should be gone for that file
3. **Run tests**: Ensure logging still works

---

## Template for New Files

```csharp
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Example;

/// <summary>
/// Example service with high-performance logging.
/// </summary>
public partial class ExampleService : IExampleService
{
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(ILogger<ExampleService> logger)
    {
        _logger = logger;
    }

    #region LoggerMessage Definitions
    [LoggerMessage(Level = LogLevel.Information, Message = "Example operation started")]
    private static partial void LogOperationStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Example operation failed")]
    private static partial void LogOperationFailed(ILogger logger, Exception ex);
    #endregion

    public async Task DoSomethingAsync()
    {
        LogOperationStarted(_logger);
        try
        {
            // ... implementation
        }
        catch (Exception ex)
        {
            LogOperationFailed(_logger, ex);
            throw;
        }
    }
}
```
