namespace SaveState.Core.RetroArch.Models;

/// <summary>
/// Input mapping configuration.
/// </summary>
public class InputMapping
{
    public string RetroButton { get; init; } = string.Empty;
    public string MappedKey { get; init; } = string.Empty;
    public InputType Type { get; init; }
    public int PlayerIndex { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Types of input.
/// </summary>
public enum InputType
{
    Keyboard,
    GamepadButton,
    GamepadAxis,
    Mouse,
    Pointer,
    Lightgun
}

/// <summary>
/// Controller configuration.
/// </summary>
public class ControllerConfig
{
    public int PlayerIndex { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceGuid { get; init; } = string.Empty;
    public string DeviceIndex { get; init; } = string.Empty;
    public ControllerType ControllerType { get; init; }
    public List<InputMapping> Mappings { get; init; } = new();
    public bool AnalogToDigitalEnabled { get; init; }
    public int RumbleStrength { get; init; } = 100;
}

/// <summary>
/// Types of controllers.
/// </summary>
public enum ControllerType
{
    Unknown,
    Standard,
    ArcadeStick,
    Lightgun,
    Mouse,
    Pointer
}

/// <summary>
/// Hotkey configuration.
/// </summary>
public class HotkeyConfig
{
    public string Action { get; init; } = string.Empty;
    public string Mapping { get; init; } = string.Empty;
    public InputType InputType { get; init; }
    public bool Enable { get; init; } = true;
}

/// <summary>
/// RetroPad button enumeration.
/// </summary>
public enum RetroPadButton
{
    B,
    Y,
    Select,
    Start,
    Up,
    Down,
    Left,
    Right,
    A,
    X,
    L,
    R,
    L2,
    R2,
    L3,
    R3
}

/// <summary>
/// Analog stick configuration.
/// </summary>
public class AnalogStickConfig
{
    public string Name { get; init; } = string.Empty;
    public string XPlusMapping { get; init; } = string.Empty;
    public string XMinusMapping { get; init; } = string.Empty;
    public string YPlusMapping { get; init; } = string.Empty;
    public string YMinusMapping { get; init; } = string.Empty;
    public float Deadzone { get; init; } = 0.1f;
    public float Sensitivity { get; init; } = 1.0f;
}
