using SaveState.Core.Common;
using SaveState.Core.RetroArch;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing RetroArch cores.
/// </summary>
public interface ICoreManagementEngine
{
    /// <summary>
    /// Gets installed cores.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchCore>>> GetInstalledCoresAsync(string retroArchPath, string? coresPathOverride, CancellationToken ct = default);

    /// <summary>
    /// Gets available cores for download.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchCore>>> GetAvailableCoresAsync(CancellationToken ct = default);

    /// <summary>
    /// Installs a core.
    /// </summary>
    Task<Result> InstallCoreAsync(string retroArchPath, string coreName, CancellationToken ct = default);

    /// <summary>
    /// Updates a core.
    /// </summary>
    Task<Result> UpdateCoreAsync(string retroArchPath, string coreName, CancellationToken ct = default);

    /// <summary>
    /// Uninstalls a core.
    /// </summary>
    Task<Result> UninstallCoreAsync(string coresDirectory, string coreName, CancellationToken ct = default);

    /// <summary>
    /// Gets core info file content.
    /// </summary>
    Task<Result<string>> GetCoreInfoAsync(string coresDirectory, string coreName, CancellationToken ct = default);
}
