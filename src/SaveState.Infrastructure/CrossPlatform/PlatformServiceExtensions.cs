using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.CrossPlatform;

/// <summary>
/// Platform-specific service registration helpers.
/// PHASE 7: REQUIRED - Cross-Platform Support
/// </summary>
public static class PlatformServiceExtensions
{
    /// <summary>
    /// Registers platform-specific audio services.
    /// </summary>
    public static IServiceCollection AddPlatformAudioServices(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows audio already handled by default services
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS uses MacOSAudioService
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux uses LinuxAudioService
        }

        return services;
    }

    /// <summary>
    /// Gets the current platform name.
    /// </summary>
    public static string GetPlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";

        return "Unknown";
    }

    /// <summary>
    /// Checks if running on a Unix-like system.
    /// </summary>
    public static bool IsUnixLike()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    /// <summary>
    /// Gets the .NET Runtime Identifier for the current platform.
    /// </summary>
    public static string GetRuntimeIdentifier()
    {
        return RuntimeInformation.RuntimeIdentifier;
    }
}
