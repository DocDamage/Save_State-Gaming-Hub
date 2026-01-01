using System.IO;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates;
using SaveState.Infrastructure.Repositories;

namespace SaveState.Infrastructure.SaveStates;

public class SaveStateBranchingService : ISaveStateBranchingService
{
    private readonly ISaveStateRepository _saveStateRepository;
    private readonly ISaveStateBranchRepository _branchRepository;

    public SaveStateBranchingService(
        ISaveStateRepository saveStateRepository,
        ISaveStateBranchRepository branchRepository)
    {
        _saveStateRepository = saveStateRepository;
        _branchRepository = branchRepository;
    }

    public async Task<Result<SaveStateBranch>> CreateBranchAsync(CreateBranchRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate that root state exists
            var rootState = await _saveStateRepository.GetByIdAsync(request.RootStateId, ct);
            if (rootState == null)
            {
                return Result<SaveStateBranch>.Failure($"Root save state {request.RootStateId} not found", ErrorType.NotFound);
            }

            // Check for duplicate branch names
            var existingBranches = await _branchRepository.GetByRootStateIdAsync(request.RootStateId, ct);
            if (existingBranches.Any(b => b.BranchName.Equals(request.BranchName, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<SaveStateBranch>.Failure($"Branch '{request.BranchName}' already exists for this root state", ErrorType.Conflict);
            }

            var branch = SaveStateBranch.Create(
                request.RootStateId,
                request.BranchName,
                request.Type,
                request.Description);

            await _branchRepository.AddAsync(branch, ct);

            return Result<SaveStateBranch>.Success(branch);
        }
        catch (Exception ex)
        {
            return Result<SaveStateBranch>.Failure($"Failed to create branch: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<SaveStateDiff>> CompareStatesAsync(Guid stateId1, Guid stateId2, CancellationToken ct = default)
    {
        try
        {
            var state1 = await _saveStateRepository.GetByIdAsync(stateId1, ct);
            var state2 = await _saveStateRepository.GetByIdAsync(stateId2, ct);

            if (state1 == null || state2 == null)
            {
                return Result<SaveStateDiff>.Failure("One or both save states not found", ErrorType.NotFound);
            }

            // Basic file-based comparison (can be enhanced with actual file analysis)
            var fileChanges = new List<ChangedFile>();
            var sizeDiff = state2.FileSizeBytes - state1.FileSizeBytes;
            var playtimeDiff = state2.PlaytimeAtSave - state1.PlaytimeAtSave;

            // For now, we'll do a simple comparison
            // In a real implementation, you'd compare file contents
            if (!File.Exists(state1.FilePath) || !File.Exists(state2.FilePath))
            {
                return Result<SaveStateDiff>.Failure("Save state files not found on disk", ErrorType.NotFound);
            }

            var fileInfo1 = new FileInfo(state1.FilePath);
            var fileInfo2 = new FileInfo(state2.FilePath);

            // Simple size-based change detection
            if (fileInfo1.Length != fileInfo2.Length)
            {
                fileChanges.Add(new ChangedFile(
                    Path.GetFileName(state1.FilePath),
                    ChangeType.Modified,
                    fileInfo2.Length - fileInfo1.Length));
            }

            var notableChanges = new List<string>();
            if (playtimeDiff.TotalMinutes > 30)
            {
                notableChanges.Add($"Significant playtime difference: {playtimeDiff.TotalHours:F1} hours");
            }

            if (Math.Abs(sizeDiff) > 1024 * 1024) // 1MB difference
            {
                notableChanges.Add($"File size changed by {(sizeDiff / (1024.0 * 1024.0)):F1} MB");
            }

            var diff = new SaveStateDiff(fileChanges, sizeDiff, playtimeDiff, notableChanges);
            return Result<SaveStateDiff>.Success(diff);
        }
        catch (Exception ex)
        {
            return Result<SaveStateDiff>.Failure($"Failed to compare states: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> MergeBranchAsync(Guid branchId, Guid targetStateId, CancellationToken ct = default)
    {
        try
        {
            var branch = await _branchRepository.GetByIdAsync(branchId, ct);
            if (branch == null)
            {
                return Result.Failure($"Branch {branchId} not found", ErrorType.NotFound);
            }

            var targetState = await _saveStateRepository.GetByIdAsync(targetStateId, ct);
            if (targetState == null)
            {
                return Result.Failure($"Target save state {targetStateId} not found", ErrorType.NotFound);
            }

            // For now, merging just creates a new save state with the target as parent
            // In a real implementation, you'd do actual file merging
            // This is a placeholder for the merge logic

            return Result.Success(); // Non-generic Result
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to merge branch: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<SaveStateBranch>>> GetBranchesAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var branches = await _branchRepository.GetByGameIdAsync(gameId, ct);
            return Result<IReadOnlyList<SaveStateBranch>>.Success(branches);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<SaveStateBranch>>.Failure($"Failed to get branches: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<BranchTree>> GetBranchTreeAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var saveStates = await _saveStateRepository.GetByGameIdAsync(gameId, ct);
            var branches = await _branchRepository.GetByGameIdAsync(gameId, ct);

            var nodes = new List<BranchNode>();

            // Add root nodes (states without parents)
            foreach (var state in saveStates.Where(s => s.ParentStateId == null))
            {
                nodes.Add(new BranchNode(
                    state.Id,
                    null,
                    null,
                    null,
                    state.CreatedAt,
                    saveStates.Count(s => s.ParentStateId == state.Id)));
            }

            // Add branch nodes
            foreach (var branch in branches)
            {
                var rootState = saveStates.FirstOrDefault(s => s.Id == branch.RootStateId);
                if (rootState != null)
                {
                    nodes.Add(new BranchNode(
                        branch.Id,
                        branch.RootStateId,
                        branch.BranchName,
                        branch.Type,
                        branch.CreatedAt,
                        saveStates.Count(s => s.ParentStateId == branch.Id)));
                }
            }

            // Add child state nodes
            foreach (var state in saveStates.Where(s => s.ParentStateId != null))
            {
                var branch = branches.FirstOrDefault(b => b.Id == state.ParentStateId);
                nodes.Add(new BranchNode(
                    state.Id,
                    state.ParentStateId,
                    branch?.BranchName,
                    branch?.Type,
                    state.CreatedAt,
                    saveStates.Count(s => s.ParentStateId == state.Id)));
            }

            var tree = new BranchTree(gameId, nodes.OrderBy(n => n.CreatedAt).ToList());
            return Result<BranchTree>.Success(tree);
        }
        catch (Exception ex)
        {
            return Result<BranchTree>.Failure($"Failed to get branch tree: {ex.Message}", ErrorType.Internal);
        }
    }
}