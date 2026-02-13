using SaveState.Core.Common;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Interface for discovering stable pointer paths to dynamic memory addresses.
/// </summary>
public interface IPointerPathFinder
{
    /// <summary>
    /// Finds pointer paths that lead to a target address.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="targetAddress">The address currently holding the value.</param>
    /// <param name="maxDepth">Maximum number of offsets in the chain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing a list of discovered pointer paths.</returns>
    Task<Result<IReadOnlyList<PointerPath>>> FindPathsAsync(
        int processId,
        long targetAddress,
        int maxDepth = 2,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a discovered pointer path.
/// </summary>
public record PointerPath(string ModuleName, long BaseOffset, IReadOnlyList<long> Offsets)
{
    public override string ToString() => $"[{ModuleName} + 0x{BaseOffset:X}]" +
        (Offsets.Count > 0 ? " -> " + string.Join(" -> ", Offsets.Select(o => $"0x{o:X}")) : "");
}
