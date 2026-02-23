namespace SaveState.Core.RgbSync.Models;

public enum RgbDeviceType
{
    Keyboard,
    Mouse,
    Headset,
    Mousepad,
    HeadsetStand,
    Keypad,
    Lightbar,
    Memory,
    Gpu,
    Motherboard,
    LedStrip,
    Cooler,
    Case,
    Fan,
    Psu,
    Speaker,
    Monitor,
    Chair,
    Speakerpad
}

public enum RgbEffectType
{
    Static,
    Breathing,
    Flashing,
    ColorCycle,
    Rainbow,
    Wave,
    Ripple,
    Reactive,
    Starlight,
    Gradient,
    SpectrumCycle,
    Pulse,
    Temperature,
    GameState,
    AudioVisualizer,
    ScreenSync
}

public record RgbDevice
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public RgbDeviceType Type { get; set; }
    public int LedCount { get; set; }
    public List<RgbLed> Leds { get; set; } = new();
    public RgbZone[] Zones { get; set; } = Array.Empty<RgbZone>();
    public bool IsConnected { get; set; }
    public bool SupportsDirectMode { get; set; }
    public string? ProviderId { get; set; }
}

public record RgbLed
{
    public int Index { get; set; }
    public string? Name { get; set; }
    public RgbColor Color { get; set; } = new(0, 0, 0);
    public float Brightness { get; set; } = 1.0f;
}

public record RgbZone
{
    public string Name { get; set; } = string.Empty;
    public int StartLedIndex { get; set; }
    public int LedCount { get; set; }
}

public record RgbColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    
    public RgbColor() {}
    public RgbColor(byte r, byte g, byte b) { R = r; G = g; B = b; }
    
    public static RgbColor FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return new RgbColor(0, 0, 0);
        
        hex = hex.Trim().Replace("#", "");
        
        if (hex.Length != 6)
            return new RgbColor(0, 0, 0);
        
        try
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new RgbColor(r, g, b);
        }
        catch
        {
            return new RgbColor(0, 0, 0);
        }
    }
    
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
    
    public static RgbColor Black => new(0, 0, 0);
    public static RgbColor White => new(255, 255, 255);
    public static RgbColor Red => new(255, 0, 0);
    public static RgbColor Green => new(0, 255, 0);
    public static RgbColor Blue => new(0, 0, 255);
    public static RgbColor Yellow => new(255, 255, 0);
    public static RgbColor Cyan => new(0, 255, 255);
    public static RgbColor Magenta => new(255, 0, 255);
    public static RgbColor Orange => new(255, 165, 0);
    public static RgbColor Purple => new(128, 0, 128);
}

public record RgbEffect
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RgbEffectType Type { get; set; }
    public List<RgbColor> Colors { get; set; } = new();
    public float Speed { get; set; } = 1.0f;
    public float Brightness { get; set; } = 1.0f;
    public RgbDirection Direction { get; set; } = RgbDirection.Forward;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

public enum RgbDirection
{
    Forward,
    Backward,
    Up,
    Down,
    Inward,
    Outward
}

public record RgbProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public Dictionary<Guid, RgbEffect> DeviceEffects { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public record RgbSyncGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> DeviceIds { get; set; } = new();
    public RgbEffect SharedEffect { get; set; } = new();
}

public enum GameStateRgbTrigger
{
    HealthLow,
    HealthCritical,
    ManaLow,
    LevelUp,
    AchievementUnlocked,
    SaveStateCreated,
    BossEncounter,
    GameOver,
    Victory,
    Loading,
    Menu,
    Playing
}

public record GameStateRgbConfig
{
    public GameStateRgbTrigger Trigger { get; set; }
    public RgbEffect Effect { get; set; } = new();
    public int DurationMs { get; set; } = 3000;
    public bool Interruptible { get; set; } = true;
    public int Priority { get; set; } = 1;
}

public record RgbProviderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsEnabled { get; set; }
    public int DeviceCount { get; set; }
    public string? ConnectionStatus { get; set; }
}
