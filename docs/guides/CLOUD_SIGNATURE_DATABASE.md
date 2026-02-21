# Cloud Signature Database

## Overview

The Cloud Signature Database enables community-driven sharing of game memory signatures. Users can contribute signatures they've discovered and download signatures shared by others, creating a collaborative repository of game memory patterns.

## Features

### For Users

- **Auto-Sync**: Signatures automatically sync daily from the cloud
- **Search**: Find signatures for any supported game
- **Ratings**: Upvote/downvote signatures based on reliability
- **Reports**: Report broken or malicious signatures
- **Import**: One-click import of cloud signatures to local database

### For Contributors

- **Easy Upload**: Share signatures with one click
- **Recognition**: Build reputation as a trusted contributor
- **Verification**: Community verification system for quality assurance
- **Tracking**: Monitor download counts and ratings

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│              CommunitySignatureManager                       │
│         (UI interactions, notifications)                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              Application / Infrastructure                    │
│                                                            │
│   ┌─────────────────────┐    ┌─────────────────────────┐   │
│   │ CloudSignatureService│    │ SignatureSyncService    │   │
│   │   (HTTP API client)  │    │   (Background sync)     │   │
│   └─────────────────────┘    └─────────────────────────┘   │
│                                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Cloud API                               │
│         api.savestatereborn.com/signatures                   │
└─────────────────────────────────────────────────────────────┘
```

## How It Works

### 1. Automatic Sync

Every 24 hours (configurable), SaveStateReborn automatically checks for new signatures:

1. Downloads new signatures for games in your library
2. Updates existing signatures if newer versions are available
3. Marks deprecated signatures as inactive
4. Notifies you of any important updates

Configuration in `appsettings.json`:
```json
{
  "CloudSignatureDatabase": {
    "BaseUrl": "https://api.savestatereborn.com/signatures",
    "ApiKey": "your-api-key",
    "SyncIntervalHours": 24,
    "Enabled": true
  }
}
```

### 2. Manual Search

Search the cloud database on-demand:

1. Open **Game Memory Intelligence**
2. Click **"Search Community Database"**
3. Enter game name or browse categories
4. Review signature details and ratings
5. Click **Import** to add to your local database

### 3. Contributing Signatures

Share your discoveries with the community:

1. Find a working memory address using the Memory Scanner
2. Right-click the signature → **"Share to Community"**
3. Add optional notes about the game version or conditions
4. Submit for review
5. Your signature will be available after moderator approval

## Signature Status

| Status | Description | Visibility |
|--------|-------------|------------|
| **Pending** | Awaiting moderator review | Private |
| **Verified** | Approved by moderators | Public |
| **Community Verified** | High rating (>90%) from users | Public, Featured |
| **Deprecated** | No longer works with current game version | Hidden |
| **Reported** | Under investigation due to reports | Hidden |

## Privacy & Security

- **Anonymous Uploads**: Your identity is not required (optional author name)
- **Memory Patterns Only**: Only signature patterns are shared, no personal data
- **Verification Hashes**: All signatures are hashed to detect tampering
- **Rate Limiting**: Prevents abuse and ensures fair access

## Rate Limits

| Operation | Limit | Reset Period |
|-----------|-------|--------------|
| Upload | 10 signatures | Per hour |
| Search | 100 requests | Per minute |
| Sync | 1 request | Per hour |
| Vote | 50 votes | Per hour |

## API Reference

### Search Signatures

```csharp
var result = await _cloudDb.SearchSignaturesAsync(new CloudSignatureSearchRequest
{
    GameTitle = "Celeste",
    PatternType = "health",
    SortBy = SignatureSortBy.MostPopular,
    Take = 50
});

if (result.IsSuccess)
{
    foreach (var sig in result.Value.Signatures)
    {
        Console.WriteLine($"{sig.Name}: {sig.Pattern}");
    }
}
```

### Upload Signature

```csharp
var signature = new GameMemorySignature
{
    GameTitle = "MyGame",
    Name = "PlayerHealth",
    Pattern = "A1 ?? ?? ?? ?? 8B",
    Offset = 0,
    ValueType = "int32"
};

