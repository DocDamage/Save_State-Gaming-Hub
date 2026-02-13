using SaveState.Core.Common;

namespace SaveState.Core.RgbSync.Models;

/// <summary>
/// Represents an RGB device vendor.
/// </summary>
public enum RgbVendor
{
    Razer,
    Corsair,
    Logitech,
    SteelSeries,
    CoolerMaster,
    Asus,
    Msi,
    Gigabyte,
    Unknown
}

/// <summary>
/// Represents an RGB device.
/// </summary>
public record RgbDevice
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string DeviceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RgbVendor Vendor { get; init; }
    public RgbDeviceType Type { get; init; }
    public int LedCount { get; init; }
    public bool IsConnected { get; init; }
    public IReadOnlyList<RgbLed> Leds { get; init; } = Array.Empty<RgbLed>();
    public RgbRectangle? Bounds { get; init; }
}

/// <summary>
/// Types of RGB devices.
/// </summary>
public enum RgbDeviceType
{
    Keyboard,
    Mouse,
    Mousepad,
    Headset,
    HeadsetStand,
    Memory,
    Motherboard,
    GraphicsCard,
    Cooler,
    LedStrip,
    Fan,
    Case,
    Speaker,
    Monitor
}

/// <summary>
/// Represents an individual RGB LED.
/// </summary>
public record RgbLed
{
    public int Index { get; init; }
    public string Name { get; init; } = string.Empty;
    public RgbColor Color { get; init; } = new();
    public float X { get; init; }
    public float Y { get; init; }
}

/// <summary>
/// RGB color representation.
/// </summary>
public record RgbColor
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }

    public RgbColor() { }

    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public static RgbColor Black => new(0, 0, 0);
    public static RgbColor White => new(255, 255, 255);
    public static RgbColor Red => new(255, 0, 0);
    public static RgbColor Green => new(0, 255, 0);
    public static RgbColor Blue => new(0, 0, 255);
    public static RgbColor Yellow => new(255, 255, 0);
    public static RgbColor Cyan => new(0, 255, 255);
    public static RgbColor Magenta => new(255, 0, 255);
}

/// <summary>
/// Rectangle bounds for device positioning.
/// </summary>
public record RgbRectangle
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
}

/// <summary>
/// Represents an RGB lighting effect.
/// </summary>
public record RgbEffect
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public RgbEffectType Type { get; init; }
    public IReadOnlyList<RgbColor> Colors { get; init; } = Array.Empty<RgbColor>();
    public float Speed { get; init; } = 1.0f;
    public float Brightness { get; init; } = 1.0f;
    public RgbEffectDirection Direction { get; init; } = RgbEffectDirection.LeftToRight;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of RGB effects.
/// </summary>
public enum RgbEffectType
{
    Static,
    Breathing,
    Flashing,
    SpectrumCycle,
    Rainbow,
    Wave,
    Ripple,
    Reactive,
    Starlight,
    Custom
}

/// <summary>
/// Direction for RGB effects.
/// </summary>
public enum RgbEffectDirection
{
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop,
    CenterOut,
    OutCenter,
    Clockwise,
    CounterClockwise
}

/// <summary>
/// Represents a game event that triggers RGB effects.
/// </summary>
public record GameRgbEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string EventType { get; init; } = string.Empty;
    public string? GameId { get; init; }
    public RgbColor PrimaryColor { get; init; } = RgbColor.White;
    public RgbColor SecondaryColor { get; init; } = RgbColor.Black;
    public float Intensity { get; init; } = 1.0f;
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(1);
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Predefined game event types for RGB sync.
/// </summary>
public static class GameRgbEventTypes
{
    public const string GameStart = "GameStart";
    public const string GameEnd = "GameEnd";
    public const string AchievementUnlocked = "AchievementUnlocked";
    public const string LevelUp = "LevelUp";
    public const string HealthLow = "HealthLow";
    public const string HealthCritical = "HealthCritical";
    public const string ManaLow = "ManaLow";
    public const string DamageTaken = "DamageTaken";
    public const string EnemyKilled = "EnemyKilled";
    public const string ComboStreak = "ComboStreak";
    public const string Victory = "Victory";
    public const string Defeat = "Defeat";
    public const string Loading = "Loading";
    public const string Cutscene = "Cutscene";
}

/// <summary>
/// Represents health indicator configuration for RGB devices.
/// </summary>
public record HealthIndicatorConfig
{
    public bool Enabled { get; init; } = true;
    public string TargetDeviceId { get; init; } = string.Empty;
    public RgbColor FullHealthColor { get; init; } = RgbColor.Green;
    public RgbColor MediumHealthColor { get; init; } = RgbColor.Yellow;
    public RgbColor LowHealthColor { get; init; } = RgbColor.Red;
    public RgbColor CriticalHealthColor { get; init; } = new RgbColor(255, 0, 0);
    public int MediumHealthThreshold { get; init; } = 60;
    public int LowHealthThreshold { get; init; } = 30;
    public int CriticalHealthThreshold { get; init; } = 10;
    public HealthIndicatorStyle Style { get; init; } = HealthIndicatorStyle.Progressive;
}

/// <summary>
/// Styles for health indicators.
/// </summary>
public enum HealthIndicatorStyle
{
    Progressive,
    Pulsing,
    Flashing,
    Solid
}

/// <summary>
/// Configuration for RGB sync.
/// </summary>
public record RgbSyncConfiguration
{
    public bool Enabled { get; init; } = true;
    public bool SyncWithGameEvents { get; init; } = true;
    public bool HealthIndicatorEnabled { get; init; } = true;
    public HealthIndicatorConfig HealthIndicator { get; init; } = new();
    public RgbEffect DefaultEffect { get; init; } = new() { Type = RgbEffectType.SpectrumCycle };
    public IReadOnlyDictionary<string, RgbEffect> GameSpecificEffects { get; init; } = new Dictionary<string, RgbEffect>();
    public float GlobalBrightness { get; init; } = 1.0f;
}

/// <summary>
/// Represents RGB device SDK information.
/// </summary>
public record RgbSdkInfo
{
    public RgbVendor Vendor { get; init; }
    public string Version { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string? ErrorMessage { get; init; }
}
