using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;

namespace SaveState.Core.SaveStates.Services;

public interface ISaveStateBranchingService
{
    Task<Result<SaveStateBranch>> CreateBranchAsync(CreateBranchRequest request, CancellationToken ct = default);
    Task<Result<SaveStateDiff>> CompareStatesAsync(Guid stateId1, Guid stateId2, CancellationToken ct = default);
    Task<Result> MergeBranchAsync(Guid branchId, Guid targetStateId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SaveStateBranch>>> GetBranchesAsync(Guid gameId, CancellationToken ct = default);
    Task<Result<BranchTree>> GetBranchTreeAsync(Guid gameId, CancellationToken ct = default);
}

public sealed record CreateBranchRequest(
    Guid RootStateId,
    string BranchName,
    BranchType Type,
    string? Description = null);

public sealed record SaveStateDiff(
    IReadOnlyList<ChangedFile> FileChanges,
    long SizeDifference,
    TimeSpan PlaytimeDifference,
    IReadOnlyList<string> NotableChanges);

public sealed record ChangedFile(
    string FileName,
    ChangeType ChangeType,
    long SizeDifference);

public enum ChangeType
{
    Added,
    Modified,
    Deleted
}

public sealed record BranchTree(
    Guid RootId,
    IReadOnlyList<BranchNode> Nodes);

public sealed record BranchNode(
    Guid StateId,
    Guid? ParentId,
    string? BranchName,
    BranchType? BranchType,
    DateTime CreatedAt,
    int ChildCount);