using SaveState.Core.Common;

namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Resolves replay file paths from various input formats.
/// </summary>
public interface IReplayPathResolver
{
    /// <summary>
    /// Resolves a replay path to an actual file path.
    /// </summary>
    Result<string> ResolveReplayPath(string replayPath);
}
