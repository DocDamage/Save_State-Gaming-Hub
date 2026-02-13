using SaveState.Core.Common;
using SaveState.Core.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing save states.
/// </summary>
public interface ISaveStateEngine
{
    /// <summary>
    /// Creates a save state.
    /// </summary>
    Task<Result<string>> CreateSaveStateAsync(int slot = -1, CancellationToken ct = default);

    /// <summary>
    /// Loads a save state from a slot.
    /// </summary>
    Task<Result> LoadSaveStateAsync(int slot, CancellationToken ct = default);

    /// <summary>
    /// Loads a save state from a file.
    /// </summary>
    Task<Result> LoadSaveStateFromFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Gets save states for a game.
    /// </summary>
    Task<Result<IReadOnlyList<SaveStateInfo>>> GetSaveStatesAsync(string gamePath, CancellationToken ct = default);

    /// <summary>
    /// Deletes a save state.
    /// </summary>
    Task<Result> DeleteSaveStateAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot.
    /// </summary>
    Task<Result<string>> CaptureScreenshotAsync(string? screenshotDirectory, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent screenshot.
    /// </summary>
    Task<Result<string?>> GetMostRecentScreenshotAsync(string screenshotDirectory, CancellationToken ct = default);
}
