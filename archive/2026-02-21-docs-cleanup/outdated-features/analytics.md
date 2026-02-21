# 📊 Analytics & Statistics

**Status**: ✅ Implemented
**Last Updated**: January 2, 2026
**Layer**: Core + Application + Infrastructure + Presentation
**Related**: [Game Library](game-library.md), [Achievements](achievements.md)

---

## Overview

Analytics tracks your gaming habits with detailed statistics and visualizations.

### Key Features

- **Playtime Tracking**: Session-by-session time logging
- **Gaming Heatmap**: Visualize when you play
- **Trends Analysis**: Weekly/monthly comparisons
- **Top Games**: Most played games leaderboard
- **Goal Tracking**: Set and monitor gaming goals

## UI Components (Phase 4 Complete)

### Dashboard Widgets

| Widget | Data | Status |
|--------|------|--------|
| Today's Stats | Playtime, sessions, achievements | ✅ |
| Activity Feed | Recent gaming activities | ✅ |
| Goals Progress | Achievement tracking | ✅ |
| Weekly Trends | Chart with % changes | ✅ |

### Analytics Tab

- **Statistics Cards**: Total playtime, streak, active days
- **Gaming Heatmap**: Year view with intensity colors
- **Top 10 Games**: Hours played leaderboard
- **Time Distribution**: By day of week and hour

## Architecture

### Domain Model

```csharp
public class GameSession : EntityBase
{
    public Guid GameId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public TimeSpan Duration => EndTime - StartTime ?? TimeSpan.Zero;
}
```

### Services

```csharp
public interface IAnalyticsService
{
    Task<PlaytimeStats> GetPlaytimeStatsAsync(DateRange range, CancellationToken ct);
    Task<IReadOnlyList<TopGame>> GetTopGamesAsync(int count, CancellationToken ct);
    Task<HeatmapData> GetHeatmapDataAsync(int year, CancellationToken ct);
}
```

## Implementation Files

| Component | File |
|-----------|------|
| Service | `Infrastructure/Analytics/AnalyticsService.cs` |
| Repository | `Infrastructure/Analytics/GameSessionRepository.cs` |
| ViewModel | `Presentation/ViewModels/AnalyticsViewModel.cs` |

## Data Points Tracked

| Metric | Description | Resolution |
|--------|-------------|------------|
| Session Duration | Time per gaming session | Per session |
| Daily Playtime | Total time per day | Daily aggregate |
| Launch Count | Times game launched | Per game |
| Active Days | Days with gaming activity | Daily |
| Streak | Consecutive active days | Daily |

## Configuration

```json
{
  "Analytics": {
    "SessionTimeout": "00:30:00",
    "AutoTrack": true,
    "RetentionDays": 365
  }
}
```

## API Usage

```csharp
// Get weekly trends
var stats = await analyticsService.GetPlaytimeStatsAsync(
    DateRange.LastWeek,
    ct
);

// Get heatmap for 2026
var heatmap = await analyticsService.GetHeatmapDataAsync(2026, ct);
```

---

**Related**: [Game Library](game-library.md), [Achievements](achievements.md)
