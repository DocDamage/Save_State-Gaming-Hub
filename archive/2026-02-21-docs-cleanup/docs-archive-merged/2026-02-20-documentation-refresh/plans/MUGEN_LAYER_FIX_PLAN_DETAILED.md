# MUGEN Layer Fix Plan - Detailed Implementation Guide

**Date:** February 13, 2026  
**Current Errors:** 241  
**Estimated Time:** 6-8 hours  
**Priority:** Critical - Blocking Phase 2 Development

---

## Table of Contents

1. [Error Analysis & Patterns](#error-analysis--patterns)
2. [Phase 1: Core Infrastructure Services](#phase-1-core-infrastructure-services)
3. [Phase 2: Content & Educational Services](#phase-2-content--educational-services)
4. [Phase 3: Advanced Feature Services](#phase-3-advanced-feature-services)
5. [Phase 4: Final Cleanup](#phase-4-final-cleanup)
6. [Testing & Verification](#testing--verification)
7. [Common Patterns & Solutions](#common-patterns--solutions)

---

## Error Analysis & Patterns

### Error Categories Breakdown

```
Total Errors: 241
├── Missing Engine Methods:     145 (60%)
├── Model Property Mismatches:   60 (25%)
├── Constructor Mismatches:      24 (10%)
├── Type Conversions:            12 (5%)
```

### Common Error Patterns

#### Pattern 1: Missing Engine Method
```csharp
// Error:
// CS1061: 'MatchDataEngine' does not contain a definition for 'RecordMatch'

// Service usage (line 60 in MatchAnalyticsService.cs):
_matchDataEngine.RecordMatch(matchData);

// Fix needed in MatchDataEngine.cs:
public Result RecordMatch(MatchData matchData)
{
    // Implementation
}
```

#### Pattern 2: Missing Model Property
```csharp
// Error:
// CS0117: 'MobileNotification' does not contain a definition for 'IsRead'

// Engine usage:
notification.IsRead = false;

// Fix needed in MobileNotification model:
public bool IsRead { get; set; }
```

#### Pattern 3: Constructor Mismatch
```csharp
// Error:
// CS1729: 'ListingEngine' does not contain a constructor that takes 2 arguments

// Service usage:
_listingEngine = new ListingEngine(logger, cache);

// Fix needed - add constructor:
public ListingEngine(ILogger<ListingEngine> logger, ICacheService cache)
{
    _logger = logger;
    _cache = cache;
}
```

---

## Phase 1: Core Infrastructure Services

### 1.1 LiveSync Service (15 errors) - 30 minutes

#### File: `LiveSync/Engines/ConflictResolutionEngine.cs`

**Current Error (Line 285 in LiveSyncService.cs):**
```csharp
var result = await _conflictResolutionEngine.ResolveConflictAsync(
    accountId, conflict, resolution, ct);
```

**Required Method Implementation:**
```csharp
public async Task<ConflictResolutionResult> ResolveConflictAsync(
    string accountId,
    AccountConflict conflict,
    ConflictResolution resolution,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Resolving conflict {ConflictId} for account {AccountId} with strategy {Strategy}",
        conflict.ConflictId, accountId, resolution.Strategy);

    try
    {
        var result = new ConflictResolutionResult
        {
            ResolutionId = Guid.NewGuid().ToString(),
            AccountId = accountId,
            ConflictId = conflict.ConflictId,
            Strategy = resolution.Strategy,
            ResolvedAt = DateTime.UtcNow,
            Success = true
        };

        // Apply resolution strategy
        switch (resolution.Strategy)
        {
            case ConflictResolutionStrategy.KeepLocal:
                result.AppliedData = conflict.LocalData;
                break;
            case ConflictResolutionStrategy.KeepRemote:
                result.AppliedData = conflict.RemoteData;
                break;
            case ConflictResolutionStrategy.Merge:
                result.AppliedData = MergeData(conflict.LocalData, conflict.RemoteData);
                break;
            default:
                throw new NotSupportedException($"Strategy {resolution.Strategy} not supported");
        }

        // Store resolution
        _resolutions[result.ResolutionId] = result;

        _logger.LogInformation(
            "Conflict {ConflictId} resolved successfully with resolution {ResolutionId}",
            conflict.ConflictId, result.ResolutionId);

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to resolve conflict {ConflictId}", conflict.ConflictId);
        throw;
    }
}

private Dictionary<string, object> MergeData(
    Dictionary<string, object> local,
    Dictionary<string, object> remote)
{
    var merged = new Dictionary<string, object>(local);
    foreach (var kvp in remote)
    {
        if (!merged.ContainsKey(kvp.Key))
        {
            merged[kvp.Key] = kvp.Value;
        }
        // For conflicting keys, keep the newer timestamp if available
        // Otherwise default to local (implementation-specific)
    }
    return merged;
}
```

**Edge Cases to Handle:**
1. Null conflict data
2. Unsupported resolution strategies
3. Concurrent resolution attempts for same conflict
4. Storage failures after resolution applied

**Unit Test:**
```csharp
[Fact]
public async Task ResolveConflictAsync_KeepLocalStrategy_ReturnsLocalData()
{
    var engine = new ConflictResolutionEngine(_loggerMock.Object);
    var conflict = CreateTestConflict();
    var resolution = new ConflictResolution 
    { 
        Strategy = ConflictResolutionStrategy.KeepLocal 
    };

    var result = await engine.ResolveConflictAsync("acc123", conflict, resolution);

    result.Success.Should().BeTrue();
    result.AppliedData.Should().BeEquivalentTo(conflict.LocalData);
}
```

---

#### File: `LiveSync/Engines/SyncEngine.cs`

**Missing Methods:**

1. **CalculateSyncHealth** (Line 351 in LiveSyncService.cs)
```csharp
public SyncHealth CalculateSyncHealth(string accountId)
{
    _logger.LogDebug("Calculating sync health for account {AccountId}", accountId);

    if (!_syncHistory.TryGetValue(accountId, out var history))
    {
        return new SyncHealth 
        { 
            AccountId = accountId,
            Status = SyncHealthStatus.NoData,
            Score = 0 
        };
    }

    var recentSyncs = history
        .Where(s => s.Timestamp > DateTime.UtcNow.AddDays(-7))
        .ToList();

    if (!recentSyncs.Any())
    {
        return new SyncHealth
        {
            AccountId = accountId,
            Status = SyncHealthStatus.Stale,
            Score = 0
        };
    }

    var successRate = (float)recentSyncs.Count(s => s.Success) / recentSyncs.Count;
    var avgDuration = recentSyncs.Average(s => s.DurationMs);
    var conflictRate = (float)recentSyncs.Count(s => s.Conflicts > 0) / recentSyncs.Count;

    var score = CalculateHealthScore(successRate, avgDuration, conflictRate);

    return new SyncHealth
    {
        AccountId = accountId,
        Status = score > 0.8f ? SyncHealthStatus.Healthy :
                 score > 0.5f ? SyncHealthStatus.Warning : SyncHealthStatus.Critical,
        Score = score,
        LastSync = recentSyncs.Max(s => s.Timestamp),
        SuccessRate = successRate,
        AverageSyncDurationMs = (float)avgDuration,
        ConflictRate = conflictRate
    };
}

private float CalculateHealthScore(float successRate, double avgDuration, float conflictRate)
{
    // Weighted scoring: success (50%), speed (30%), conflicts (20%)
    var successScore = successRate * 0.5f;
    var speedScore = avgDuration < 1000 ? 0.3f :
                     avgDuration < 5000 ? 0.2f :
                     avgDuration < 10000 ? 0.1f : 0f;
    var conflictScore = (1f - conflictRate) * 0.2f;

    return successScore + speedScore + conflictScore;
}
```

2. **CalculateDataCompleteness** (Line 352 in LiveSyncService.cs)
```csharp
public DataCompletenessReport CalculateDataCompleteness(string accountId)
{
    _logger.LogDebug("Calculating data completeness for account {AccountId}", accountId);

    var report = new DataCompletenessReport
    {
        AccountId = accountId,
        CalculatedAt = DateTime.UtcNow
    };

    // Check each data type
    var categories = new[] { "SaveData", "Settings", "Achievements", "Progress" };
    var categoryStatus = new Dictionary<string, DataCategoryStatus>();

    foreach (var category in categories)
    {
        var localCount = GetLocalDataCount(accountId, category);
        var cloudCount = GetCloudDataCount(accountId, category);
        var expectedCount = GetExpectedDataCount(accountId, category);

        var status = new DataCategoryStatus
        {
            Category = category,
            LocalCount = localCount,
            CloudCount = cloudCount,
            ExpectedCount = expectedCount,
            Completeness = expectedCount > 0 ? 
                (float)Math.Max(localCount, cloudCount) / expectedCount : 1f,
            SyncRequired = localCount != cloudCount
        };

        categoryStatus[category] = status;
    }

    report.Categories = categoryStatus;
    report.OverallCompleteness = categoryStatus.Values.Average(c => c.Completeness);
    report.RequiresSync = categoryStatus.Values.Any(c => c.SyncRequired);

    return report;
}
```

3. **PerformSyncAsync** (Line 541 in LiveSyncService.cs)
```csharp
public async Task<SyncResult> PerformSyncAsync(
    string accountId,
    SyncDirection direction,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Performing sync for account {AccountId} with direction {Direction}",
        accountId, direction);

    var stopwatch = Stopwatch.StartNew();
    var result = new SyncResult
    {
        SyncId = Guid.NewGuid().ToString(),
        AccountId = accountId,
        Direction = direction,
        StartedAt = DateTime.UtcNow
    };

    try
    {
        switch (direction)
        {
            case SyncDirection.Upload:
                result.ItemsSynced = await UploadLocalDataAsync(accountId, ct);
                break;
            case SyncDirection.Download:
                result.ItemsSynced = await DownloadCloudDataAsync(accountId, ct);
                break;
            case SyncDirection.Bidirectional:
                var uploadResult = await UploadLocalDataAsync(accountId, ct);
                var downloadResult = await DownloadCloudDataAsync(accountId, ct);
                result.ItemsSynced = uploadResult + downloadResult;
                break;
        }

        result.Success = true;
        result.Conflicts = _conflictCount.GetValueOrDefault(accountId, 0);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Sync failed for account {AccountId}", accountId);
        result.Success = false;
        result.ErrorMessage = ex.Message;
    }
    finally
    {
        stopwatch.Stop();
        result.DurationMs = (float)stopwatch.Elapsed.TotalMilliseconds;
        result.CompletedAt = DateTime.UtcNow;

        // Record in history
        if (!_syncHistory.ContainsKey(accountId))
            _syncHistory[accountId] = new List<SyncRecord>();
        _syncHistory[accountId].Add(new SyncRecord
        {
            Timestamp = result.CompletedAt.Value,
            Success = result.Success,
            DurationMs = result.DurationMs,
            Conflicts = result.Conflicts
        });
    }

    return result;
}
```

**Edge Cases:**
- Network timeouts during sync
- Partial sync failures (some items succeed, others fail)
- Conflicts detected during bidirectional sync
- Account locked or disabled
- Storage quota exceeded

---

#### File: `LiveSync/Engines/MigrationEngine.cs`

**Missing Method:**

```csharp
public async Task<MigrationResult> MigrateAsync(
    string accountId,
    MigrationConfig config,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Starting migration for account {AccountId} from {SourceVersion} to {TargetVersion}",
        accountId, config.SourceVersion, config.TargetVersion);

    var result = new MigrationResult
    {
        MigrationId = Guid.NewGuid().ToString(),
        AccountId = accountId,
        StartedAt = DateTime.UtcNow,
        SourceVersion = config.SourceVersion,
        TargetVersion = config.TargetVersion
    };

    var stopwatch = Stopwatch.StartNew();

    try
    {
        // Pre-migration validation
        var validationResult = await ValidateMigrationPrerequisites(accountId, config);
        if (!validationResult.Valid)
        {
            result.Success = false;
            result.ErrorMessage = $"Prerequisites not met: {validationResult.Reason}";
            return result;
        }

        // Backup current data
        var backupId = await CreateBackupAsync(accountId);
        result.BackupId = backupId;

        // Perform migration in stages
        var stages = GetMigrationStages(config.SourceVersion, config.TargetVersion);
        foreach (var stage in stages)
        {
            _logger.LogDebug("Executing migration stage: {StageName}", stage.Name);
            
            var stageResult = await ExecuteMigrationStageAsync(accountId, stage, ct);
            result.StagesCompleted++;
            
            if (!stageResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Stage '{stage.Name}' failed: {stageResult.Error}";
                
                // Attempt rollback
                await RollbackMigrationAsync(accountId, backupId);
                result.RollbackPerformed = true;
                
                return result;
            }
        }

        // Post-migration verification
        var verification = await VerifyMigrationAsync(accountId, config);
        result.ItemsMigrated = verification.ItemsMigrated;
        result.Success = verification.Success;

        if (!verification.Success)
        {
            await RollbackMigrationAsync(accountId, backupId);
            result.RollbackPerformed = true;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Migration failed for account {AccountId}", accountId);
        result.Success = false;
        result.ErrorMessage = ex.Message;
    }
    finally
    {
        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        result.CompletedAt = DateTime.UtcNow;
    }

    return result;
}
```

---

### 1.2 NetworkFeatures Service (12 errors) - 25 minutes

#### File: `NetworkFeatures/Engines/LobbyEngine.cs`

**Missing Methods:**

1. **CreateLobby** (Line 151 in NetworkFeaturesService.cs)
```csharp
public Result<Lobby> CreateLobby(LobbyConfiguration config, string hostPlayerId)
{
    _logger.LogInformation(
        "Creating lobby '{LobbyName}' for player {HostPlayerId}",
        config.Name, hostPlayerId);

    try
    {
        // Validate configuration
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            return Result.Failure<Lobby>("Lobby name is required");
        }

        if (config.MaxPlayers < 2 || config.MaxPlayers > 64)
        {
            return Result.Failure<Lobby>("Max players must be between 2 and 64");
        }

        var lobby = new Lobby
        {
            LobbyId = GenerateLobbyId(),
            Name = config.Name,
            HostPlayerId = hostPlayerId,
            MaxPlayers = config.MaxPlayers,
            CurrentPlayers = 1,
            Status = LobbyStatus.Open,
            GameMode = config.GameMode,
            Region = config.Region,
            HasPassword = !string.IsNullOrEmpty(config.Password),
            CreatedAt = DateTime.UtcNow,
            Players = new List<LobbyPlayer>
            {
                new LobbyPlayer
                {
                    PlayerId = hostPlayerId,
                    JoinedAt = DateTime.UtcNow,
                    IsHost = true,
                    IsReady = false
                }
            }
        };

        // Store lobby
        _lobbies[lobby.LobbyId] = lobby;

        _logger.LogInformation(
            "Lobby {LobbyId} created successfully", lobby.LobbyId);

        return Result.Success(lobby);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create lobby");
        return Result.Failure<Lobby>($"Failed to create lobby: {ex.Message}");
    }
}

private string GenerateLobbyId()
{
    // Generate readable lobby ID (e.g., "ABC-123-XYZ")
    var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    var random = new Random();
    var id = new StringBuilder();
    
    for (int i = 0; i < 3; i++)
    {
        if (i > 0) id.Append('-');
        for (int j = 0; j < 3; j++)
        {
            id.Append(chars[random.Next(chars.Length)]);
        }
    }
    
    return id.ToString();
}
```

2. **ValidateLobbyJoin** (Line 170 in NetworkFeaturesService.cs)
```csharp
public Result<(bool CanJoin, string Error)> ValidateLobbyJoin(
    string lobbyId,
    string playerId,
    string? password = null)
{
    _logger.LogDebug(
        "Validating join for player {PlayerId} to lobby {LobbyId}",
        playerId, lobbyId);

    if (!_lobbies.TryGetValue(lobbyId, out var lobby))
    {
        return Result.Success<(bool, string)>((false, "Lobby not found"));
    }

    // Check if player is already in lobby
    if (lobby.Players.Any(p => p.PlayerId == playerId))
    {
        return Result.Success<(bool, string)>((false, "Already in lobby"));
    }

    // Check lobby status
    if (lobby.Status != LobbyStatus.Open)
    {
        return Result.Success<(bool, string)>((false, "Lobby is not open"));
    }

    // Check capacity
    if (lobby.CurrentPlayers >= lobby.MaxPlayers)
    {
        return Result.Success<(bool, string)>((false, "Lobby is full"));
    }

    // Check password
    if (lobby.HasPassword && lobby.Password != password)
    {
        return Result.Success<(bool, string)>((false, "Invalid password"));
    }

    // Check if player is banned
    if (lobby.BannedPlayers?.Contains(playerId) == true)
    {
        return Result.Success<(bool, string)>((false, "You are banned from this lobby"));
    }

    return Result.Success<(bool, string)>((true, string.Empty));
}
```

3. **FilterLobbies** (Line 193 in NetworkFeaturesService.cs)
```csharp
public IReadOnlyList<Lobby> FilterLobbies(LobbyFilter filter)
{
    _logger.LogDebug("Filtering lobbies with criteria");

    var query = _lobbies.Values.AsEnumerable();

    // Apply filters
    if (!string.IsNullOrEmpty(filter.GameMode))
    {
        query = query.Where(l => l.GameMode == filter.GameMode);
    }

    if (!string.IsNullOrEmpty(filter.Region))
    {
        query = query.Where(l => l.Region == filter.Region);
    }

    if (filter.HideFull)
    {
        query = query.Where(l => l.CurrentPlayers < l.MaxPlayers);
    }

    if (filter.HidePasswordProtected)
    {
        query = query.Where(l => !l.HasPassword);
    }

    if (filter.MinPlayers.HasValue)
    {
        query = query.Where(l => l.CurrentPlayers >= filter.MinPlayers.Value);
    }

    if (filter.MaxPlayers.HasValue)
    {
        query = query.Where(l => l.MaxPlayers <= filter.MaxPlayers.Value);
    }

    // Sort by relevance (open lobbies first, then by creation time)
    query = query
        .OrderByDescending(l => l.Status == LobbyStatus.Open)
        .ThenByDescending(l => l.CreatedAt);

    // Apply limit
    var results = query
        .Take(filter.Limit > 0 ? filter.Limit : 50)
        .ToList();

    _logger.LogDebug("Found {Count} lobbies matching criteria", results.Count);

    return results;
}
```

**Edge Cases:**
- Concurrent lobby creation (duplicate IDs)
- Player joining while lobby is being deleted
- Password timing attacks (use constant-time comparison)
- Region matching for optimal latency

---

### 1.3 MatchAnalytics Service (18 errors) - 35 minutes

#### File: `MatchAnalytics/Engines/ReportingEngine.cs`

**Constructor Fix:**
```csharp
// Current constructor:
public ReportingEngine(ILogger<ReportingEngine> logger)

// Required constructor (takes 3 arguments):
public ReportingEngine(
    ILogger<ReportingEngine> logger,
    ICacheService cache,
    ITimeProvider timeProvider)
{
    _logger = logger;
    _cache = cache;
    _timeProvider = timeProvider;
    _reports = new ConcurrentDictionary<string, MatchReport>();
}
```

---

#### File: `MatchAnalytics/Engines/MatchDataEngine.cs`

**Missing Methods:**

1. **ValidateMatchData**
```csharp
public Result ValidateMatchData(MatchData matchData)
{
    if (matchData == null)
        return Result.Failure("Match data is null");

    if (string.IsNullOrEmpty(matchData.MatchId))
        return Result.Failure("MatchId is required");

    if (string.IsNullOrEmpty(matchData.Player1Id) || string.IsNullOrEmpty(matchData.Player2Id))
        return Result.Failure("Both player IDs are required");

    if (matchData.Player1Id == matchData.Player2Id)
        return Result.Failure("Players must be different");

    if (matchData.StartTime == default)
        return Result.Failure("Start time is required");

    if (matchData.EndTime.HasValue && matchData.EndTime <= matchData.StartTime)
        return Result.Failure("End time must be after start time");

    return Result.Success();
}
```

2. **RecordMatch**
```csharp
public Result RecordMatch(MatchData matchData)
{
    var validation = ValidateMatchData(matchData);
    if (validation.IsFailure)
        return validation;

    _matches[matchData.MatchId] = matchData;
    
    // Update player match lists
    if (!_playerMatches.ContainsKey(matchData.Player1Id))
        _playerMatches[matchData.Player1Id] = new List<string>();
    _playerMatches[matchData.Player1Id].Add(matchData.MatchId);

    if (!_playerMatches.ContainsKey(matchData.Player2Id))
        _playerMatches[matchData.Player2Id] = new List<string>();
    _playerMatches[matchData.Player2Id].Add(matchData.MatchId);

    _logger.LogInformation("Recorded match {MatchId}", matchData.MatchId);
    return Result.Success();
}
```

3. **FindMatch**
```csharp
public Result<MatchData> FindMatch(string matchId)
{
    if (_matches.TryGetValue(matchId, out var match))
        return Result.Success(match);

    return Result.Failure<MatchData>($"Match {matchId} not found");
}
```

4. **GetPlayerMatches**
```csharp
public IReadOnlyList<MatchData> GetPlayerMatches(
    string playerId,
    int limit = 50,
    DateTime? since = null)
{
    if (!_playerMatches.TryGetValue(playerId, out var matchIds))
        return new List<MatchData>();

    var query = matchIds
        .Select(id => _matches.TryGetValue(id, out var m) ? m : null)
        .Where(m => m != null)
        .AsEnumerable();

    if (since.HasValue)
        query = query.Where(m => m.StartTime >= since.Value);

    return query
        .OrderByDescending(m => m.StartTime)
        .Take(limit)
        .ToList();
}
```

5. **GetMatchesInRange**
```csharp
public IReadOnlyList<MatchData> GetMatchesInRange(DateTime start, DateTime end)
{
    return _matches.Values
        .Where(m => m.StartTime >= start && m.StartTime <= end)
        .OrderBy(m => m.StartTime)
        .ToList();
}
```

6. **GetRecentPlayerMatches**
```csharp
public IReadOnlyList<MatchData> GetRecentPlayerMatches(string playerId, int count = 10)
{
    return GetPlayerMatches(playerId, count, DateTime.UtcNow.AddDays(-30));
}
```

---

## Phase 2: Content & Educational Services

### 2.1 MugenContentMarketplace Service (28 errors) - 45 minutes

[Additional detailed sections would continue here for all remaining services...]

---

## Common Patterns & Solutions

### Pattern: Async Method with CancellationToken

**Template:**
```csharp
public async Task<Result<T>> MethodNameAsync(
    TParam param,
    CancellationToken ct = default)
{
    _logger.LogInformation("Starting operation");

    try
    {
        ct.ThrowIfCancellationRequested();

        // Operation logic
        var result = await DoSomethingAsync(ct);

        ct.ThrowIfCancellationRequested();

        return Result.Success(result);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Operation was cancelled");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed");
        return Result.Failure<T>($"Operation failed: {ex.Message}");
    }
}
```

### Pattern: Dictionary-based Storage with Thread Safety

**Template:**
```csharp
private readonly ConcurrentDictionary<string, T> _items = new();

public Result Store(T item)
{
    if (item == null)
        return Result.Failure("Item cannot be null");

    _items[item.Id] = item;
    return Result.Success();
}

public Result<T> Get(string id)
{
    return _items.TryGetValue(id, out var item)
        ? Result.Success(item)
        : Result.Failure<T>($"Item {id} not found");
}
```

### Pattern: Result Type Conversion

**For methods returning tuples:**
```csharp
// Service expects:
var (canJoin, error) = engine.ValidateLobbyJoin(...);

// Engine returns:
public Result<(bool CanJoin, string Error)> ValidateLobbyJoin(...)
{
    return Result.Success((true, string.Empty));
}
```

---

## Testing & Verification

### Build Verification Command
```bash
# Build Application project
dotnet build src/SaveState.Application/SaveState.Application.csproj --no-restore 2>&1 | grep -E "(error|warning|Build succeeded|Build FAILED)"

# Count remaining errors
dotnet build src/SaveState.Application/SaveState.Application.csproj 2>&1 | grep ": error" | wc -l
```

### Smoke Test Checklist
- [ ] All engine constructors can be instantiated
- [ ] All public methods have valid signatures
- [ ] No null reference exceptions in common paths
- [ ] Async methods properly handle cancellation

---

*Document Version: 1.0*  
*Last Updated: February 13, 2026*


---

## Phase 2: Content & Educational Services (Continued)

### 2.1 MugenContentMarketplace Service (28 errors) - 45 minutes

#### File: `ContentMarketplace/Engines/ListingEngine.cs`

**Constructor Fix:**
```csharp
// Required constructor:
public ListingEngine(
    ILogger<ListingEngine> logger,
    ICacheService cache)
{
    _logger = logger;
    _cache = cache;
    _listings = new ConcurrentDictionary<string, ContentListing>();
    _featuredListings = new List<string>();
    _categoryIndex = new ConcurrentDictionary<string, List<string>>();
}
```

**Missing Methods:**

1. **GetFeaturedContentAsync**
```csharp
public async Task<IReadOnlyList<ContentListing>> GetFeaturedContentAsync(
    int count = 10,
    CancellationToken ct = default)
{
    _logger.LogDebug("Retrieving {Count} featured content items", count);

    var featured = _featuredListings
        .Take(count)
        .Select(id => _listings.TryGetValue(id, out var listing) ? listing : null)
        .Where(l => l != null && l.Status == ListingStatus.Active)
        .ToList();

    return await Task.FromResult(featured);
}
```

2. **GetContentByCategoryAsync**
```csharp
public async Task<IReadOnlyList<ContentListing>> GetContentByCategoryAsync(
    string category,
    ContentFilter? filter = null,
    CancellationToken ct = default)
{
    _logger.LogDebug("Retrieving content for category {Category}", category);

    if (!_categoryIndex.TryGetValue(category, out var listingIds))
    {
        return await Task.FromResult(new List<ContentListing>());
    }

    var query = listingIds
        .Select(id => _listings.TryGetValue(id, out var l) ? l : null)
        .Where(l => l != null && l.Status == ListingStatus.Active)
        .AsEnumerable();

    // Apply filters
    if (filter != null)
    {
        if (filter.MinRating.HasValue)
            query = query.Where(l => l.AverageRating >= filter.MinRating.Value);
        
        if (filter.MaxPrice.HasValue)
            query = query.Where(l => l.Price <= filter.MaxPrice.Value);
        
        if (filter.Tags?.Any() == true)
            query = query.Where(l => filter.Tags.Any(t => l.Tags.Contains(t)));
    }

    var results = query
        .OrderByDescending(l => l.CreatedAt)
        .Take(filter?.Limit ?? 50)
        .ToList();

    return await Task.FromResult(results);
}
```

3. **UploadContentAsync**
```csharp
public async Task<Result<ContentListing>> UploadContentAsync(
    string creatorId,
    ContentUploadRequest request,
    Stream contentStream,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Uploading content '{Title}' by creator {CreatorId}",
        request.Title, creatorId);

    try
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<ContentListing>("Title is required");

        if (request.Title.Length > 100)
            return Result.Failure<ContentListing>("Title must be 100 characters or less");

        // Create listing
        var listing = new ContentListing
        {
            ListingId = Guid.NewGuid().ToString(),
            CreatorId = creatorId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags ?? new List<string>(),
            Price = request.Price,
            Status = request.PublishImmediately ? ListingStatus.Active : ListingStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Version = "1.0.0",
            DownloadCount = 0,
            RatingCount = 0,
            AverageRating = 0
        };

        // Store content file
        var filePath = await StoreContentFileAsync(listing.ListingId, contentStream, ct);
        listing.FilePath = filePath;
        listing.FileSize = contentStream.Length;

        // Save listing
        _listings[listing.ListingId] = listing;
        
        // Update category index
        if (!_categoryIndex.ContainsKey(listing.Category))
            _categoryIndex[listing.Category] = new List<string>();
        _categoryIndex[listing.Category].Add(listing.ListingId);

        _logger.LogInformation(
            "Content uploaded successfully with ID {ListingId}", listing.ListingId);

        return Result.Success(listing);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to upload content");
        return Result.Failure<ContentListing>($"Upload failed: {ex.Message}");
    }
}
```

4. **GetItem**
```csharp
public Result<ContentListing> GetItem(string listingId)
{
    if (_listings.TryGetValue(listingId, out var listing))
        return Result.Success(listing);

    return Result.Failure<ContentListing>($"Listing {listingId} not found");
}
```

**Edge Cases:**
- Duplicate content detection (hash comparison)
- Storage quota exceeded
- Invalid file formats
- Concurrent uploads with same title

---

#### File: `ContentMarketplace/Engines/PurchaseEngine.cs`

**Constructor Fix:**
```csharp
public PurchaseEngine(
    ILogger<PurchaseEngine> logger,
    IUserAccountService userService,
    IPaymentGateway paymentGateway,
    INotificationService notificationService)
{
    _logger = logger;
    _userService = userService;
    _paymentGateway = paymentGateway;
    _notificationService = notificationService;
    _purchases = new ConcurrentDictionary<string, PurchaseRecord>();
    _userLibraries = new ConcurrentDictionary<string, List<string>>();
}
```

**Missing Methods:**

1. **PurchaseContentAsync**
```csharp
public async Task<Result<PurchaseResult>> PurchaseContentAsync(
    string userId,
    string listingId,
    PaymentMethod paymentMethod,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Processing purchase of listing {ListingId} by user {UserId}",
        listingId, userId);

    try
    {
        // Check if already purchased
        if (HasPurchased(userId, listingId))
        {
            return Result.Failure<PurchaseResult>("Content already in library");
        }

        // Get listing details
        var listing = await GetListingAsync(listingId, ct);
        if (listing == null)
        {
            return Result.Failure<PurchaseResult>("Listing not found");
        }

        // Check if free
        if (listing.Price == 0)
        {
            return await ProcessFreePurchaseAsync(userId, listing);
        }

        // Process payment
        var paymentResult = await _paymentGateway.ProcessPaymentAsync(
            userId, listing.Price, paymentMethod, ct);

        if (!paymentResult.Success)
        {
            return Result.Failure<PurchaseResult>(
                $"Payment failed: {paymentResult.ErrorMessage}");
        }

        // Record purchase
        var purchase = new PurchaseRecord
        {
            PurchaseId = Guid.NewGuid().ToString(),
            UserId = userId,
            ListingId = listingId,
            Price = listing.Price,
            PaymentMethod = paymentMethod,
            TransactionId = paymentResult.TransactionId,
            PurchasedAt = DateTime.UtcNow
        };

        _purchases[purchase.PurchaseId] = purchase;

        // Add to user library
        if (!_userLibraries.ContainsKey(userId))
            _userLibraries[userId] = new List<string>();
        _userLibraries[userId].Add(listingId);

        // Send notification
        await _notificationService.NotifyPurchaseCompleteAsync(userId, listing, ct);

        return Result.Success(new PurchaseResult
        {
            Success = true,
            PurchaseId = purchase.PurchaseId,
            TransactionId = paymentResult.TransactionId,
            DownloadUrl = GenerateDownloadUrl(listingId, purchase.PurchaseId)
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Purchase failed");
        return Result.Failure<PurchaseResult>($"Purchase failed: {ex.Message}");
    }
}
```

2. **HasPurchased**
```csharp
public bool HasPurchased(string userId, string listingId)
{
    if (!_userLibraries.TryGetValue(userId, out var library))
        return false;

    return library.Contains(listingId);
}
```

**Edge Cases:**
- Payment gateway timeout
- Double-charge prevention
- Refund processing
- Content removed after purchase (keep in library)

---

### 2.2 EducationalContent Service (12 errors) - 30 minutes

#### File: `Educational/Engines/ContentEngine.cs`

**Missing Methods:**

1. **QueryTutorials**
```csharp
public IReadOnlyList<Tutorial> QueryTutorials(TutorialQuery query)
{
    _logger.LogDebug("Querying tutorials with filters");

    var results = _tutorials.Values.AsEnumerable();

    if (!string.IsNullOrEmpty(query.Difficulty))
        results = results.Where(t => t.Difficulty == query.Difficulty);

    if (!string.IsNullOrEmpty(query.Character))
        results = results.Where(t => t.RelatedCharacters.Contains(query.Character));

    if (query.Tags?.Any() == true)
        results = results.Where(t => query.Tags.Any(tag => t.Tags.Contains(tag)));

    if (query.MinDuration.HasValue)
        results = results.Where(t => t.Duration >= query.MinDuration.Value);

    if (query.MaxDuration.HasValue)
        results = results.Where(t => t.Duration <= query.MaxDuration.Value);

    return results
        .OrderByDescending(t => t.CreatedAt)
        .Take(query.Limit > 0 ? query.Limit : 20)
        .ToList();
}
```

2. **GetTutorial**
```csharp
public Result<Tutorial> GetTutorial(string tutorialId)
{
    if (_tutorials.TryGetValue(tutorialId, out var tutorial))
    {
        // Track view
        tutorial.ViewCount++;
        tutorial.LastAccessedAt = DateTime.UtcNow;
        return Result.Success(tutorial);
    }

    return Result.Failure<Tutorial>($"Tutorial {tutorialId} not found");
}
```

---

## Phase 3: Advanced Feature Services

### 3.1 NarrativeMemory Service (10 errors) - 25 minutes

#### File: `NarrativeMemory/Engines/CrystalEngine.cs`

**Missing Methods:**

1. **GenerateCrystalAsync**
```csharp
public async Task<Result<MemoryCrystal>> GenerateCrystalAsync(
    string playerId,
    MatchHighlight highlight,
    CrystalOptions options,
    CancellationToken ct = default)
{
    _logger.LogInformation(
        "Generating memory crystal for player {PlayerId} from highlight {HighlightId}",
        playerId, highlight.HighlightId);

    try
    {
        // Generate unique crystal ID
        var crystalId = GenerateCrystalId(highlight);

        var crystal = new MemoryCrystal
        {
            CrystalId = crystalId,
            PlayerId = playerId,
            CreatedAt = DateTime.UtcNow,
            MatchId = highlight.MatchId,
            HighlightId = highlight.HighlightId,
            Title = options.CustomTitle ?? GenerateTitle(highlight),
            Description = options.CustomDescription ?? GenerateDescription(highlight),
            GameMoment = highlight.Timestamp,
            Characters = highlight.CharactersInvolved,
            Significance = CalculateSignificance(highlight),
            VisualStyle = options.VisualStyle,
            Effects = options.Effects ?? new List<CrystalEffect>(),
            Metadata = new CrystalMetadata
            {
                OriginalVideoUrl = highlight.VideoUrl,
                ThumbnailUrl = highlight.ThumbnailUrl,
                Duration = highlight.Duration,
                Quality = options.Quality
            }
        };

        // Apply visual effects
        await ApplyVisualEffectsAsync(crystal, ct);

        // Store crystal
        _crystals[crystalId] = crystal;

        // Update player collection
        if (!_playerCrystals.ContainsKey(playerId))
            _playerCrystals[playerId] = new List<string>();
        _playerCrystals[playerId].Add(crystalId);

        _logger.LogInformation("Crystal {CrystalId} generated successfully", crystalId);

        return Result.Success(crystal);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to generate crystal");
        return Result.Failure<MemoryCrystal>($"Generation failed: {ex.Message}");
    }
}
```

**Edge Cases:**
- Duplicate crystal generation (same highlight)
- Storage limit reached
- Invalid visual style selection
- Corrupted highlight data

---

### 3.2 MobileCompanion Service (45 errors) - 90 minutes

This service has the most errors. Most are model property issues.

#### File: `MobileCompanion/Models/NotificationModels.cs`

**Required Properties:**
```csharp
public class MobileNotification
{
    public string NotificationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; }  // ADD THIS
    public DateTime? ExpiresAt { get; set; }
    public bool IsRead { get; set; }  // ADD THIS
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

#### File: `MobileCompanion/Models/StreamingModels.cs`

**Required Properties:**
```csharp
public class LiveMatchData
{
    public string MatchId { get; set; } = default!;
    public string Player1Name { get; set; } = default!;  // ADD THIS
    public string Player2Name { get; set; } = default!;  // ADD THIS
    public float Player1Health { get; set; }  // ADD THIS
    public float Player2Health { get; set; }  // ADD THIS
    public int Player1Wins { get; set; }
    public int Player2Wins { get; set; }
    public int RoundNumber { get; set; }  // ADD THIS
    public TimeSpan TimeRemaining { get; set; }  // ADD THIS
    public bool IsActive { get; set; }  // ADD THIS
    public string CurrentStage { get; set; } = default!;
    public DateTime LastUpdated { get; set; }
}
```

#### File: `MobileCompanion/Models/UiModels.cs`

**Enum Extension:**
```csharp
public enum QuickActionType
{
    ViewProfile,
    SendMessage,
    ViewMatch,
    ChallengePlayer,
    // ADD THESE:
    StartMatch,
    OpenTraining,
    OpenCharacterSelect
}

public class SocialActivity
{
    public string ActivityId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string ActivityType { get; set; } = default!;  // ADD THIS
    public string Description { get; set; } = default!;  // ADD THIS
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ContentItem
{
    public string ItemId { get; set; } = default!;  // ADD THIS
    public string Title { get; set; } = default!;  // ADD THIS
    public string Description { get; set; } = default!;  // ADD THIS
    public ContentType Type { get; set; }
    public int Priority { get; set; }  // ADD THIS
    public string? ThumbnailUrl { get; set; }
    public DateTime PublishedAt { get; set; }
}
```

**Edge Cases:**
- Null property values in serialization
- Property name mismatches in JSON
- Enum value validation
- Default value handling

---

### 3.3 EmergingTechnologies Service (12 errors) - 20 minutes

#### File: `EmergingTechnologies/Engines/MotionTrackingEngine.cs`

**Type Conversion Fixes:**

```csharp
// Change from double to float literals
float smoothingFactor = 0.5f;  // Was: 0.5
float confidenceThreshold = 0.85f;  // Was: 0.85

// Vector3 conversion helper
private System.Numerics.Vector3 ConvertVector3(Vector3 appVector)
{
    return new System.Numerics.Vector3(
        (float)appVector.X,
        (float)appVector.Y,
        (float)appVector.Z);
}

private Vector3 ConvertVector3(System.Numerics.Vector3 numericsVector)
{
    return new Vector3(
        numericsVector.X,
        numericsVector.Y,
        numericsVector.Z);
}

// Add extension methods for custom Vector3
public static class Vector3Extensions
{
    public static float Length(this Vector3 v)
    {
        return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
    }

    public static Vector3 Normalize(this Vector3 v)
    {
        var length = v.Length();
        if (length == 0)
            return new Vector3(0, 0, 0);
        
        return new Vector3(
            v.X / length,
            v.Y / length,
            v.Z / length);
    }
}
```

**Edge Cases:**
- Division by zero in normalization
- Floating point precision errors
- Coordinate system mismatches (left/right handed)
- Quaternion to Euler angle conversion gimbal lock

---

## Phase 4: Final Cleanup

### 4.1 BalanceTuning Service (1 error) - 5 minutes

#### File: `BalanceTuning/Engines/MonitoringEngine.cs`

**Type Mismatch Fix:**
```csharp
// Change from:
using SaveState.Application.Mugen.Models.BalanceTuning;

// To explicit namespace to resolve TrendDirection conflict:
public Models.BalanceTuning.TrendDirection CalculateTrend(
    float currentValue, 
    float previousValue)
{
    var change = currentValue - previousValue;
    var threshold = previousValue * 0.05f; // 5% threshold

    if (Math.Abs(change) < threshold)
        return Models.BalanceTuning.TrendDirection.Stable;
    
    return change > 0 
        ? Models.BalanceTuning.TrendDirection.Increasing 
        : Models.BalanceTuning.TrendDirection.Decreasing;
}
```

---

## Complete Implementation Checklist

### Phase 1: Core Infrastructure
- [ ] LiveSync/Engines/ConflictResolutionEngine.cs - ResolveConflictAsync
- [ ] LiveSync/Engines/SyncEngine.cs - CalculateSyncHealth, CalculateDataCompleteness, PerformSyncAsync
- [ ] LiveSync/Engines/MigrationEngine.cs - MigrateAsync
- [ ] NetworkFeatures/Engines/LobbyEngine.cs - CreateLobby, ValidateLobbyJoin, FilterLobbies
- [ ] NetworkFeatures/Engines/SpectatorEngine.cs - ValidateSpectateRequest, CreateSpectatorSession
- [ ] NetworkFeatures/Engines/MatchmakingEngine.cs - FindOpponentAsync
- [ ] MatchAnalytics/Engines/ReportingEngine.cs - Constructor fix
- [ ] MatchAnalytics/Engines/MatchDataEngine.cs - ValidateMatchData, RecordMatch, FindMatch, GetPlayerMatches, GetMatchesInRange, GetRecentPlayerMatches
- [ ] MatchAnalytics/Engines/PatternEngine.cs - AnalyzeMatchAsync, IdentifyPatternsAsync
- [ ] MatchAnalytics/Engines/StatisticEngine.cs - CalculatePlayerStatisticsAsync
- [ ] MatchAnalytics/Engines/VisualizationEngine.cs - PrepareTrendVisualization

### Phase 2: Content & Educational
- [ ] ContentMarketplace/Engines/ListingEngine.cs - Constructor, GetFeaturedContentAsync, GetContentByCategoryAsync, GetContentDetailsAsync, UploadContentAsync, GetItem, InitializeSampleContent
- [ ] ContentMarketplace/Engines/PurchaseEngine.cs - Constructor, PurchaseContentAsync, DownloadContentAsync, GetUserLibraryAsync, VerifyContentAccess, HasPurchased
- [ ] ContentMarketplace/Engines/ReviewEngine.cs - RateContentAsync, GetContentReviewsAsync, SubmitReviewAsync
- [ ] ContentMarketplace/Engines/SearchEngine.cs - SearchContentAsync, AdvancedSearchAsync
- [ ] ContentMarketplace/Engines/AnalyticsEngine.cs - GetCreatorDashboardAsync, GetMarketplaceStatsAsync, GetSalesMetricsAsync, GetTrendingContentAsync
- [ ] Educational/Engines/ContentEngine.cs - QueryTutorials, GetTutorial, QueryStrategyGuides, GetStrategyGuide, GetMechanicsGuide
- [ ] Educational/Engines/LearningPathEngine.cs - GetLearningPath
- [ ] Educational/Engines/ProgressEngine.cs - GetUserProgressAsync, CalculateCategoryProgress
- [ ] Educational/Engines/AssessmentEngine.cs - CreatePracticeSessionAsync, AnalyzeMatchAsync

### Phase 3: Advanced Features
- [ ] NarrativeMemory/Engines/CrystalEngine.cs - GenerateCrystalAsync, EnhanceCrystalAsync
- [ ] NarrativeMemory/Engines/TimelineEngine.cs - CreateAlternateTimelineAsync, ReplayTimelineAsync
- [ ] NarrativeMemory/Engines/SynthesisEngine.cs - SynthesizeMoveAsync
- [ ] NarrativeMemory/Engines/ButterflyEngine.cs - TriggerEffectAsync
- [ ] MobileCompanion/Models/NotificationModels.cs - Add IsRead, CreatedAt properties
- [ ] MobileCompanion/Models/StreamingModels.cs - Add Player1Name, Player2Name, Player1Health, Player2Health, RoundNumber, TimeRemaining, IsActive
- [ ] MobileCompanion/Models/UiModels.cs - Add enum values, ActivityType, Description, ItemId, Title, Description, Priority
- [ ] MobileCompanion/Engines/* - Fix property references
- [ ] EmergingTechnologies/Engines/MotionTrackingEngine.cs - Fix float literals, Vector3 conversions

### Phase 4: Cleanup
- [ ] BalanceTuning/Engines/MonitoringEngine.cs - Fix TrendDirection namespace

---

## Error Resolution Patterns Summary

### Constructor Mismatches
**Pattern:** Service instantiates engine with N arguments, engine only has constructor with M arguments.

**Solution:** Add the required constructor parameters or use default values.

```csharp
// Before:
public Engine(ILogger<Engine> logger) { }

// After:
public Engine(
    ILogger<Engine> logger,
    ICacheService cache,
    ITimeProvider timeProvider) 
{ 
    _logger = logger;
    _cache = cache;
    _timeProvider = timeProvider;
}
```

### Missing Methods
**Pattern:** Service calls engine.MethodName() but method doesn't exist.

**Solution:** Add the method with the exact signature expected by the service.

### Missing Model Properties
**Pattern:** Engine accesses model.Property but property doesn't exist.

**Solution:** Add the property to the model class with appropriate type.

```csharp
// Add to model class:
public PropertyType PropertyName { get; set; } = default!;
```

### Type Conversion Issues
**Pattern:** Cannot convert TypeA to TypeB.

**Solution:** Add explicit conversion methods or use correct literal suffixes.

```csharp
// Float literal:
float value = 0.5f;  // Not 0.5

// Vector conversion:
public static TargetType Convert(SourceType source) { }
```

---

## Post-Implementation Verification

### Build Commands
```bash
# Full build with error count
dotnet build src/SaveState.Application/SaveState.Application.csproj 2>&1 | tail -20

# Check specific error categories
dotnet build src/SaveState.Application/SaveState.Application.csproj 2>&1 | grep "CS1061" | head -10  # Missing method
dotnet build src/SaveState.Application/SaveState.Application.csproj 2>&1 | grep "CS0117" | head -10  # Missing property
dotnet build src/SaveState.Application/SaveState.Application.csproj 2>&1 | grep "CS1729" | head -10  # Constructor mismatch
```

### Expected Results
- CS1061 errors: 0
- CS0117 errors: 0
- CS1729 errors: 0
- Total errors: 0
- Warnings: < 50 (existing)

### Success Criteria
- [ ] `dotnet build` completes with 0 errors
- [ ] No new warnings introduced
- [ ] All engine constructors accept required parameters
- [ ] All service-engine interactions compile successfully
- [ ] Model classes have all required properties

---

*Document Version: 2.0*  
*Last Updated: February 13, 2026*
