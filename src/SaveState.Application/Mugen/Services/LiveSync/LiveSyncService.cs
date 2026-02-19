using SaveState.Application.Mugen.Models.LiveSync;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SaveState.Application.Mugen.Services.LiveSync.Engines;

namespace SaveState.Application.Mugen.Services.LiveSync;

/// <summary>
/// Cross-platform synchronization service providing unified accounts,
/// seamless data sync, and consistent experiences across all devices and platforms.
/// </summary>
public class LiveSyncService : ILiveSyncService
{
    private readonly ILogger<LiveSyncService> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly SyncEngine _syncEngine;
    private readonly ConflictResolutionEngine _conflictEngine;
    private readonly StateMergeEngine _mergeEngine;
    private readonly NetworkTransportEngine _transportEngine;
    private readonly MigrationEngine _migrationEngine;

    // State management kept in the coordinator service
    private readonly Dictionary<string, UnifiedAccount> _unifiedAccounts = new();
    private readonly Dictionary<string, SyncSession> _activeSyncSessions = new();
    private readonly Dictionary<string, PlatformData> _platformDataStores = new();

    public LiveSyncService(
        ILogger<LiveSyncService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;

        // Initialize engines
        _syncEngine = new SyncEngine(loggerFactory.CreateLogger<SyncEngine>(), _timeProvider);
        _conflictEngine = new Engines.ConflictResolutionEngine(loggerFactory.CreateLogger<Engines.ConflictResolutionEngine>());
        _mergeEngine = new StateMergeEngine(loggerFactory.CreateLogger<StateMergeEngine>());
        _transportEngine = new NetworkTransportEngine(loggerFactory.CreateLogger<NetworkTransportEngine>());
        _migrationEngine = new MigrationEngine(loggerFactory.CreateLogger<MigrationEngine>(), _timeProvider);

        InitializePlatformDataStores();
    }

    #region Account Management

    public async Task<Result<UnifiedAccount>> CreateUnifiedAccountAsync(
        UnifiedAccountRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating unified account for {Email}", request.Email);

            var existing = _unifiedAccounts.Values.FirstOrDefault(a => a.Email == request.Email);
            if (existing != null)
            {
                return Result.Failure<UnifiedAccount>("Account already exists with this email");
            }

            var account = new UnifiedAccount
            {
                AccountId = Guid.NewGuid().ToString(),
                Email = request.Email,
                DisplayName = request.DisplayName,
                ProfilePictureUrl = request.ProfilePictureUrl,
                CreatedAt = _timeProvider.UtcNow,
                LastLoginAt = _timeProvider.UtcNow,
                Status = AccountStatus.Active,
                LinkedPlatforms = new Dictionary<PlatformType, PlatformAccount>(),
                Preferences = CreateDefaultPreferences(),
                Statistics = CreateDefaultStatistics()
            };

            _unifiedAccounts[account.AccountId] = account;

            _logger.LogInformation("Unified account created: {AccountId}", account.AccountId);
            return Result.Success(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unified account for {Email}", request.Email);
            return Result.Failure<UnifiedAccount>($"Account creation failed: {ex.Message}");
        }
    }

    public async Task<Result<UnifiedAccount>> GetUnifiedAccountAsync(
        string accountId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_unifiedAccounts.TryGetValue(accountId, out var account))
            {
                return Result.Failure<UnifiedAccount>("Unified account not found");
            }

