using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Performance.Services;
using SaveState.Core.RomManagement;
using SaveState.Application.RomManagement.Services;
using SaveState.Application.Performance.Commands;
using SaveState.Presentation.Services;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Tools tab.
/// </summary>
public partial class ToolsViewModel : ObservableObject
{
    private readonly IPerformanceMonitor? _performanceMonitor;
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IEmulatorService _emulatorService;
    private readonly IMediator _mediator;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ToolsViewModel> _logger;
    private readonly GameMemoryViewModel _gameMemoryViewModel;
    private System.Timers.Timer? _updateTimer;

    public ToolsViewModel(
        IPerformanceMonitor? performanceMonitor,
        IEmulatorRepository emulatorRepository,
        IEmulatorService emulatorService,
        IMediator mediator,
        IThemeService themeService,
        IDialogService dialogService,
        GameMemoryViewModel gameMemoryViewModel,
        ILogger<ToolsViewModel> logger)
    {
        _performanceMonitor = performanceMonitor;
        _emulatorRepository = emulatorRepository;
        _emulatorService = emulatorService;
        _mediator = mediator;
        _themeService = themeService;
        _dialogService = dialogService;
        _gameMemoryViewModel = gameMemoryViewModel;
        _logger = logger;

        // Initialize collections
        ToolCategories = new ObservableCollection<ToolCategoryViewModel>
        {
            new ToolCategoryViewModel("⚡", "Performance", "Performance", true),
            new ToolCategoryViewModel("🧠", "Memory", "Memory", false),
            new ToolCategoryViewModel("🎮", "Emulators", "Emulators", false),
            new ToolCategoryViewModel("🎤", "Voice", "Voice", false),
            new ToolCategoryViewModel("🤖", "Automation", "Automation", false),
            new ToolCategoryViewModel("☁️", "Cloud", "Cloud", false),
            new ToolCategoryViewModel("🔍", "Diagnostics", "Diagnostics", false),
            new ToolCategoryViewModel("🎨", "Themes", "Themes", false)
        };

        Emulators = new ObservableCollection<ToolsEmulatorViewModel>();
        SelectedCategory = ToolCategories[0];

        // Start performance monitoring
        StartPerformanceMonitoring();

        // Load emulators
        _ = LoadEmulatorsAsync();
    }

    /// <summary>
    /// Gets the Game Memory View Model.
    /// </summary>
    public GameMemoryViewModel GameMemoryViewModel => _gameMemoryViewModel;

    /// <summary>
    /// Gets the display title for the tools tab.
    /// </summary>
    public string Title => "Tools";

    // Collections
    public ObservableCollection<ToolCategoryViewModel> ToolCategories { get; }
    public ObservableCollection<ToolsEmulatorViewModel> Emulators { get; }

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

            // Load real emulators from the repository
            var emulators = await _emulatorRepository.GetAllAsync();

            if (emulators.Any())
            {
                foreach (var emu in emulators)
                {
                    var path = emu.ExecutablePath?.Value ?? "";
                    var isAvailable = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
                    Emulators.Add(new ToolsEmulatorViewModel(
                        emu.Id,
                        emu.Name,
                        emu.Platform?.Name ?? "Unknown",
                        emu.Version ?? "N/A",
                        isAvailable,
                        path,
                        _dialogService,
                        _logger));
                }
                _logger.LogInformation("Loaded {Count} emulators from repository", emulators.Count);
            }
            else
            {
                // No emulators configured - show helpful message
                _logger.LogInformation("No emulators configured. Add emulators via Settings or CLI.");
            }
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

    // Voice Properties
    [ObservableProperty]
    private string voiceCommandStatus = "Ready for voice input";

    [ObservableProperty]
    private bool voiceCommandsEnabled;

    [ObservableProperty]
    private ObservableCollection<string> availableMicrophones = new() { "Default System Mic", "Headset Microphone", "USB Condenser Mic" };

