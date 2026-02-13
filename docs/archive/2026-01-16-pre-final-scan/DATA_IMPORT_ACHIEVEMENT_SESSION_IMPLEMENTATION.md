# DataImportService Achievement and Session Parsing Implementation

**Implementation Date**: January 7, 2026
**Status**: ✅ **COMPLETED**
**Build Status**: 0 Errors, 1,134 Warnings (pre-existing)

## Overview

This implementation completes the achievement and session history import functionality for the `DataImportService`, enabling full backup restoration and data migration capabilities for SaveState Reborn.

## Features Implemented

### 1. **Achievement Import** 🏆

#### Full JSON Parsing

- **Achievement definitions** with name, description, type, points, icon path
- **Achievement criteria** (JSON object) for complex unlock conditions
- **User achievement progress** with current progress, target progress, unlock status
- **Duplicate detection** based on achievement name
- **Merge support** to skip existing achievements when enabled

#### Implementation Details

```csharp
public async Task<Result<DataImportResult>> ImportAchievementsAsync(
    string filePath,
    bool mergeWithExisting = true,
    CancellationToken ct = default)
```

**Parsing Logic:**

1. Read and parse JSON file
2. Validate "achievements" property exists
3. For each achievement:
   - Parse achievement definition
   - Check for duplicates (by name)
   - Create Achievement entity with proper type
   - Set icon path and criteria
   - Import user progress data
4. Save all changes in single transaction

**Supported Achievement Types:**

- `GameCompletion`
- `PlayTime`
- `Collection`
- `Social`
- `Special` (default)

**JSON Format:**

```json
{
  "achievements": [
    {
      "name": "First Steps",
      "description": "Complete the tutorial",
      "type": "GameCompletion",
      "points": 10,
      "iconPath": "achievements/first_steps.png",
      "targetValue": 1,
      "criteria": {
        "type": "tutorial_complete",
        "value": true
      },
      "userProgress": [
        {
          "userId": "guid-here",
          "currentProgress": 1,
          "targetProgress": 1,
          "isUnlocked": true
        }
      ]
    }
  ]
}
```

---

### 2. **Session History Import** 📊

#### Full JSON Parsing

- **Game session data** with start time, end time, duration
- **Session end reasons** (UserClosed, ProcessCrashed, SystemShutdown, etc.)
- **Session notes** for user annotations
- **Game ID validation** to ensure referenced games exist
- **Duplicate detection** based on game ID and start time
- **Reflection-based timestamp setting** for historical data

#### Implementation Details

```csharp
public async Task<Result<DataImportResult>> ImportSessionHistoryAsync(
    string filePath,
    bool mergeWithExisting = true,
    CancellationToken ct = default)
```

**Parsing Logic:**

1. Read and parse JSON file
2. Validate "sessions" property exists
3. For each session:
   - Parse game ID and validate game exists
   - Parse start time (required)
   - Check for duplicates (same game, same start time)
   - Create GameSession entity
   - Set start time using reflection
   - Parse end time and reason if present
   - Set session notes if present
4. Save all changes in single transaction

**Supported End Reasons:**

- `UserClosed` (default)
- `ProcessCrashed`
- `SystemShutdown`
- `Timeout`
- `ApplicationExit`
- `Unknown`

**JSON Format:**

```json
{
  "sessions": [
    {
      "gameId": "guid-here",
      "startedAt": "2026-01-07T10:00:00Z",
      "endedAt": "2026-01-07T12:30:00Z",
      "endReason": "UserClosed",
      "notes": "Great gaming session!"
    }
  ]
}
```

---

## Code Changes

### Files Modified

**`DataImportService.cs`**

- ✅ Added `using SaveState.Core.GameLibrary.Enums;` for SessionEndReason
- ✅ Implemented `ImportAchievementsAsync` (lines 311-471)
- ✅ Implemented `ImportSessionHistoryAsync` (lines 473-604)
- ✅ Added helper method `ImportUserAchievementProgressAsync` (lines 778-841)

### Key Improvements

1. **Cyclomatic Complexity Reduction**
   - Extracted user achievement progress import to separate helper method
   - Reduced main method complexity from 27 to below 26 threshold

