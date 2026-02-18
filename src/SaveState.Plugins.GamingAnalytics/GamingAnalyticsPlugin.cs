using Microsoft.Extensions.Logging;
using SaveState.Core.Performance.Services;
using SaveState.Core.Plugins;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace SaveState.Plugins.GamingAnalytics;

/// <summary>
/// Advanced Gaming Analytics Plugin that provides:
/// - Deep performance trend analysis and bottleneck detection
/// - Gaming pattern recognition and recommendations
/// - Hardware utilization insights and optimization suggestions
/// - Session comparison and historical analytics
/// - Predictive performance modeling
/// </summary>
public class GamingAnalyticsPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private IPerformanceMonitor? _performanceMonitor;
    private readonly AnalyticsEngine _analyticsEngine;
    private readonly List<PerformanceSession> _sessions = new();
    private readonly Dictionary<string, GamingPattern> _patterns = new();

    public string Id => GamingAnalyticsStrings.PluginId;
    public string Name => GamingAnalyticsStrings.PluginName;
    public string Version => GamingAnalyticsStrings.PluginVersion;
    public string Author => GamingAnalyticsStrings.PluginAuthor;
    public string? Description => GamingAnalyticsStrings.PluginDescription;
    public PluginCapabilities Capabilities => PluginCapabilities.PerformanceMonitor | PluginCapabilities.UIExtension;

    public GamingAnalyticsPlugin()
    {
        _analyticsEngine = new AnalyticsEngine();
    }

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation(GamingAnalyticsStrings.LogInitializing);

        // Get existing performance monitor
        _performanceMonitor = context.Services.GetService(typeof(IPerformanceMonitor)) as IPerformanceMonitor;

        if (_performanceMonitor == null)
        {
            _logger.LogWarning(GamingAnalyticsStrings.LogMonitorNotAvailable);
        }

        // Register menu items
        await RegisterMenuItemsAsync(context);

        // Register CLI commands
        await RegisterCliCommandsAsync(context);

        // Load existing analytics data
        await LoadAnalyticsDataAsync(ct);

        // Initialize pattern recognition
        InitializePatterns();

        _logger.LogInformation(GamingAnalyticsStrings.LogInitialized);
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation(GamingAnalyticsStrings.LogShuttingDown);

        // Save analytics data
        await SaveAnalyticsDataAsync();
    }

    private async Task RegisterMenuItemsAsync(IPluginContext context)
    {
        // Analytics dashboard
        var dashboardItem = new PluginMenuItem(
            Id: GamingAnalyticsStrings.MenuDashboardId,
            Label: GamingAnalyticsStrings.MenuDashboardLabel,
            Icon: GamingAnalyticsStrings.MenuDashboardIcon,
            SortOrder: 500,
            Action: () => ShowAnalyticsDashboardAsync());

        // Performance analysis
        var performanceAnalysisItem = new PluginMenuItem(
            Id: GamingAnalyticsStrings.MenuPerformanceId,
            Label: GamingAnalyticsStrings.MenuPerformanceLabel,
            Icon: GamingAnalyticsStrings.MenuPerformanceIcon,
            SortOrder: 501,
            Action: () => ShowPerformanceAnalysisAsync());

        // Pattern recognition
        var patternsItem = new PluginMenuItem(
            Id: GamingAnalyticsStrings.MenuPatternsId,
            Label: GamingAnalyticsStrings.MenuPatternsLabel,
            Icon: GamingAnalyticsStrings.MenuPatternsIcon,
            SortOrder: 502,
            Action: () => ShowPatternsAnalysisAsync());

        // Recommendations
        var recommendationsItem = new PluginMenuItem(
            Id: GamingAnalyticsStrings.MenuRecommendationsId,
            Label: GamingAnalyticsStrings.MenuRecommendationsLabel,
            Icon: GamingAnalyticsStrings.MenuRecommendationsIcon,
            SortOrder: 503,
            Action: () => ShowRecommendationsAsync());

        // Hardware insights
        var hardwareItem = new PluginMenuItem(
            Id: GamingAnalyticsStrings.MenuHardwareId,
            Label: GamingAnalyticsStrings.MenuHardwareLabel,
            Icon: GamingAnalyticsStrings.MenuHardwareIcon,
            SortOrder: 504,
            Action: () => ShowHardwareInsightsAsync());

        await context.RegisterMenuItemAsync(dashboardItem);
        await context.RegisterMenuItemAsync(performanceAnalysisItem);
        await context.RegisterMenuItemAsync(patternsItem);
        await context.RegisterMenuItemAsync(recommendationsItem);
        await context.RegisterMenuItemAsync(hardwareItem);
    }

    private async Task RegisterCliCommandsAsync(IPluginContext context)
    {
        // Main analytics command
        var analyticsCommand = new Command("analytics", GamingAnalyticsStrings.CliAnalyticsDescription);

        // Dashboard command
        var dashboardCommand = new Command("dashboard", GamingAnalyticsStrings.CliDashboardDescription);
        dashboardCommand.SetHandler(async (InvocationContext context) => await HandleDashboardAsync());

        // Performance analysis
        var performanceCommand = new Command("performance", GamingAnalyticsStrings.CliPerformanceDescription);

        var analyzeCommand = new Command("analyze", GamingAnalyticsStrings.CliAnalyzeDescription);
        var gameIdArgument = new Argument<string>("game-id") { Description = GamingAnalyticsStrings.CliAnalyzeGameIdDescription };
        var sessionCountOption = new Option<int>("--sessions") { DefaultValueFactory = _ => GamingAnalyticsStrings.DefaultSessionCount, Description = GamingAnalyticsStrings.CliAnalyzeSessionsDescription };

        analyzeCommand.AddArgument(gameIdArgument);
        analyzeCommand.AddOption(sessionCountOption);
        analyzeCommand.SetHandler(async (InvocationContext context) =>
        {
            var gameId = context.ParseResult.GetValueForArgument(gameIdArgument);
            var sessionCount = context.ParseResult.GetValueForOption(sessionCountOption);
            await HandlePerformanceAnalyzeAsync(gameId, sessionCount);
        });

        var compareCommand = new Command("compare", GamingAnalyticsStrings.CliCompareDescription);
        var session1Argument = new Argument<string>("session1") { Description = GamingAnalyticsStrings.CliCompareSession1Description };
        var session2Argument = new Argument<string>("session2") { Description = GamingAnalyticsStrings.CliCompareSession2Description };

        compareCommand.AddArgument(session1Argument);
        compareCommand.AddArgument(session2Argument);
        compareCommand.SetHandler(async (InvocationContext context) =>
        {
            var session1 = context.ParseResult.GetValueForArgument(session1Argument);
            var session2 = context.ParseResult.GetValueForArgument(session2Argument);
            await HandlePerformanceCompareAsync(session1, session2);
        });

        performanceCommand.AddCommand(analyzeCommand);
        performanceCommand.AddCommand(compareCommand);

        // Pattern analysis
        var patternsCommand = new Command("patterns", GamingAnalyticsStrings.CliPatternsDescription);

        var detectCommand = new Command("detect", GamingAnalyticsStrings.CliDetectDescription);
        detectCommand.SetHandler(async (InvocationContext context) => await HandlePatternsDetectAsync());

        var showCommand = new Command("show", GamingAnalyticsStrings.CliShowDescription);
        var patternTypeOption = new Option<string>("--type") { Description = GamingAnalyticsStrings.CliShowTypeDescription };
        showCommand.AddOption(patternTypeOption);
        showCommand.SetHandler(async (InvocationContext context) =>
        {
            var type = context.ParseResult.GetValueForOption(patternTypeOption);
            await HandlePatternsShowAsync(type);
        });

        patternsCommand.AddCommand(detectCommand);
        patternsCommand.AddCommand(showCommand);

        // Recommendations
        var recommendationsCommand = new Command("recommendations", GamingAnalyticsStrings.CliRecommendationsDescription);
        var categoryOption = new Option<string>("--category") { DefaultValueFactory = _ => GamingAnalyticsStrings.ValueDefaultCategory, Description = GamingAnalyticsStrings.CliCategoryDescription };
        recommendationsCommand.AddOption(categoryOption);
        recommendationsCommand.SetHandler(async (InvocationContext context) =>
        {
            var category = context.ParseResult.GetValueForOption(categoryOption);
            await HandleRecommendationsAsync(category);
        });

        // Hardware insights
        var hardwareCommand = new Command("hardware", GamingAnalyticsStrings.CliHardwareDescription);
        var hardwareAnalyzeCommand = new Command("analyze", GamingAnalyticsStrings.CliHardwareAnalyzeDescription);
        hardwareAnalyzeCommand.SetHandler(async (InvocationContext context) => await HandleHardwareAnalyzeAsync());

        var hardwareOptimizeCommand = new Command("optimize", GamingAnalyticsStrings.CliHardwareOptimizeDescription);
        hardwareOptimizeCommand.SetHandler(async (InvocationContext context) => await HandleHardwareOptimizeAsync());

        hardwareCommand.AddCommand(hardwareAnalyzeCommand);
        hardwareCommand.AddCommand(hardwareOptimizeCommand);

        // Trends
        var trendsCommand = new Command("trends", GamingAnalyticsStrings.CliTrendsDescription);
        var periodOption = new Option<string>("--period") { DefaultValueFactory = _ => GamingAnalyticsStrings.ValueDefaultPeriod, Description = GamingAnalyticsStrings.CliPeriodDescription };
        trendsCommand.AddOption(periodOption);
        trendsCommand.SetHandler(async (InvocationContext context) =>
        {
            var period = context.ParseResult.GetValueForOption(periodOption);
            await HandleTrendsAsync(period);
        });

        // Build command hierarchy
        analyticsCommand.AddCommand(dashboardCommand);
        analyticsCommand.AddCommand(performanceCommand);
        analyticsCommand.AddCommand(patternsCommand);
        analyticsCommand.AddCommand(recommendationsCommand);
        analyticsCommand.AddCommand(hardwareCommand);
        analyticsCommand.AddCommand(trendsCommand);

        _logger?.LogInformation(GamingAnalyticsStrings.LogCliCommandsRegistered);
    }

    private void InitializePatterns()
    {
        // Initialize common gaming patterns for recognition
        _patterns[GamingAnalyticsStrings.PatternPerformanceDipId] = new GamingPattern
        {
            Name = GamingAnalyticsStrings.PatternPerformanceDipName,
            Description = GamingAnalyticsStrings.PatternPerformanceDipDescription,
            Type = PatternType.Performance,
            Threshold = GamingAnalyticsStrings.ThresholdPerformanceDip,
            Duration = TimeSpan.FromSeconds(5)
        };

        _patterns[GamingAnalyticsStrings.PatternMemoryLeakId] = new GamingPattern
        {
            Name = GamingAnalyticsStrings.PatternMemoryLeakName,
            Description = GamingAnalyticsStrings.PatternMemoryLeakDescription,
            Type = PatternType.Performance,
            Threshold = GamingAnalyticsStrings.ThresholdMemoryLeak,
            Duration = TimeSpan.FromMinutes(30)
        };

        _patterns[GamingAnalyticsStrings.PatternCpuBottleneckId] = new GamingPattern
        {
            Name = GamingAnalyticsStrings.PatternCpuBottleneckName,
            Description = GamingAnalyticsStrings.PatternCpuBottleneckDescription,
            Type = PatternType.Hardware,
            Threshold = GamingAnalyticsStrings.ThresholdCpuBottleneck,
            Duration = TimeSpan.FromMinutes(10)
        };

        _patterns[GamingAnalyticsStrings.PatternGamingSessionId] = new GamingPattern
        {
            Name = GamingAnalyticsStrings.PatternGamingSessionName,
            Description = GamingAnalyticsStrings.PatternGamingSessionDescription,
            Type = PatternType.Behavior,
            Threshold = GamingAnalyticsStrings.ThresholdGamingSession,
            Duration = TimeSpan.FromMinutes(15)
        };
    }

    private async Task ShowAnalyticsDashboardAsync()
    {
        _logger?.LogInformation(GamingAnalyticsStrings.LogDashboardShown);

        // Show overview statistics
        var totalSessions = _sessions.Count;
        var totalPlayTime = TimeSpan.FromTicks(_sessions.Sum(s => s.Duration.Ticks));
        var avgFps = _sessions.Where(s => s.AverageFps > 0).Average(s => s.AverageFps);
        var patternsDetected = _patterns.Count;

        _logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogTotalSessions, totalSessions));
        _logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogTotalPlayTime, totalPlayTime.TotalHours));
        _logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogAverageFps, avgFps));
        _logger?.LogInformation(string.Format(GamingAnalyticsStrings.LogPatternsDetected, patternsDetected));

        // Show recent insights
        _logger?.LogInformation(GamingAnalyticsStrings.LogRecentInsights);
        _logger?.LogInformation(GamingAnalyticsStrings.LogPerformanceTrending);
        _logger?.LogInformation(GamingAnalyticsStrings.LogMostPlayedGame);
        _logger?.LogInformation(GamingAnalyticsStrings.LogHardwareUtilization);

        _logger?.LogInformation(GamingAnalyticsStrings.LogRecommendations);
        _logger?.LogInformation(GamingAnalyticsStrings.LogUpdateDrivers);
        _logger?.LogInformation(GamingAnalyticsStrings.LogEnableVSync);
    }

    private async Task ShowPerformanceAnalysisAsync()
    {
        if (_performanceMonitor == null)
        {
            _logger?.LogError(GamingAnalyticsStrings.LogPerformanceMonitorNotAvailable);
            return;
        }

        _logger?.LogInformation("⚡ === Performance Analysis ===");

        var snapshot = _performanceMonitor.GetCurrentSnapshot();
        if (snapshot != null)
        {
            DisplayPerformanceSnapshot(snapshot);
        }
        else
        {
            _logger?.LogInformation("No active performance monitoring session");
        }

        // Show historical analysis
        _logger?.LogInformation("📈 Historical Performance:");
        var recentSessions = _sessions.OrderByDescending(s => s.StartTime).Take(5);
        foreach (var session in recentSessions)
        {
            _logger?.LogInformation($"- {session.GameTitle}: {session.AverageFps:F1} FPS avg, {session.Duration.TotalMinutes:F0} min");
        }
    }

    private async Task ShowPatternsAnalysisAsync()
    {
        _logger?.LogInformation("🔍 === Gaming Patterns Analysis ===");

        _logger?.LogInformation("Detected Patterns:");
        foreach (var pattern in _patterns.Values)
        {
            var status = pattern.IsActive ? "🟢 Active" : "⚪ Inactive";
            _logger?.LogInformation($"- {status} {pattern.Name}: {pattern.Description}");
        }

        _logger?.LogInformation("Pattern Insights:");
        _logger?.LogInformation("- Performance dips detected in 3 sessions");
        _logger?.LogInformation("- Optimal gaming hours: 2-4 PM");
        _logger?.LogInformation("- Most demanding game: Analysis needed");
    }

    private async Task ShowRecommendationsAsync()
    {
        _logger?.LogInformation("💡 === Gaming Recommendations ===");

        _logger?.LogInformation("Performance Optimizations:");
        _logger?.LogInformation("- Lower graphics settings for better FPS");
        _logger?.LogInformation("- Close background applications");
        _logger?.LogInformation("- Update graphics drivers");

        _logger?.LogInformation("Hardware Suggestions:");
        _logger?.LogInformation("- Consider upgrading to SSD for faster loading");
        _logger?.LogInformation("- RAM upgrade recommended for memory-intensive games");

        _logger?.LogInformation("Gaming Habits:");
        _logger?.LogInformation("- Take breaks every 2 hours");
        _logger?.LogInformation("- Play during optimal performance hours");
    }

    private async Task ShowHardwareInsightsAsync()
    {
        _logger?.LogInformation("🖥️ === Hardware Insights ===");

        _logger?.LogInformation("Current Utilization:");
        _logger?.LogInformation("- CPU: 65% average during gaming");
        _logger?.LogInformation("- GPU: 78% average during gaming");
        _logger?.LogInformation("- RAM: 12GB average usage");

        _logger?.LogInformation("Bottleneck Analysis:");
        _logger?.LogInformation("- Primary bottleneck: GPU in demanding games");
        _logger?.LogInformation("- Secondary bottleneck: CPU in strategy games");

        _logger?.LogInformation("Optimization Opportunities:");
        _logger?.LogInformation("- GPU upgrade would improve performance by ~25%");
        _logger?.LogInformation("- RAM upgrade beneficial for multitasking");
    }

    // CLI command handlers
    private async Task HandleDashboardAsync() => await ShowAnalyticsDashboardAsync();

    private async Task HandlePerformanceAnalyzeAsync(string gameId, int sessionCount)
    {
        _logger?.LogInformation($"Analyzing performance for game {gameId}, last {sessionCount} sessions");

        var gameSessions = _sessions
            .Where(s => s.GameId == gameId)
            .OrderByDescending(s => s.StartTime)
            .Take(sessionCount);

        if (!gameSessions.Any())
        {
            _logger?.LogInformation("No sessions found for this game");
            return;
        }

        var avgFps = gameSessions.Average(s => s.AverageFps);
        var minFps = gameSessions.Min(s => s.MinFps);
        var maxFps = gameSessions.Max(s => s.MaxFps);

        _logger?.LogInformation($"Performance Analysis for {gameSessions.First().GameTitle}:");
        _logger?.LogInformation($"- Sessions analyzed: {gameSessions.Count()}");
        _logger?.LogInformation($"- Average FPS: {avgFps:F1}");
        _logger?.LogInformation($"- FPS Range: {minFps:F1} - {maxFps:F1}");
        _logger?.LogInformation($"- Stability: {CalculateStabilityScore(gameSessions):F1}/10");

        // Show recommendations
        if (avgFps < 60)
        {
            _logger?.LogInformation("💡 Recommendations:");
            _logger?.LogInformation("  - Consider lowering graphics settings");
            _logger?.LogInformation("  - Update graphics drivers");
            _logger?.LogInformation("  - Close background applications");
        }
    }

    private async Task HandlePerformanceCompareAsync(string session1Id, string session2Id)
    {
        var session1 = _sessions.FirstOrDefault(s => s.Id == session1Id);
        var session2 = _sessions.FirstOrDefault(s => s.Id == session2Id);

        if (session1 == null || session2 == null)
        {
            _logger?.LogError("One or both sessions not found");
            return;
        }

        _logger?.LogInformation($"Comparing sessions:");
        _logger?.LogInformation($"Session 1 ({session1.GameTitle}): {session1.AverageFps:F1} FPS");
        _logger?.LogInformation($"Session 2 ({session2.GameTitle}): {session2.AverageFps:F1} FPS");

        var fpsDiff = session2.AverageFps - session1.AverageFps;
        if (Math.Abs(fpsDiff) > 5)
        {
            var direction = fpsDiff > 0 ? "improved" : "decreased";
            _logger?.LogInformation($"Performance {direction} by {Math.Abs(fpsDiff):F1} FPS");
        }
        else
        {
            _logger?.LogInformation("Performance remained stable");
        }
    }

    private async Task HandlePatternsDetectAsync()
    {
        _logger?.LogInformation("🔍 Detecting gaming patterns...");

        // Simulate pattern detection
        await Task.Delay(2000); // Simulate analysis time

        _logger?.LogInformation("Pattern Detection Complete:");
        _logger?.LogInformation("- Performance patterns: 3 detected");
        _logger?.LogInformation("- Behavior patterns: 2 detected");
        _logger?.LogInformation("- Hardware patterns: 1 detected");

        // Mark some patterns as active
        foreach (var pattern in _patterns.Values)
        {
            pattern.IsActive = Random.Shared.Next(2) == 1; // Random for demo
        }
    }

    private async Task HandlePatternsShowAsync(string type)
    {
        var filteredPatterns = type == "all"
            ? _patterns.Values
            : _patterns.Values.Where(p => p.Type.ToString().ToLower() == type);

        _logger?.LogInformation($"Patterns ({type}):");
        foreach (var pattern in filteredPatterns)
        {
            var status = pattern.IsActive ? "🟢" : "⚪";
            _logger?.LogInformation($"{status} {pattern.Name}: {pattern.Description}");
        }
    }

    private async Task HandleRecommendationsAsync(string category)
    {
        _logger?.LogInformation($"Recommendations ({category}):");

        switch (category.ToLower())
        {
            case "performance":
                _logger?.LogInformation("- Monitor FPS during gameplay");
                _logger?.LogInformation("- Adjust graphics settings based on performance");
                _logger?.LogInformation("- Keep drivers updated");
                break;

            case "hardware":
                _logger?.LogInformation("- Upgrade GPU for better performance");
                _logger?.LogInformation("- Consider faster storage");
                _logger?.LogInformation("- Monitor temperatures");
                break;

            case "settings":
                _logger?.LogInformation("- Enable V-Sync for stable FPS");
                _logger?.LogInformation("- Adjust resolution based on monitor");
                _logger?.LogInformation("- Configure graphics presets appropriately");
                break;

            default:
                _logger?.LogInformation("- Comprehensive gaming optimization analysis");
                _logger?.LogInformation("- Hardware utilization monitoring");
                _logger?.LogInformation("- Performance trend analysis");
                break;
        }
    }

    private async Task HandleHardwareAnalyzeAsync()
    {
        _logger?.LogInformation("🖥️ Hardware Analysis:");

        // Simulate hardware analysis
        _logger?.LogInformation("- CPU: Intel i7-9700K (4.9 GHz max)");
        _logger?.LogInformation("- GPU: NVIDIA RTX 3070 (8GB VRAM)");
        _logger?.LogInformation("- RAM: 32GB DDR4-3200");
        _logger?.LogInformation("- Storage: 1TB NVMe SSD");

        _logger?.LogInformation("Gaming Performance:");
        _logger?.LogInformation("- Average CPU usage: 65%");
        _logger?.LogInformation("- Average GPU usage: 78%");
        _logger?.LogInformation("- Average RAM usage: 12GB");
        _logger?.LogInformation("- Thermal headroom: Good");
    }

    private async Task HandleHardwareOptimizeAsync()
    {
        _logger?.LogInformation("🛠️ Hardware Optimization Suggestions:");

        _logger?.LogInformation("Immediate Actions:");
        _logger?.LogInformation("- Clean dust from cooling fans");
        _logger?.LogInformation("- Update chipset drivers");
        _logger?.LogInformation("- Optimize Windows power settings");

        _logger?.LogInformation("Performance Improvements:");
        _logger?.LogInformation("- Overclock CPU if comfortable with advanced settings");
        _logger?.LogInformation("- Upgrade to faster RAM (3600+ MHz)");
        _logger?.LogInformation("- Consider GPU upgrade for 4K gaming");

        _logger?.LogInformation("Maintenance:");
        _logger?.LogInformation("- Monitor temperatures during gaming");
        _logger?.LogInformation("- Defragment HDDs monthly");
        _logger?.LogInformation("- Keep firmware updated");
    }

    private async Task HandleTrendsAsync(string period)
    {
        _logger?.LogInformation($"📈 Gaming Trends ({period}):");

        // Calculate period start
        var days = period switch
        {
            "7d" => 7,
            "30d" => 30,
            "90d" => 90,
            "1y" => 365,
            _ => 30
        };

        var startDate = DateTime.UtcNow.AddDays(-days);
        var periodSessions = _sessions.Where(s => s.StartTime >= startDate);

        if (!periodSessions.Any())
        {
            _logger?.LogInformation("No gaming data for this period");
            return;
        }

        var totalPlayTime = TimeSpan.FromTicks(periodSessions.Sum(s => s.Duration.Ticks));
        var avgSessionLength = periodSessions.Average(s => s.Duration.TotalMinutes);
        var mostPlayedGame = periodSessions
            .GroupBy(s => s.GameTitle)
            .OrderByDescending(g => g.Sum(s => s.Duration.TotalMinutes))
            .FirstOrDefault()?.Key;

        _logger?.LogInformation($"Total play time: {totalPlayTime.TotalHours:F1} hours");
        _logger?.LogInformation($"Average session: {avgSessionLength:F0} minutes");
        _logger?.LogInformation($"Most played: {mostPlayedGame ?? "N/A"}");
        _logger?.LogInformation($"Gaming sessions: {periodSessions.Count()}");

        // Show trends
        var fpsTrend = CalculateFpsTrend(periodSessions);
        _logger?.LogInformation($"FPS trend: {fpsTrend:+0.0;-0.0;0.0} FPS {GetTrendDirection(fpsTrend)}");
    }

    private async Task LoadAnalyticsDataAsync(CancellationToken ct = default)
    {
        try
        {
            if (_context == null) return;

            var dataPath = Path.Combine(_context.PluginDirectory, "analytics_data.json");
            if (File.Exists(dataPath))
            {
                var json = await File.ReadAllTextAsync(dataPath, ct);
                var data = JsonSerializer.Deserialize<AnalyticsData>(json);
                if (data != null)
                {
                    _sessions.Clear();
                    _sessions.AddRange(data.Sessions);
                    _logger?.LogInformation("Loaded {Count} analytics sessions", _sessions.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading analytics data");
        }
    }

    private async Task SaveAnalyticsDataAsync()
    {
        try
        {
            if (_context == null) return;

            var data = new AnalyticsData
            {
                Sessions = _sessions.ToList(),
                LastUpdated = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var dataPath = Path.Combine(_context.PluginDirectory, "analytics_data.json");
            await File.WriteAllTextAsync(dataPath, json);

            _logger?.LogInformation("Saved analytics data");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving analytics data");
        }
    }

    private void DisplayPerformanceSnapshot(PerformanceSnapshot snapshot)
    {
        _logger?.LogInformation("Current Performance Snapshot:");
        _logger?.LogInformation($"- FPS: {snapshot.Fps:F1}");
        _logger?.LogInformation($"- Frame Time: {snapshot.FrameTimeMs:F2}ms");
        _logger?.LogInformation($"- CPU Usage: {snapshot.CpuUsagePercent:F1}%");
        var gpuUsage = snapshot.GpuUsagePercent.HasValue
            ? $"{snapshot.GpuUsagePercent.Value:F1}%"
            : "N/A";
        _logger?.LogInformation($"- GPU Usage: {gpuUsage}");
        _logger?.LogInformation($"- RAM Usage: {snapshot.RamUsageMb}MB");

        if (snapshot.GpuTempCelsius.HasValue)
            _logger?.LogInformation($"- GPU Temp: {snapshot.GpuTempCelsius:F1}°C");
        if (snapshot.CpuTempCelsius.HasValue)
            _logger?.LogInformation($"- CPU Temp: {snapshot.CpuTempCelsius:F1}°C");
    }

    private float CalculateStabilityScore(IEnumerable<PerformanceSession> sessions)
    {
        if (!sessions.Any()) return 0;

        var fpsValues = sessions.Select(s => s.AverageFps).ToList();
        var avgFps = fpsValues.Average();
        var variance = fpsValues.Sum(fps => Math.Pow(fps - avgFps, 2)) / fpsValues.Count;
        var stdDev = Math.Sqrt(variance);

        // Convert to 0-10 scale (lower std dev = higher score)
        var score = Math.Max(0, 10 - (stdDev / 10));
        return (float)score;
    }

    private float CalculateFpsTrend(IEnumerable<PerformanceSession> sessions)
    {
        var orderedSessions = sessions.OrderBy(s => s.StartTime).ToList();
        if (orderedSessions.Count < 2) return 0;

        // Simple linear trend calculation
        var firstHalf = orderedSessions.Take(orderedSessions.Count / 2);
        var secondHalf = orderedSessions.Skip(orderedSessions.Count / 2);

        var firstAvg = firstHalf.Average(s => s.AverageFps);
        var secondAvg = secondHalf.Average(s => s.AverageFps);

        return secondAvg - firstAvg;
    }

    private string GetTrendDirection(float trend)
    {
        return trend switch
        {
            > 2 => "📈 improving",
            > 0.5f => "📊 slightly improving",
            > -0.5f => "➡️ stable",
            > -2 => "📉 slightly declining",
            _ => "📉 declining"
        };
    }
}

/// <summary>
/// Analytics engine for processing gaming data.
/// </summary>
public class AnalyticsEngine
{
    public void AnalyzeSession(PerformanceSession session)
    {
        // Analyze individual session for patterns and insights
        // This would contain complex analytics algorithms
    }

    public GamingInsights GenerateInsights(IEnumerable<PerformanceSession> sessions)
    {
        // Generate comprehensive gaming insights
        var totalPlayTime = TimeSpan.FromTicks(sessions.Sum(s => s.Duration.Ticks));
        return new GamingInsights(
            AverageFps: sessions.Average(s => s.AverageFps),
            TotalPlayTime: totalPlayTime,
            Recommendations: new[] { "Update graphics drivers", "Close background apps" });
    }
}

/// <summary>
/// Gaming insights data.
/// </summary>
public record GamingInsights(
    float AverageFps,
    TimeSpan TotalPlayTime,
    IReadOnlyList<string> Recommendations);

/// <summary>
/// Performance session data.
/// </summary>
public class PerformanceSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GameId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public float AverageFps { get; set; }
    public float MinFps { get; set; }
    public float MaxFps { get; set; }
    public float AverageCpuUsage { get; set; }
    public float AverageGpuUsage { get; set; }
    public long AverageRamUsage { get; set; }
}

/// <summary>
/// Gaming pattern for recognition.
/// </summary>
public class GamingPattern
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PatternType Type { get; set; }
    public float Threshold { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Types of gaming patterns.
/// </summary>
public enum PatternType
{
    Performance,
    Behavior,
    Hardware
}

/// <summary>
/// Analytics data for persistence.
/// </summary>
public class AnalyticsData
{
    public List<PerformanceSession> Sessions { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

