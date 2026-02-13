namespace SaveState.Core.RetroArch.Models;

/// <summary>
/// Display settings for RetroArch.
/// </summary>
public class DisplaySettings
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int RefreshRate { get; init; }
    public bool IsFullscreen { get; init; }
    public bool IsWindowedFullscreen { get; init; }
    public int PositionX { get; init; }
    public int PositionY { get; init; }
    public float CurrentAspectRatio { get; init; }
    public string? CurrentShader { get; init; }
}

/// <summary>
/// Shader configuration.
/// </summary>
public class ShaderConfig
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public ShaderType Type { get; init; }
    public Dictionary<string, float> Parameters { get; init; } = new();
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Types of shaders.
/// </summary>
public enum ShaderType
{
    Cg,
    GLSL,
    Slang,
    Preset
}

/// <summary>
/// Overlay configuration.
/// </summary>
public class OverlayConfig
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public float Opacity { get; init; } = 0.7f;
    public bool FullScreen { get; init; }
    public bool HideInMenu { get; init; } = true;
    public List<OverlayElement> Elements { get; init; } = new();
}

/// <summary>
/// An element in an overlay.
/// </summary>
public class OverlayElement
{
    public string Id { get; init; } = string.Empty;
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public string? ImagePath { get; init; }
    public string? MappedInput { get; init; }
}
