using SaveState.Core.Common;
using SaveState.Core.Common.Base;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Service interface for display calibration and profile management.
/// </summary>
public interface IDisplayCalibrator
{
    /// <summary>
    /// Gets the current display settings.
    /// </summary>
    Task<Result<DisplaySettings>> GetCurrentSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a display profile for a specific game.
    /// </summary>
    Task<Result<DisplayProfile>> CreateGameProfileAsync(Guid gameId, DisplaySettings settings, CancellationToken ct = default);

    /// <summary>
    /// Gets an existing display profile.
    /// </summary>
    Task<Result<DisplayProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets all display profiles for a game.
    /// </summary>
    Task<Result<IReadOnlyList<DisplayProfile>>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Applies a display profile.
    /// </summary>
    Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Reverts to the original display settings.
    /// </summary>
    Task<Result> RevertSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available refresh rates for the primary display.
    /// </summary>
    Task<Result<IReadOnlyList<int>>> GetAvailableRefreshRatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available resolutions for the primary display.
    /// </summary>
    Task<Result<IReadOnlyList<DisplayResolution>>> GetAvailableResolutionsAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents display settings.
/// </summary>
public sealed record DisplaySettings(
    int Width,
    int Height,
    int RefreshRate,
    bool VSync,
    bool HdrEnabled,
    bool GSync,
    bool FullscreenOptimizations,
    int BitDepth = 32);

/// <summary>
/// Represents a display resolution option.
/// </summary>
public sealed record DisplayResolution(
    int Width,
    int Height,
    string AspectRatio);

/// <summary>
/// Represents a saved display profile for a game.
/// </summary>
public class DisplayProfile : EntityBase
{
    public Guid? GameId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public DisplaySettings Settings { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAppliedAt { get; private set; }

    private DisplayProfile() { }

    public static DisplayProfile Create(Guid? gameId, string name, DisplaySettings settings, bool isDefault = false)
    {
        return new DisplayProfile
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Settings = settings,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkApplied() => LastAppliedAt = DateTime.UtcNow;
    public void SetAsDefault(bool isDefault) => IsDefault = isDefault;
    public void UpdateSettings(DisplaySettings settings) => Settings = settings;
}
