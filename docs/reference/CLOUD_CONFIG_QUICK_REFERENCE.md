# Cloud Configuration - Quick Reference

**Quick lookup for cloud feature configuration**

---

## 🚀 Quick Start

### 1. Enable Cloud Features

Add to `appsettings.json`:

```json
{
  "CloudGaming": {
    "Enabled": true,
    "NetworkMonitoring": {
      "StoreHistoricalData": true
    }
  }
}
```

### 2. Configure Providers

```json
{
  "CloudGaming": {
    "GeForceNow": {
      "Enabled": true
    },
    "XboxCloud": {
      "Enabled": true,
      "AccountEmail": "your-email@example.com",
      "HasGamePassUltimate": true
    }
  }
}
```

### 3. Use in Code

```csharp
// Get available providers
var providers = await _cloudGamingManager.GetAvailableProvidersAsync();

// Start session (validates configuration automatically)
var session = await _cloudGamingManager.StartSessionAsync(gameId, provider);

// Get network history
var history = await _networkMonitor.GetQualityHistoryAsync(
    DateTime.UtcNow.AddHours(-24),
    DateTime.UtcNow);
```

---

## 📚 Configuration Reference

### Cloud Sync Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `PreferredProvider` | string | "" | "OneDrive" or "GoogleDrive" |
| `AutoSyncOnExit` | bool | true | Auto-sync when game exits |
| `OneDrive:ClientId` | string | "" | Azure AD application ID |
| `GoogleDrive:ClientId` | string | "" | Google OAuth client ID |

### Cloud Gaming Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | bool | false | Enable cloud gaming features |
| `DefaultProvider` | string | "GeForceNow" | Default cloud gaming service |

### Network Monitoring

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | bool | true | Enable network monitoring |
| `MonitoringIntervalSeconds` | int | 60 | How often to check quality |
| `StoreHistoricalData` | bool | true | Store historical measurements |
| `HistoricalDataRetentionDays` | int | 30 | Days to keep history |
| `MinimumLatencyMs` | int | 100 | Max acceptable latency |
| `MinimumBandwidthMbps` | int | 10 | Min required bandwidth |
| `MaximumPacketLossPercent` | double | 2.0 | Max acceptable packet loss |

---

## 🔐 API Key Setup

### Google Drive

1. Visit https://console.cloud.google.com/
2. Create project → Enable Drive API
3. Create OAuth 2.0 credentials (Desktop app)
4. Copy Client ID → paste in config

### OneDrive

1. Visit https://portal.azure.com/
2. Azure AD → App registrations → New
3. Add redirect URI: `http://localhost`
4. Copy Application ID → paste in config

### Amazon Luna

**Note**: No public API yet. Field reserved for future use.

### Shadow PC

Email support@shadow.tech to request API access.

---

## ✅ Validation

### Check Configuration at Runtime

```csharp
// Validate specific provider
var validation = ValidateProviderConfiguration(CloudGamingProvider.XboxCloud);
if (!validation.IsSuccess)
{
    Console.WriteLine($"Config error: {validation.Error}");
}

// Check startup logs
// [INFO] Cloud Gaming enabled - Default provider: GeForceNow
// [INFO] Configured cloud gaming providers: GeForce NOW, Xbox Cloud
```

### Common Validation Errors

| Error | Solution |
|-------|----------|
| "Cloud gaming is disabled" | Set `CloudGaming:Enabled` = true |
| "ClientId is required" | Add OneDrive or Google Drive Client ID |
| "Requires Game Pass Ultimate" | Set `HasGamePassUltimate` = true |
| "API key required" | Add Amazon Luna or Shadow PC API key |

---

## 🐛 Quick Troubleshooting

### Network Quality Issues

```csharp
// Get current quality
var quality = await _networkMonitor.GetCurrentQualityAsync();
Console.WriteLine($"Latency: {quality.Value.LatencyMs}ms");
Console.WriteLine($"Bandwidth: {quality.Value.BandwidthMbps}Mbps");
Console.WriteLine($"Quality: {quality.Value.Level}");

// Check if sufficient for cloud gaming
var sufficient = await _networkMonitor.IsQualitySufficientForCloudGamingAsync(provider);
```

### Historical Data Not Storing

Check configuration:
```json
{
  "CloudGaming": {
    "NetworkMonitoring": {
      "StoreHistoricalData": true  // Must be true
    }
  }
}
```

### Provider Not Available

```csharp
// Returns only configured providers
var providers = await _cloudGamingManager.GetAvailableProvidersAsync();

// Empty list = no providers configured
// Configure at least one provider in appsettings.json
```

---

## 📊 Usage Examples

### Get Network Statistics

```csharp
var history = await _networkMonitor.GetQualityHistoryAsync(
    DateTime.UtcNow.AddDays(-7),  // Last 7 days
    DateTime.UtcNow);

if (history.IsSuccess)
{
    var avg Latency = history.Value.Average(h => h.LatencyMs);
    var avgBandwidth = history.Value.Average(h => h.BandwidthMbps);
    
    Console.WriteLine($"Avg Latency: {avgLatency}ms");
    Console.WriteLine($"Avg Bandwidth: {avgBandwidth}Mbps");
    Console.WriteLine($"Sample Count: {history.Value.Count}");
}
```

### Start Cloud Gaming Session

```csharp
// Validates configuration automatically
var result = await _cloudGamingManager.StartSessionAsync(
    gameId, 
    CloudGamingProvider.GeForceNow);

if (result.IsFailure)
{
    // User-friendly error message
    Console.WriteLine(result.Error);
    // Examples:
    // "Cloud gaming is disabled in configuration..."
    // "Xbox Cloud Gaming requires Game Pass Ultimate subscription"
    // "Network quality does not meet minimum requirements..."
}
else
{
    var session = result.Value;
    Console.WriteLine($"Session started: {session.Id}");
    Console.WriteLine($"Initial quality: {session.InitialQuality.Level}");
}
```

---

## 📖 Full Documentation

- **Setup Guide**: `docs/guides/PHASE3_CONFIGURATION_GUIDE.md`
- **Completion Summary**: `docs/status/PHASE3_COMPLETION_SUMMARY.md`
- **Project Status**: `docs/status/DEVELOPMENT_STATUS.md`

---

## 🆘 Support

### Configuration Issues
- Check `appsettings.json` syntax
- Verify API keys are correct
- Review startup logs for errors

### Runtime Issues
- Enable Debug logging
- Check network connectivity
- Verify provider is enabled

### API Key Issues
- Google: Ensure OAuth consent screen configured
- Azure: Enable "Allow public client flows"
- Shadow/Luna: Contact provider support

---

**Last Updated**: January 13, 2026  
**Phase**: 3 Complete  
**Status**: ✅ Production Ready