var result = await _cloudDb.UploadSignatureAsync(new CloudSignatureUploadRequest
{
    GameTitle = "MyGame",
    GameVersion = "1.0.0",
    Platform = "PC",
    Signature = signature,
    Author = "YourName",
    Notes = "Works on Steam version"
});
```

### Vote on Signature

```csharp
// Upvote
await _cloudDb.VoteSignatureAsync("sig-123", isUpvote: true);

// Downvote
await _cloudDb.VoteSignatureAsync("sig-123", isUpvote: false);
```

### Get Signature Stats

```csharp
var stats = await _cloudDb.GetSignatureStatsAsync("sig-123");
if (stats.IsSuccess)
{
    Console.WriteLine($"Rating: {stats.Value.Rating:P}");
    Console.WriteLine($"Downloads: {stats.Value.TotalDownloads}");
    Console.WriteLine($"Success Rate: {stats.Value.SuccessRate:P}");
}
```

### Sync Signatures

```csharp
// Get changes since last sync
var changes = await _cloudDb.GetChangesSinceAsync(DateTime.UtcNow.AddDays(-1));

// Get sync manifest
var manifest = await _cloudDb.GetSyncManifestAsync();
Console.WriteLine($"Total signatures: {manifest.Value.TotalSignatures}");
```

## Background Sync Service

The `SignatureSyncService` runs as a hosted background service:

```csharp
// Register in DI
services.AddHostedService<SignatureSyncService>();

// Manual sync trigger
await syncService.ForceSyncAsync();

// Subscribe to sync events
syncService.SignaturesSynced += (sender, e) =>
{
    Console.WriteLine($"Synced {e.AddedCount} new, {e.UpdatedCount} updated");
};
```

## Troubleshooting

### Sync Not Working

1. Check internet connectivity
2. Verify API key in settings
3. Check logs for specific error messages
4. Try manual sync from settings

### Signature Not Found

- Game may not be in the database yet
- Try variations of the game title
- Check if the game version matches

### Upload Rejected

- Ensure pattern is valid hex with wildcards (??)
- Verify game title is correct
- Check rate limits haven't been exceeded
- Signature may already exist

## Best Practices

### For Users

1. **Verify Before Import**: Check ratings and download counts
2. **Test Signatures**: Verify imported signatures work with your game version
3. **Report Issues**: Help the community by reporting broken signatures
4. **Rate Signatures**: Upvote reliable signatures, downvote broken ones

### For Contributors

1. **Test Thoroughly**: Ensure your signature works consistently
2. **Version Specificity**: Include game version information
3. **Clear Names**: Use descriptive names (e.g., "PlayerHealth" not "hp")
4. **Add Notes**: Document any special conditions or requirements
5. **Unique Patterns**: Avoid overly broad patterns that may match incorrectly

## Configuration Options

```json
{
  "CloudSignatureDatabase": {
    "BaseUrl": "https://api.savestatereborn.com/signatures",
    "ApiKey": "",
    "SyncIntervalHours": 24,
    "Enabled": true,
    "CacheDurationMinutes": 5,
    "MaxSignaturesPerGame": 100,
    "AutoImportVerified": false
  }
}
```

| Option | Description | Default |
|--------|-------------|---------|
| `BaseUrl` | Cloud API endpoint | `https://api.savestatereborn.com/signatures` |
| `ApiKey` | Your API key for authentication | Empty |
| `SyncIntervalHours` | Hours between automatic syncs | 24 |
| `Enabled` | Enable/disable cloud features | true |
| `CacheDurationMinutes` | How long to cache search results | 5 |
| `MaxSignaturesPerGame` | Maximum signatures to store per game | 100 |
| `AutoImportVerified` | Auto-import verified signatures | false |

## Contributing to the Database

To contribute signatures:

1. **Find Working Signatures**: Use the Memory Scanner tool
2. **Test Extensively**: Verify across multiple game sessions
3. **Document**: Add clear descriptions and game version info
4. **Submit**: Use the "Share to Community" feature
5. **Respond**: Check for moderator feedback on your submissions

## Support

For issues with the Cloud Signature Database:

- Check the [FAQ](FAQ.md)
- Report API issues: `api-support@savestatereborn.com`
- Request game support: Use the in-app feedback feature

---

*Last Updated: February 2026*
