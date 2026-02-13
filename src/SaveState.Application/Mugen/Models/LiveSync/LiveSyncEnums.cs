namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Platform types for cross-platform synchronization.
/// </summary>
public enum PlatformType
{
    Desktop,
    Mobile,
    Web,
    Console
}

/// <summary>
/// Account status for unified accounts.
/// </summary>
public enum AccountStatus
{
    Active,
    Suspended,
    Deactivated
}

/// <summary>
/// Visibility settings for account data.
/// </summary>
public enum Visibility
{
    Public,
    Friends,
    Private
}

/// <summary>
/// Synchronization status for sync operations.
/// </summary>
public enum SyncStatus
{
    Active,
    Inactive,
    Error,
    Pending,
    Completed,
    Failed
}

/// <summary>
/// Synchronization mode options.
/// </summary>
public enum SyncMode
{
    Full,
    Incremental,
    PreferencesOnly,
    ProgressOnly
}

/// <summary>
/// Types of conflicts that can occur during synchronization.
/// </summary>
public enum ConflictType
{
    DataMismatch,
    TimestampConflict,
    DeletionConflict
}

/// <summary>
/// Strategies for resolving sync conflicts.
/// </summary>
public enum ResolutionStrategy
{
    UseLocal,
    UseRemote,
    Merge,
    Manual
}
