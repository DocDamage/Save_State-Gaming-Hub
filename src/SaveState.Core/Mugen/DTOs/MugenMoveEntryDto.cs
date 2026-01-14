namespace SaveState.Core.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN character move information.
/// </summary>
public class MugenMoveEntryDto
{
    /// <summary>
    /// Gets or sets the move name.
    /// </summary>
    public string MoveName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input command for the move.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the move type (e.g., Normal, Special, Super).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the damage value.
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// Gets or sets the startup frames.
    /// </summary>
    public int Startup { get; set; }

    /// <summary>
    /// Gets or sets the active frames.
    /// </summary>
    public int Active { get; set; }

    /// <summary>
    /// Gets or sets the recovery frames.
    /// </summary>
    public int Recovery { get; set; }

    /// <summary>
    /// Gets or sets the block advantage.
    /// </summary>
    public int BlockAdvantage { get; set; }

    /// <summary>
    /// Gets or sets the hit advantage.
    /// </summary>
    public int HitAdvantage { get; set; }

    /// <summary>
    /// Gets or sets the move properties (comma-separated, e.g., "projectile,safe").
    /// </summary>
    public string Properties { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional notes about the move.
    /// </summary>
    public string? Notes { get; set; }
}
