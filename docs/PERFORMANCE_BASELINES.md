# Performance Baselines

This document defines the performance targets and baselines for SaveStateReborn.

## Overview

Performance is a critical aspect of SaveStateReborn user experience. These baselines ensure the application remains responsive and efficient across various usage scenarios.

## Startup Performance

| Metric | Target | Maximum | Notes |
|--------|--------|---------|-------|
| Cold Start | < 3s | 5s | First launch after system boot |
| Warm Start | < 1s | 2s | Subsequent launches |
| Database Initialization | < 500ms | 1s | Migrations and seeding |
| Service Registration | < 100ms | 200ms | DI container setup |
| Cache Warmup (5K games) | < 200ms | 500ms | Initial data loading |

### Measurement Method

```csharp
[Fact]
public async Task ColdStartup_ShouldCompleteWithinBaseline()
{
    var stopwatch = Stopwatch.StartNew();
    await StartApplicationAsync();
    stopwatch.Stop();
    
    stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
}
```

## UI Performance

| Metric | Target | Minimum | Notes |
|--------|--------|---------|-------|
| Page Navigation | < 200ms | 500ms | Time to render new page |
| List Initialization (1K items) | < 100ms | 200ms | Initial render with virtualization |
| List Scrolling | > 55 FPS | 30 FPS | Smooth scrolling experience |
| Search (as-you-type) | < 50ms | 100ms | Debounced search response |
| Filter Application | < 100ms | 200ms | Apply complex filters |
| Animation Frame Budget | 16.67ms | 33ms | 60 FPS target |
| Dialog Open | < 100ms | 200ms | Modal/dialog transitions |
| Context Menu | < 50ms | 100ms | Right-click menu display |

### Measurement Method

```csharp
[Fact]
public void ListScrolling_ShouldMaintainTargetFPS()
{
    var fpsCounter = new FpsCounter();
    SimulateScrollOperations(fpsCounter, duration: TimeSpan.FromSeconds(5));
    
    fpsCounter.AverageFps.Should().BeGreaterThan(55);
}
```

## Memory Usage

| Scenario | Target | Maximum | Notes |
|----------|--------|---------|-------|
| Base Application | < 150MB | 200MB | After startup, no games loaded |
| With Library (10K games) | < 300MB | 500MB | Full library cached |
| With Browser (5 tabs) | < 400MB | 600MB | Web browser active |
| During Game Recording | < 1GB | 1.5GB | Video capture in progress |
| Peak (Game Running) | < 1.5GB | 2GB | All features active |
| Save State Creation | +50MB | +100MB | Temporary increase |
| Long-running (24h) | < 2x base | 3x base | No memory leaks |

### Memory Profiling

```csharp
[Fact]
public void GameLibraryMemory_10KGames()
{
    var profiler = new MemoryProfiler();
    profiler.Start();
    
    var games = GenerateGames(10000);
    var cache = BuildCache(games);
    
    profiler.Stop();
    profiler.MemoryUsed.Should().BeLessThan(500 * 1024 * 1024);
}
```

## Database Performance

| Operation | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Query 1K games | < 50ms | 100ms | Simple filter |
| Query 10K games | < 100ms | 200ms | With joins |
| Search (indexed) | < 50ms | 100ms | Full-text search |
| Search (non-indexed) | < 200ms | 500ms | Complex filter |
| Insert 100 games | < 100ms | 200ms | Batch insert |
| Update single game | < 20ms | 50ms | Single row |
| Delete with cascade | < 100ms | 200ms | With related data |
| Connection Open | < 10ms | 20ms | Pool acquisition |

### Query Benchmarks

```csharp
[Benchmark]
public List<Game> Query_1000Games()
{
    return _dbContext.Games
        .Where(g => g.Status == GameStatus.Installed)
        .Take(1000)
        .ToList();
}
```

## Save State Operations

| Operation | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Create Save State | < 50ms | 100ms | Without compression |
| Create with Compression | < 200ms | 500ms | GZip level 5 |
| Load Save State | < 30ms | 50ms | From SSD |
| Load with Decompression | < 100ms | 200ms | GZip decompression |
| Branch Creation | < 20ms | 50ms | Tree node creation |
| Tree Traversal (100 nodes) | < 10ms | 20ms | DFS/BFS |
| Export Save State | < 500ms | 1s | To disk |
| Import Save State | < 300ms | 500ms | From disk |

### Save State Benchmarks

```csharp
[Benchmark]
public SaveStateNode CreateSaveState()
{
    return new SaveStateNode
    {
        Id = Guid.NewGuid(),
        Name = "Quick Save",
        Data = _saveData,
        CreatedAt = DateTime.UtcNow
    };
}
```

