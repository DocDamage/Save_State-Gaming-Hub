namespace SaveState.Core.RetroArch.Models;

/// <summary>
/// Detailed RetroArch configuration.
/// </summary>
public class RetroArchConfigInfo
{
    public string? SavefileDirectory { get; set; }
    public string? SavestateDirectory { get; set; }
    public string? SystemDirectory { get; set; }
    public string? CoreAssetsDirectory { get; set; }
    public string? ScreenshotDirectory { get; set; }
    public string? PlaylistDirectory { get; set; }
    public string? ContentDirectory { get; set; }
    public string? ConfigDirectory { get; set; }
    public bool CloudSyncEnabled { get; set; }
    public string? CloudSyncUrl { get; set; }
    public string? CloudSyncUsername { get; set; }
    public VideoConfig Video { get; set; } = new();
    public InputConfig Input { get; set; } = new();
    public AudioConfig Audio { get; set; } = new();
    public NetworkConfig Network { get; set; } = new();
    public List<ConfigOption> CustomOptions { get; set; } = new();
}

/// <summary>
/// Video configuration settings.
/// </summary>
public class VideoConfig
{
    public VideoDriver Driver { get; set; } = VideoDriver.D3D11;
    public bool Fullscreen { get; set; }
    public bool WindowedFullscreen { get; set; } = true;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 720;
    public int FullscreenWidth { get; set; } = 1920;
    public int FullscreenHeight { get; set; } = 1080;
    public bool VSync { get; set; } = true;
    public int RefreshRate { get; set; } = 60;
    public bool HardSync { get; set; }
    public int HardSyncFrames { get; set; }
    public bool BlackFrameInsertion { get; set; }
    public float AspectRatio { get; set; }
    public bool IntegerScale { get; set; }
    public string? ShaderPath { get; set; }
}

/// <summary>
/// Input configuration settings.
/// </summary>
public class InputConfig
{
    public InputDriver Driver { get; set; } = InputDriver.DInput;
    public int MaxPlayers { get; set; } = 2;
    public bool MenuToggleGamepadCombo { get; set; }
    public bool MenuSwapOkCancelButtons { get; set; }
    public bool InputPollTypeBehavior { get; set; }
    public List<InputDeviceConfig> Devices { get; set; } = new();
}

/// <summary>
/// Audio configuration settings.
/// </summary>
public class AudioConfig
{
    public string Driver { get; set; } = "xaudio";
    public bool Enable { get; set; } = true;
    public int Volume { get; set; } = 100;
    public bool Mute { get; set; }
    public int Latency { get; set; } = 64;
    public bool Sync { get; set; } = true;
}

/// <summary>
/// Network configuration settings.
/// </summary>
public class NetworkConfig
{
    public bool NetworkCommandEnable { get; set; }
    public string NetworkCommandHost { get; set; } = "127.0.0.1";
    public int NetworkCommandPort { get; set; } = 55355;
    public bool NetplayEnable { get; set; }
    public string? NetplayHost { get; set; }
    public int NetplayPort { get; set; } = 55435;
    public bool NetplayRequirePassword { get; set; }
}

/// <summary>
/// A configuration option.
/// </summary>
public class ConfigOption
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Input device configuration.
/// </summary>
public class InputDeviceConfig
{
    public int PlayerIndex { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceGuid { get; init; } = string.Empty;
    public Dictionary<string, string> Mappings { get; init; } = new();
    public bool AnalogToDigitalType { get; init; }
}
