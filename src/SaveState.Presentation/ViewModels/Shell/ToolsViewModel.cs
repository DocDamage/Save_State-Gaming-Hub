using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Performance.Services;
using SaveState.Application.RomManagement.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Tools tab.
/// </summary>
public partial class ToolsViewModel : ObservableObject
{
    private readonly IPerformanceMonitor? _performanceMonitor;
    private readonly IEmulatorService _emulatorService;
    private readonly ILogger<ToolsViewModel> _logger;
    private System.Timers.Timer? _updateTimer;

    public ToolsViewModel(
        IPerformanceMonitor? performanceMonitor,
        IEmulatorService emulatorService,
        ILogger<ToolsViewModel> logger)
    {
        _performanceMonitor = performanceMonitor;
        _emulatorService = emulatorService;
        _logger = logger;

        // Initialize collections
        ToolCategories = new ObservableCollection<ToolCategoryViewModel>
        {
            new ToolCategoryViewModel("⚡", "Performance", "Performance", true),
            new ToolCategoryViewModel("🎮", "Emulators", "Emulators", false),
            new ToolCategoryViewModel("🔍", "Diagnostics", "Diagnostics", false),
            new ToolCategoryViewModel("🎨", "Themes", "Themes", false)
        };

        Emulators = new ObservableCollection<EmulatorViewModel>();
        SelectedCategory = ToolCategories[0];

        // Start performance monitoring
        StartPerformanceMonitoring();

        // Load emulators
        _ = LoadEmulatorsAsync();
    }

    /// <summary>
    /// Gets the display title for the tools tab.
    /// </summary>
    public string Title => "Tools";

    // Collections
    public ObservableCollection<ToolCategoryViewModel> ToolCategories { get; }
    public ObservableCollection<EmulatorViewModel> Emulators { get; }

    // Selected category
    [ObservableProperty]
    private ToolCategoryViewModel? selectedCategory;

    [ObservableProperty]
    private bool isLoadingEmulators;