## Cloud Sync Performance

| Operation | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Sync Metadata | < 1s | 3s | Game list sync |
| Upload 10MB | < 5s | 10s | Save state upload |
| Download 10MB | < 3s | 5s | Save state download |
| Conflict Resolution | < 100ms | 500ms | Auto-merge |
| Full Sync (100 saves) | < 30s | 60s | Initial sync |
| Delta Sync | < 5s | 10s | Incremental sync |

## Search Performance

| Scenario | Target | Maximum | Notes |
|----------|--------|---------|-------|
| Simple Search | < 50ms | 100ms | Single term |
| Multi-term Search | < 100ms | 200ms | AND/OR operators |
| Fuzzy Search | < 200ms | 500ms | Levenshtein distance |
| Facet Aggregation | < 100ms | 200ms | Category counts |
| Autocomplete | < 20ms | 50ms | Prefix matching |
| Index Build (10K) | < 2s | 5s | Full-text index |

### Search Benchmarks

```csharp
[Benchmark]
public List<Game> Search_Indexed()
{
    return _searchIndex["elden"];
}
```

## Concurrency Performance

| Scenario | Target | Maximum | Notes |
|----------|--------|---------|-------|
| 10 Concurrent Users | < 100ms avg | 200ms | Response time |
| 50 Concurrent Users | < 200ms avg | 500ms | Response time |
| 100 Concurrent Users | < 500ms avg | 1s | Response time |
| Deadlock Rate | 0% | < 1% | Across all operations |
| Thread Pool Saturation | < 80% | 95% | Under load |
| Lock Contention | < 1% | 5% | Time spent waiting |

### Load Test

```csharp
[Fact(Skip = "Long-running load test")]
public async Task ConcurrentUsers_LoadTest()
{
    var result = await LoadTestingFramework.RunLoadTestAsync(
        operation: () => _gameService.SearchAsync("test"),
        concurrentOperations: 50,
        totalOperations: 1000,
        timeout: TimeSpan.FromMinutes(5));
    
    result.SuccessRate.Should().BeGreaterThan(95);
    result.AverageDuration.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
}
```

## File System Operations

| Operation | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Scan Directory (1K files) | < 100ms | 200ms | File enumeration |
| Copy 100MB | < 2s | 5s | Local SSD to SSD |
| Move 100MB | < 500ms | 1s | Same volume |
| Delete 100 files | < 100ms | 200ms | Batch delete |
| File Watch Event | < 10ms | 50ms | Change notification |

## Network Operations

| Operation | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| HTTP GET (API) | < 200ms | 500ms | Steam API call |
| HTTP POST (small) | < 100ms | 300ms | Telemetry |
| HTTP POST (1MB) | < 2s | 5s | Image upload |
| WebSocket Message | < 50ms | 100ms | Real-time sync |
| DNS Resolution | < 50ms | 100ms | Cached |
| TLS Handshake | < 200ms | 500ms | New connection |

## Platform-Specific Targets

### Windows
- All baselines apply as primary targets
- Native memory reading: < 1ms per operation
- Full feature set enabled

### Linux/Steam Deck
- Startup: +50% tolerance (slow storage)
- UI: Same targets as Windows
- Memory reading: < 10ms per operation
- Value freezing: +10x tolerance (stutter expected)

### macOS
- Startup: +50% tolerance
- Memory reading only: No writing targets
- Limited memory modification support

## Performance Regression Thresholds

| Metric | Warning | Critical |
|--------|---------|----------|
| Startup Time | +20% | +50% |
| Memory Usage | +20% | +50% |
| Query Time | +30% | +100% |
| UI FPS | -10% | -25% |
| Test Duration | +20% | +50% |

## Monitoring and Alerting

### Automated Benchmarks
- Run on every PR (fast benchmarks only)
- Run nightly (full benchmark suite)
- Compare against baseline branch
- Alert on regression > 20%

### Performance Dashboard
- Startup times by version
- Memory usage trends
- Query performance heatmaps
- UI frame time distributions

## Testing Environment

All baselines measured on:

**Reference Hardware:**
- CPU: AMD Ryzen 5 5600X / Intel Core i5-11400
- RAM: 16GB DDR4-3200
- Storage: NVMe SSD (3500 MB/s read)
- GPU: NVIDIA GTX 1660 / AMD RX 5600 XT

**Software:**
- OS: Windows 11 23H2 / Ubuntu 22.04 LTS
- .NET: 9.0.x
- Database: SQLite with WAL mode

## Continuous Improvement

Performance baselines are reviewed quarterly and updated based on:
- User feedback and telemetry
- New feature requirements
- Hardware evolution
- Competitor benchmarks

Last Updated: February 22, 2026