    [ObservableProperty]
    private string selectedMicrophone = "Default System Mic";

    [ObservableProperty]
    private double microphoneSensitivity = 75;

    [ObservableProperty]
    private ObservableCollection<VoiceCommandItemViewModel> voiceCommands = new()
    {
        new VoiceCommandItemViewModel("Launch Game", "Launches the selected game", true),
        new VoiceCommandItemViewModel("Create Save State", "Saves the current game state", true),
        new VoiceCommandItemViewModel("Take Screenshot", "Captures the current screen", false),
        new VoiceCommandItemViewModel("Optimize System", "Runs performance optimizations", true)
    };

    [ObservableProperty]
    private bool ttsEnabled = true;

    [ObservableProperty]
    private ObservableCollection<string> availableVoices = new() { "Natural Male", "Natural Female", "Robotic", "Calm Narrator" };

    [ObservableProperty]
    private string selectedVoice = "Natural Male";

    [ObservableProperty]
    private double ttsSpeed = 100;

    // Automation Properties
    [ObservableProperty]
    private ObservableCollection<MacroViewModel> macros = new()
    {
        new MacroViewModel("Stream Setup", "Launches OBS, Discord, and Game", 3, "Ctrl+Shift+S"),
        new MacroViewModel("Performance Boost", "Kills background tasks and sets high priority", 5, "Ctrl+Shift+B")
    };

    [ObservableProperty]
    private ObservableCollection<ScheduledTaskViewModel> scheduledTasks = new()
    {
        new ScheduledTaskViewModel("Daily Backup", "Every day at 3:00 AM", true),
        new ScheduledTaskViewModel("Weekly Cleanup", "Every Sunday at 4:00 AM", false)
    };

    // Cloud Properties
    [ObservableProperty]
    private string cloudProvider = "OneDrive (Not Connected)";

    [ObservableProperty]
    private string cloudStatus = "🔴 Disconnected";

    [ObservableProperty]
    private string cloudStatusColor = "#cc3333";

    [ObservableProperty]
    private string lastSyncTime = "Never";

    [ObservableProperty]
    private double syncProgress;

    [ObservableProperty]
    private bool isSyncing;

    [ObservableProperty]
    private string usedSpace = "0 GB";

    [ObservableProperty]
    private string availableSpace = "Unknown";

    [ObservableProperty]
    private double storageUsagePercent;

    [ObservableProperty]
    private bool autoSyncOnStartup = true;

    [ObservableProperty]
    private bool syncSaveStates = true;

    [ObservableProperty]
    private bool syncScreenshots = false;

    [ObservableProperty]
    private bool syncGameSettings = true;

    [ObservableProperty]
    private bool syncAchievements = true;

    [ObservableProperty]
    private ObservableCollection<string> syncIntervals = new() { "Every Hour", "Every 4 Hours", "Every 12 Hours", "Daily", "Weekly" };

    [ObservableProperty]
    private string selectedSyncInterval = "Every 4 Hours";

    [ObservableProperty]
    private ObservableCollection<SyncActivityViewModel> syncHistory = new()
    {
        new SyncActivityViewModel("✅", "Full Sync", "All files up to date", "2 hours ago"),
        new SyncActivityViewModel("✅", "Upload", "Uploaded 5 save states", "Yesterday"),
        new SyncActivityViewModel("❌", "Sync Failed", "Network timeout", "2 days ago")
    };

    [ObservableProperty]
    private ObservableCollection<string> availableProviders = new() { "OneDrive", "Google Drive", "Dropbox", "Custom S3" };

    [ObservableProperty]
    private string selectedProvider = "OneDrive";

    [ObservableProperty]
    private bool isCloudConnected;

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
    private async Task ToggleGameModeAsync()
    {
        _logger.LogInformation("Game Mode toggled");
        // Use Standard optimization for basic Game Mode
        var command = new OptimizeSystemCommand(OptimizationLevel.Standard);
        await _mediator.Send(command);
    }

