using System;

namespace SaveState.Sdk;

[Flags]
public enum PluginCapabilities
{
    None = 0,
    GameProvider = 1 << 0,
    MetadataScraper = 1 << 1,
    ThemeProvider = 1 << 2,
    Importer = 1 << 3,
    Exporter = 1 << 4,
    UIExtension = 1 << 5,
    AIService = 1 << 6,
    CloudStorage = 1 << 7,
    SocialFeatures = 1 << 8,
    InputProvider = 1 << 9,
    PerformanceMonitor = 1 << 10,
    SaveStateProvider = 1 << 11,
    SystemOptimization = 1 << 12,
    LaunchExperience = 1 << 13,
    MacroSystem = 1 << 14,
    SteamDeckIntegration = 1 << 15,
    BatteryOptimization = 1 << 16,
    TouchControls = 1 << 17,
    CloudGaming = 1 << 18,
    MemoryIntelligence = 1 << 19
}
