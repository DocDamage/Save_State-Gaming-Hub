using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.BackupArchive;

/// <summary>
/// Service for Git-like branching of save states.
/// </summary>
public interface ISaveStateBranchingService
{
    /// <summary>
    /// Initializes a save state repository for a game.
    /// </summary>
    Task<Result<SaveStateRepository>> InitializeRepositoryAsync(string gameId, string gameName, CancellationToken ct = default);

    /// <summary>
    /// Gets a save state repository.
    /// </summary>
    Task<Result<SaveStateRepository>> GetRepositoryAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new branch.
    /// </summary>
    Task<Result<SaveStateBranch>> CreateBranchAsync(string gameId, string branchName, string? fromBranch = null, string? fromCommit = null, CancellationToken ct = default);

    /// <summary>
    /// Lists all branches in a repository.
    /// </summary>
    Task<Result<IReadOnlyList<SaveStateBranch>>> ListBranchesAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Switches to a different branch.
    /// </summary>
    Task<Result> CheckoutBranchAsync(string gameId, string branchName, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active branch.
    /// </summary>
    Task<Result<SaveStateBranch>> GetCurrentBranchAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Creates a commit (save point) in the current branch.
    /// </summary>
    Task<Result<SaveStateCommit>> CommitAsync(string gameId, string message, byte[] saveData, Dictionary<string, object>? metadata = null, CancellationToken ct = default);

    /// <summary>
    /// Gets commit history for a branch.
    /// </summary>
    Task<Result<IReadOnlyList<SaveStateCommit>>> GetCommitHistoryAsync(string gameId, string? branchName = null, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific commit.
    /// </summary>
    Task<Result<SaveStateCommit>> GetCommitAsync(string gameId, string commitHash, CancellationToken ct = default);

    /// <summary>
    /// Loads a specific commit (checkout).
    /// </summary>
    Task<Result<byte[]>> LoadCommitAsync(string gameId, string commitHash, CancellationToken ct = default);

    /// <summary>
    /// Merges a branch into the current branch.
    /// </summary>
    Task<Result<MergeResult>> MergeBranchAsync(string gameId, string sourceBranch, MergeOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Reverts to a previous commit.
    /// </summary>
    Task<Result<SaveStateCommit>> RevertAsync(string gameId, string commitHash, CancellationToken ct = default);

    /// <summary>
    /// Resets a branch to a specific commit.
    /// </summary>
    Task<Result> ResetAsync(string gameId, string branchName, string commitHash, bool hard = false, CancellationToken ct = default);

    /// <summary>
    /// Deletes a branch.
    /// </summary>
    Task<Result> DeleteBranchAsync(string gameId, string branchName, bool force = false, CancellationToken ct = default);

    /// <summary>
    /// Compares two commits.
    /// </summary>
    Task<Result<CommitComparison>> CompareCommitsAsync(string gameId, string baseCommit, string targetCommit, CancellationToken ct = default);

    /// <summary>
    /// Tags a commit.
    /// </summary>
    Task<Result<SaveStateTag>> TagCommitAsync(string gameId, string commitHash, string tagName, string? message = null, CancellationToken ct = default);

    /// <summary>
    /// Lists all tags in a repository.
    /// </summary>
    Task<Result<IReadOnlyList<SaveStateTag>>> ListTagsAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the diff between two commits.
    /// </summary>
    Task<Result<CommitDiff>> GetDiffAsync(string gameId, string commit1, string commit2, CancellationToken ct = default);

    /// <summary>
    /// Creates a stash (temporary save).
    /// </summary>
    Task<Result<SaveStateStash>> StashAsync(string gameId, string message, byte[] saveData, CancellationToken ct = default);

    /// <summary>
    /// Lists all stashes.
    /// </summary>
    Task<Result<IReadOnlyList<SaveStateStash>>> ListStashesAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Applies a stash.
    /// </summary>
    Task<Result<byte[]>> ApplyStashAsync(string gameId, string stashId, bool deleteAfter = true, CancellationToken ct = default);

    /// <summary>
    /// Deletes a stash.
    /// </summary>
    Task<Result> DeleteStashAsync(string gameId, string stashId, CancellationToken ct = default);

    /// <summary>
    /// Exports a branch to a file.
    /// </summary>
    Task<Result<string>> ExportBranchAsync(string gameId, string branchName, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Imports a branch from a file.
    /// </summary>
    Task<Result<SaveStateBranch>> ImportBranchAsync(string gameId, string filePath, string? branchName = null, CancellationToken ct = default);

    /// <summary>
    /// Gets repository statistics.
    /// </summary>
    Task<Result<BranchingStatistics>> GetStatisticsAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Optimizes the repository (garbage collection).
    /// </summary>
    Task<Result<OptimizationResult>> OptimizeRepositoryAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a branch is created.
    /// </summary>
    event EventHandler<BranchCreatedEventArgs>? BranchCreated;

    /// <summary>
    /// Event raised when a commit is created.
    /// </summary>
    event EventHandler<CommitCreatedEventArgs>? CommitCreated;

    /// <summary>
    /// Event raised when branches are merged.
    /// </summary>
    event EventHandler<BranchMergedEventArgs>? BranchMerged;
}

/// <summary>
/// Save state repository for a game.
/// </summary>
public sealed record SaveStateRepository(
    string GameId,
    string GameName,
    string Path,
    DateTime InitializedAt,
    int BranchCount,
    int CommitCount);

/// <summary>
/// Save state branch.
/// </summary>
public sealed record SaveStateBranch(
    string Name,
    string GameId,
    string? ParentBranch,
    string HeadCommitHash,
    DateTime CreatedAt,
    DateTime LastCommitAt,
    int CommitCount,
    bool IsActive);

/// <summary>
/// Save state commit.
/// </summary>
public sealed record SaveStateCommit(
    string Hash,
    string GameId,
    string BranchName,
    string Message,
    string? ParentHash,
    DateTime Timestamp,
    long Size,
    string? Author = null,
    IReadOnlyList<string>? Tags = null,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// Save state tag.
/// </summary>
public sealed record SaveStateTag(
    string Name,
    string CommitHash,
    string? Message,
    DateTime CreatedAt);

/// <summary>
/// Save state stash.
/// </summary>
public sealed record SaveStateStash(
    string Id,
    string GameId,
    string Message,
    string BranchName,
    DateTime CreatedAt,
    long Size);

/// <summary>
/// Merge options.
/// </summary>
public sealed record MergeOptions(
    MergeStrategy Strategy,
    bool CreateCommit = true,
    string? CommitMessage = null);

/// <summary>
/// Merge result.
/// </summary>
public sealed record MergeResult(
    bool Success,
    string TargetBranch,
    string SourceBranch,
    string? MergeCommitHash,
    IReadOnlyList<MergeConflict>? Conflicts,
    bool HasConflicts);

/// <summary>
/// Merge conflict.
/// </summary>
public sealed record MergeConflict(
    string File,
    string BaseVersion,
    string OursVersion,
    string TheirsVersion);

/// <summary>
/// Commit comparison.
/// </summary>
public sealed record CommitComparison(
    string BaseCommit,
    string TargetCommit,
    int CommitsAhead,
    int CommitsBehind,
    IReadOnlyList<SaveStateCommit> DifferentCommits);

/// <summary>
/// Commit diff.
/// </summary>
public sealed record CommitDiff(
    string Commit1,
    string Commit2,
    long SizeDifference,
    IReadOnlyList<DiffEntry> Entries);

/// <summary>
/// Diff entry.
/// </summary>
public sealed record DiffEntry(
    string Path,
    DiffType Type,
    long? OldSize,
    long? NewSize);

/// <summary>
/// Branching statistics.
/// </summary>
public sealed record BranchingStatistics(
    string GameId,
    int BranchCount,
    int CommitCount,
    int TagCount,
    int StashCount,
    long TotalSize,
    long CompressedSize,
    DateTime CalculatedAt);

/// <summary>
/// Optimization result.
/// </summary>
public sealed record OptimizationResult(
    int ObjectsRemoved,
    long SpaceReclaimed,
    TimeSpan Duration);

/// <summary>
/// Merge strategies.
/// </summary>
public enum MergeStrategy
{
    FastForward,
    Recursive,
    Ours,
    Theirs,
    NoCommit
}

/// <summary>
/// Diff types.
/// </summary>
public enum DiffType
{
    Added,
    Modified,
    Deleted,
    Renamed
}

/// <summary>
/// Event args for branch created events.
/// </summary>
public sealed class BranchCreatedEventArgs : EventArgs
{
    public string GameId { get; }
    public string BranchName { get; }
    public string? ParentBranch { get; }
    public DateTime CreatedAt { get; }

    public BranchCreatedEventArgs(string gameId, string branchName, string? parentBranch)
        : this(gameId, branchName, parentBranch, SystemTimeProvider.Instance.UtcNow)
    {
    }

    public BranchCreatedEventArgs(string gameId, string branchName, string? parentBranch, DateTime createdAt)
    {
        GameId = gameId;
        BranchName = branchName;
        ParentBranch = parentBranch;
        CreatedAt = createdAt;
    }
}

/// <summary>
/// Event args for commit created events.
/// </summary>
public sealed class CommitCreatedEventArgs : EventArgs
{
    public string GameId { get; }
    public string BranchName { get; }
    public string CommitHash { get; }
    public string Message { get; }
    public DateTime CreatedAt { get; }

    public CommitCreatedEventArgs(string gameId, string branchName, string commitHash, string message)
        : this(gameId, branchName, commitHash, message, SystemTimeProvider.Instance.UtcNow)
    {
    }

    public CommitCreatedEventArgs(string gameId, string branchName, string commitHash, string message, DateTime createdAt)
    {
        GameId = gameId;
        BranchName = branchName;
        CommitHash = commitHash;
        Message = message;
        CreatedAt = createdAt;
    }
}

/// <summary>
/// Event args for branch merged events.
/// </summary>
public sealed class BranchMergedEventArgs : EventArgs
{
    public string GameId { get; }
    public string TargetBranch { get; }
    public string SourceBranch { get; }
    public string? MergeCommitHash { get; }
    public bool HasConflicts { get; }
    public DateTime MergedAt { get; }

    public BranchMergedEventArgs(string gameId, string targetBranch, string sourceBranch, string? mergeCommitHash, bool hasConflicts)
        : this(gameId, targetBranch, sourceBranch, mergeCommitHash, hasConflicts, SystemTimeProvider.Instance.UtcNow)
    {
    }

    public BranchMergedEventArgs(string gameId, string targetBranch, string sourceBranch, string? mergeCommitHash, bool hasConflicts, DateTime mergedAt)
    {
        GameId = gameId;
        TargetBranch = targetBranch;
        SourceBranch = sourceBranch;
        MergeCommitHash = mergeCommitHash;
        HasConflicts = hasConflicts;
        MergedAt = mergedAt;
    }
}
