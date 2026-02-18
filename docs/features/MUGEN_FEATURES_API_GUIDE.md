# MUGEN Features API Guide

Complete API reference for MUGEN/IKEMEN platform features.

**Version:** 2.4.0  
**Last Updated:** February 2026

---

## Table of Contents

1. [Character Fusion](#character-fusion)
2. [Death Battle](#death-battle)
3. [AI Battle Analyzer](#ai-battle-analyzer)
4. [Frame Data Viewer](#frame-data-viewer)
5. [RetroAchievements](#retroachievements)
6. [Cloud Sync](#cloud-sync)

---

## Character Fusion

DBZ/Vegito-style character fusion system.

### Fuse Characters

```csharp
var result = await mediator.Send(new FuseCharactersCommand(
    Parent1Id: Guid.Parse("..."),
    Parent2Id: Guid.Parse("..."),
    CustomName: "Vegito", // Optional
    FusionType: FusionType.Potara, // Potara, FusionDance, DNAFusion, Custom
    Customization: new FusionCustomizationOptions {
        Parent1StatPercentage = 50,
        PrimaryColor = "#FF0000"
    }));

if (result.IsSuccess) {
    Console.WriteLine($"Created: {result.Value.Name}");
    Console.WriteLine($"Power Level: {result.Value.Stats.PowerLevel}");
    Console.WriteLine($"Tier: {result.Value.Stats.Tier}");
}
```

### Analyze Fusion Potential

```csharp
var analysis = await mediator.Send(new AnalyzeFusionPotentialQuery(
    Parent1Id: gokuId,
    Parent2Id: vegetaId));

Console.WriteLine($"Compatibility: {analysis.Value.CompatibilityScore}%");
Console.WriteLine($"Predicted Power: {analysis.Value.PredictedStats.PowerLevel}");
```

### Generate MUGEN Character Files

```csharp
var outputPath = await mediator.Send(new GenerateMugenCharacterCommand(
    FusionId: fusionId,
    OutputDirectory: "chars/"));

// Generated:
// - chars/Vegito/Vegito.def
// - chars/Vegito/Vegito.cmd
// - chars/Vegito/Vegito.cns
```

### Get Fusion Leaderboard

```csharp
var leaderboard = await mediator.Send(
    new GetFusionLeaderboardQuery(Top: 100));

foreach (var entry in leaderboard.Value) {
    Console.WriteLine($"#{entry.Rank} {entry.Name} - PL: {entry.PowerLevel}");
}
```

---

## Death Battle

YouTube-style battle simulations with research and analysis.

### Create Death Battle

```csharp
var battle = await mediator.Send(new CreateDeathBattleCommand(
    Combatant1Id: supermanId,
    Combatant2Id: gokuId,
    CustomBattleCode: "SUPER-VS-SAIYAN", // Optional
    IsPublic: true,
    Tags: new[] { "DC", "DragonBall", "Heroes" }));

Console.WriteLine($"Battle Code: {battle.Value.BattleCode}");
```

### Run Simulations

```csharp
var sim = await mediator.Send(new RunDeathBattleSimulationsQuery(
    BattleCode: "SUPER-VS-SAIYAN",
    SimulationCount: 10000));

Console.WriteLine($"Combatant 1 Win Rate: {sim.Value.Combatant1WinRate:F1}%");
Console.WriteLine($"Most Likely Scenario: {sim.Value.MostLikelyScenario}");
```

### Conclude Battle

```csharp
var result = await mediator.Send(new ConcludeDeathBattleCommand(
    BattleCode: "SUPER-VS-SAIYAN",
    WinnerId: gokuId,
    Outcome: DeathBattleOutcome.KO,
    Reasoning: "Goku's superior combat speed..."));
```

### Get Random Matchup

```csharp
var matchup = await mediator.Send(new GetRandomDeathBattleMatchupQuery());
Console.WriteLine($"Suggested: {matchup.Value.Character1Id} vs {matchup.Value.Character2Id}");
```

---

## AI Battle Analyzer

AI-powered replay analysis and training recommendations.

### Analyze Battle Replay

```csharp
var analysis = await mediator.Send(new AnalyzeBattleCommand(
    CharacterName: "Ryu",
    OpponentName: "Ken",
    ReplayFilePath: "replays/match01.rep",
    Options: new BattleAnalysisOptions {
        DetectPatterns = true,
        IdentifyWeaknesses = true,
        GenerateRecommendations = true,
        UseAiInsights = true,
        AnalysisDepth = 3 // 1-5
    }));

Console.WriteLine($"Performance Rating: {analysis.Value.PerformanceRating}/100");
Console.WriteLine($"Detected Patterns: {analysis.Value.Patterns.Count}");
Console.WriteLine($"Identified Weaknesses: {analysis.Value.Weaknesses.Count}");
```

### Get Training Plan

```csharp
var plan = await mediator.Send(new GenerateTrainingPlanQuery(
    CharacterName: "Ryu",
    SessionMinutes: 30));

foreach (var rec in plan.Value) {
    Console.WriteLine($"Focus: {rec.Focus} (Priority: {rec.Priority})");
    Console.WriteLine($"Drills: {string.Join(", ", rec.Drills.Select(d => d.Name))}");
}
```

### Get Performance Trend

```csharp
var trend = await mediator.Send(new GetPerformanceTrendQuery(
    CharacterName: "Ryu",
    Since: DateTime.UtcNow.AddMonths(-1)));

Console.WriteLine($"Win Rate: {trend.Value.WinRate:F1}%");
Console.WriteLine($"Trend: {trend.Value.OverallTrend}");
Console.WriteLine($"Analysis: {trend.Value.Analysis}");
```

### Real-Time Analysis

```csharp
// Start session
var session = await mediator.Send(new StartRealTimeAnalysisCommand(
    CharacterName: "Ryu",
    OpponentName: "Ken"));

// Feed frame data during match
await mediator.Send(new FeedFrameDataCommand(
    SessionId: session.Value.SessionId,
    Snapshot: new FrameDataSnapshot {
        FrameNumber = 100,
        PlayerHealth = 80,
        OpponentHealth = 60,
        CurrentAction = "Hadouken"
    }));

// Stop and get results
var analysis = await mediator.Send(
    new StopRealTimeAnalysisCommand(session.Value.SessionId));
```

---

## Frame Data Viewer

MUGEN frame data parser and analyzer.

### Load Character Frame Data

```csharp
var frameData = await mediator.Send(
    new LoadCharacterFrameDataQuery("chars/Ryu"));

Console.WriteLine($"Character: {frameData.Value.CharacterName}");
Console.WriteLine($"Moves: {frameData.Value.AllMoves.Count}");
```

### Analyze Matchup

```csharp
var matchup = await mediator.Send(
    new AnalyzeMatchupQuery("Ryu", "Ken"));

Console.WriteLine($"Advantage: {matchup.Value.Advantage}");
Console.WriteLine($"Key Factors: {string.Join(", ", matchup.Value.KeyFactors)}");
```

### Find Punishable Moves

```csharp
var punishable = await mediator.Send(
    new GetPunishableMovesQuery("Ken", playerSpeed: 5));

foreach (var move in punishable.Value) {
    Console.WriteLine($"{move.MoveName} is -{move.FrameDisadvantage} on block");
    Console.WriteLine($"  Punish with: {string.Join(", ", move.OptimalPunishes)}");
}
```

### Compare Moves

```csharp
var comparison = await mediator.Send(new CompareMovesQuery(
    Character1Name: "Ryu",
    Move1Name: "Hadouken",
    Character2Name: "Ken",
    Move2Name: "Hadouken"));

Console.WriteLine($"Winner: {comparison.Value.Winner}");
Console.WriteLine($"Speed Diff: {comparison.Value.SpeedDifference} frames");
```

---

## RetroAchievements

RetroAchievements.org integration.

### Get User Summary

```csharp
var user = await mediator.Send(
    new GetUserSummaryQuery("username"));

Console.WriteLine($"Total Points: {user.Value.TotalPoints}");
Console.WriteLine($"Rank: #{user.Value.Rank}");
```

### Get Game Achievements

```csharp
var achievements = await mediator.Send(
    new GetGameAchievementsQuery(gameId: 12345));

foreach (var ach in achievements.Value) {
    Console.WriteLine($"{ach.Title} ({ach.Points} pts)");
    Console.WriteLine($"  {ach.Description}");
}
```

### Track User Progress

```csharp
var progress = await mediator.Send(
    new GetUserGameProgressQuery("username", gameId: 12345));

var earned = progress.Value.Count(p => p.IsUnlocked);
var total = progress.Value.Count;
Console.WriteLine($"Progress: {earned}/{total} ({(double)earned/total*100:F1}%)");
```

### Rich Presence

```csharp
// Start monitoring
await mediator.Send(new StartRichPresenceCommand(gameId: 12345));

// Events will be raised:
// - AchievementUnlocked
// - ProgressUpdated

// Stop monitoring
await mediator.Send(new StopRichPresenceCommand());
```

---

## Cloud Sync

Save state synchronization across devices.

### Upload Save State

```csharp
var cloudState = await mediator.Send(new UploadSaveStateCommand(
    LocalFilePath: "saves/state1.sav",
    Name: "Level 3-2 Boss Checkpoint",
    Options: new CloudUploadOptions {
        Provider = "GoogleDrive",
        Compress = true,
        Encrypt = true,
        GameId = 123,
        Platform = "SNES",
        Tags = new[] { "boss", "checkpoint" }
    }));

Console.WriteLine($"Uploaded: {cloudState.Value.CloudId}");
```

### Sync All States

```csharp
var result = await mediator.Send(new SyncCloudSaveStatesCommand(
    Options: new SyncOptions {
        ResolveConflictsAutomatically = false,
        DefaultConflictStrategy = ConflictResolutionStrategy.NewestWins
    }));

Console.WriteLine($"Uploaded: {result.Value.UploadedCount}");
Console.WriteLine($"Downloaded: {result.Value.DownloadedCount}");
Console.WriteLine($"Conflicts: {result.Value.ConflictCount}");
```

### Get Sync Stats

```csharp
var stats = await mediator.Send(new GetSyncStatsQuery());

Console.WriteLine($"Total States: {stats.Value.TotalSaveStates}");
Console.WriteLine($"Storage Used: {stats.Value.TotalStorageBytes / 1024 / 1024} MB");
Console.WriteLine($"Pending Sync: {stats.Value.PendingCount}");
```

---

## Error Handling

All APIs use the Result pattern:

```csharp
var result = await mediator.Send(new SomeCommand());

if (result.IsSuccess) {
    // Use result.Value
} else {
    Console.WriteLine($"Error: {result.Error}");
    Console.WriteLine($"Type: {result.ErrorType}");
    // ErrorType: Validation, NotFound, Unauthorized, Internal, External
}
```

---

## Database Schema

### New Tables

```sql
-- Character Fusion
FusedCharacters (Id, Name, Parent1Id, Parent2Id, FusionType, Stats_Json, ...)
FusionBattleHistory (Id, FusedCharacterId, OpponentId, Won, ...)

-- Death Battle
DeathBattleMatches (Id, BattleCode, Combatant1_Json, Combatant2_Json, Winner_Json, ...)
DeathBattleSuggestions (Id, Combatant1Id, Combatant2Id, Upvotes, ...)

-- AI Battle Analysis
AiBattleAnalyses (Id, CharacterName, OpponentName, Stats_Json, Patterns_Json, ...)

-- Cloud Sync
CloudSaveStates (Id, UserId, GameId, CloudId, Provider, Status, ...)

-- Frame Data
CharacterFrameData (Id, CharacterName, Moves_Json, ...)
MoveFrameData (Id, CharacterFrameDataId, MoveName, StartupFrames, ...)
```

---

*For more information, see the implementation files in the respective feature directories.*
