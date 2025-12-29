# Achievement System

SaveState Reborn includes a comprehensive achievement system for tracking player progress and milestones.

## Overview

The achievement system provides:

- **Progress Tracking**: Monitor advancement toward goals
- **Unlock Rewards**: Celebrate completed achievements
- **Persistent Storage**: Save progress across sessions
- **Multiple Categories**: Game completion, play time, collection goals

## Architecture

### Domain Model

```csharp
// Achievement definition
public class Achievement : EntityBase
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string IconPath { get; private set; }
    public int Points { get; private set; }
    public AchievementType Type { get; private set; }
    public string? Criteria { get; private set; }
}

// User progress tracking
public class UserAchievement : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid AchievementId { get; private set; }
    public int Progress { get; private set; }
    public bool IsUnlocked { get; private set; }
    public DateTime? UnlockedAt { get; private set; }
}
```

### Achievement Types

```csharp
public enum AchievementType
{
    GameCompletion,    // Complete specific games
    PlayTime,         // Accumulate play time
    Collection,       // Build game collections
    Social,           // Multiplayer achievements
    Technical,        // Technical milestones
    Exploration       // Discovery achievements
}
```

## API Usage

### Creating Achievements

```csharp
var createCommand = new CreateAchievementCommand(
    Name: "First Victory",
    Description: "Win your first fighting game match",
    IconPath: "icons/first_win.png",
    Points: 10,
    Type: AchievementType.Social
);

var achievementId = await mediator.Send(createCommand);
```

### Updating Progress

```csharp
var updateCommand = new UpdateUserAchievementProgressCommand(
    UserId: userId,
    AchievementType: AchievementType.Social,
    ProgressIncrement: 1
);

await mediator.Send(updateCommand);
```

### Querying Achievements

```csharp
var query = new GetUserAchievementsQuery(
    UserId: userId,
    IncludeLocked: true
);

var achievements = await mediator.Send(query);
```

## Achievement Examples

### Fighting Game Achievements

| Name | Description | Type | Points |
|:---|:---|:---|:---|
| First Blood | Win your first match | Social | 10 |
| Combo Master | Perform a 10-hit combo | Technical | 25 |
| Character Collector | Unlock 10 characters | Collection | 50 |
| Training Warrior | Spend 1 hour in training | PlayTime | 30 |
| Versus Veteran | Win 100 matches | Social | 100 |

### Game Library Achievements

| Name | Description | Type | Points |
|:---|:---|:---|:---|
| Library Builder | Add 50 games to library | Collection | 75 |
| Completionist | Complete 10 games | GameCompletion | 100 |
| Genre Explorer | Play 5 different genres | Exploration | 40 |
| Time Traveler | Play games from 5 decades | Exploration | 60 |

## Progress Calculation

### Automatic Progress Tracking

The system automatically tracks progress for:

- **Match Victories**: Increment social achievement progress
- **Training Time**: Accumulate play time statistics
- **Character Unlocks**: Track collection progress
- **Game Completions**: Monitor completion status

### Manual Progress Updates

```csharp
// Award achievement points manually
await achievementService.UpdateUserAchievementProgressAsync(
    userId,
    AchievementType.Collection,
    progressIncrement: 1,
    metadata: "Unlocked Ryu character"
);
```

## UI Integration

### Achievement Display

```xml
<!-- Achievement Card -->
<Border Background="{StaticResource SurfaceBrush}"
        CornerRadius="8"
        Margin="8"
        Opacity="{Binding IsUnlocked, Converter={StaticResource BoolToOpacity}}">
    <Grid>
        <Image Source="{Binding IconPath}" Width="64" Height="64" />
        <TextBlock Text="{Binding Name}" FontWeight="Bold" />
        <TextBlock Text="{Binding Description}" Opacity="0.8" />
        <ProgressBar Value="{Binding ProgressPercentage}" Maximum="100" />
    </TextBlock>
</Border>
```

### Notification System

```csharp
// Show achievement unlocked notification
if (userAchievement.IsUnlocked && !wasPreviouslyUnlocked)
{
    await notificationService.ShowAchievementNotificationAsync(
        userAchievement.Achievement.Name,
        userAchievement.Achievement.Description,
        userAchievement.Achievement.Points
    );
}
```

## Database Schema

```sql
-- Achievement definitions
CREATE TABLE Achievements (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    IconPath NVARCHAR(255) NOT NULL,
    Points INT NOT NULL,
    Type INT NOT NULL,
    Criteria NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

-- User achievement progress
CREATE TABLE UserAchievements (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    AchievementId UNIQUEIDENTIFIER NOT NULL,
    Progress INT NOT NULL DEFAULT 0,
    IsUnlocked BIT NOT NULL DEFAULT 0,
    UnlockedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (AchievementId) REFERENCES Achievements(Id)
);
```

## Configuration

### Achievement Settings

```json
{
  "achievements": {
    "autoTrack": true,
    "showNotifications": true,
    "allowMultipleUnlocks": false,
    "pointsMultiplier": 1.0
  }
}
```

## Testing

### Achievement Service Tests

```csharp
[Fact]
public async Task Should_Unlock_Achievement_When_Progress_Complete()
{
    // Arrange
    var service = new AchievementService(repository);

    // Act
    await service.UpdateUserAchievementProgressAsync(
        userId, AchievementType.Social, 10);

    // Assert
    var achievements = await service.GetUserAchievementsAsync(userId);
    achievements.Should().Contain(a => a.IsUnlocked);
}
```

## Performance Considerations

- **Lazy Loading**: Achievement data loaded on-demand
- **Batch Updates**: Progress updates batched for efficiency
- **Indexing**: Database indexes on UserId and AchievementId
- **Caching**: Frequently accessed achievements cached

## Extensibility

### Custom Achievement Types

```csharp
public class CustomAchievementType
{
    public string Name { get; set; }
    public Func<UserAction, int> ProgressCalculator { get; set; }
    public Func<int, bool> UnlockCondition { get; set; }
}
```

### Achievement Plugins

```csharp
public interface IAchievementPlugin
{
    string PluginName { get; }
    IEnumerable<Achievement> GetAchievements();
    Task UpdateProgressAsync(UserAction action);
}
```