2. **Error Handling**
   - Comprehensive try-catch blocks for each item
   - Detailed error messages with context
   - Continues processing on individual failures
   - Returns summary with imported/skipped/failed counts

3. **Logging**
   - Uses LoggerMessage source generators for performance
   - Debug logging for skipped items
   - Warning logging for user progress failures
   - Information logging for summaries

4. **Data Validation**
   - Required field validation (name, game ID, start time)
   - GUID parsing with error handling
   - Enum parsing with fallback defaults
   - Game existence verification for sessions

---

## Usage Examples

### Import Achievements

```csharp
var result = await _dataImportService.ImportAchievementsAsync(
    filePath: "C:\\backups\\achievements.json",
    mergeWithExisting: true,
    ct: cancellationToken);

if (result.IsSuccess)
{
    Console.WriteLine($"Imported: {result.Value.ItemsImported}");
    Console.WriteLine($"Skipped: {result.Value.ItemsSkipped}");
    Console.WriteLine($"Failed: {result.Value.ItemsFailed}");

    foreach (var error in result.Value.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Import Session History

```csharp
var result = await _dataImportService.ImportSessionHistoryAsync(
    filePath: "C:\\backups\\sessions.json",
    mergeWithExisting: true,
    ct: cancellationToken);

if (result.IsSuccess)
{
    Console.WriteLine(result.Value.Message);
    // "Imported 150 sessions, skipped 25, failed 0"
}
```

### Full Backup Restoration

```csharp
var restoreOptions = new RestoreOptions(
    RestoreGameLibrary: true,
    RestoreUserSettings: true,
    RestoreSaveFileMetadata: true,
    RestoreAchievements: true,      // ✅ Now fully functional
    RestoreSessionHistory: true,    // ✅ Now fully functional
    RestoreActualSaveFiles: false,
    CreateBackupBeforeRestore: true
);

var result = await _dataImportService.RestoreFromBackupAsync(
    backupPath: "C:\\backups\\savestate_backup_20260107.zip",
    restoreOptions: restoreOptions,
    ct: cancellationToken);
```

---

## Technical Details

### Reflection Usage

Both implementations use reflection to set timestamp properties that are normally set by entity constructors:

```csharp
// Set StartedAt for historical sessions
var startedAtProperty = typeof(GameSession).GetProperty("StartedAt");
if (startedAtProperty != null)
{
    startedAtProperty.SetValue(session, startedAt);
}

// Set EndedAt for completed sessions
var endedAtProperty = typeof(GameSession).GetProperty("EndedAt");
if (endedAtProperty != null)
{
    endedAtProperty.SetValue(session, endedAt);
}
```

**Rationale:**

- Entity constructors set timestamps to `DateTime.UtcNow`
- Import needs to preserve historical timestamps
- Reflection allows setting private setters for data migration

---

## Error Handling Strategy

### Achievement Import Errors

| Error Type | Handling | Impact |
|------------|----------|--------|
| Empty name | Skip, add to errors | Item skipped |
| Invalid type | Use default (Special) | Item imported |
| Missing icon | Use default path | Item imported |
| Invalid user ID | Skip user progress | Achievement imported, progress skipped |
| Duplicate achievement | Skip if merging | Item skipped |
| JSON parse error | Fail entire import | Operation fails |

### Session Import Errors

| Error Type | Handling | Impact |
|------------|----------|--------|
| Invalid game ID | Skip, add to errors | Item skipped |
| Non-existent game | Skip, add to errors | Item skipped |
| Invalid start time | Skip, add to errors | Item skipped |
| Duplicate session | Skip if merging | Item skipped |
| Invalid end reason | Use default (UserClosed) | Item imported |
| JSON parse error | Fail entire import | Operation fails |

---

## Performance Considerations

### Database Operations

**Achievements:**

- 1 query per achievement (duplicate check)
- 1 query per user progress (duplicate check)
- 1 bulk save at end

**Sessions:**

- 1 query per session (game existence check)
- 1 query per session (duplicate check)
- 1 bulk save at end

### Optimization Opportunities

1. **Batch Duplicate Checks**

```csharp
// Instead of individual queries
var existingAchievementNames = await _dbContext.Achievements
    .Select(a => a.Name)
    .ToListAsync(ct);

