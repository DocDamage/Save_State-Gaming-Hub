namespace SaveState.Plugins.GamingAnalytics;

/// <summary>
/// String constants for Gaming Analytics Plugin.
/// </summary>
public static class GamingAnalyticsStrings
{
    // Plugin Info
    public const string PluginId = "savestate.gaming.analytics";
    public const string PluginName = "Advanced Gaming Analytics";
    public const string PluginVersion = "1.0.0";
    public const string PluginAuthor = "SaveState Team";
    public const string PluginDescription = "Deep performance analytics, pattern recognition, and gaming insights";

    // Log Messages
    public const string LogInitializing = "Initializing Advanced Gaming Analytics plugin";
    public const string LogMonitorNotAvailable = "IPerformanceMonitor not available - performance features will be limited";
    public const string LogInitialized = "Advanced Gaming Analytics plugin initialized";
    public const string LogShuttingDown = "Shutting down Advanced Gaming Analytics plugin";
    public const string LogCliCommandsRegistered = "Gaming Analytics CLI commands registered";
    public const string LogDashboardShown = "📊 === Gaming Analytics Dashboard ===";
    public const string LogTotalSessions = "Total Sessions: {0}";
    public const string LogTotalPlayTime = "Total Play Time: {0:F1} hours";
    public const string LogAverageFps = "Average FPS: {0:F1}";
    public const string LogPatternsDetected = "Patterns Detected: {0}";
    public const string LogRecentInsights = "🔍 Recent Insights:";
    public const string LogPerformanceTrending = "- Performance trending: Stable";
    public const string LogMostPlayedGame = "- Most played game: Analysis needed";
    public const string LogHardwareUtilization = "- Hardware utilization: Optimal";
    public const string LogRecommendations = "💡 Recommendations:";
    public const string LogUpdateDrivers = "- Consider updating graphics drivers";
    public const string LogEnableVSync = "- Enable V-Sync for smoother gameplay";
    public const string LogPerformanceMonitorNotAvailable = "Performance monitor not available";

    // Menu Items
    public const string MenuDashboardId = "analytics.dashboard";
    public const string MenuDashboardLabel = "Analytics Dashboard";
    public const string MenuDashboardIcon = "📊";
    public const string MenuPerformanceId = "analytics.performance";
    public const string MenuPerformanceLabel = "Performance Analysis";
    public const string MenuPerformanceIcon = "⚡";
    public const string MenuPatternsId = "analytics.patterns";
    public const string MenuPatternsLabel = "Gaming Patterns";
    public const string MenuPatternsIcon = "🔍";
    public const string MenuRecommendationsId = "analytics.recommendations";
    public const string MenuRecommendationsLabel = "Gaming Recommendations";
    public const string MenuRecommendationsIcon = "💡";
    public const string MenuHardwareId = "analytics.hardware";
    public const string MenuHardwareLabel = "Hardware Insights";
    public const string MenuHardwareIcon = "🖥️";

    // CLI Commands
    public const string CliAnalyticsDescription = "Advanced gaming analytics and insights";
    public const string CliDashboardDescription = "Show analytics dashboard";
    public const string CliPerformanceDescription = "Performance analysis commands";
    public const string CliAnalyzeDescription = "Analyze performance for a game";
    public const string CliAnalyzeGameIdDescription = "Game ID to analyze";
    public const string CliAnalyzeSessionsDescription = "Number of recent sessions to analyze";
    public const string CliCompareDescription = "Compare performance between sessions";
    public const string CliCompareSession1Description = "First session ID";
    public const string CliCompareSession2Description = "Second session ID";
    public const string CliPatternsDescription = "Gaming pattern recognition";
    public const string CliDetectDescription = "Detect patterns in gaming data";
    public const string CliShowDescription = "Show detected patterns";
    public const string CliShowTypeDescription = "Pattern type to show (all, performance, behavior, hardware)";
    public const string CliRecommendationsDescription = "Get gaming recommendations";
    public const string CliCategoryDescription = "Category (performance, hardware, settings)";
    public const string CliHardwareDescription = "Hardware utilization insights";
    public const string CliHardwareAnalyzeDescription = "Analyze hardware utilization";
    public const string CliHardwareOptimizeDescription = "Get hardware optimization suggestions";
    public const string CliTrendsDescription = "Gaming trend analysis";
    public const string CliPeriodDescription = "Time period (7d, 30d, 90d, 1y)";

    // Pattern Names
    public const string PatternPerformanceDipId = "performance_dip";
    public const string PatternPerformanceDipName = "Performance Dip";
    public const string PatternPerformanceDipDescription = "Sudden drop in FPS during gameplay";
    public const string PatternMemoryLeakId = "memory_leak";
    public const string PatternMemoryLeakName = "Memory Leak";
    public const string PatternMemoryLeakDescription = "Gradual increase in RAM usage over time";
    public const string PatternCpuBottleneckId = "cpu_bottleneck";
    public const string PatternCpuBottleneckName = "CPU Bottleneck";
    public const string PatternCpuBottleneckDescription = "CPU usage consistently high while GPU is underutilized";
    public const string PatternGamingSessionId = "gaming_session";
    public const string PatternGamingSessionName = "Gaming Session";
    public const string PatternGamingSessionDescription = "Active gaming session with stable performance";

    // Common Values
    public const string ValueAll = "all";
    public const string ValueDefaultCategory = "all";
    public const string ValueDefaultPeriod = "30d";
    public const int DefaultSessionCount = 5;
    public const float ThresholdPerformanceDip = 0.15f;
    public const float ThresholdMemoryLeak = 100f;
    public const float ThresholdCpuBottleneck = 80f;
    public const float ThresholdGamingSession = 30f;
}
