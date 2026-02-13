using SaveState.Core.Common;
using SaveState.Core.RetroArch;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for detecting RetroArch installation paths.
/// </summary>
public interface IPathDetectionEngine
{
    /// <summary>
    /// Detects RetroArch installation path.
    /// </summary>
    Task<Result<string>> DetectRetroArchPathAsync(RetroArchOptions options, CancellationToken ct = default);

    /// <summary>
    /// Validates if the given path is a valid RetroArch installation.
    /// </summary>
    bool IsValidRetroArchPath(string path);

    /// <summary>
    /// Gets the common paths where RetroArch might be installed.
    /// </summary>
    IReadOnlyList<string> GetCommonInstallationPaths();

    /// <summary>
    /// Gets the RetroArch version from the executable.
    /// </summary>
    Task<Result<string>> GetVersionAsync(string retroArchPath, CancellationToken ct = default);
}
