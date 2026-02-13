namespace SaveState.Core.RetroArch.Models;

/// <summary>
/// Types of RetroArch cores.
/// </summary>
public enum CoreType
{
    Unknown,
    Nintendo,
    Sega,
    PlayStation,
    Arcade,
    HomeComputer,
    Handheld,
    Atari,
    NEC,
    SNK,
    Microsoft
}

/// <summary>
/// Save state format supported by RetroArch.
/// </summary>
public enum SaveStateFormat
{
    Standard,
    Libretro,
    CoreSpecific
}

/// <summary>
/// Video driver types available in RetroArch.
/// </summary>
public enum VideoDriver
{
    D3D11,
    D3D12,
    OpenGL,
    Vulkan,
    Software
}

/// <summary>
/// Input driver types available in RetroArch.
/// </summary>
public enum InputDriver
{
    DInput,
    XInput,
    Raw,
    SDL
}

/// <summary>
/// Cloud sync provider types.
/// </summary>
public enum CloudSyncProvider
{
    None,
    AwsS3,
    AzureBlob,
    GoogleCloud
}
