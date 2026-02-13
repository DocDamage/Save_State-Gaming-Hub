namespace SaveState.Core.Common.Constants;

/// <summary>
/// Standardized log message templates used throughout the application.
/// These templates follow structured logging conventions with named placeholders.
/// </summary>
public static class LogMessages
{
    // General Operation Logging
    public const string OperationStarted = "{Operation} started";
    public const string OperationCompleted = "{Operation} completed successfully";
    public const string OperationFailed = "{Operation} failed: {Reason}";
    public const string OperationCancelled = "{Operation} was cancelled";
    public const string OperationTimedOut = "{Operation} timed out after {TimeoutMs}ms";

    // CRUD Operations
    public const string EntityCreated = "{EntityType} created: {EntityId}";
    public const string EntityUpdated = "{EntityType} updated: {EntityId}";
    public const string EntityDeleted = "{EntityType} deleted: {EntityId}";
    public const string EntityRetrieved = "{EntityType} retrieved: {EntityId}";
    public const string EntityNotFound = "{EntityType} not found: {EntityId}";
    public const string EntityAlreadyExists = "{EntityType} already exists: {EntityId}";

    // Game Library
    public const string GameLaunched = "Game launched: {GameId} - {GameName}";
    public const string GameClosed = "Game closed: {GameId} - {GameName}";
    public const string GameInstalled = "Game installed: {GameId}";
    public const string GameUninstalled = "Game uninstalled: {GameId}";
    public const string GameAdded = "Game added to library: {GameId}";
    public const string GameRemoved = "Game removed from library: {GameId}";
    public const string GameScanned = "Scanned {Count} games from {Source}";
    public const string GameMetadataUpdated = "Game metadata updated: {GameId}";

    // Save States
    public const string SaveStateCreated = "Save state created: {SaveStateId} for game {GameId}";
    public const string SaveStateLoaded = "Save state loaded: {SaveStateId}";
    public const string SaveStateDeleted = "Save state deleted: {SaveStateId}";
    public const string SaveStateSynced = "Save state synchronized: {SaveStateId}";
    public const string AutoSaveTriggered = "Auto-save triggered for game {GameId}";

    // Cloud Sync
    public const string CloudSyncStarted = "Cloud sync started for {Provider}";
    public const string CloudSyncCompleted = "Cloud sync completed: {FilesUploaded} uploaded, {FilesDownloaded} downloaded";
    public const string CloudSyncFailed = "Cloud sync failed: {Reason}";
    public const string CloudUploadStarted = "Cloud upload started: {FileName}";
    public const string CloudUploadCompleted = "Cloud upload completed: {FileName}";
    public const string CloudDownloadStarted = "Cloud download started: {FileName}";
    public const string CloudDownloadCompleted = "Cloud download completed: {FileName}";
    public const string CloudAuthenticated = "Authenticated with cloud provider: {Provider}";
    public const string CloudAuthenticationFailed = "Cloud authentication failed: {Provider} - {Reason}";

    // ROM/Emulator
    public const string RomScanned = "ROM scanned: {RomName}";
    public const string RomImported = "ROM imported: {RomId}";
    public const string RomLaunched = "ROM launched: {RomName} with emulator {EmulatorId}";
    public const string EmulatorRegistered = "Emulator registered: {EmulatorId}";
    public const string EmulatorNotFound = "Emulator not found: {EmulatorId}";

    // MUGEN Specific
    public const string MugenCharacterLoaded = "MUGEN character loaded: {CharacterName}";
    public const string MugenCharacterAdded = "MUGEN character added to roster: {CharacterName}";
    public const string MugenCharacterRemoved = "MUGEN character removed from roster: {CharacterName}";
    public const string MugenMatchStarted = "MUGEN match started: {Player1} vs {Player2}";
    public const string MugenMatchCompleted = "MUGEN match completed: Winner - {Winner}";
    public const string MugenTournamentCreated = "MUGEN tournament created: {TournamentId}";
    public const string MugenTournamentStarted = "MUGEN tournament started: {TournamentId}";
    public const string MugenTournamentCompleted = "MUGEN tournament completed: {TournamentId}";

    // User Management
    public const string UserAuthenticated = "User authenticated: {UserId}";
    public const string UserLoginFailed = "User login failed: {Username} - {Reason}";
    public const string UserRegistered = "User registered: {UserId}";
    public const string UserLoggedOut = "User logged out: {UserId}";
    public const string TokenRefreshed = "Token refreshed for user: {UserId}";
    public const string TokenRefreshFailed = "Token refresh failed: {Reason}";
    public const string ApiKeyCreated = "API key created: {KeyId} for user {UserId}";
    public const string ApiKeyRevoked = "API key revoked: {KeyId}";