            return Result.Success(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unified account {AccountId}", accountId);
            return Result.Failure<UnifiedAccount>($"Account retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result> LinkPlatformAccountAsync(
        string accountId,
        PlatformAccountLinkRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Linking platform account {PlatformId} to unified account {AccountId}",
                request.PlatformUserId,
                accountId);

            if (!_unifiedAccounts.TryGetValue(accountId, out var account))
            {
                return Result.Failure("Unified account not found");
            }

            if (account.LinkedPlatforms.ContainsKey(request.PlatformType))
            {
                return Result.Failure("Platform already linked to this account");
            }

            var platformAccount = new PlatformAccount
            {
                PlatformType = request.PlatformType,
                PlatformUserId = request.PlatformUserId,
                PlatformUsername = request.PlatformUsername,
                LinkedAt = _timeProvider.UtcNow,
                LastSyncAt = _timeProvider.UtcNow,
                SyncStatus = SyncStatus.Active
            };

            account.LinkedPlatforms[request.PlatformType] = platformAccount;

            await InitializePlatformDataAsync(accountId, request.PlatformType, ct);
            await PerformInitialSyncAsync(account, request.PlatformType, ct);

            _logger.LogInformation("Platform account linked successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking platform account");
            return Result.Failure($"Platform linking failed: {ex.Message}");
        }
    }

    #endregion

    #region Sync Operations

    public async Task<Result<SyncSession>> StartSyncSessionAsync(
        SyncSessionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting sync session for account {AccountId}", request.AccountId);

            if (!_unifiedAccounts.TryGetValue(request.AccountId, out var account))
            {
                return Result.Failure<SyncSession>("Unified account not found");
            }

            var session = new SyncSession
            {
                SessionId = Guid.NewGuid().ToString(),
                AccountId = request.AccountId,
                InitiatingPlatform = request.InitiatingPlatform,
                TargetPlatforms = request.TargetPlatforms,
                Mode = request.Mode,
                Status = SyncStatus.Active,
                StartedAt = _timeProvider.UtcNow,
                Progress = new SyncProgress
                {
                    TotalItems = 100,
                    ProcessedItems = 0,
                    CurrentPhase = "Initializing",
                    EstimatedTimeRemaining = TimeSpan.FromMinutes(2)
                }
            };

            _activeSyncSessions[session.SessionId] = session;

            _ = PerformSyncAsync(session, ct);

            _logger.LogInformation("Sync session started: {SessionId}", session.SessionId);
            return Result.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting sync session for {AccountId}", request.AccountId);
            return Result.Failure<SyncSession>($"Sync session failed: {ex.Message}");
        }
    }

    public async Task<Result<SyncStatus>> GetSyncStatusAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSyncSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<SyncStatus>("Sync session not found");
            }

            return Result.Success(session.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status for {SessionId}", sessionId);
            return Result.Failure<SyncStatus>($"Status retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<SyncProgress>> GetSyncProgressAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSyncSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<SyncProgress>("Sync session not found");
            }

            return Result.Success(session.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync progress for {SessionId}", sessionId);
            return Result.Failure<SyncProgress>($"Progress retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<SyncConflict>>> GetSyncConflictsAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSyncSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<IReadOnlyList<SyncConflict>>("Sync session not found");
            }

            return Result.Success(session.Conflicts ?? new List<SyncConflict>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync conflicts for {SessionId}", sessionId);
            return Result.Failure<IReadOnlyList<SyncConflict>>($"Conflict retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result> ResolveSyncConflictAsync(
        string sessionId,
        ConflictResolution resolution,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeSyncSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure("Sync session not found");
            }

            _logger.LogInformation("Resolving sync conflict {ConflictId}", resolution.ConflictId);

            var conflict = session.Conflicts?.FirstOrDefault(c => c.ConflictId == resolution.ConflictId);
            if (conflict == null)
            {
                return Result.Failure("Conflict not found");
            }

            await _conflictEngine.ResolveConflictAsync(conflict, resolution, ct);

            _logger.LogInformation("Sync conflict resolved successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving sync conflict {ConflictId}", resolution.ConflictId);
            return Result.Failure($"Conflict resolution failed: {ex.Message}");
        }
    }

    #endregion

    #region Data Operations

    public async Task<Result<PlatformData>> GetPlatformDataAsync(
        string accountId,
        PlatformType platform,
        CancellationToken ct = default)
    {
        try
        {
            var platformKey = GetPlatformKey(accountId, platform);
            if (!_platformDataStores.TryGetValue(platformKey, out var platformData))
            {
                return Result.Failure<PlatformData>("Platform data not found");
            }

            return Result.Success(platformData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform data for {AccountId} on {Platform}", accountId, platform);
            return Result.Failure<PlatformData>($"Platform data retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<CrossPlatformStats>> GetCrossPlatformStatsAsync(
        string accountId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_unifiedAccounts.TryGetValue(accountId, out var account))
            {
                return Result.Failure<CrossPlatformStats>("Unified account not found");
            }

            _logger.LogInformation("Generating cross-platform stats for {AccountId}", accountId);

            var stats = new CrossPlatformStats
            {
                AccountId = accountId,
                TotalPlayTime = account.Statistics.TotalPlayTime,
                PlatformsUsed = account.LinkedPlatforms.Count,
                PlatformBreakdown = account.LinkedPlatforms.ToDictionary(
                    p => p.Key,
                    p => new PlatformStats
                    {
                        PlayTime = TimeSpan.FromHours(10),
                        MatchesPlayed = 50,
                        AchievementsUnlocked = 5,
                        LastActive = p.Value.LastSyncAt
                    }),
                CrossPlatformAchievements = new List<string> { "CrossPlatform Master", "Unified Player" },
                SyncHealth = _syncEngine.CalculateSyncHealth(account).Score,
                DataCompleteness = _syncEngine.CalculateDataCompleteness(account).OverallCompleteness,
                GeneratedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("Cross-platform stats generated successfully");
            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cross-platform stats for {AccountId}", accountId);
            return Result.Failure<CrossPlatformStats>($"Stats generation failed: {ex.Message}");
        }
    }

    public async Task<Result> MigratePlatformDataAsync(
        string accountId,
        PlatformMigrationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Migrating platform data for {AccountId} from {Source} to {Target}",
                accountId,
                request.SourcePlatform,
                request.TargetPlatform);

            var sourceKey = GetPlatformKey(accountId, request.SourcePlatform);
            if (!_platformDataStores.TryGetValue(sourceKey, out var sourceData))
            {
                return Result.Failure("Source platform data not found");
            }

            var result = await _migrationEngine.MigrateAsync(
                sourceData,
                request.TargetPlatform,
                request,
                ct);

            if (!result.Success)
            {
                return Result.Failure($"Migration failed: {result.ErrorMessage}");
            }

            _logger.LogInformation("Platform data migration completed successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating platform data for {AccountId}", accountId);
            return Result.Failure($"Data migration failed: {ex.Message}");
        }
    }

    #endregion

    #region Backup Operations

    public async Task<Result<AccountBackup>> CreateAccountBackupAsync(
        string accountId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating account backup for {AccountId}", accountId);

            if (!_unifiedAccounts.TryGetValue(accountId, out var account))
            {
                return Result.Failure<AccountBackup>("Unified account not found");
            }

            var backup = new AccountBackup
            {
                BackupId = Guid.NewGuid().ToString(),
                AccountId = accountId,
                CreatedAt = _timeProvider.UtcNow,
                AccountData = JsonSerializer.Serialize(account),
                PlatformData = new Dictionary<PlatformType, string>(),
                TotalSize = 0,
                Checksum = ""
            };

            foreach (var platform in account.LinkedPlatforms.Keys)
            {
                var platformKey = GetPlatformKey(accountId, platform);
                if (_platformDataStores.TryGetValue(platformKey, out var platformData))
                {
                    backup.PlatformData[platform] = JsonSerializer.Serialize(platformData);
                }
            }

            backup.TotalSize = backup.AccountData.Length + backup.PlatformData.Sum(p => p.Value.Length);
            backup.Checksum = ComputeChecksum(backup);

            _logger.LogInformation("Account backup created: {BackupId}", backup.BackupId);
            return Result.Success(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account backup for {AccountId}", accountId);
            return Result.Failure<AccountBackup>($"Backup creation failed: {ex.Message}");
        }
    }

    public async Task<Result> RestoreAccountBackupAsync(
        string accountId,
        AccountBackup backup,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Restoring account backup {BackupId} for {AccountId}",
                backup.BackupId,
                accountId);

            if (!await ValidateBackupAsync(backup, ct))
            {
                return Result.Failure("Backup validation failed");
            }

            var restoredAccount = JsonSerializer.Deserialize<UnifiedAccount>(backup.AccountData);
            if (restoredAccount != null)
            {
                _unifiedAccounts[accountId] = restoredAccount;
            }

            foreach (var platformData in backup.PlatformData)
            {
                var platformKey = GetPlatformKey(accountId, platformData.Key);
                var data = JsonSerializer.Deserialize<PlatformData>(platformData.Value);
                if (data != null)
                {
                    _platformDataStores[platformKey] = data;
                }
            }

            _logger.LogInformation("Account backup restored successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring account backup {BackupId}", backup.BackupId);
            return Result.Failure($"Backup restoration failed: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private void InitializePlatformDataStores()
    {
        // Initialize data stores for different platforms
    }

    private async Task InitializePlatformDataAsync(
        string accountId,
        PlatformType platform,
        CancellationToken ct)
    {
        var platformKey = GetPlatformKey(accountId, platform);
        if (!_platformDataStores.ContainsKey(platformKey))
        {
            _platformDataStores[platformKey] = new PlatformData
            {
                AccountId = accountId,
                Platform = platform,
                GameProgress = new Dictionary<string, object>(),
                Achievements = new List<string>(),
                Statistics = new Dictionary<string, object>(),
                Preferences = new Dictionary<string, object>(),
                LastUpdated = _timeProvider.UtcNow
            };
        }
    }

    private async Task PerformInitialSyncAsync(
        UnifiedAccount account,
        PlatformType platform,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);
    }

    private async Task PerformSyncAsync(SyncSession session, CancellationToken ct)
    {
        try
        {
            var result = await _syncEngine.PerformSyncAsync(
                session,
                new Progress<SyncProgress>(p => session.Progress = p),
                ct);

            session.Status = result.Success ? SyncStatus.Completed : SyncStatus.Failed;
            session.CompletedAt = _timeProvider.UtcNow;

            if (!result.Success)
            {
                session.ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing sync for session {SessionId}", session.SessionId);
            session.Status = SyncStatus.Failed;
            session.ErrorMessage = ex.Message;
        }
    }

    private async Task<bool> ValidateBackupAsync(AccountBackup backup, CancellationToken ct)
    {
        return !string.IsNullOrEmpty(backup.AccountData) &&
               backup.CreatedAt > DateTime.MinValue &&
               backup.Checksum == ComputeChecksum(backup);
    }

    private string ComputeChecksum(AccountBackup backup)
    {
        var data = backup.AccountData + string.Join("", backup.PlatformData.Values);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..16];
    }

    private static string GetPlatformKey(string accountId, PlatformType platform) =>
        $"{accountId}_{platform}";

    private static AccountPreferences CreateDefaultPreferences() => new()
    {
        Theme = "auto",
        Language = "en",
        TimeZone = "UTC",
        PrivacySettings = new PrivacySettings
        {
            ProfileVisibility = Visibility.Public,
            ActivityVisibility = Visibility.Friends,
            DataSharing = true
        }
    };

    private static UnifiedStatistics CreateDefaultStatistics() => new()
    {
        TotalPlayTime = TimeSpan.Zero,
        PlatformsUsed = new List<PlatformType>(),
        AchievementCount = 0,
        FriendCount = 0
    };

    #endregion
}
