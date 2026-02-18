using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Engine for managing spectator overlays.
/// </summary>
public class OverlayEngine : IOverlayEngine
{
    private readonly ILogger<OverlayEngine> _logger;
    private static readonly IReadOnlyList<string> DefaultOverlays = new List<string>
    {
        "HealthBars",
        "Timer",
        "ComboCounter",
        "CharacterNames",
        "InputDisplay",
        "FrameData",
        "DamageNumbers",
        "Hitboxes",
        "StageInfo",
        "Chat"
    };

    public OverlayEngine(ILogger<OverlayEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all available overlay types.
    /// </summary>
    public IReadOnlyList<string> GetAvailableOverlays()
    {
        return DefaultOverlays;
    }

    /// <summary>
    /// Toggles an overlay on or off for a session.
    /// </summary>
    public Result ToggleOverlay(SpectatorSession session, string overlayType, bool enabled)
    {
        if (!DefaultOverlays.Contains(overlayType))
        {
            return Result.Failure($"Invalid overlay type: {overlayType}");
        }

        var activeOverlays = session.ActiveOverlays?.ToList() ?? new List<string>();

        if (enabled)
        {
            if (!activeOverlays.Contains(overlayType))
            {
                activeOverlays.Add(overlayType);
            }
        }
        else
        {
            activeOverlays.Remove(overlayType);
        }

        session.ActiveOverlays = activeOverlays;

        _logger.LogInformation(
            "{Action} overlay {OverlayType} for session {SessionId}",
            enabled ? "Enabled" : "Disabled",
            overlayType,
            session.SessionId);

        return Result.Success();
    }
}
