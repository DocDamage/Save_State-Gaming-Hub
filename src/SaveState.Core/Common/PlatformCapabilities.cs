namespace SaveState.Core.Common;

/// <summary>
/// Detects platform-specific capabilities at runtime.
/// </summary>
public static class PlatformCapabilities
{
    public static OSPlatformType CurrentPlatform => GetCurrentPlatform();
    
    public static bool SupportsMemoryWriting => CheckMemoryWriteSupport();
    public static bool SupportsValueFreezing => CheckFreezeSupport();
    public static bool RequiresElevation => CheckElevationRequirement();
    public static string PlatformName => GetPlatformName();
    public static string PlatformVersion => GetPlatformVersion();
    
    private static OSPlatformType GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows()) return OSPlatformType.Windows;
        if (OperatingSystem.IsLinux()) return OSPlatformType.Linux;
        if (OperatingSystem.IsMacOS()) return OSPlatformType.MacOS;
        return OSPlatformType.Unknown;
    }
    
    private static bool CheckMemoryWriteSupport()
    {
        return CurrentPlatform switch
        {
            OSPlatformType.Windows => true,
            OSPlatformType.Linux => true, // With CAP_SYS_PTRACE
            OSPlatformType.MacOS => false, // SIP blocks this
            _ => false
        };
    }
    
    private static bool CheckFreezeSupport()
    {
        return CurrentPlatform switch
        {
            OSPlatformType.Windows => true,
            OSPlatformType.Linux => true, // Limited (100ms interval)
            OSPlatformType.MacOS => false,
            _ => false
        };
    }
    
    private static bool CheckElevationRequirement()
    {
        return CurrentPlatform switch
        {
            OSPlatformType.Windows => false, // Can work without admin
            OSPlatformType.Linux => true, // CAP_SYS_PTRACE or sudo needed
            OSPlatformType.MacOS => true, // Entitlements or root needed
            _ => true
        };
    }
    
    private static string GetPlatformName()
    {
        return CurrentPlatform switch
        {
            OSPlatformType.Windows => "Windows",
            OSPlatformType.Linux => "Linux",
            OSPlatformType.MacOS => "macOS",
            _ => "Unknown"
        };
    }
    
    private static string GetPlatformVersion()
    {
        return Environment.OSVersion.VersionString;
    }
    
    public static string GetWriteCapabilityExplanation()
    {
        return CurrentPlatform switch
        {
            OSPlatformType.Windows => 
                "Full memory writing support. No additional setup required.",
            OSPlatformType.Linux => 
                "Memory writing requires CAP_SYS_PTRACE capability. " +
                "Run: sudo setcap cap_sys_ptrace=eip ./SaveStateReborn",
            OSPlatformType.MacOS => 
                "Memory writing is blocked by System Integrity Protection (SIP) " +
                "and Hardened Runtime. This is intentional macOS security design. " +
                "Consider using Windows for full memory editing.",
            _ => "Platform not supported."
        };
    }
    
    public static string GetFreezeCapabilityExplanation()
    {
        return CurrentPlatform switch
        {
            OSPlatformType.Windows => 
                "Real-time value freezing at 10ms intervals. Smooth gameplay.",
            OSPlatformType.Linux => 
                "Value freezing available at 100ms intervals. " +
                "May cause slight game stuttering compared to Windows.",
            OSPlatformType.MacOS => 
                "Value freezing not available. macOS security features prevent " +
                "the continuous memory writes required for freezing.",
            _ => "Platform not supported."
        };
    }
}

/// <summary>
/// Operating system platform types.
/// </summary>
public enum OSPlatformType
{
    Windows,
    Linux,
    MacOS,
    Unknown
}