// Check in memory
if (existingAchievementNames.Contains(name) && mergeWithExisting)
{
    itemsSkipped++;
    continue;
}
```

1. **Bulk Insert**

```csharp
// Collect all entities first
var achievementsToAdd = new List<Achievement>();
var userAchievementsToAdd = new List<UserAchievement>();

// ... parse and collect ...

// Bulk add
_dbContext.Achievements.AddRange(achievementsToAdd);
_dbContext.UserAchievements.AddRange(userAchievementsToAdd);
await _dbContext.SaveChangesAsync(ct);
```

1. **Parallel Processing**

```csharp
// For large imports (1000+ items)
var tasks = achievements.Select(async achievement =>
{
    return await ProcessAchievementAsync(achievement, ct);
});

var results = await Task.WhenAll(tasks);
```

---

## Testing Recommendations

### Unit Tests

```csharp
[Fact]
public async Task ImportAchievements_ValidJson_ImportsSuccessfully()
{
    // Arrange
    var json = CreateValidAchievementJson();
    await File.WriteAllTextAsync("test.json", json);

    // Act
    var result = await _service.ImportAchievementsAsync("test.json");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(1, result.Value.ItemsImported);
    Assert.Equal(0, result.Value.ItemsFailed);
}

[Fact]
public async Task ImportAchievements_DuplicateName_SkipsWhenMerging()
{
    // Arrange
    await SeedAchievement("First Steps");
    var json = CreateAchievementJson("First Steps");
    await File.WriteAllTextAsync("test.json", json);

    // Act
    var result = await _service.ImportAchievementsAsync("test.json", mergeWithExisting: true);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(0, result.Value.ItemsImported);
    Assert.Equal(1, result.Value.ItemsSkipped);
}

[Fact]
public async Task ImportSessions_NonExistentGame_SkipsSession()
{
    // Arrange
    var json = CreateSessionJson(Guid.NewGuid()); // Non-existent game
    await File.WriteAllTextAsync("test.json", json);

    // Act
    var result = await _service.ImportSessionHistoryAsync("test.json");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(0, result.Value.ItemsImported);
    Assert.Equal(1, result.Value.ItemsSkipped);
    Assert.Contains("non-existent game", result.Value.Errors[0]);
}
```

### Integration Tests

```csharp
[Fact]
public async Task FullBackupRestore_WithAchievementsAndSessions_RestoresAllData()
{
    // Arrange
    var backupPath = CreateTestBackup(
        achievements: 10,
        sessions: 50,
        games: 5
    );

    // Act
    var result = await _service.RestoreFromBackupAsync(
        backupPath,
        new RestoreOptions(RestoreAchievements: true, RestoreSessionHistory: true)
    );

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(60, result.Value.ItemsImported); // 10 + 50

    var achievements = await _dbContext.Achievements.CountAsync();
    var sessions = await _dbContext.GameSessions.CountAsync();

    Assert.Equal(10, achievements);
    Assert.Equal(50, sessions);
}
```

---

## Build Status

✅ **Build Successful**

- **0 Errors**
- 1,134 Warnings (pre-existing, unrelated)
- Build Time: 6.97 seconds

---

## Future Enhancements

### 1. **Batch Processing**

- Process large imports in batches of 100-500 items
- Reduce memory footprint
- Provide progress callbacks

### 2. **Validation Schema**

- JSON schema validation before parsing
- Early error detection
- Better error messages

### 3. **Import Preview**

- Dry-run mode to preview changes
- Show what will be imported/skipped
- Conflict resolution UI

### 4. **Incremental Imports**

- Track last import timestamp
- Only import new/modified items
- Faster subsequent imports

### 5. **Import Mapping**

- Map old achievement IDs to new IDs
- Handle renamed achievements
- Support data transformations

---

## Conclusion

This implementation completes the data import functionality for SaveState Reborn, enabling:

- ✅ **Full achievement restoration** with user progress
- ✅ **Complete session history import** with all metadata
- ✅ **Robust error handling** with detailed reporting
- ✅ **Production-ready** backup/restore capabilities
- ✅ **Data migration** from other systems

The implementation follows all project standards including:

- Result pattern for error handling
- Async/await best practices
- LoggerMessage source generators
- Clean Architecture separation
- Comprehensive XML documentation

**All TODOs for achievement and session parsing are now resolved!** 🎉
