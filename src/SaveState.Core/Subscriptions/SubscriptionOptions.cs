// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Subscriptions;

/// <summary>
/// Configuration options for subscription management.
/// </summary>
public class SubscriptionOptions
{
    public const string SectionName = "Subscriptions";

    /// <summary>
    /// Enables automatic syncing of subscription catalogs.
    /// </summary>
    public bool AutoSyncEnabled { get; set; } = true;

    /// <summary>
    /// Interval between automatic syncs (in hours).
    /// </summary>
    [Range(1, 168)]
    public int AutoSyncIntervalHours { get; set; } = 6;

    /// <summary>
    /// Enables notifications for games leaving soon.
    /// </summary>
    public bool LeavingSoonNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Number of days before a game leaves to trigger notifications.
    /// </summary>
    [Range(1, 30)]
    public int LeavingSoonNotificationDays { get; set; } = 7;

    /// <summary>
    /// Enables notifications for new arrivals.
    /// </summary>
    public bool NewArrivalNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Xbox Game Pass configuration.
    /// </summary>
    public XboxGamePassConfig XboxGamePass { get; set; } = new();

    /// <summary>
    /// PlayStation Plus configuration.
    /// </summary>
    public PlayStationPlusConfig PlayStationPlus { get; set; } = new();

    /// <summary>
    /// EA Play configuration.
    /// </summary>
    public EaPlayConfig EaPlay { get; set; } = new();

    /// <summary>
    /// Ubisoft+ configuration.
    /// </summary>
    public UbisoftPlusConfig UbisoftPlus { get; set; } = new();

    /// <summary>
    /// GeForce NOW configuration.
    /// </summary>
    public GeForceNowConfig GeForceNow { get; set; } = new();
}

/// <summary>
/// Xbox Game Pass specific configuration.
/// </summary>
public class XboxGamePassConfig
{
    public bool Enabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public string Region { get; set; } = "en-US";
}

/// <summary>
/// PlayStation Plus specific configuration.
/// </summary>
public class PlayStationPlusConfig
{
    public bool Enabled { get; set; } = true;
    public string? NpssoToken { get; set; }
    public string Region { get; set; } = "en-US";
}

/// <summary>
/// EA Play specific configuration.
/// </summary>
public class EaPlayConfig
{
    public bool Enabled { get; set; } = true;
    public string? Email { get; set; }
}

/// <summary>
/// Ubisoft+ specific configuration.
/// </summary>
public class UbisoftPlusConfig
{
    public bool Enabled { get; set; } = true;
    public string? Email { get; set; }
}

/// <summary>
/// GeForce NOW specific configuration.
/// </summary>
public class GeForceNowConfig
{
    public bool Enabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public string Region { get; set; } = "US";
}