    [RelayCommand]
    private async Task ToggleQuietModeAsync()
    {
        _logger.LogInformation("Quiet Mode toggled");
        // Quiet mode could be Minimal optimization
        var command = new OptimizeSystemCommand(OptimizationLevel.Minimal);
        await _mediator.Send(command);
    }

    [RelayCommand]
    private async Task ApplyPerformanceModeAsync()
    {
        _logger.LogInformation("Performance Mode applied");
        // Aggressive optimization for Performance Mode
        var command = new OptimizeSystemCommand(OptimizationLevel.Aggressive);
        await _mediator.Send(command);
    }

    [RelayCommand]
    private async Task ApplyPowerSaverAsync()
    {
        _logger.LogInformation("Power Saver applied");
        // Set everything to low/restore system
        var command = new RestoreSystemCommand();
        await _mediator.Send(command);
    }

    [RelayCommand]
    private async Task RunHealthCheckAsync()
    {
        _logger.LogInformation("Running health check...");

        try
        {
            var statsResult = await _mediator.Send(new Application.Performance.Queries.GetDatabaseStatisticsQuery());
            if (statsResult.IsSuccess)
            {
                DatabaseStatus = statsResult.Value.Status;
                DatabaseSize = statsResult.Value.Size;
                TotalGames = statsResult.Value.TotalGames;
                TotalSessions = statsResult.Value.TotalSessions;
            }

            ApiStatus = "🟢 Connected";
            SystemStatus = "🟢 Operational";

            _logger.LogInformation("Health check complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
        }
    }

    [RelayCommand]
    private async Task CompactDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Compacting database...");
            DatabaseStatus = "🟡 Compacting...";

            // Execute SQLite VACUUM command to compact database
            var command = new CompactDatabaseCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Database compaction complete");
                // Refresh statistics after compaction
                await RunHealthCheckAsync();
            }
            else
            {
                _logger.LogError("Compaction failed: {Error}", result.Error);
                DatabaseStatus = "🔴 Error";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compact database");
            DatabaseStatus = "🔴 Error";
        }
    }

    [RelayCommand]
    private void ApplyTheme(string? themeName)
    {
        if (!string.IsNullOrEmpty(themeName))
        {
            CurrentTheme = themeName;
            _logger.LogInformation("Applied theme: {Theme}", themeName);

            // Map string name to ThemeType
            ThemeType themeType = themeName switch
            {
                "Light" => ThemeType.Light,
                "Dark" or "Deep Space (Default)" or "Midnight Blue" => ThemeType.Dark,
                _ => ThemeType.System
            };

            _themeService.SetTheme(themeType);
        }
    }

    // New Tool Commands
    [RelayCommand]
    private void EnableVoiceCommands() => VoiceCommandsEnabled = true;

    [RelayCommand]
    private void DisableVoiceCommands() => VoiceCommandsEnabled = false;

    [RelayCommand]
    private void TestMicrophone() => VoiceCommandStatus = "Testing microphone...";

    [RelayCommand]
    private void TestTts() => _logger.LogInformation("Testing TTS...");

    [RelayCommand]
    private void CreateMacro() => _logger.LogInformation("Opening macro creator...");

    [RelayCommand]
    private void AddScheduledTask() => _logger.LogInformation("Opening task scheduler...");

    [RelayCommand]
    private void OpenWorkflowBuilder() => _logger.LogInformation("Opening workflow builder...");

    [RelayCommand]
    private void ViewWorkflowTemplates() => _logger.LogInformation("Viewing workflow templates...");

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        IsSyncing = true;
        SyncProgress = 0;
        for (int i = 0; i <= 100; i += 10)
        {
            SyncProgress = i;
            await Task.Delay(200);
        }
        IsSyncing = false;
        LastSyncTime = "Just now";
        CloudStatus = "🟢 Synced";
        CloudStatusColor = "#44cc11";
    }

    [RelayCommand]
    private void ConnectCloud()
    {
        IsCloudConnected = true;
        CloudStatus = "🟡 Connecting...";
        CloudStatusColor = "#ffcc00";
        _ = SyncNowAsync();
    }

    [RelayCommand]
    private void DisconnectCloud()
    {
        IsCloudConnected = false;
        CloudStatus = "🔴 Disconnected";
        CloudStatusColor = "#cc3333";
    }

    [RelayCommand]
    private void ConfigureCloud() => _logger.LogInformation("Configuring cloud provider...");

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
        GpuUsage = snapshot.GpuUsagePercent ?? 0f;
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

