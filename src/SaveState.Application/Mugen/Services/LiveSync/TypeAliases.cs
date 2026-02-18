// Type aliases for backward compatibility with existing code.
// These aliases map the old naming convention to the new clean names.
// 
// NOTE: These aliases are deprecated. New code should use the types from:
// - SaveState.Application.Mugen.Models.LiveSync (for models)
// - SaveState.Application.Mugen.Services.LiveSync (for service interface)

using SaveState.Application.Mugen.Models.LiveSync;
using SaveState.Application.Mugen.Services.LiveSync;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Services.LiveSync.Engines;

#pragma warning disable CS0618 // Type or member is obsolete

namespace SaveState.Application.Mugen.Services;

#region Service Aliases

/// <summary>
/// Backward compatibility alias for <see cref="LiveSyncService"/>.
/// </summary>
[Obsolete("Use LiveSyncService from SaveState.Application.Mugen.Services.LiveSync instead")]
public class CrossPlatformSyncService : LiveSyncService
{
    public CrossPlatformSyncService(
        ILogger<LiveSyncService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
        : base(logger, loggerFactory, cache, timeProvider)
    {
    }
}

/// <summary>
/// Backward compatibility alias for <see cref="ILiveSyncService"/>.
/// </summary>
[Obsolete("Use ILiveSyncService from SaveState.Application.Mugen.Services.LiveSync instead")]
public interface CrossPlatformSyncServiceICrossPlatformSyncService : ILiveSyncService
{
}

#endregion

#region Enum Aliases

/// <summary>
/// Backward compatibility alias for <see cref="PlatformType"/>.
/// </summary>
[Obsolete("Use PlatformType from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServicePlatformType { Desktop, Mobile, Web, Console }

/// <summary>
/// Backward compatibility alias for <see cref="AccountStatus"/>.
/// </summary>
[Obsolete("Use AccountStatus from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServiceAccountStatus { Active, Suspended, Deactivated }

/// <summary>
/// Backward compatibility alias for <see cref="Visibility"/>.
/// </summary>
[Obsolete("Use Visibility from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServiceVisibility { Public, Friends, Private }

/// <summary>
/// Backward compatibility alias for <see cref="SyncStatus"/>.
/// </summary>
[Obsolete("Use SyncStatus from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServiceSyncStatus { Active, Inactive, Error, Pending, Completed, Failed }

/// <summary>
/// Backward compatibility alias for <see cref="SyncMode"/>.
/// </summary>
[Obsolete("Use SyncMode from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServiceSyncMode { Full, Incremental, PreferencesOnly, ProgressOnly }

/// <summary>
/// Backward compatibility alias for <see cref="ConflictType"/>.
/// </summary>
[Obsolete("Use ConflictType from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServiceConflictType { DataMismatch, TimestampConflict, DeletionConflict }

/// <summary>
/// Backward compatibility alias for <see cref="ResolutionStrategy"/>.
/// </summary>
[Obsolete("Use ResolutionStrategy from SaveState.Application.Mugen.Models.LiveSync instead")]
public enum CrossPlatformSyncServiceResolutionStrategy { UseLocal, UseRemote, Merge, Manual }

#endregion

#region Model Aliases

/// <summary>
/// Backward compatibility alias for <see cref="UnifiedAccount"/>.
/// </summary>
[Obsolete("Use UnifiedAccount from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceUnifiedAccount : UnifiedAccount
{
}

/// <summary>
/// Backward compatibility alias for <see cref="UnifiedAccountRequest"/>.
/// </summary>
[Obsolete("Use UnifiedAccountRequest from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceUnifiedAccountRequest : UnifiedAccountRequest
{
}

/// <summary>
/// Backward compatibility alias for <see cref="PlatformAccount"/>.
/// </summary>
[Obsolete("Use PlatformAccount from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServicePlatformAccount : PlatformAccount
{
}

/// <summary>
/// Backward compatibility alias for <see cref="PlatformAccountLinkRequest"/>.
/// </summary>
[Obsolete("Use PlatformAccountLinkRequest from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServicePlatformAccountLinkRequest : PlatformAccountLinkRequest
{
}

/// <summary>
/// Backward compatibility alias for <see cref="AccountPreferences"/>.
/// </summary>
[Obsolete("Use AccountPreferences from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceAccountPreferences : AccountPreferences
{
}

/// <summary>
/// Backward compatibility alias for <see cref="PrivacySettings"/>.
/// </summary>
[Obsolete("Use PrivacySettings from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServicePrivacySettings : PrivacySettings
{
}

/// <summary>
/// Backward compatibility alias for <see cref="UnifiedStatistics"/>.
/// </summary>
[Obsolete("Use UnifiedStatistics from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceUnifiedStatistics : UnifiedStatistics
{
}

/// <summary>
/// Backward compatibility alias for <see cref="SyncSession"/>.
/// </summary>
[Obsolete("Use SyncSession from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceSyncSession : SyncSession
{
}

/// <summary>
/// Backward compatibility alias for <see cref="SyncSessionRequest"/>.
/// </summary>
[Obsolete("Use SyncSessionRequest from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceSyncSessionRequest : SyncSessionRequest
{
}

/// <summary>
/// Backward compatibility alias for <see cref="SyncProgress"/>.
/// </summary>
[Obsolete("Use SyncProgress from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceSyncProgress : SyncProgress
{
}

/// <summary>
/// Backward compatibility alias for <see cref="SyncConflict"/>.
/// </summary>
[Obsolete("Use SyncConflict from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceSyncConflict : SyncConflict
{
}

/// <summary>
/// Backward compatibility alias for <see cref="ConflictResolution"/>.
/// </summary>
[Obsolete("Use ConflictResolution from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceSyncConflictResolution : ConflictResolution
{
}

/// <summary>
/// Backward compatibility alias for <see cref="PlatformData"/>.
/// </summary>
[Obsolete("Use PlatformData from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServicePlatformData : PlatformData
{
}

/// <summary>
/// Backward compatibility alias for <see cref="CrossPlatformStats"/>.
/// </summary>
[Obsolete("Use CrossPlatformStats from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceCrossPlatformStats : CrossPlatformStats
{
}

/// <summary>
/// Backward compatibility alias for <see cref="PlatformStats"/>.
/// </summary>
[Obsolete("Use PlatformStats from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServicePlatformStats : PlatformStats
{
}

/// <summary>
/// Backward compatibility alias for <see cref="PlatformMigrationRequest"/>.
/// </summary>
[Obsolete("Use PlatformMigrationRequest from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServicePlatformMigrationRequest : PlatformMigrationRequest
{
}

/// <summary>
/// Backward compatibility alias for <see cref="AccountBackup"/>.
/// </summary>
[Obsolete("Use AccountBackup from SaveState.Application.Mugen.Models.LiveSync instead")]
public class CrossPlatformSyncServiceBackupData : AccountBackup
{
}

#endregion

#region Engine Aliases

/// <summary>
/// Backward compatibility alias for <see cref="SaveState.Application.Mugen.Services.LiveSync.Engines.SyncEngine"/>.
/// </summary>
[Obsolete("Use SyncEngine from SaveState.Application.Mugen.Services.LiveSync.Engines instead")]
public class CrossPlatformSyncServiceSyncEngine : SyncEngine
{
    public CrossPlatformSyncServiceSyncEngine(ILogger<SyncEngine> logger, ITimeProvider timeProvider) : base(logger, timeProvider)
    {
    }
}

/// <summary>
/// Backward compatibility alias for <see cref="SaveState.Application.Mugen.Services.LiveSync.Engines.ConflictResolutionEngine"/>.
/// </summary>
[Obsolete("Use ConflictResolutionEngine from SaveState.Application.Mugen.Services.LiveSync.Engines instead")]
public class CrossPlatformSyncServiceConflictResolver : ConflictResolutionEngine
{
    public CrossPlatformSyncServiceConflictResolver(ILogger<ConflictResolutionEngine> logger) : base(logger)
    {
    }
}

/// <summary>
/// Backward compatibility alias for <see cref="SaveState.Application.Mugen.Services.LiveSync.Engines.MigrationEngine"/>.
/// </summary>
[Obsolete("Use MigrationEngine from SaveState.Application.Mugen.Services.LiveSync.Engines instead")]
public class CrossPlatformSyncServiceDataMigrationManager : MigrationEngine
{
    public CrossPlatformSyncServiceDataMigrationManager(ILogger<MigrationEngine> logger) : base(logger)
    {
    }
}

#endregion
