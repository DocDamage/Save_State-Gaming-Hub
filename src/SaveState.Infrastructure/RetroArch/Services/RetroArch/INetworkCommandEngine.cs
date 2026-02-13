using SaveState.Core.Common;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for sending network commands to RetroArch.
/// </summary>
public interface INetworkCommandEngine
{
    /// <summary>
    /// Sends a command to RetroArch.
    /// </summary>
    Task<Result<string>> SendCommandAsync(string command, CancellationToken ct = default);

    /// <summary>
    /// Checks if RetroArch is running and responding to commands.
    /// </summary>
    Task<Result<bool>> IsRunningAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the RetroArch version.
    /// </summary>
    Task<Result<string>> GetVersionAsync(CancellationToken ct = default);

    /// <summary>
    /// Pauses the running core.
    /// </summary>
    Task<Result> PauseAsync(CancellationToken ct = default);

    /// <summary>
    /// Resumes the running core.
    /// </summary>
    Task<Result> ResumeAsync(CancellationToken ct = default);

    /// <summary>
    /// Resets the running core.
    /// </summary>
    Task<Result> ResetAsync(CancellationToken ct = default);

    /// <summary>
    /// Toggles the RetroArch menu.
    /// </summary>
    Task<Result> ToggleMenuAsync(CancellationToken ct = default);

    /// <summary>
    /// Quits RetroArch.
    /// </summary>
    Task<Result> QuitAsync(CancellationToken ct = default);
}