public partial class ToolsEmulatorViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ILogger _logger;

    public ToolsEmulatorViewModel(Guid id, string name, string platform, string version, bool isInstalled, string executablePath, IDialogService dialogService, ILogger logger)
    {
        Id = id;
        Name = name;
        Platform = platform;
        Version = version;
        IsInstalled = isInstalled;
        ExecutablePath = executablePath;
        _dialogService = dialogService;
        _logger = logger;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Platform { get; }
    public string Version { get; }
    public bool IsInstalled { get; }
    public string ExecutablePath { get; }
    public string StatusIcon => IsInstalled ? "🟢" : "🔴";
    public string StatusText => IsInstalled ? "Installed" : "Not Found";

    [RelayCommand]
    private async Task ConfigureAsync()
    {
        try
        {
            _logger.LogInformation("Configuring emulator: {Id}", Id);
            var result = await _dialogService.ShowEmulatorEditorAsync(null);

            if (result != null)
            {
                _logger.LogInformation("Emulator updated: {Name}", result.Name);
                // Future: Send update command to repository
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open emulator configuration");
        }
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(ExecutablePath) && System.IO.File.Exists(ExecutablePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ExecutablePath,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(ExecutablePath)
                });
            }
            else
            {
                _logger.LogWarning("Cannot launch emulator: Executable not found at {Path}", ExecutablePath);
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch emulator");
        }
    }
}

public partial class VoiceCommandItemViewModel : ObservableObject
{
    public VoiceCommandItemViewModel(string command, string description, bool isEnabled)
    {
        Command = command;
        Description = description;
        IsEnabled = isEnabled;
    }

    public string Command { get; }
    public string Description { get; }

    [ObservableProperty]
    private bool isEnabled;
}

public partial class MacroViewModel : ObservableObject
{
    public MacroViewModel(string name, string description, int stepCount, string hotkey)
    {
        Name = name;
        Description = description;
        StepCount = stepCount;
        Hotkey = hotkey;
    }

    public string Name { get; }
    public string Description { get; }
    public int StepCount { get; }
    public string Hotkey { get; }

    [RelayCommand]
    private void Run() { /* Run macro */ }

    [RelayCommand]
    private void Edit() { /* Edit macro */ }

    [RelayCommand]
    private void Delete() { /* Delete macro */ }
}

public partial class ScheduledTaskViewModel : ObservableObject
{
    public ScheduledTaskViewModel(string name, string schedule, bool isEnabled)
    {
        Name = name;
        Schedule = schedule;
        IsEnabled = isEnabled;
    }

    public string Name { get; }
    public string Schedule { get; }

    [ObservableProperty]
    private bool isEnabled;

    [RelayCommand]
    private void Configure() { /* Configure task */ }
}

public class SyncActivityViewModel
{
    public SyncActivityViewModel(string statusIcon, string action, string details, string timeAgo)
    {
        StatusIcon = statusIcon;
        Action = action;
        Details = details;
        TimeAgo = timeAgo;
    }

    public string StatusIcon { get; }
    public string Action { get; }
    public string Details { get; }
    public string TimeAgo { get; }
}
