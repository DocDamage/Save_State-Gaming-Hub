using SaveState.Core.Mugen.DTOs;

namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Backwards compatibility class for MugenMoveEntryDto moved to Core layer.
/// Application layer should use the Core DTO directly or map from it.
/// </summary>
[Obsolete("Use SaveState.Core.Mugen.DTOs.MugenMoveEntryDto instead", false)]
public class MugenMoveEntry
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

    /// <summary>
    /// Create from Core DTO.
    /// </summary>
    public static MugenMoveEntry FromCore(MugenMoveEntryDto core)
    {
        return new MugenMoveEntry
        {
            MoveName = core.MoveName,
            Command = core.Command,
            Type = core.Type,
            Damage = core.Damage,
            Startup = core.Startup,
            Active = core.Active,
            Recovery = core.Recovery,
            BlockAdvantage = core.BlockAdvantage,
            HitAdvantage = core.HitAdvantage,
            Properties = core.Properties,
            Notes = core.Notes
        };
    }

    /// <summary>
    /// Convert to Core DTO.
    /// </summary>
    public MugenMoveEntryDto ToCore()
    {
        return new MugenMoveEntryDto
        {
            MoveName = MoveName,
            Command = Command,
            Type = Type,
            Damage = Damage,
            Startup = Startup,
            Active = Active,
            Recovery = Recovery,
            BlockAdvantage = BlockAdvantage,
            HitAdvantage = HitAdvantage,
            Properties = Properties,
            Notes = Notes
        };
    }
}