    // Social Features
    public const string FriendRequestSent = "Friend request sent: {FromUser} to {ToUser}";
    public const string FriendRequestAccepted = "Friend request accepted: {FromUser} and {ToUser} are now friends";
    public const string FriendRequestDeclined = "Friend request declined: {FromUser} declined {ToUser}";
    public const string FriendRemoved = "Friend removed: {UserId} removed {FriendId}";
    public const string PostCreated = "Post created: {PostId} by user {UserId}";
    public const string ReviewSubmitted = "Review submitted: {ReviewId} for game {GameId}";
    public const string CollectionShared = "Collection shared: {CollectionId} with {TargetUser}";

    // AI/Assistant
    public const string AiRequestStarted = "AI request started: {RequestType}";
    public const string AiRequestCompleted = "AI request completed: {RequestType} in {DurationMs}ms";
    public const string AiRequestFailed = "AI request failed: {RequestType} - {Error}";
    public const string AiProviderSwitched = "Switched to fallback AI provider: {Provider}";
    public const string AiCacheHit = "AI cache hit: {CacheKey}";
    public const string AiCacheMiss = "AI cache miss: {CacheKey}";

    // Plugins
    public const string PluginLoaded = "Plugin loaded: {PluginId} v{Version}";
    public const string PluginUnloaded = "Plugin unloaded: {PluginId}";
    public const string PluginEnabled = "Plugin enabled: {PluginId}";
    public const string PluginDisabled = "Plugin disabled: {PluginId}";
    public const string PluginError = "Plugin error: {PluginId} - {Error}";

    // Caching
    public const string CacheHit = "Cache hit: {Key}";
    public const string CacheMiss = "Cache miss: {Key}";
    public const string CacheSet = "Cache set: {Key} with TTL {Ttl}s";
    public const string CacheInvalidated = "Cache invalidated: {Key}";
    public const string CacheWarmed = "Cache warmed with {Count} entries";

    // External Services
    public const string ExternalApiCall = "External API call: {Service} - {Endpoint}";
    public const string ExternalApiSuccess = "External API success: {Service} - {Endpoint}";
    public const string ExternalApiError = "External API error: {Service} - {StatusCode} - {Error}";
    public const string SteamApiCalled = "Steam API called: {Method}";
    public const string DiscordApiCalled = "Discord API called: {Method}";

    // System/Performance
    public const string MemoryPressureDetected = "Memory pressure detected: {MemoryUsageMB}MB used";
    public const string PerformanceWarning = "Performance warning: {Operation} took {DurationMs}ms";
    public const string BackgroundTaskStarted = "Background task started: {TaskName}";
    public const string BackgroundTaskCompleted = "Background task completed: {TaskName}";
    public const string BackgroundTaskFailed = "Background task failed: {TaskName} - {Error}";

    // Import/Export
    public const string ImportStarted = "Import started: {Source} - {Format}";
    public const string ImportCompleted = "Import completed: {Count} items imported";
    public const string ImportFailed = "Import failed: {Source} - {Error}";
    public const string ExportStarted = "Export started: {Target} - {Format}";
    public const string ExportCompleted = "Export completed: {Count} items exported";

    // Security
    public const string SecurityEvent = "Security event: {EventType} - {Details}";
    public const string SuspiciousActivity = "Suspicious activity detected: {Activity}";
    public const string RateLimitHit = "Rate limit hit: {ClientId} - {Endpoint}";

    // Data Portability
    public const string DataExportRequested = "Data export requested by user: {UserId}";
    public const string DataImportStarted = "Data import started for user: {UserId}";
    public const string SettingsImported = "Imported {Section} settings";

    // Workflow/Automation
    public const string WorkflowCreated = "Workflow created: {Name} ({Id})";
    public const string WorkflowExecuted = "Workflow executed: {Name} ({Id})";
    public const string WorkflowFailed = "Workflow failed: {Name} ({Id}) - {Error}";
    public const string MacroRecorded = "Macro recorded: {MacroId}";
    public const string MacroExecuted = "Macro executed: {MacroId}";

    // Streaming
    public const string StreamStarted = "Stream started: {StreamId}";
    public const string StreamStopped = "Stream stopped: {StreamId}";
    public const string StreamError = "Stream error: {StreamId} - {Error}";
}
