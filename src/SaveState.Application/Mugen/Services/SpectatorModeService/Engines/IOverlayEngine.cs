using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Interface for managing spectator overlays.
/// </summary>
public interface IOverlayEngine
{
    /// <summary>
    /// Gets all available overlay types.
    /// </summary>
    IReadOnlyList<string> GetAvailableOverlays();

    /// <summary>
    /// Toggles an overlay on or off for a session.
    /// </summary>
    Result ToggleOverlay(SpectatorSession session, string overlayType, bool enabled);
}