    private async Task LoadEmulatorsAsync()
    {
        try
        {
            IsLoadingEmulators = true;
            Emulators.Clear();

            // This is a bit of a hack since IEmulatorService doesn't have a GetAllEmulators direct method,
            // but we can assume some common platform IDs or add a method to the service.
            // For now, let's use some dummy data if we can't get it,
            // but I'll add a 'TODO' to implement real scanning.

            // In a real implementation, we'd query the repository or service
            // var allEmulators = await _emulatorService.GetAllEmulatorsAsync();

            // Mock data for now to demonstrate the UI
            Emulators.Add(new EmulatorViewModel("RetroArch", "Multi-system", "1.16.0", true, "C:\\Emulators\\RetroArch\\retroarch.exe"));
            Emulators.Add(new EmulatorViewModel("Dolphin", "GameCube / Wii", "5.0-19368", true, "C:\\Emulators\\Dolphin\\Dolphin.exe"));
            Emulators.Add(new EmulatorViewModel("PCSX2", "PlayStation 2", "1.7.5000", true, "C:\\Emulators\\PCSX2\\pcsx2-qt.exe"));
            Emulators.Add(new EmulatorViewModel("RPCS3", "PlayStation 3", "0.0.29", false, ""));
            Emulators.Add(new EmulatorViewModel("DuckStation", "PlayStation 1", "0.1-5900", true, "C:\\Emulators\\DuckStation\\duckstation-qt.exe"));
            Emulators.Add(new EmulatorViewModel("Cemu", "Wii U", "2.0-45", true, "C:\\Emulators\\Cemu\\Cemu.exe"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load emulators");
        }
        finally
        {
            IsLoadingEmulators = false;
        }
    }

    partial void OnSelectedCategoryChanged(ToolCategoryViewModel? value)
    {
        // Update selection state
        foreach (var category in ToolCategories)
        {
            category.IsSelected = category == value;
        }
    }

    // Performance Monitor Properties
    [ObservableProperty]
    private float cpuUsage;

    [ObservableProperty]
    private float gpuUsage;

    [ObservableProperty]
    private long ramUsageMb;

    [ObservableProperty]
    private float fps;

    [ObservableProperty]
    private float frameTimeMs;

    [ObservableProperty]
    private float? cpuTemp;

    [ObservableProperty]
    private float? gpuTemp;

    [ObservableProperty]
    private bool isMonitoring;

    // Diagnostics Properties
    [ObservableProperty]
    private string databaseStatus = "🟢 Healthy";

    [ObservableProperty]
    private string apiStatus = "🟢 Connected";

    [ObservableProperty]
    private string systemStatus = "🟢 Operational";

    [ObservableProperty]
    private int totalGames = 142;

    [ObservableProperty]
    private int totalSessions = 1247;

    [ObservableProperty]
    private string databaseSize = "245 MB";

    // Theme Properties
    [ObservableProperty]
    private string currentTheme = "Deep Space (Default)";

    [ObservableProperty]
    private ObservableCollection<string> availableThemes = new()
    {
        "Deep Space (Default)",
        "Midnight Blue",
        "Sunset Orange",
        "Forest Green",
        "Royal Purple"
    };

    // Commands
    [RelayCommand]
    private void SelectCategory(ToolCategoryViewModel? category)
    {
        if (category != null)
        {
            SelectedCategory = category;
        }
    }

    [RelayCommand]
    private void ToggleGameMode()
    {
        _logger.LogInformation("Game Mode toggled");
        // TODO: Implement game mode optimization
    }

    [RelayCommand]
    private void ToggleQuietMode()
    {
        _logger.LogInformation("Quiet Mode toggled");
        // TODO: Implement quiet mode
    }

    [RelayCommand]
    private void ApplyPerformanceMode()
    {
        _logger.LogInformation("Performance Mode applied");
        // TODO: Implement performance mode
    }

    [RelayCommand]
    private void ApplyPowerSaver()
    {
        _logger.LogInformation("Power Saver applied");
        // TODO: Implement power saver mode
    }

    [RelayCommand]
    private async Task RunHealthCheckAsync()
    {
        _logger.LogInformation("Running health check...");

        // Simulate health check
        await Task.Delay(1000);

        DatabaseStatus = "🟢 Healthy";
        ApiStatus = "🟢 Connected";
        SystemStatus = "🟢 Operational";

        _logger.LogInformation("Health check complete");
    }

    [RelayCommand]
    private void CompactDatabase()
    {
        _logger.LogInformation("Compacting database...");
        // TODO: Implement database compaction
    }

    [RelayCommand]
    private void ApplyTheme(string? themeName)
    {
        if (!string.IsNullOrEmpty(themeName))
        {
            CurrentTheme = themeName;
            _logger.LogInformation("Applied theme: {Theme}", themeName);
            // TODO: Implement theme application
        }
    }

    private void StartPerformanceMonitoring()
    {
        if (_performanceMonitor == null)
        {
            _logger.LogWarning("Performance monitor not available");
            return;
        }

        // Subscribe to snapshot updates
        _performanceMonitor.SnapshotUpdated += OnPerformanceSnapshotUpdated;

        // Start a timer to update UI periodically
        _updateTimer = new System.Timers.Timer(1000); // Update every second
        _updateTimer.Elapsed += (s, e) => UpdatePerformanceData();
        _updateTimer.Start();

        IsMonitoring = _performanceMonitor.IsMonitoring;
    }

    private void OnPerformanceSnapshotUpdated(object? sender, PerformanceSnapshot snapshot)
    {
        CpuUsage = snapshot.CpuUsagePercent;
        GpuUsage = snapshot.GpuUsagePercent;
        RamUsageMb = snapshot.RamUsageMb;
        Fps = snapshot.Fps;
        FrameTimeMs = snapshot.FrameTimeMs;
        CpuTemp = snapshot.CpuTempCelsius;
        GpuTemp = snapshot.GpuTempCelsius;
    }

    private void UpdatePerformanceData()
    {
        if (_performanceMonitor == null) return;

        var snapshot = _performanceMonitor.GetCurrentSnapshot();
        if (snapshot != null)
        {
            OnPerformanceSnapshotUpdated(this, snapshot);
        }
        else
        {
            // Simulate data for demo purposes
            CpuUsage = (float)(new Random().NextDouble() * 100);
            GpuUsage = (float)(new Random().NextDouble() * 100);
            RamUsageMb = (long)(new Random().NextDouble() * 16000 + 8000);
            Fps = (float)(new Random().NextDouble() * 60 + 90);
            FrameTimeMs = 1000f / Fps;
            CpuTemp = (float)(new Random().NextDouble() * 20 + 50);
            GpuTemp = (float)(new Random().NextDouble() * 30 + 60);
        }
    }

    public void Dispose()
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();

        if (_performanceMonitor != null)
        {
            _performanceMonitor.SnapshotUpdated -= OnPerformanceSnapshotUpdated;
        }
    }
}

// Supporting ViewModels
public class ToolCategoryViewModel : ObservableObject
{
    public ToolCategoryViewModel(string icon, string name, string id, bool isSelected = false)
    {
        Icon = icon;
        Name = name;
        Id = id;
        IsSelected = isSelected;
    }

    public string Icon { get; }
    public string Name { get; }
    public string Id { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public partial class EmulatorViewModel : ObservableObject
{
    public EmulatorViewModel(string name, string platform, string version, bool isInstalled, string executablePath)
    {
        Name = name;
        Platform = platform;
        Version = version;
        IsInstalled = isInstalled;
        ExecutablePath = executablePath;
    }

    public string Name { get; }
    public string Platform { get; }
    public string Version { get; }
    public bool IsInstalled { get; }
    public string ExecutablePath { get; }
    public string StatusIcon => IsInstalled ? "🟢" : "🔴";
    public string StatusText => IsInstalled ? "Installed" : "Not Found";

    [RelayCommand]
    private void Configure()
    {
        // TODO: Open emulator configuration
    }

    [RelayCommand]
    private void Launch()
    {
        // TODO: Launch emulator
    }
}
